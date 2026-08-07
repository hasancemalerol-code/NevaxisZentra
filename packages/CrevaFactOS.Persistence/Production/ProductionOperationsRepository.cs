using System.Text.Json;
using System.Text.Json.Serialization;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Outbox;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Production;

/// <summary>
/// Concrete implementation for downtime, setup, and manual cycle operational events.
/// Each write is atomic: domain record + ProductionOrder version/counters + audit + outbox
/// are committed in the same transaction via TenantDatabaseSession.ExecuteWriteAsync.
/// </summary>
public sealed class ProductionOperationsRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IProductionOperationsRepository
{
    private const int DefaultReadLimit = 50;
    private const int MaxReadLimit = 200;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ────────────────────────────────── Downtime ──────────────────────────────

    public async Task<ProductionOperationsWriteResult> StartDowntimeAsync(
        ProductionOrderOperationContext context,
        StartDowntimeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (!TryGetDowntimeCategory(command.ReasonCode, out var category))
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidDowntimeReason,
                $"Unknown downtime reason code: '{command.ReasonCode}'.");
        }

        if (command.ReasonCode is "other" or "process-instability" && string.IsNullOrWhiteSpace(command.Comment))
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.CommentRequired,
                "A comment is required for this downtime reason.");
        }

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var order = await LoadOrderAsync(context, command.ProductionOrderId, ct);
                    if (order is null)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.NotFound,
                            "Production order was not found.");
                    }

                    if (order.Version != command.ExpectedVersion)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.ConcurrencyConflict,
                            "The production order has been modified. Please reload and try again.");
                    }

                    if (order.Status != ProductionOrderStatus.InProgress)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.InvalidTransition,
                            $"Downtime can only be started on an InProgress order. Current status: '{ProductionOrderValueConversions.ToStorage(order.Status)}'.");
                    }

                    var hasActiveDowntime = await dbContext.ProductionDowntimes
                        .AnyAsync(
                            d =>
                                d.TenantId == context.TenantId
                                && d.ProductionOrderId == command.ProductionOrderId
                                && d.EndedAtUtc == null,
                            ct);

                    if (hasActiveDowntime)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.ActiveDowntimeExists,
                            "There is already an active downtime for this production order.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actor = context.IdentitySubjectId.ToString();

                    var downtime = new ProductionDowntime
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ProductionOrderId = command.ProductionOrderId,
                        Category = category,
                        ReasonCode = command.ReasonCode,
                        StartedAtUtc = now,
                        Comment = command.Comment?.Trim(),
                        StartedBySubjectId = actor,
                        CorrelationId = correlationId,
                        Version = 1,
                    };

                    dbContext.ProductionDowntimes.Add(downtime);

                    // Transition order: InProgress → Paused
                    order.Status = ProductionOrderStatus.Paused;
                    order.LastModifiedBySubjectId = actor;
                    order.Version += 1;
                    order.UpdatedAtUtc = now;

                    AddAudit(context, ProductionAuditActions.ProductionDowntimeStart,
                        ProductionAuditEventTypes.ProductionDowntimeStarted,
                        "production.downtime", downtime.Id.ToString(), correlationId, now,
                        new { productionOrderId = command.ProductionOrderId.ToString(), downtimeId = downtime.Id.ToString(), reasonCode = command.ReasonCode, category });

                    AddOutbox(context, ProductionOutboxMessageTypes.ProductionDowntimeStarted,
                        "production-downtime", downtime.Id.ToString(), correlationId, now,
                        new Dictionary<string, string?> {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionOrderId"] = command.ProductionOrderId.ToString(),
                            ["downtimeId"] = downtime.Id.ToString(),
                            ["reasonCode"] = command.ReasonCode,
                            ["category"] = category,
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionOperationsWriteResult.Success(downtime.Id, order.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.ConcurrencyConflict,
                "The production order was modified by another user. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production operations service is unavailable.");
        }
    }

    public async Task<ProductionOperationsWriteResult> EndDowntimeAsync(
        ProductionOrderOperationContext context,
        EndDowntimeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var order = await LoadOrderAsync(context, command.ProductionOrderId, ct);
                    if (order is null)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.NotFound,
                            "Production order was not found.");
                    }

                    if (order.Version != command.ExpectedVersion)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.ConcurrencyConflict,
                            "The production order has been modified. Please reload and try again.");
                    }

                    if (order.Status != ProductionOrderStatus.Paused)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.InvalidTransition,
                            $"Downtime can only be ended on a Paused order. Current status: '{ProductionOrderValueConversions.ToStorage(order.Status)}'.");
                    }

                    var downtime = await dbContext.ProductionDowntimes
                        .SingleOrDefaultAsync(
                            d =>
                                d.TenantId == context.TenantId
                                && d.Id == command.DowntimeId,
                            ct);

                    if (downtime is null)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.DowntimeNotFound,
                            "Downtime record was not found.");
                    }

                    if (downtime.ProductionOrderId != command.ProductionOrderId)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.DowntimeWrongOrder,
                            "Downtime does not belong to this production order.");
                    }

                    if (downtime.EndedAtUtc is not null)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.DowntimeAlreadyEnded,
                            "This downtime has already ended.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actor = context.IdentitySubjectId.ToString();

                    var durationSeconds = (long)Math.Ceiling((now - downtime.StartedAtUtc).TotalSeconds);

                    downtime.EndedAtUtc = now;
                    downtime.DurationSeconds = durationSeconds;
                    downtime.EndedBySubjectId = actor;
                    downtime.Version += 1;

                    // Transition order: Paused → InProgress + accumulate downtime
                    order.Status = ProductionOrderStatus.InProgress;
                    order.TotalDowntimeSeconds += durationSeconds;
                    order.LastModifiedBySubjectId = actor;
                    order.Version += 1;
                    order.UpdatedAtUtc = now;

                    AddAudit(context, ProductionAuditActions.ProductionDowntimeEnd,
                        ProductionAuditEventTypes.ProductionDowntimeEnded,
                        "production.downtime", downtime.Id.ToString(), correlationId, now,
                        new { productionOrderId = command.ProductionOrderId.ToString(), downtimeId = downtime.Id.ToString(), durationSeconds });

                    AddOutbox(context, ProductionOutboxMessageTypes.ProductionDowntimeEnded,
                        "production-downtime", downtime.Id.ToString(), correlationId, now,
                        new Dictionary<string, string?> {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionOrderId"] = command.ProductionOrderId.ToString(),
                            ["downtimeId"] = downtime.Id.ToString(),
                            ["durationSeconds"] = durationSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionOperationsWriteResult.Success(downtime.Id, order.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.ConcurrencyConflict,
                "The production order was modified by another user. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production operations service is unavailable.");
        }
    }

    public async Task<ProductionOperationsListResult<DowntimeReadModel>> ListDowntimesAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (context.TenantId == Guid.Empty)
        {
            return ProductionOperationsListResult.Fail<DowntimeReadModel>(
                ProductionOrderFailureCodes.InvalidScope, "Tenant identifier is required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var effective = Math.Clamp(limit, 1, MaxReadLimit);
                    var items = await dbContext.ProductionDowntimes
                        .AsNoTracking()
                        .Where(d =>
                            d.TenantId == context.TenantId
                            && d.FacilityId == context.FacilityId
                            && d.ProductionOrderId == productionOrderId)
                        .OrderByDescending(d => d.StartedAtUtc)
                        .Take(effective)
                        .Select(d => new DowntimeReadModel(
                            d.Id,
                            d.Category,
                            d.ReasonCode,
                            d.StartedAtUtc,
                            d.EndedAtUtc,
                            d.DurationSeconds,
                            d.Comment,
                            d.StartedBySubjectId,
                            d.EndedBySubjectId,
                            d.EndedAtUtc == null))
                        .ToListAsync(ct);

                    return ProductionOperationsListResult.Success(items);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionOperationsListResult.Fail<DowntimeReadModel>(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production operations service is unavailable.");
        }
    }

    // ────────────────────────────────── Setup / Changeover ────────────────────

    public async Task<ProductionOperationsWriteResult> StartSetupSessionAsync(
        ProductionOrderOperationContext context,
        StartSetupSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (!IsValidSetupType(command.SetupType))
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidSetupType,
                $"Unknown setup type: '{command.SetupType}'.");
        }

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var order = await LoadOrderAsync(context, command.ProductionOrderId, ct);
                    if (order is null)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.NotFound,
                            "Production order was not found.");
                    }

                    if (order.Version != command.ExpectedVersion)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.ConcurrencyConflict,
                            "The production order has been modified. Please reload and try again.");
                    }

                    // Setup allowed on Ready or Paused
                    if (order.Status != ProductionOrderStatus.Ready
                        && order.Status != ProductionOrderStatus.Paused)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.InvalidTransition,
                            $"Setup can only be started on a Ready or Paused order. Current status: '{ProductionOrderValueConversions.ToStorage(order.Status)}'.");
                    }

                    var hasActiveSetup = await dbContext.ProductionSetupSessions
                        .AnyAsync(
                            s =>
                                s.TenantId == context.TenantId
                                && s.ProductionOrderId == command.ProductionOrderId
                                && s.CompletedAtUtc == null,
                            ct);

                    if (hasActiveSetup)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.ActiveSetupExists,
                            "There is already an active setup session for this production order.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actor = context.IdentitySubjectId.ToString();

                    var session = new ProductionSetupSession
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ProductionOrderId = command.ProductionOrderId,
                        SetupType = command.SetupType,
                        StartedAtUtc = now,
                        Comment = command.Comment?.Trim(),
                        StartedBySubjectId = actor,
                        CorrelationId = correlationId,
                        Version = 1,
                    };

                    dbContext.ProductionSetupSessions.Add(session);

                    // Order status does not change; only version increments
                    order.LastModifiedBySubjectId = actor;
                    order.Version += 1;
                    order.UpdatedAtUtc = now;

                    AddAudit(context, ProductionAuditActions.ProductionSetupStart,
                        ProductionAuditEventTypes.ProductionSetupStarted,
                        "production.setup", session.Id.ToString(), correlationId, now,
                        new { productionOrderId = command.ProductionOrderId.ToString(), setupSessionId = session.Id.ToString(), setupType = command.SetupType });

                    AddOutbox(context, ProductionOutboxMessageTypes.ProductionSetupStarted,
                        "production-setup-session", session.Id.ToString(), correlationId, now,
                        new Dictionary<string, string?> {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionOrderId"] = command.ProductionOrderId.ToString(),
                            ["setupSessionId"] = session.Id.ToString(),
                            ["setupType"] = command.SetupType,
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionOperationsWriteResult.Success(session.Id, order.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.ConcurrencyConflict,
                "The production order was modified by another user. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production operations service is unavailable.");
        }
    }

    public async Task<ProductionOperationsWriteResult> CompleteSetupSessionAsync(
        ProductionOrderOperationContext context,
        CompleteSetupSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var order = await LoadOrderAsync(context, command.ProductionOrderId, ct);
                    if (order is null)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.NotFound,
                            "Production order was not found.");
                    }

                    if (order.Version != command.ExpectedVersion)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.ConcurrencyConflict,
                            "The production order has been modified. Please reload and try again.");
                    }

                    var session = await dbContext.ProductionSetupSessions
                        .SingleOrDefaultAsync(
                            s =>
                                s.TenantId == context.TenantId
                                && s.Id == command.SetupSessionId,
                            ct);

                    if (session is null)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.SetupSessionNotFound,
                            "Setup session was not found.");
                    }

                    if (session.ProductionOrderId != command.ProductionOrderId)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.SetupSessionWrongOrder,
                            "Setup session does not belong to this production order.");
                    }

                    if (session.CompletedAtUtc is not null)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.SetupSessionAlreadyCompleted,
                            "This setup session has already been completed.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actor = context.IdentitySubjectId.ToString();

                    var durationSeconds = (long)Math.Ceiling((now - session.StartedAtUtc).TotalSeconds);

                    session.CompletedAtUtc = now;
                    session.DurationSeconds = durationSeconds;
                    session.CompletedBySubjectId = actor;
                    session.Version += 1;

                    // Accumulate setup time on order
                    order.TotalSetupSeconds += durationSeconds;
                    order.LastModifiedBySubjectId = actor;
                    order.Version += 1;
                    order.UpdatedAtUtc = now;

                    AddAudit(context, ProductionAuditActions.ProductionSetupComplete,
                        ProductionAuditEventTypes.ProductionSetupCompleted,
                        "production.setup", session.Id.ToString(), correlationId, now,
                        new { productionOrderId = command.ProductionOrderId.ToString(), setupSessionId = session.Id.ToString(), durationSeconds });

                    AddOutbox(context, ProductionOutboxMessageTypes.ProductionSetupCompleted,
                        "production-setup-session", session.Id.ToString(), correlationId, now,
                        new Dictionary<string, string?> {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionOrderId"] = command.ProductionOrderId.ToString(),
                            ["setupSessionId"] = session.Id.ToString(),
                            ["durationSeconds"] = durationSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionOperationsWriteResult.Success(session.Id, order.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.ConcurrencyConflict,
                "The production order was modified by another user. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production operations service is unavailable.");
        }
    }

    public async Task<ProductionOperationsListResult<SetupSessionReadModel>> ListSetupSessionsAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (context.TenantId == Guid.Empty)
        {
            return ProductionOperationsListResult.Fail<SetupSessionReadModel>(
                ProductionOrderFailureCodes.InvalidScope, "Tenant identifier is required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var effective = Math.Clamp(limit, 1, MaxReadLimit);
                    var items = await dbContext.ProductionSetupSessions
                        .AsNoTracking()
                        .Where(s =>
                            s.TenantId == context.TenantId
                            && s.FacilityId == context.FacilityId
                            && s.ProductionOrderId == productionOrderId)
                        .OrderByDescending(s => s.StartedAtUtc)
                        .Take(effective)
                        .Select(s => new SetupSessionReadModel(
                            s.Id,
                            s.SetupType,
                            s.StartedAtUtc,
                            s.CompletedAtUtc,
                            s.DurationSeconds,
                            s.Comment,
                            s.StartedBySubjectId,
                            s.CompletedBySubjectId,
                            s.CompletedAtUtc == null))
                        .ToListAsync(ct);

                    return ProductionOperationsListResult.Success(items);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionOperationsListResult.Fail<SetupSessionReadModel>(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production operations service is unavailable.");
        }
    }

    // ────────────────────────────────── Manual Cycle ──────────────────────────

    public async Task<ProductionOperationsWriteResult> RecordManualCycleAsync(
        ProductionOrderOperationContext context,
        RecordManualCycleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (command.CycleDurationMilliseconds <= 0)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidCycleDuration,
                "Cycle duration must be greater than zero.");
        }

        // Max 24 hours (86 400 000 ms) as a sanity bound
        if (command.CycleDurationMilliseconds > 86_400_000L)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidCycleDuration,
                "Cycle duration exceeds the maximum allowed value of 24 hours.");
        }

        if (command.QuantityProduced.HasValue && command.QuantityProduced.Value < 0)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidCycleQuantity,
                "Cycle quantity produced cannot be negative.");
        }

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var order = await LoadOrderAsync(context, command.ProductionOrderId, ct);
                    if (order is null)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.NotFound,
                            "Production order was not found.");
                    }

                    if (order.Version != command.ExpectedVersion)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.ConcurrencyConflict,
                            "The production order has been modified. Please reload and try again.");
                    }

                    if (order.Status != ProductionOrderStatus.InProgress)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.InvalidTransition,
                            $"Manual cycles can only be recorded on an InProgress order. Current status: '{ProductionOrderValueConversions.ToStorage(order.Status)}'.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actor = context.IdentitySubjectId.ToString();

                    // Increment cycle count atomically; cycle number = new count
                    var cycleNumber = order.ManualCycleCount + 1;
                    var cycleEndedAt = now;
                    var cycleStartedAt = cycleEndedAt - TimeSpan.FromMilliseconds(command.CycleDurationMilliseconds);

                    var cycle = new ProductionCycleEntry
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ProductionOrderId = command.ProductionOrderId,
                        CycleNumber = cycleNumber,
                        CycleStartedAtUtc = cycleStartedAt,
                        CycleEndedAtUtc = cycleEndedAt,
                        CycleDurationMilliseconds = command.CycleDurationMilliseconds,
                        QuantityProduced = command.QuantityProduced,
                        RecordedBySubjectId = actor,
                        RecordedAtUtc = now,
                        Source = "manual",
                        Comment = command.Comment?.Trim(),
                        CorrelationId = correlationId,
                    };

                    dbContext.ProductionCycleEntries.Add(cycle);

                    // Update order counters atomically
                    order.ManualCycleCount = cycleNumber;
                    order.TotalManualCycleDurationMilliseconds += command.CycleDurationMilliseconds;
                    order.LastCycleAtUtc = now;
                    order.LastModifiedBySubjectId = actor;
                    order.Version += 1;
                    order.UpdatedAtUtc = now;

                    AddAudit(context, ProductionAuditActions.ProductionCycleManualRecord,
                        ProductionAuditEventTypes.ProductionCycleManualRecorded,
                        "production.cycle", cycle.Id.ToString(), correlationId, now,
                        new { productionOrderId = command.ProductionOrderId.ToString(), cycleId = cycle.Id.ToString(), cycleNumber, cycleDurationMilliseconds = command.CycleDurationMilliseconds });

                    AddOutbox(context, ProductionOutboxMessageTypes.ProductionManualCycleRecorded,
                        "production-cycle-entry", cycle.Id.ToString(), correlationId, now,
                        new Dictionary<string, string?> {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionOrderId"] = command.ProductionOrderId.ToString(),
                            ["cycleId"] = cycle.Id.ToString(),
                            ["cycleNumber"] = cycleNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            ["cycleDurationMilliseconds"] = command.CycleDurationMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionOperationsWriteResult.Success(cycle.Id, order.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.ConcurrencyConflict,
                "The production order was modified by another user. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production operations service is unavailable.");
        }
    }

    public async Task<ProductionOperationsListResult<CycleEntryReadModel>> ListCycleEntriesAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (context.TenantId == Guid.Empty)
        {
            return ProductionOperationsListResult.Fail<CycleEntryReadModel>(
                ProductionOrderFailureCodes.InvalidScope, "Tenant identifier is required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var effective = Math.Clamp(limit, 1, MaxReadLimit);
                    var items = await dbContext.ProductionCycleEntries
                        .AsNoTracking()
                        .Where(c =>
                            c.TenantId == context.TenantId
                            && c.FacilityId == context.FacilityId
                            && c.ProductionOrderId == productionOrderId)
                        .OrderByDescending(c => c.RecordedAtUtc)
                        .Take(effective)
                        .Select(c => new CycleEntryReadModel(
                            c.Id,
                            c.CycleNumber,
                            c.CycleStartedAtUtc,
                            c.CycleEndedAtUtc,
                            c.CycleDurationMilliseconds,
                            c.QuantityProduced,
                            c.RecordedBySubjectId,
                            c.RecordedAtUtc,
                            c.Source,
                            c.Comment))
                        .ToListAsync(ct);

                    return ProductionOperationsListResult.Success(items);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionOperationsListResult.Fail<CycleEntryReadModel>(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production operations service is unavailable.");
        }
    }

    // ────────────────────────────────── Runtime Summary ───────────────────────

    public async Task<ProductionRuntimeSummaryResult> GetRuntimeSummaryAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default)
    {
        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionRuntimeSummaryResult.Fail(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(
                async ct =>
                {
                    var order = await dbContext.ProductionOrders
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            o =>
                                o.TenantId == context.TenantId
                                && o.FacilityId == context.FacilityId
                                && o.Id == productionOrderId,
                            ct);

                    if (order is null)
                    {
                        return ProductionRuntimeSummaryResult.Fail(
                            ProductionOrderFailureCodes.NotFound,
                            "Production order was not found.");
                    }

                    var now = DateTimeOffset.UtcNow;

                    // Active downtime (only if Paused and there is an active session)
                    DowntimeReadModel? activeDowntime = null;
                    if (order.Status == ProductionOrderStatus.Paused)
                    {
                        var ad = await dbContext.ProductionDowntimes
                            .AsNoTracking()
                            .Where(d =>
                                d.TenantId == context.TenantId
                                && d.ProductionOrderId == productionOrderId
                                && d.EndedAtUtc == null)
                            .OrderByDescending(d => d.StartedAtUtc)
                            .FirstOrDefaultAsync(ct);

                        if (ad is not null)
                        {
                            activeDowntime = new DowntimeReadModel(
                                ad.Id,
                                ad.Category,
                                ad.ReasonCode,
                                ad.StartedAtUtc,
                                ad.EndedAtUtc,
                                ad.DurationSeconds,
                                ad.Comment,
                                ad.StartedBySubjectId,
                                ad.EndedBySubjectId,
                                IsActive: true);
                        }
                    }

                    // Active setup
                    SetupSessionReadModel? activeSetup = null;
                    {
                        var ass = await dbContext.ProductionSetupSessions
                            .AsNoTracking()
                            .Where(s =>
                                s.TenantId == context.TenantId
                                && s.ProductionOrderId == productionOrderId
                                && s.CompletedAtUtc == null)
                            .OrderByDescending(s => s.StartedAtUtc)
                            .FirstOrDefaultAsync(ct);

                        if (ass is not null)
                        {
                            activeSetup = new SetupSessionReadModel(
                                ass.Id,
                                ass.SetupType,
                                ass.StartedAtUtc,
                                ass.CompletedAtUtc,
                                ass.DurationSeconds,
                                ass.Comment,
                                ass.StartedBySubjectId,
                                ass.CompletedBySubjectId,
                                IsActive: true);
                        }
                    }

                    // Derived elapsed / net running
                    long? elapsedSeconds = null;
                    long? netRunningSeconds = null;

                    if (order.ActualStartAtUtc.HasValue)
                    {
                        var reference = order.ActualEndAtUtc ?? now;
                        elapsedSeconds = (long)Math.Max(0, Math.Ceiling((reference - order.ActualStartAtUtc.Value).TotalSeconds));

                        // Active downtime contribution (not yet stored in TotalDowntimeSeconds)
                        long activeDowntimeSecs = 0;
                        if (activeDowntime is not null)
                        {
                            activeDowntimeSecs = (long)Math.Max(0, Math.Ceiling((now - activeDowntime.StartedAtUtc).TotalSeconds));
                        }

                        var totalDowntime = order.TotalDowntimeSeconds + activeDowntimeSecs;
                        netRunningSeconds = Math.Max(0, elapsedSeconds.Value - totalDowntime);
                    }

                    long? averageCycleMs = null;
                    if (order.ManualCycleCount > 0)
                    {
                        averageCycleMs = order.TotalManualCycleDurationMilliseconds / order.ManualCycleCount;
                    }

                    var summary = new ProductionRuntimeSummary(
                        order.ActualStartAtUtc,
                        order.ActualEndAtUtc,
                        elapsedSeconds,
                        order.TotalDowntimeSeconds,
                        order.TotalSetupSeconds,
                        netRunningSeconds,
                        order.ManualCycleCount,
                        averageCycleMs,
                        order.LastCycleAtUtc,
                        activeDowntime,
                        activeSetup);

                    return ProductionRuntimeSummaryResult.Success(summary);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionRuntimeSummaryResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production operations service is unavailable.");
        }
    }

    // ─────────────────────────────── Helpers ──────────────────────────────────

    private Task<ProductionOrder?> LoadOrderAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        CancellationToken ct) =>
        dbContext.ProductionOrders
            .SingleOrDefaultAsync(
                o =>
                    o.TenantId == context.TenantId
                    && o.FacilityId == context.FacilityId
                    && o.Id == productionOrderId,
                ct);

    private void AddAudit(
        ProductionOrderOperationContext context,
        string action,
        string eventType,
        string resourceType,
        string resourceId,
        Guid correlationId,
        DateTimeOffset now,
        object metadata)
    {
        dbContext.AuditRecords.Add(new AuditRecord
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            IdentitySubjectId = context.IdentitySubjectId,
            TenantMembershipId = context.MembershipId,
            EventType = eventType,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Outcome = AuditOutcome.Success,
            PermissionKey = context.PermissionKey,
            CorrelationId = correlationId,
            Source = AuditSource.Api,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            MetadataJson = JsonSerializer.Serialize(metadata, JsonOptions),
        });
    }

    // ────────────────────────── 4L: Recover Downtime ─────────────────────────

    public async Task<ProductionOperationsWriteResult> RecoverDowntimeAsync(
        ProductionOrderOperationContext context,
        RecoverDowntimeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        if (string.IsNullOrWhiteSpace(command.Resolution))
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.CommentRequired,
                "A resolution reason is required to recover a stale downtime.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var order = await dbContext.ProductionOrders
                        .SingleOrDefaultAsync(
                            o => o.TenantId == context.TenantId
                                && o.FacilityId == context.FacilityId
                                && o.Id == command.ProductionOrderId,
                            ct);

                    if (order is null)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.NotFound,
                            "Production order was not found.");
                    }

                    if (order.Version != command.ExpectedVersion)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.ConcurrencyConflict,
                            "The production order has been modified. Please reload and try again.");
                    }

                    var downtime = await dbContext.ProductionDowntimes
                        .SingleOrDefaultAsync(
                            d => d.TenantId == context.TenantId
                                && d.ProductionOrderId == command.ProductionOrderId
                                && d.Id == command.DowntimeId,
                            ct);

                    if (downtime is null)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.DowntimeNotFound,
                            "Downtime session was not found.");
                    }

                    if (downtime.EndedAtUtc is not null)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.DowntimeNotActive,
                            "Only active (not yet ended) downtime sessions can be recovered.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    var durationSeconds = (long)(now - downtime.StartedAtUtc).TotalSeconds;

                    downtime.EndedAtUtc = now;
                    downtime.EndedBySubjectId = actorSubjectId;
                    downtime.DurationSeconds = durationSeconds;
                    downtime.RecoveredAtUtc = now;
                    downtime.RecoveredBySubjectId = actorSubjectId;
                    downtime.RecoveryReason = command.Resolution.Trim();
                    downtime.Version += 1;

                    order.TotalDowntimeSeconds += durationSeconds;
                    order.LastModifiedBySubjectId = actorSubjectId;
                    order.Version += 1;
                    order.UpdatedAtUtc = now;

                    if (command.ResumeProduction && order.Status == ProductionOrderStatus.Paused)
                    {
                        order.Status = ProductionOrderStatus.InProgress;
                    }

                    var auditAction = ProductionAuditActions.ProductionDowntimeRecover;
                    var auditEventType = ProductionAuditEventTypes.ProductionDowntimeRecovered;
                    var outboxType = ProductionOutboxMessageTypes.ProductionDowntimeRecovered;

                    AddAudit(context, auditAction, auditEventType, "production.order", order.Id.ToString(),
                        correlationId, now, new
                        {
                            productionOrderId = order.Id.ToString(),
                            downtimeId = downtime.Id.ToString(),
                            durationSeconds,
                            resumeProduction = command.ResumeProduction,
                        });

                    AddOutbox(context, outboxType, "production-order", order.Id.ToString(), correlationId, now,
                        new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionOrderId"] = order.Id.ToString(),
                            ["downtimeId"] = downtime.Id.ToString(),
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionOperationsWriteResult.Success(downtime.Id, order.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.ConcurrencyConflict,
                "The production order was modified concurrently. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production operations service is unavailable.");
        }
    }

    // ────────────────────────── 4L: Recover Setup ─────────────────────────────

    public async Task<ProductionOperationsWriteResult> RecoverSetupSessionAsync(
        ProductionOrderOperationContext context,
        RecoverSetupSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        if (string.IsNullOrWhiteSpace(command.Resolution))
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.CommentRequired,
                "A resolution reason is required to recover a stale setup session.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var order = await dbContext.ProductionOrders
                        .SingleOrDefaultAsync(
                            o => o.TenantId == context.TenantId
                                && o.FacilityId == context.FacilityId
                                && o.Id == command.ProductionOrderId,
                            ct);

                    if (order is null)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.NotFound,
                            "Production order was not found.");
                    }

                    if (order.Version != command.ExpectedVersion)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.ConcurrencyConflict,
                            "The production order has been modified. Please reload and try again.");
                    }

                    var session = await dbContext.ProductionSetupSessions
                        .SingleOrDefaultAsync(
                            s => s.TenantId == context.TenantId
                                && s.ProductionOrderId == command.ProductionOrderId
                                && s.Id == command.SetupSessionId,
                            ct);

                    if (session is null)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.SetupSessionNotFound,
                            "Setup session was not found.");
                    }

                    if (session.CompletedAtUtc is not null)
                    {
                        return ProductionOperationsWriteResult.Fail(
                            ProductionOrderFailureCodes.SetupSessionNotActive,
                            "Only active (not yet completed) setup sessions can be recovered.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    var durationSeconds = (long)(now - session.StartedAtUtc).TotalSeconds;

                    session.CompletedAtUtc = now;
                    session.CompletedBySubjectId = actorSubjectId;
                    session.DurationSeconds = durationSeconds;
                    session.RecoveredAtUtc = now;
                    session.RecoveredBySubjectId = actorSubjectId;
                    session.RecoveryReason = command.Resolution.Trim();
                    session.Version += 1;

                    order.TotalSetupSeconds += durationSeconds;
                    order.LastModifiedBySubjectId = actorSubjectId;
                    order.Version += 1;
                    order.UpdatedAtUtc = now;

                    AddAudit(context, ProductionAuditActions.ProductionSetupRecover,
                        ProductionAuditEventTypes.ProductionSetupRecovered, "production.order", order.Id.ToString(),
                        correlationId, now, new
                        {
                            productionOrderId = order.Id.ToString(),
                            setupSessionId = session.Id.ToString(),
                            durationSeconds,
                        });

                    AddOutbox(context, ProductionOutboxMessageTypes.ProductionSetupRecovered,
                        "production-order", order.Id.ToString(), correlationId, now,
                        new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionOrderId"] = order.Id.ToString(),
                            ["setupSessionId"] = session.Id.ToString(),
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionOperationsWriteResult.Success(session.Id, order.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.ConcurrencyConflict,
                "The production order was modified concurrently. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionOperationsWriteResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production operations service is unavailable.");
        }
    }

    // ────────────────────────── 4L: Health Summary ────────────────────────────

    public async Task<ProductionHealthSummaryResult> GetHealthSummaryAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        ProductionExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionHealthSummaryResult.Fail(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            var order = await dbContext.ProductionOrders
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    o => o.TenantId == context.TenantId
                        && o.FacilityId == context.FacilityId
                        && o.Id == productionOrderId,
                    cancellationToken);

            if (order is null)
            {
                return ProductionHealthSummaryResult.Fail(
                    ProductionOrderFailureCodes.NotFound,
                    "Production order was not found.");
            }

            var now = DateTimeOffset.UtcNow;

            // Active sessions
            var activeDowntime = await dbContext.ProductionDowntimes
                .AsNoTracking()
                .Where(d => d.TenantId == context.TenantId
                    && d.ProductionOrderId == productionOrderId
                    && d.EndedAtUtc == null)
                .Select(d => new { d.Id, d.StartedAtUtc })
                .FirstOrDefaultAsync(cancellationToken);

            var activeSetup = await dbContext.ProductionSetupSessions
                .AsNoTracking()
                .Where(s => s.TenantId == context.TenantId
                    && s.ProductionOrderId == productionOrderId
                    && s.CompletedAtUtc == null)
                .Select(s => new { s.Id, s.StartedAtUtc })
                .FirstOrDefaultAsync(cancellationToken);

            // Exceptions
            var exceptionCounts = await dbContext.ProductionExecutionExceptions
                .Where(e => e.TenantId == context.TenantId
                    && e.ProductionOrderId == productionOrderId
                    && ProductionExceptionStatuses.Unresolved.Contains(e.Status))
                .GroupBy(e => e.Severity)
                .Select(g => new { Severity = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var openExceptionCount = exceptionCounts.Sum(g => g.Count);
            var criticalExceptionCount = exceptionCounts
                .Where(g => g.Severity == ProductionExceptionSeverities.Critical)
                .Sum(g => g.Count);

            // Stale detection (read-time deterministic)
            var downtimeIsStale = activeDowntime is not null
                && (now - activeDowntime.StartedAtUtc).TotalMinutes > options.StaleDowntimeThresholdMinutes;

            var setupIsStale = activeSetup is not null
                && (now - activeSetup.StartedAtUtc).TotalMinutes > options.StaleSetupThresholdMinutes;

            // Schedule overrun
            var scheduleOverrun = order.PlannedEndAtUtc < now
                && order.Status != ProductionOrderStatus.Completed;

            // Quantity progress
            decimal? quantityProgress = order.PlannedQuantity.HasValue && order.PlannedQuantity.Value > 0
                ? Math.Min(order.GoodQuantity / order.PlannedQuantity.Value * 100m, 100m)
                : null;

            // Last operational activity
            var lastActivity = new[] {
                order.LastProductionEntryAtUtc,
                order.LastCycleAtUtc,
                order.ActualStartAtUtc,
            }
            .Where(t => t.HasValue)
            .Select(t => t!.Value)
            .DefaultIfEmpty()
            .Max();

            DateTimeOffset? lastActivityUtc = lastActivity == default ? null : lastActivity;

            // Attention signals
            var requiresAttention = openExceptionCount > 0
                || criticalExceptionCount > 0
                || downtimeIsStale
                || setupIsStale
                || scheduleOverrun;

            var healthStatus = criticalExceptionCount > 0
                ? "critical"
                : requiresAttention
                    ? "attention"
                    : "normal";

            var summary = new ProductionExecutionHealthSummary(
                Status: healthStatus,
                OpenExceptionCount: openExceptionCount,
                CriticalExceptionCount: criticalExceptionCount,
                HasActiveDowntime: activeDowntime is not null,
                DowntimeIsStale: downtimeIsStale,
                HasActiveSetup: activeSetup is not null,
                SetupIsStale: setupIsStale,
                QuantityProgress: quantityProgress,
                ScheduleOverrun: scheduleOverrun,
                LastOperationalActivityAtUtc: lastActivityUtc,
                RequiresAttention: requiresAttention);

            return ProductionHealthSummaryResult.Success(summary);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionHealthSummaryResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production health summary service is unavailable.");
        }
    }

    private void AddOutbox(
        ProductionOrderOperationContext context,
        string messageType,
        string aggregateType,
        string aggregateId,
        Guid correlationId,
        DateTimeOffset now,
        Dictionary<string, string?> payload)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            MessageType = messageType,
            SchemaVersion = OutboxMessageTypes.SchemaVersion,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            CorrelationId = correlationId,
            CausationId = null,
            IdempotencyKey = $"{aggregateId}:{messageType}:{now:O}",
            PayloadJson = JsonSerializer.Serialize(payload),
            HeadersJson = "{}",
            Status = OutboxMessageStatus.Pending,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            AvailableAtUtc = now,
            AttemptCount = 0,
        });
    }

    // ── Taxonomy helpers ───────────────────────────────────────────────────────

    private static readonly HashSet<string> PlannedReasonCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "break", "planned-maintenance", "material-change", "cleaning", "quality-check", "operator-change",
    };

    private static readonly HashSet<string> UnplannedReasonCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "machine-fault", "mold-fault", "material-shortage", "quality-hold",
        "utility-failure", "operator-unavailable", "process-instability", "other",
    };

    private static bool TryGetDowntimeCategory(string reasonCode, out string category)
    {
        if (PlannedReasonCodes.Contains(reasonCode))
        {
            category = "planned";
            return true;
        }

        if (UnplannedReasonCodes.Contains(reasonCode))
        {
            category = "unplanned";
            return true;
        }

        category = string.Empty;
        return false;
    }

    private static readonly HashSet<string> ValidSetupTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "initial-setup", "mold-change", "material-change", "product-change", "parameter-adjustment", "other",
    };

    private static bool IsValidSetupType(string setupType) =>
        ValidSetupTypes.Contains(setupType);
}

