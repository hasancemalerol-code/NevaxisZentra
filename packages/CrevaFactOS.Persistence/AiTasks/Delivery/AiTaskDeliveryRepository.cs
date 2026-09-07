using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace CrevaFactOS.Persistence.AiTasks.Delivery;

public sealed class AiTaskDeliveryRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IAiTaskDeliveryChannelRegistry channelRegistry,
    IHostEnvironment hostEnvironment) : IAiTaskDeliveryRepository
{
    public Task<AiTaskDeliveryWriteResult> EnqueueAsync(
        AiTaskOperationContext context,
        EnqueueAiTaskDeliveryCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateEnqueue(command);
                if (validationError is not null)
                {
                    return Fail(AiTaskDeliveryFailureCodes.ValidationFailed, validationError);
                }

                if (command.Channel == AiTaskDeliveryChannels.DevelopmentLoopback
                    && !hostEnvironment.IsDevelopment()
                    && hostEnvironment.EnvironmentName != "Testing")
                {
                    return Fail(AiTaskDeliveryFailureCodes.ChannelUnavailable, "Development loopback is not available.");
                }

                try
                {
                    channelRegistry.Resolve(command.Channel);
                }
                catch (InvalidOperationException)
                {
                    return Fail(AiTaskDeliveryFailureCodes.ChannelUnknown, "Unknown delivery channel.");
                }

                var readiness = channelRegistry.Resolve(command.Channel).GetReadiness();
                if (!readiness.Available && command.Channel != AiTaskDeliveryChannels.DevelopmentLoopback)
                {
                    return Fail(AiTaskDeliveryFailureCodes.ChannelUnavailable, "Kanal yapılandırılmadı.");
                }

                var task = await dbContext.AiTasks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        t => t.TenantId == context.TenantId
                             && t.FacilityId == context.FacilityId
                             && t.Id == command.AiTaskId,
                        ct);

                if (task is null)
                {
                    return Fail(AiTaskDeliveryFailureCodes.TaskNotFound, "AI task not found.");
                }

                var existing = await dbContext.AiTaskDeliveries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        d => d.TenantId == context.TenantId && d.IdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return new AiTaskDeliveryWriteResult(true, null, null, MapDelivery(existing));
                }

                var now = DateTimeOffset.UtcNow;
                var delivery = new AiTaskDelivery
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    AiTaskId = command.AiTaskId,
                    Channel = command.Channel.Trim(),
                    RecipientKind = command.RecipientKind.Trim(),
                    RecipientUserId = command.RecipientUserId,
                    MaskedRecipient = command.MaskedRecipient?.Trim(),
                    Status = AiTaskDeliveryStatuses.Queued,
                    IdempotencyKey = command.IdempotencyKey.Trim(),
                    PayloadVersion = AiTaskDeliveryConstants.PayloadVersion,
                    DeliveryProjectionJson = command.DeliveryProjectionJson
                        ?? AiTaskSafeProjectionBuilder.BuildDeliveryProjectionJson(task),
                    AttemptCount = 0,
                    MaxAttempts = command.MaxAttempts ?? AiTaskDeliveryConstants.DefaultMaxAttempts,
                    NextAttemptAtUtc = now,
                    RequestedByUserId = context.IdentitySubjectId,
                    RequestedAtUtc = now,
                    SecureAccessGrantId = command.SecureAccessGrantId,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                };

                dbContext.AiTaskDeliveries.Add(delivery);
                await AppendAuditAsync(context, task, AiTaskDeliveryAuditEventTypes.DeliveryRequested, now, ct);
                await AppendAuditAsync(context, task, AiTaskDeliveryAuditEventTypes.DeliveryQueued, now, ct);
                await dbContext.SaveChangesAsync(ct);
                return new AiTaskDeliveryWriteResult(true, null, null, MapDelivery(delivery));
            },
            cancellationToken);

    public Task<AiTaskDeliveryDetailReadModel?> GetByIdAsync(
        AiTaskOperationContext context,
        Guid deliveryId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var delivery = await dbContext.AiTaskDeliveries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        d => d.TenantId == context.TenantId && d.Id == deliveryId,
                        ct);

                if (delivery is null)
                {
                    return null;
                }

                var attempts = await dbContext.AiTaskDeliveryAttempts
                    .AsNoTracking()
                    .Where(a => a.TenantId == context.TenantId && a.DeliveryId == deliveryId)
                    .OrderBy(a => a.AttemptNumber)
                    .ToListAsync(ct);

                return new AiTaskDeliveryDetailReadModel(MapDelivery(delivery), attempts.Select(MapAttempt).ToList());
            },
            cancellationToken);

    public Task<IReadOnlyList<AiTaskDeliveryReadModel>> ListForTaskAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var items = await dbContext.AiTaskDeliveries
                    .AsNoTracking()
                    .Where(d => d.TenantId == context.TenantId && d.AiTaskId == taskId)
                    .OrderByDescending(d => d.RequestedAtUtc)
                    .ToListAsync(ct);

                return (IReadOnlyList<AiTaskDeliveryReadModel>)items.Select(MapDelivery).ToList();
            },
            cancellationToken);

    public Task<AiTaskDeliveryListReadModel> ListAsync(
        AiTaskOperationContext context,
        AiTaskDeliveryListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;
                var query = dbContext.AiTaskDeliveries.AsNoTracking()
                    .Where(d => d.TenantId == context.TenantId && d.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(filters.Channel))
                {
                    query = query.Where(d => d.Channel == filters.Channel.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.Status))
                {
                    query = query.Where(d => d.Status == filters.Status.Trim());
                }

                if (filters.TaskId is not null)
                {
                    query = query.Where(d => d.AiTaskId == filters.TaskId);
                }

                if (filters.RecipientUserId is not null)
                {
                    query = query.Where(d => d.RecipientUserId == filters.RecipientUserId);
                }

                if (filters.FromUtc is not null)
                {
                    query = query.Where(d => d.RequestedAtUtc >= filters.FromUtc);
                }

                if (filters.ToUtc is not null)
                {
                    query = query.Where(d => d.RequestedAtUtc <= filters.ToUtc);
                }

                var total = await query.CountAsync(ct);
                var items = await query
                    .OrderByDescending(d => d.RequestedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                return new AiTaskDeliveryListReadModel(
                    items.Select(MapDelivery).ToList(),
                    total,
                    page,
                    pageSize);
            },
            cancellationToken);

    public Task<AiTaskDeliveryWriteResult> RetryAsync(
        AiTaskOperationContext context,
        Guid deliveryId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var delivery = await dbContext.AiTaskDeliveries
                    .FirstOrDefaultAsync(d => d.TenantId == context.TenantId && d.Id == deliveryId, ct);

                if (delivery is null)
                {
                    return Fail(AiTaskDeliveryFailureCodes.NotFound, "Delivery not found.");
                }

                if (delivery.Status is not AiTaskDeliveryStatuses.FailedRetryable
                    and not AiTaskDeliveryStatuses.DeadLetter)
                {
                    return Fail(AiTaskDeliveryFailureCodes.InvalidTransition, "Delivery cannot be retried.");
                }

                var now = DateTimeOffset.UtcNow;
                AiTaskDeliveryStateMachine.EnsureTransition(delivery.Status, AiTaskDeliveryStatuses.Queued);
                delivery.Status = AiTaskDeliveryStatuses.Queued;
                delivery.NextAttemptAtUtc = now;
                delivery.LeaseOwner = null;
                delivery.LeaseExpiresAtUtc = null;
                delivery.UpdatedAtUtc = now;
                await dbContext.SaveChangesAsync(ct);
                return new AiTaskDeliveryWriteResult(true, null, null, MapDelivery(delivery));
            },
            cancellationToken);

    public Task<AiTaskDeliveryWriteResult> CancelAsync(
        AiTaskOperationContext context,
        Guid deliveryId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var delivery = await dbContext.AiTaskDeliveries
                    .FirstOrDefaultAsync(d => d.TenantId == context.TenantId && d.Id == deliveryId, ct);

                if (delivery is null)
                {
                    return Fail(AiTaskDeliveryFailureCodes.NotFound, "Delivery not found.");
                }

                if (delivery.Status is not AiTaskDeliveryStatuses.Queued
                    and not AiTaskDeliveryStatuses.FailedRetryable)
                {
                    return Fail(AiTaskDeliveryFailureCodes.InvalidTransition, "Delivery cannot be cancelled.");
                }

                var now = DateTimeOffset.UtcNow;
                AiTaskDeliveryStateMachine.EnsureTransition(delivery.Status, AiTaskDeliveryStatuses.Cancelled);
                delivery.Status = AiTaskDeliveryStatuses.Cancelled;
                delivery.CancelledAtUtc = now;
                delivery.UpdatedAtUtc = now;

                var task = await dbContext.AiTasks.AsNoTracking()
                    .FirstOrDefaultAsync(
                        t => t.TenantId == context.TenantId && t.Id == delivery.AiTaskId,
                        ct);
                if (task is not null)
                {
                    await AppendAuditAsync(context, task, AiTaskDeliveryAuditEventTypes.DeliveryCancelled, now, ct);
                }

                await dbContext.SaveChangesAsync(ct);
                return new AiTaskDeliveryWriteResult(true, null, null, MapDelivery(delivery));
            },
            cancellationToken);

    public Task<AiTaskDeliveryWorkerHealthReadModel> GetWorkerHealthAsync(
        AiTaskOperationContext context,
        bool includeDevelopmentChannels,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var now = DateTimeOffset.UtcNow;
                var deliveries = dbContext.AiTaskDeliveries.AsNoTracking()
                    .Where(d => d.TenantId == context.TenantId);

                var queued = await deliveries.CountAsync(d => d.Status == AiTaskDeliveryStatuses.Queued, ct);
                var retryable = await deliveries.CountAsync(d => d.Status == AiTaskDeliveryStatuses.FailedRetryable, ct);
                var deadLetter = await deliveries.CountAsync(d => d.Status == AiTaskDeliveryStatuses.DeadLetter, ct);
                var oldest = await deliveries
                    .Where(d => d.Status == AiTaskDeliveryStatuses.Queued)
                    .OrderBy(d => d.RequestedAtUtc)
                    .Select(d => d.RequestedAtUtc)
                    .FirstOrDefaultAsync(ct);

                return new AiTaskDeliveryWorkerHealthReadModel(
                    true,
                    AiTaskDeliveryWorkerState.LastHeartbeatUtc,
                    queued,
                    retryable,
                    deadLetter,
                    oldest == default ? null : now - oldest,
                    channelRegistry.ListReadiness(includeDevelopmentChannels));
            },
            cancellationToken);

    private static string? ValidateEnqueue(EnqueueAiTaskDeliveryCommand command)
    {
        if (command.AiTaskId == Guid.Empty)
        {
            return "Task id is required.";
        }

        if (string.IsNullOrWhiteSpace(command.Channel) || !AiTaskDeliveryChannels.All.Contains(command.Channel.Trim()))
        {
            return "Channel is invalid.";
        }

        if (string.IsNullOrWhiteSpace(command.RecipientKind)
            || !AiTaskDeliveryRecipientKinds.All.Contains(command.RecipientKind.Trim()))
        {
            return "Recipient kind is invalid.";
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return "Idempotency key is required.";
        }

        return null;
    }

    private async Task AppendAuditAsync(
        AiTaskOperationContext context,
        AiTask task,
        string eventType,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        dbContext.AiTaskAuditEvents.Add(new AiTaskAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            TaskId = task.Id,
            EventType = eventType,
            OccurredAtUtc = occurredAtUtc,
            ActorType = AiTaskActorTypes.User,
            ActorId = context.IdentitySubjectId.ToString(),
            ActorLabel = "Delivery",
            CorrelationId = context.CorrelationId,
        });
        await Task.CompletedTask;
    }

    private static AiTaskDeliveryWriteResult Fail(string code, string message) =>
        new(false, code, message, null);

    private static AiTaskDeliveryReadModel MapDelivery(AiTaskDelivery delivery) => new(
        delivery.Id,
        delivery.AiTaskId,
        delivery.Channel,
        delivery.RecipientKind,
        delivery.RecipientUserId,
        delivery.MaskedRecipient,
        delivery.Status,
        delivery.IdempotencyKey,
        delivery.AttemptCount,
        delivery.MaxAttempts,
        delivery.NextAttemptAtUtc,
        delivery.LastErrorCode,
        delivery.LastErrorCategory,
        delivery.RequestedAtUtc,
        delivery.SentAtUtc,
        delivery.CancelledAtUtc,
        delivery.SecureAccessGrantId,
        delivery.CreatedAtUtc);

    private static AiTaskDeliveryAttemptReadModel MapAttempt(AiTaskDeliveryAttempt attempt) => new(
        attempt.Id,
        attempt.AttemptNumber,
        attempt.AdapterKey,
        attempt.StartedAtUtc,
        attempt.CompletedAtUtc,
        attempt.Outcome,
        attempt.ProviderStatusCode,
        attempt.ProviderMessageId,
        attempt.ErrorCategory,
        attempt.SanitizedErrorCode,
        attempt.RetryScheduledForUtc);
}
