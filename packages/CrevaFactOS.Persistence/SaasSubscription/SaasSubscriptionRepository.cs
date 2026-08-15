using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CrevaFactOS.Persistence.SaasCommercial;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.SaasSubscription;

public sealed class SaasSubscriptionRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : ISaasSubscriptionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Task<SaasSubscriptionSummaryReadModel?> GetCurrentSubscriptionSummaryAsync(
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var subscription = await dbContext.SaasTenantSubscriptions.AsNoTracking()
                    .SingleOrDefaultAsync(item => item.IsCurrent, ct);
                return subscription is null ? null : await MapSummaryAsync(subscription, ct);
            },
            cancellationToken);

    public Task<IReadOnlyList<SaasSubscriptionTransitionHistoryReadModel>> ListTransitionHistoryAsync(
        Guid subscriptionId,
        int limit = 50,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var boundedLimit = Math.Clamp(limit, 1, 200);
                var history = await dbContext.SaasTenantSubscriptionTransitionHistory.AsNoTracking()
                    .Where(item => item.SubscriptionId == subscriptionId)
                    .OrderByDescending(item => item.OccurredAtUtc)
                    .Take(boundedLimit)
                    .ToListAsync(ct);
                return (IReadOnlyList<SaasSubscriptionTransitionHistoryReadModel>)history
                    .Select(MapTransition)
                    .ToList();
            },
            cancellationToken);

    public Task<IReadOnlyList<SaasSubscriptionAuditEventReadModel>> ListAuditEventsAsync(
        int limit = 50,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var boundedLimit = Math.Clamp(limit, 1, 200);
                var events = await dbContext.SaasSubscriptionAuditEvents.AsNoTracking()
                    .OrderByDescending(item => item.OccurredAtUtc)
                    .Take(boundedLimit)
                    .ToListAsync(ct);
                return (IReadOnlyList<SaasSubscriptionAuditEventReadModel>)events
                    .Select(MapAudit)
                    .ToList();
            },
            cancellationToken);

    public Task<SaasSubscriptionWriteResult> AssignOrChangeDevelopmentSubscriptionAsync(
        AssignDevelopmentSaasSubscriptionCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "subscription.development.assign",
            command,
            async ct =>
            {
                if (!SaasSubscriptionValueConversions.IsValidStatus(command.SubscriptionStatus))
                {
                    return SaasSubscriptionWriteResult.Fail(
                        SaasSubscriptionFailureCodes.ValidationFailed,
                        "Subscription status is invalid.");
                }

                if (!SaasCommercialValueConversions.IsValidBillingCadence(command.BillingCadence))
                {
                    return SaasSubscriptionWriteResult.Fail(
                        SaasSubscriptionFailureCodes.ValidationFailed,
                        "Billing cadence is invalid.");
                }

                var planVersionExists = await dbContext.SaasCommercialPlanVersions.AsNoTracking()
                    .AnyAsync(version => version.Id == command.CommercialPlanVersionId, ct);
                if (!planVersionExists)
                {
                    return SaasSubscriptionWriteResult.Fail(
                        SaasSubscriptionFailureCodes.NotFound,
                        "Commercial plan version was not found.");
                }

                var now = DateTimeOffset.UtcNow;
                var current = await dbContext.SaasTenantSubscriptions
                    .SingleOrDefaultAsync(item => item.IsCurrent, ct);
                if (current is not null)
                {
                    current.IsCurrent = false;
                    current.UpdatedAtUtc = now;
                    current.Version++;
                }

                var subscription = new SaasTenantSubscription
                {
                    Id = Guid.NewGuid(),
                    TenantId = await ResolveTenantIdAsync(ct),
                    CommercialPlanVersionId = command.CommercialPlanVersionId,
                    SubscriptionStatus = command.SubscriptionStatus,
                    BillingCadence = command.BillingCadence,
                    StartsAtUtc = command.StartsAtUtc,
                    CurrentPeriodStartUtc = command.CurrentPeriodStartUtc,
                    CurrentPeriodEndUtc = command.CurrentPeriodEndUtc,
                    TrialEndsAtUtc = command.TrialEndsAtUtc,
                    SourceProvenance = command.SourceProvenance,
                    IsCurrent = true,
                    Version = 1,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                };
                dbContext.SaasTenantSubscriptions.Add(subscription);

                AppendTransitionHistory(
                    subscription,
                    current?.SubscriptionStatus ?? SaasSubscriptionStatuses.Expired,
                    subscription.SubscriptionStatus,
                    command.TransitionReason,
                    actorReference,
                    command.IdempotencyKey,
                    new
                    {
                        command.CommercialPlanVersionId,
                        command.BillingCadence,
                        command.SourceProvenance,
                        replacedSubscriptionId = current?.Id,
                    },
                    now);

                AppendAudit(
                    subscription.TenantId,
                    SaasSubscriptionAuditEventTypes.DevelopmentSubscriptionAssigned,
                    SaasSubscriptionAuditEntityTypes.TenantSubscription,
                    subscription.Id.ToString("D"),
                    actorReference,
                    new
                    {
                        subscription.Id,
                        command.CommercialPlanVersionId,
                        subscription.SubscriptionStatus,
                        command.SourceProvenance,
                    },
                    correlationId,
                    now);

                await dbContext.SaveChangesAsync(ct);
                return SaasSubscriptionWriteResult.Ok();
            },
            cancellationToken);

    public Task<SaasSubscriptionWriteResult> TransitionSubscriptionAsync(
        TransitionSaasSubscriptionCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            command.IdempotencyKey,
            "subscription.transition",
            command,
            async ct =>
            {
                if (!SaasSubscriptionValueConversions.IsValidStatus(command.TargetStatus))
                {
                    return SaasSubscriptionWriteResult.Fail(
                        SaasSubscriptionFailureCodes.ValidationFailed,
                        "Target status is invalid.");
                }

                var subscription = await dbContext.SaasTenantSubscriptions
                    .SingleOrDefaultAsync(item => item.Id == command.SubscriptionId && item.IsCurrent, ct);
                if (subscription is null)
                {
                    return SaasSubscriptionWriteResult.Fail(
                        SaasSubscriptionFailureCodes.NotFound,
                        "Current subscription was not found.");
                }

                if (subscription.Version != command.ExpectedVersion)
                {
                    return SaasSubscriptionWriteResult.Fail(
                        SaasSubscriptionFailureCodes.ConcurrencyConflict,
                        "Subscription version is stale.");
                }

                if (!SaasSubscriptionValueConversions.IsValidTransition(
                        subscription.SubscriptionStatus,
                        command.TargetStatus))
                {
                    return SaasSubscriptionWriteResult.Fail(
                        SaasSubscriptionFailureCodes.InvalidTransition,
                        "Subscription transition is not allowed.");
                }

                var now = DateTimeOffset.UtcNow;
                var fromStatus = subscription.SubscriptionStatus;
                subscription.SubscriptionStatus = command.TargetStatus;
                subscription.UpdatedAtUtc = now;
                subscription.Version++;

                if (command.CancelAtPeriodEnd.HasValue)
                {
                    subscription.CancelAtPeriodEnd = command.CancelAtPeriodEnd.Value;
                }

                switch (command.TargetStatus)
                {
                    case SaasSubscriptionStatuses.GracePeriod:
                        subscription.GraceUntilUtc = command.GraceUntilUtc ?? subscription.GraceUntilUtc;
                        break;
                    case SaasSubscriptionStatuses.Suspended:
                        subscription.SuspendedAtUtc = now;
                        break;
                    case SaasSubscriptionStatuses.Cancelled:
                        subscription.CancelledAtUtc = now;
                        break;
                    case SaasSubscriptionStatuses.Active:
                        subscription.SuspendedAtUtc = null;
                        subscription.GraceUntilUtc = null;
                        break;
                }

                AppendTransitionHistory(
                    subscription,
                    fromStatus,
                    command.TargetStatus,
                    command.TransitionReason,
                    actorReference,
                    command.IdempotencyKey,
                    new
                    {
                        command.CancelAtPeriodEnd,
                        command.GraceUntilUtc,
                        subscription.Version,
                    },
                    now);

                AppendAudit(
                    subscription.TenantId,
                    SaasSubscriptionAuditEventTypes.SubscriptionTransitioned,
                    SaasSubscriptionAuditEntityTypes.TenantSubscription,
                    subscription.Id.ToString("D"),
                    actorReference,
                    new
                    {
                        subscription.Id,
                        fromStatus,
                        toStatus = command.TargetStatus,
                        command.TransitionReason,
                        subscription.Version,
                    },
                    correlationId,
                    now);

                await dbContext.SaveChangesAsync(ct);
                return SaasSubscriptionWriteResult.Ok();
            },
            cancellationToken);

    private async Task<SaasSubscriptionWriteResult> ExecuteMutationAsync<TCommand>(
        string? idempotencyKey,
        string operation,
        TCommand command,
        Func<CancellationToken, Task<SaasSubscriptionWriteResult>> action,
        CancellationToken cancellationToken)
    {
        return await tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (!string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    var hash = HashCommand(command);
                    var existing = await dbContext.SaasSubscriptionIdempotencyRecords
                        .SingleOrDefaultAsync(record => record.IdempotencyKey == idempotencyKey, ct);
                    if (existing is not null)
                    {
                        return existing.RequestHash == hash
                            ? SaasSubscriptionWriteResult.Ok(idempotentReplay: true)
                            : SaasSubscriptionWriteResult.Fail(
                                SaasSubscriptionFailureCodes.IdempotencyConflict,
                                "Idempotent retry conflicts with the original request.");
                    }

                    dbContext.SaasSubscriptionIdempotencyRecords.Add(new SaasSubscriptionIdempotencyRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = await ResolveTenantIdAsync(ct),
                        IdempotencyKey = idempotencyKey,
                        RequestHash = hash,
                        Operation = operation,
                        CreatedAtUtc = DateTimeOffset.UtcNow,
                    });
                }

                var result = await action(ct);
                if (!result.Succeeded)
                {
                    dbContext.ChangeTracker.Clear();
                }

                return result;
            },
            cancellationToken);
    }

    private async Task<SaasSubscriptionSummaryReadModel> MapSummaryAsync(
        SaasTenantSubscription subscription,
        CancellationToken cancellationToken)
    {
        var planVersion = await (
            from version in dbContext.SaasCommercialPlanVersions.AsNoTracking()
            join plan in dbContext.SaasCommercialPlans.AsNoTracking() on version.PlanId equals plan.Id
            where version.Id == subscription.CommercialPlanVersionId
            select new { plan.PlanCode, version.VersionNumber }).SingleOrDefaultAsync(cancellationToken);

        return new SaasSubscriptionSummaryReadModel(
            subscription.Id,
            subscription.SubscriptionStatus,
            subscription.BillingCadence,
            subscription.CommercialPlanVersionId,
            planVersion?.PlanCode,
            planVersion?.VersionNumber,
            subscription.StartsAtUtc,
            subscription.CurrentPeriodStartUtc,
            subscription.CurrentPeriodEndUtc,
            subscription.TrialEndsAtUtc,
            subscription.GraceUntilUtc,
            subscription.SuspendedAtUtc,
            subscription.CancelledAtUtc,
            subscription.CancelAtPeriodEnd,
            subscription.SourceProvenance,
            subscription.IsCurrent,
            subscription.Version);
    }

    private static SaasSubscriptionTransitionHistoryReadModel MapTransition(
        SaasTenantSubscriptionTransitionHistory history) =>
        new(
            history.Id,
            history.SubscriptionId,
            history.FromStatus,
            history.ToStatus,
            history.TransitionReason,
            history.ActorReference,
            history.OccurredAtUtc,
            history.IdempotencyKey);

    private static SaasSubscriptionAuditEventReadModel MapAudit(SaasSubscriptionAuditEvent auditEvent) =>
        new(
            auditEvent.EventType,
            auditEvent.EntityType,
            auditEvent.EntityReference,
            auditEvent.ActorReference,
            auditEvent.OccurredAtUtc,
            DeserializePayload(auditEvent.PayloadJson));

    private void AppendTransitionHistory(
        SaasTenantSubscription subscription,
        string fromStatus,
        string toStatus,
        string transitionReason,
        string actorReference,
        string? idempotencyKey,
        object evidence,
        DateTimeOffset occurredAtUtc)
    {
        dbContext.SaasTenantSubscriptionTransitionHistory.Add(new SaasTenantSubscriptionTransitionHistory
        {
            Id = Guid.NewGuid(),
            TenantId = subscription.TenantId,
            SubscriptionId = subscription.Id,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            TransitionReason = transitionReason,
            ActorReference = actorReference,
            OccurredAtUtc = occurredAtUtc,
            IdempotencyKey = idempotencyKey,
            EvidenceJson = JsonSerializer.Serialize(evidence, JsonOptions),
        });
    }

    private void AppendAudit(
        Guid tenantId,
        string eventType,
        string entityType,
        string entityReference,
        string actorReference,
        object payload,
        Guid? correlationId,
        DateTimeOffset occurredAtUtc)
    {
        dbContext.SaasSubscriptionAuditEvents.Add(new SaasSubscriptionAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = eventType,
            EntityType = entityType,
            EntityReference = entityReference,
            ActorReference = actorReference,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            CorrelationId = correlationId,
            OccurredAtUtc = occurredAtUtc,
        });
    }

    private async Task<Guid> ResolveTenantIdAsync(CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants.AsNoTracking().SingleAsync(cancellationToken);
        return tenant.Id;
    }

    private static string HashCommand<TCommand>(TCommand command)
    {
        var json = JsonSerializer.Serialize(command, JsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }

    private static object? DeserializePayload(string payloadJson)
    {
        try
        {
            return JsonSerializer.Deserialize<object>(payloadJson, JsonOptions);
        }
        catch
        {
            return payloadJson;
        }
    }
}

public sealed class UnavailableSaasSubscriptionRepository : ISaasSubscriptionRepository
{
    private static SaasSubscriptionWriteResult Unavailable() =>
        SaasSubscriptionWriteResult.Fail(
            SaasSubscriptionFailureCodes.Unavailable,
            "SaaS subscription persistence is unavailable.");

    public Task<SaasSubscriptionSummaryReadModel?> GetCurrentSubscriptionSummaryAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SaasSubscriptionSummaryReadModel?>(null);

    public Task<IReadOnlyList<SaasSubscriptionTransitionHistoryReadModel>> ListTransitionHistoryAsync(
        Guid subscriptionId,
        int limit = 50,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SaasSubscriptionTransitionHistoryReadModel>>([]);

    public Task<IReadOnlyList<SaasSubscriptionAuditEventReadModel>> ListAuditEventsAsync(
        int limit = 50,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SaasSubscriptionAuditEventReadModel>>([]);

    public Task<SaasSubscriptionWriteResult> AssignOrChangeDevelopmentSubscriptionAsync(
        AssignDevelopmentSaasSubscriptionCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<SaasSubscriptionWriteResult> TransitionSubscriptionAsync(
        TransitionSaasSubscriptionCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