/// <summary>Unavailable stub used when persistence is disabled.</summary>
public sealed class UnavailableProductionOperationsRepository : IProductionOperationsRepository
{
    private static readonly ProductionOperationsWriteResult Unavailable =
        ProductionOperationsWriteResult.Fail(
            ProductionOrderFailureCodes.SourceUnavailable,
            "Production operations service is unavailable because persistence is disabled.");

    private static readonly ProductionRuntimeSummaryResult UnavailableSummary =
        ProductionRuntimeSummaryResult.Fail(
            ProductionOrderFailureCodes.SourceUnavailable,
            "Production operations service is unavailable because persistence is disabled.");

    private static readonly ProductionHealthSummaryResult UnavailableHealth =
        ProductionHealthSummaryResult.Fail(
            ProductionOrderFailureCodes.SourceUnavailable,
            "Production operations service is unavailable because persistence is disabled.");

    public Task<ProductionOperationsWriteResult> StartDowntimeAsync(ProductionOrderOperationContext context, StartDowntimeCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable);
    public Task<ProductionOperationsWriteResult> EndDowntimeAsync(ProductionOrderOperationContext context, EndDowntimeCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable);
    public Task<ProductionOperationsListResult<DowntimeReadModel>> ListDowntimesAsync(ProductionOrderOperationContext context, Guid productionOrderId, int limit, CancellationToken cancellationToken = default) => Task.FromResult(ProductionOperationsListResult.Fail<DowntimeReadModel>(ProductionOrderFailureCodes.SourceUnavailable, "Unavailable."));
    public Task<ProductionOperationsWriteResult> StartSetupSessionAsync(ProductionOrderOperationContext context, StartSetupSessionCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable);
    public Task<ProductionOperationsWriteResult> CompleteSetupSessionAsync(ProductionOrderOperationContext context, CompleteSetupSessionCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable);
    public Task<ProductionOperationsListResult<SetupSessionReadModel>> ListSetupSessionsAsync(ProductionOrderOperationContext context, Guid productionOrderId, int limit, CancellationToken cancellationToken = default) => Task.FromResult(ProductionOperationsListResult.Fail<SetupSessionReadModel>(ProductionOrderFailureCodes.SourceUnavailable, "Unavailable."));
    public Task<ProductionOperationsWriteResult> RecordManualCycleAsync(ProductionOrderOperationContext context, RecordManualCycleCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable);
    public Task<ProductionOperationsListResult<CycleEntryReadModel>> ListCycleEntriesAsync(ProductionOrderOperationContext context, Guid productionOrderId, int limit, CancellationToken cancellationToken = default) => Task.FromResult(ProductionOperationsListResult.Fail<CycleEntryReadModel>(ProductionOrderFailureCodes.SourceUnavailable, "Unavailable."));
    public Task<ProductionRuntimeSummaryResult> GetRuntimeSummaryAsync(ProductionOrderOperationContext context, Guid productionOrderId, CancellationToken cancellationToken = default) => Task.FromResult(UnavailableSummary);
    public Task<ProductionOperationsWriteResult> RecoverDowntimeAsync(ProductionOrderOperationContext context, RecoverDowntimeCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable);
    public Task<ProductionOperationsWriteResult> RecoverSetupSessionAsync(ProductionOrderOperationContext context, RecoverSetupSessionCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable);
    public Task<ProductionHealthSummaryResult> GetHealthSummaryAsync(ProductionOrderOperationContext context, Guid productionOrderId, ProductionExecutionOptions options, CancellationToken cancellationToken = default) => Task.FromResult(UnavailableHealth);
}
