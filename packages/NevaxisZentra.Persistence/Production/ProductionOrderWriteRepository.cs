using System.Text.Json;
using System.Text.Json.Serialization;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Outbox;
using NevaxisZentra.Persistence.ProductionPlanning;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace NevaxisZentra.Persistence.Production;

public sealed class ProductionOrderWriteRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IOptions<ProductionExecutionPolicyOptions>? policyOptions = null)
    : IProductionOrderWriteRepository
{
    private ProductionExecutionPolicyOptions Policy =>
        policyOptions?.Value ?? new ProductionExecutionPolicyOptions();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ─────────────────────────── Consume Handoff ───────────────────────────

    public async Task<ProductionOrderWriteResult> ConsumeHandoffAsync(
        ProductionOrderOperationContext context,
        ConsumeHandoffCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionOrderWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    // Load handoff within tenant/facility scope
                    var handoff = await dbContext.ProductionPlanHandoffs
                        .SingleOrDefaultAsync(
                            h =>
                                h.TenantId == context.TenantId
                                && h.FacilityId == context.FacilityId
                                && h.Id == command.HandoffId,
                            ct);

                    if (handoff is null)
                    {
                        return ProductionOrderWriteResult.Fail(
                            ProductionOrderFailureCodes.HandoffNotFound,
                            "Production plan handoff was not found in the requested scope.");
                    }

                    // Idempotent: if already consumed, return the existing order
                    if (handoff.Status == ProductionPlanHandoffStatus.Consumed)
                    {
                        var existingOrder = await dbContext.ProductionOrders
                            .AsNoTracking()
                            .SingleOrDefaultAsync(
                                o =>
                                    o.TenantId == context.TenantId
                                    && o.ProductionPlanHandoffId == handoff.Id,
                                ct);

                        if (existingOrder is not null)
                        {
                            return ProductionOrderWriteResult.Success(
                                existingOrder.Id,
                                existingOrder.ReferenceCode,
                                ProductionOrderValueConversions.ToStorage(existingOrder.Status),
                                existingOrder.Version,
                                existingOrder.CreatedAtUtc,
                                existingOrder.UpdatedAtUtc);
                        }

                        return ProductionOrderWriteResult.Fail(
                            ProductionOrderFailureCodes.HandoffAlreadyConsumed,
                            "This handoff has already been consumed.");
                    }

                    if (handoff.Status != ProductionPlanHandoffStatus.Prepared)
                    {
                        return ProductionOrderWriteResult.Fail(
                            ProductionOrderFailureCodes.HandoffNotReady,
                            $"Only prepared handoffs may be consumed. Current status: '{handoff.Status}'.");
                    }

                    if (handoff.Version != command.ExpectedHandoffVersion)
                    {
                        return ProductionOrderWriteResult.Fail(
                            ProductionOrderFailureCodes.ConcurrencyConflict,
                            "The handoff has been modified. Please reload and try again.");
                    }

                    // Load the source plan to use its dates as fallback when the payload is missing them
                    var sourcePlan = await dbContext.ProductionPlans
                        .AsNoTracking()
                        .Where(p => p.TenantId == context.TenantId && p.Id == handoff.ProductionPlanId)
                        .Select(p => new { p.PlannedStartAtUtc, p.PlannedEndAtUtc })
                        .SingleOrDefaultAsync(ct);

                    // Map handoff payload → ProductionOrder
                    var mapResult = MapHandoffToOrder(
                        handoff,
                        sourcePlan?.PlannedStartAtUtc,
                        sourcePlan?.PlannedEndAtUtc);
                    if (!mapResult.Succeeded)
                    {
                        return ProductionOrderWriteResult.Fail(
                            ProductionOrderFailureCodes.HandoffInvalid,
                            mapResult.FailureReason ?? "Handoff payload could not be mapped to a production order.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    var referenceCode = BuildOrderReferenceCode(handoff.ReferenceCode);

                    var order = new ProductionOrder
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ProductionPlanHandoffId = handoff.Id,
                        SourceProductionPlanId = handoff.ProductionPlanId,
                        SourceProductionPlanVersion = handoff.ProductionPlanVersion,
                        ReferenceCode = referenceCode,
                        Title = mapResult.Title!,
                        Status = ProductionOrderStatus.Ready,
                        PlannedStartAtUtc = mapResult.PlannedStartAtUtc!.Value,
                        PlannedEndAtUtc = mapResult.PlannedEndAtUtc!.Value,
                        ShiftName = mapResult.ShiftName,
                        MachineReference = mapResult.MachineReference,
                        MoldReference = mapResult.MoldReference,
                        ProductReference = mapResult.ProductReference,
                        PlannedQuantity = mapResult.PlannedQuantity,
                        PlannedQuantityUnit = mapResult.PlannedQuantityUnit,
                        GoodQuantity = 0,
                        ScrapQuantity = 0,
                        CreatedBySubjectId = actorSubjectId,
                        LastModifiedBySubjectId = actorSubjectId,
                        Version = 1,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now,
                        HistoryJson = BuildInitialHistoryJson(now, actorSubjectId),
                    };

                    dbContext.ProductionOrders.Add(order);

                    // Mark handoff consumed
                    handoff.Status = ProductionPlanHandoffStatus.Consumed;
                    handoff.ConsumedAtUtc = now;
                    handoff.ProductionReference = referenceCode;
                    handoff.Version += 1;
                    handoff.UpdatedAtUtc = now;

                    dbContext.AuditRecords.Add(new AuditRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        IdentitySubjectId = context.IdentitySubjectId,
                        TenantMembershipId = context.MembershipId,
                        EventType = ProductionAuditEventTypes.ProductionOrderCreated,
                        Action = ProductionAuditActions.ProductionOrderCreate,
                        ResourceType = "production.order",
                        ResourceId = order.Id.ToString(),
                        Outcome = AuditOutcome.Success,
                        PermissionKey = context.PermissionKey,
                        CorrelationId = correlationId,
                        Source = AuditSource.Api,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        MetadataJson = JsonSerializer.Serialize(new
                        {
                            productionOrderId = order.Id.ToString(),
                            handoffId = handoff.Id.ToString(),
                            sourcePlanId = handoff.ProductionPlanId.ToString(),
                            referenceCode,
                            status = "ready",
                        }),
                    });

                    dbContext.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        MessageType = ProductionOutboxMessageTypes.ProductionOrderCreated,
                        SchemaVersion = OutboxMessageTypes.SchemaVersion,
                        AggregateType = "production-order",
                        AggregateId = order.Id.ToString(),
                        CorrelationId = correlationId,
                        CausationId = null,
                        IdempotencyKey = $"{order.Id}:1:{ProductionOutboxMessageTypes.ProductionOrderCreated}",
                        PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionOrderId"] = order.Id.ToString(),
                            ["handoffId"] = handoff.Id.ToString(),
                            ["referenceCode"] = referenceCode,
                            ["status"] = "ready",
                            ["occurredAtUtc"] = now.ToString("O"),
                        }),
                        HeadersJson = "{}",
                        Status = OutboxMessageStatus.Pending,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        AvailableAtUtc = now,
                        AttemptCount = 0,
                    });

                    dbContext.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        MessageType = ProductionOutboxMessageTypes.ProductionPlanHandoffConsumed,
                        SchemaVersion = OutboxMessageTypes.SchemaVersion,
                        AggregateType = "production-plan-handoff",
                        AggregateId = handoff.Id.ToString(),
                        CorrelationId = correlationId,
                        CausationId = null,
                        IdempotencyKey = $"{handoff.Id}:{handoff.Version}:{ProductionOutboxMessageTypes.ProductionPlanHandoffConsumed}",
                        PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["handoffId"] = handoff.Id.ToString(),
                            ["productionOrderId"] = order.Id.ToString(),
                            ["referenceCode"] = referenceCode,
                            ["occurredAtUtc"] = now.ToString("O"),
                        }),
                        HeadersJson = "{}",
                        Status = OutboxMessageStatus.Pending,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        AvailableAtUtc = now,
                        AttemptCount = 0,
                    });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionOrderWriteResult.Success(
                        order.Id,
                        order.ReferenceCode,
                        ProductionOrderValueConversions.ToStorage(order.Status),
                        order.Version,
                        order.CreatedAtUtc,
                        order.UpdatedAtUtc);
                },
                cancellationToken);
        }
        catch (DbUpdateException dbEx) when (IsUniqueViolation(dbEx))
        {
            return ProductionOrderWriteResult.Fail(
                ProductionOrderFailureCodes.HandoffAlreadyConsumed,
                "This handoff has already been consumed (concurrent request).");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionOrderWriteResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production order service is unavailable.");
        }
    }

    // ─────────────────────────── Start ───────────────────────────

    public async Task<ProductionOrderWriteResult> StartAsync(
        ProductionOrderOperationContext context,
        StartProductionOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        // 4L: Block Start if an active setup session exists — setup must be completed first.
        return await ExecuteLifecycleAsync(
            context,
            command.ProductionOrderId,
            command.ExpectedVersion,
            async (order, ct) =>
            {
                if (order.Status != ProductionOrderStatus.Ready)
                {
                    return ProductionOrderWriteResult.Fail(
                        ProductionOrderFailureCodes.InvalidTransition,
                        $"Only Ready orders can be started. Current status: '{ProductionOrderValueConversions.ToStorage(order.Status)}'.");
                }

                var hasActiveSetup = await dbContext.ProductionSetupSessions
                    .AnyAsync(s => s.TenantId == context.TenantId
                        && s.ProductionOrderId == order.Id
                        && s.CompletedAtUtc == null, ct);

                if (hasActiveSetup)
                {
                    return ProductionOrderWriteResult.Fail(
                        ProductionOrderFailureCodes.ActiveSetupExists,
                        "An active setup session exists. Complete or recover it before starting production.");
                }

                // 5L: primary operator policy
                if (Policy.RequirePrimaryOperatorForStart)
                {
                    var hasPrimary = await dbContext.ProductionOperatorAssignments
                        .AnyAsync(oa => oa.TenantId == context.TenantId
                            && oa.ProductionOrderId == order.Id
                            && oa.AssignmentRole == ProductionOperatorRoles.PrimaryOperator
                            && oa.ReleasedAtUtc == null, ct);

                    if (!hasPrimary)
                    {
                        return ProductionOrderWriteResult.Fail(
                            ProductionOrderFailureCodes.PrimaryOperatorRequired,
                            "A primary operator must be assigned before starting production.");
                    }
                }

                // 5L: shift policy
                if (Policy.RequireShiftForStart)
                {
                    var hasShift = await dbContext.ProductionOrderShiftAssignments
                        .AnyAsync(sa => sa.TenantId == context.TenantId
                            && sa.ProductionOrderId == order.Id
                            && sa.ReleasedAtUtc == null, ct);

                    if (!hasShift)
                    {
                        return ProductionOrderWriteResult.Fail(
                            ProductionOrderFailureCodes.ShiftRequired,
                            "A shift must be assigned before starting production.");
                    }
                }

                return null;
            },
            (order, now) =>
            {
                order.Status = ProductionOrderStatus.InProgress;
                order.ActualStartAtUtc = now;
            },
            ProductionAuditActions.ProductionOrderStart,
            ProductionAuditEventTypes.ProductionOrderStarted,
            ProductionOutboxMessageTypes.ProductionOrderStarted,
            cancellationToken);
    }

    // ─────────────────────────── Pause ───────────────────────────

    public async Task<ProductionOrderWriteResult> PauseAsync(
        ProductionOrderOperationContext context,
        PauseProductionOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteLifecycleAsync(
            context,
            command.ProductionOrderId,
            command.ExpectedVersion,
            order =>
            {
                if (order.Status != ProductionOrderStatus.InProgress)
                {
                    return ProductionOrderWriteResult.Fail(
                        ProductionOrderFailureCodes.InvalidTransition,
                        $"Only InProgress orders can be paused. Current status: '{ProductionOrderValueConversions.ToStorage(order.Status)}'.");
                }

                return null;
            },
            (order, now) =>
            {
                order.Status = ProductionOrderStatus.Paused;
                AppendHistoryEntry(order, "paused", now, context.IdentitySubjectId.ToString(), command.Reason);
            },
            ProductionAuditActions.ProductionOrderPause,
            ProductionAuditEventTypes.ProductionOrderPaused,
            ProductionOutboxMessageTypes.ProductionOrderPaused,
            cancellationToken);
    }

    // ─────────────────────────── Resume ───────────────────────────

    public async Task<ProductionOrderWriteResult> ResumeAsync(
        ProductionOrderOperationContext context,
        ResumeProductionOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        // 4L: Block Resume if an active downtime exists — end or recover it first.
        return await ExecuteLifecycleAsync(
            context,
            command.ProductionOrderId,
            command.ExpectedVersion,
            async (order, ct) =>
            {
                if (order.Status != ProductionOrderStatus.Paused)
                {
                    return ProductionOrderWriteResult.Fail(
                        ProductionOrderFailureCodes.InvalidTransition,
                        $"Only Paused orders can be resumed. Current status: '{ProductionOrderValueConversions.ToStorage(order.Status)}'.");
                }

                var hasActiveDowntime = await dbContext.ProductionDowntimes
                    .AnyAsync(d => d.TenantId == context.TenantId
                        && d.ProductionOrderId == order.Id
                        && d.EndedAtUtc == null, ct);

                if (hasActiveDowntime)
                {
                    return ProductionOrderWriteResult.Fail(
                        ProductionOrderFailureCodes.ActiveDowntimeExists,
                        "An active downtime session exists. End or recover it before resuming production.");
                }

                return null;
            },
            (order, _) =>
            {
                order.Status = ProductionOrderStatus.InProgress;
            },
            ProductionAuditActions.ProductionOrderResume,
            ProductionAuditEventTypes.ProductionOrderResumed,
            ProductionOutboxMessageTypes.ProductionOrderResumed,
            cancellationToken);
    }

    // ─────────────────────────── Complete ───────────────────────────

    public async Task<ProductionOrderWriteResult> CompleteAsync(
        ProductionOrderOperationContext context,
        CompleteProductionOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        // isShortCompletion is set inside the validation lambda but must be used for taxonomy
        // selection AFTER the lambda runs. Use a ref-box (single-element array) so the closure
        // can mutate the value before the taxonomy strings are read inside ExecuteLifecycleAsync.
        var shortFlag = new[] { false };

        // 4L: Block Complete if active sessions or critical exceptions exist.
        return await ExecuteLifecycleAsync(
            context,
            command.ProductionOrderId,
            command.ExpectedVersion,
            async (order, ct) =>
            {
                if (order.Status != ProductionOrderStatus.InProgress)
                {
                    return ProductionOrderWriteResult.Fail(
                        ProductionOrderFailureCodes.InvalidTransition,
                        $"Only InProgress orders can be completed. Current status: '{ProductionOrderValueConversions.ToStorage(order.Status)}'.");
                }

                var hasActiveDowntime = await dbContext.ProductionDowntimes
                    .AnyAsync(d => d.TenantId == context.TenantId
                        && d.ProductionOrderId == order.Id
                        && d.EndedAtUtc == null, ct);

                if (hasActiveDowntime)
                {
                    return ProductionOrderWriteResult.Fail(
                        ProductionOrderFailureCodes.ActiveDowntimeExists,
                        "An active downtime session must be ended or recovered before completing the order.");
                }

                var hasActiveSetup = await dbContext.ProductionSetupSessions
                    .AnyAsync(s => s.TenantId == context.TenantId
                        && s.ProductionOrderId == order.Id
                        && s.CompletedAtUtc == null, ct);

                if (hasActiveSetup)
                {
                    return ProductionOrderWriteResult.Fail(
                        ProductionOrderFailureCodes.ActiveSetupExists,
                        "An active setup session must be completed or recovered before completing the order.");
                }

                var hasCriticalException = await dbContext.ProductionExecutionExceptions
                    .AnyAsync(e => e.TenantId == context.TenantId
                        && e.ProductionOrderId == order.Id
                        && e.Severity == ProductionExceptionSeverities.Critical
                        && (e.Status == ProductionExceptionStatuses.Open || e.Status == ProductionExceptionStatuses.Acknowledged), ct);

                if (hasCriticalException)
                {
                    return ProductionOrderWriteResult.Fail(
                        ProductionOrderFailureCodes.CriticalExceptionOpen,
                        "One or more critical exceptions are unresolved. Resolve them before completing the order.");
                }

                // Quantity guard: if planned quantity is known, enforce completion rules
                if (order.PlannedQuantity.HasValue)
                {
                    var targetReached = order.GoodQuantity >= order.PlannedQuantity.Value;

                    if (!targetReached)
                    {
                        if (!command.AllowShortCompletion)
                        {
                            return ProductionOrderWriteResult.Fail(
                                ProductionOrderFailureCodes.ProductionTargetNotReached,
                                $"Good quantity {order.GoodQuantity} has not reached planned quantity {order.PlannedQuantity}. Use allowShortCompletion=true with a completionReason to override.");
                        }

                        if (string.IsNullOrWhiteSpace(command.CompletionReason))
                        {
                            return ProductionOrderWriteResult.Fail(
                                ProductionOrderFailureCodes.CompletionReasonRequired,
                                "A completion reason is required when completing short of the planned quantity.");
                        }

                        shortFlag[0] = true;
                    }
                }

                return null;
            },
            (order, now) =>
            {
                order.Status = ProductionOrderStatus.Completed;
                order.ActualEndAtUtc = now;

                // 5L: Clear denormalized resource fields atomically at completion.
                // Active assignments are released below in the post-apply step.
                order.CurrentShiftId = null;
                order.CurrentShiftCode = null;
                order.CurrentShiftStartAtUtc = null;
                order.CurrentShiftEndAtUtc = null;
                order.PrimaryOperatorSubjectId = null;
                order.ActiveOperatorCount = 0;
            },
            () => shortFlag[0] ? ProductionAuditActions.ProductionOrderCompleteShort : ProductionAuditActions.ProductionOrderComplete,
            () => shortFlag[0] ? ProductionAuditEventTypes.ProductionOrderCompletedShort : ProductionAuditEventTypes.ProductionOrderCompleted,
            () => shortFlag[0] ? ProductionOutboxMessageTypes.ProductionOrderCompletedShort : ProductionOutboxMessageTypes.ProductionOrderCompleted,
            cancellationToken);
    }

    // ─────────────────────────── Shared lifecycle helper ───────────────────────────

    // Overload with lazy taxonomy selectors — evaluated after validateTransition runs,
    // allowing validation lambdas to influence audit/outbox taxonomy (e.g. short completion).
    private Task<ProductionOrderWriteResult> ExecuteLifecycleAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        uint expectedVersion,
        Func<ProductionOrder, ProductionOrderWriteResult?> validateTransition,
        Action<ProductionOrder, DateTimeOffset> applyTransition,
        Func<string> auditActionSelector,
        Func<string> auditEventTypeSelector,
        Func<string> outboxMessageTypeSelector,
        CancellationToken cancellationToken) =>
        ExecuteLifecycleAsyncCore(context, productionOrderId, expectedVersion,
            (order, _) => Task.FromResult(validateTransition(order)),
            applyTransition,
            auditActionSelector, auditEventTypeSelector, outboxMessageTypeSelector,
            cancellationToken);

    private Task<ProductionOrderWriteResult> ExecuteLifecycleAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        uint expectedVersion,
        Func<ProductionOrder, ProductionOrderWriteResult?> validateTransition,
        Action<ProductionOrder, DateTimeOffset> applyTransition,
        string auditAction,
        string auditEventType,
        string outboxMessageType,
        CancellationToken cancellationToken) =>
        ExecuteLifecycleAsyncCore(context, productionOrderId, expectedVersion,
            (order, _) => Task.FromResult(validateTransition(order)),
            applyTransition,
            () => auditAction, () => auditEventType, () => outboxMessageType,
            cancellationToken);

    // 4L: Async validation overload — allows lifecycle guards to perform DB queries
    // (active session checks, critical exception checks) within the same transaction.
    private Task<ProductionOrderWriteResult> ExecuteLifecycleAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        uint expectedVersion,
        Func<ProductionOrder, CancellationToken, Task<ProductionOrderWriteResult?>> validateTransitionAsync,
        Action<ProductionOrder, DateTimeOffset> applyTransition,
        string auditAction,
        string auditEventType,
        string outboxMessageType,
        CancellationToken cancellationToken) =>
        ExecuteLifecycleAsyncCore(context, productionOrderId, expectedVersion,
            validateTransitionAsync, applyTransition,
            () => auditAction, () => auditEventType, () => outboxMessageType,
            cancellationToken);

    // 4L: Async validation overload with lazy taxonomy selectors.
    private Task<ProductionOrderWriteResult> ExecuteLifecycleAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        uint expectedVersion,
        Func<ProductionOrder, CancellationToken, Task<ProductionOrderWriteResult?>> validateTransitionAsync,
        Action<ProductionOrder, DateTimeOffset> applyTransition,
        Func<string> auditActionSelector,
        Func<string> auditEventTypeSelector,
        Func<string> outboxMessageTypeSelector,
        CancellationToken cancellationToken) =>
        ExecuteLifecycleAsyncCore(context, productionOrderId, expectedVersion,
            validateTransitionAsync, applyTransition,
            auditActionSelector, auditEventTypeSelector, outboxMessageTypeSelector,
            cancellationToken);

    private async Task<ProductionOrderWriteResult> ExecuteLifecycleAsyncCore(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        uint expectedVersion,
        Func<ProductionOrder, CancellationToken, Task<ProductionOrderWriteResult?>> validateTransitionAsync,
        Action<ProductionOrder, DateTimeOffset> applyTransition,
        Func<string> auditActionSelector,
        Func<string> auditEventTypeSelector,
        Func<string> outboxMessageTypeSelector,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionOrderWriteResult.Fail(
                ProductionOrderFailureCodes.InvalidScope,
                "Tenant and facility identifiers are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var order = await dbContext.ProductionOrders
                        .SingleOrDefaultAsync(
                            o =>
                                o.TenantId == context.TenantId
                                && o.FacilityId == context.FacilityId
                                && o.Id == productionOrderId,
                            ct);

                    if (order is null)
                    {
                        return ProductionOrderWriteResult.Fail(
                            ProductionOrderFailureCodes.NotFound,
                            "Production order was not found in the requested scope.");
                    }

                    if (order.Version != expectedVersion)
                    {
                        return ProductionOrderWriteResult.Fail(
                            ProductionOrderFailureCodes.ConcurrencyConflict,
                            "The production order has been modified. Please reload and try again.");
                    }

                    var validationFailure = await validateTransitionAsync(order, ct);
                    if (validationFailure is not null)
                    {
                        return validationFailure;
                    }

                    // Evaluate taxonomy selectors AFTER validation so lambdas like
                    // CompleteAsync can influence audit/outbox types (e.g. short completion).
                    var auditAction = auditActionSelector();
                    var auditEventType = auditEventTypeSelector();
                    var outboxMessageType = outboxMessageTypeSelector();

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var previousStatus = ProductionOrderValueConversions.ToStorage(order.Status);

                    applyTransition(order, now);

                    // 5L: Release active shift/operator assignments atomically on completion.
                    if (order.Status == ProductionOrderStatus.Completed)
                    {
                        var actorId = context.IdentitySubjectId.ToString();
                        const string completionReleaseReason = "Production order completed.";

                        var activeShiftAssignments = await dbContext.ProductionOrderShiftAssignments
                            .Where(sa => sa.TenantId == context.TenantId && sa.ProductionOrderId == order.Id && sa.ReleasedAtUtc == null)
                            .ToListAsync(ct);
                        foreach (var sa in activeShiftAssignments)
                        {
                            sa.ReleasedAtUtc = now;
                            sa.ReleasedBySubjectId = actorId;
                            sa.ReleaseReason = completionReleaseReason;
                            sa.Version++;
                        }

                        var activeOperatorAssignments = await dbContext.ProductionOperatorAssignments
                            .Where(oa => oa.TenantId == context.TenantId && oa.ProductionOrderId == order.Id && oa.ReleasedAtUtc == null)
                            .ToListAsync(ct);
                        foreach (var oa in activeOperatorAssignments)
                        {
                            oa.ReleasedAtUtc = now;
                            oa.ReleasedBySubjectId = actorId;
                            oa.ReleaseReason = completionReleaseReason;
                            oa.Version++;
                        }
                    }

                    order.LastModifiedBySubjectId = context.IdentitySubjectId.ToString();
                    order.Version += 1;
                    order.UpdatedAtUtc = now;

                    var newStatus = ProductionOrderValueConversions.ToStorage(order.Status);

                    dbContext.AuditRecords.Add(new AuditRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        IdentitySubjectId = context.IdentitySubjectId,
                        TenantMembershipId = context.MembershipId,
                        EventType = auditEventType,
                        Action = auditAction,
                        ResourceType = "production.order",
                        ResourceId = order.Id.ToString(),
                        Outcome = AuditOutcome.Success,
                        PermissionKey = context.PermissionKey,
                        CorrelationId = correlationId,
                        Source = AuditSource.Api,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        MetadataJson = JsonSerializer.Serialize(new
                        {
                            productionOrderId = order.Id.ToString(),
                            previousStatus,
                            status = newStatus,
                            version = order.Version,
                        }),
                    });

                    dbContext.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        MessageType = outboxMessageType,
                        SchemaVersion = OutboxMessageTypes.SchemaVersion,
                        AggregateType = "production-order",
                        AggregateId = order.Id.ToString(),
                        CorrelationId = correlationId,
                        CausationId = null,
                        IdempotencyKey = $"{order.Id}:{order.Version}:{outboxMessageType}",
                        PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["productionOrderId"] = order.Id.ToString(),
                            ["status"] = newStatus,
                            ["occurredAtUtc"] = now.ToString("O"),
                        }),
                        HeadersJson = "{}",
                        Status = OutboxMessageStatus.Pending,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        AvailableAtUtc = now,
                        AttemptCount = 0,
                    });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionOrderWriteResult.Success(
                        order.Id,
                        order.ReferenceCode,
                        newStatus,
                        order.Version,
                        order.CreatedAtUtc,
                        order.UpdatedAtUtc);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionOrderWriteResult.Fail(
                ProductionOrderFailureCodes.ConcurrencyConflict,
                "The production order was modified by another user. Please reload and try again.");
        }
        catch (DbUpdateException dbEx) when (IsUniqueViolation(dbEx))
        {
            // Concurrent lifecycle writes with the same expectedVersion can collide on the
            // outbox idempotency key before EF surfaces a row-version conflict.
            return ProductionOrderWriteResult.Fail(
                ProductionOrderFailureCodes.ConcurrencyConflict,
                "The production order was modified by another user. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionOrderWriteResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production order service is unavailable.");
        }
    }

    // ─────────────────────────── Payload mapping ───────────────────────────

    private sealed record HandoffMapResult(
        bool Succeeded,
        string? Title,
        DateTimeOffset? PlannedStartAtUtc,
        DateTimeOffset? PlannedEndAtUtc,
        string? ShiftName,
        string? MachineReference,
        string? MoldReference,
        string? ProductReference,
        decimal? PlannedQuantity,
        string? PlannedQuantityUnit,
        string? FailureReason);

    private static HandoffMapResult MapHandoffToOrder(
        ProductionPlanHandoff handoff,
        DateTimeOffset? fallbackStartAtUtc = null,
        DateTimeOffset? fallbackEndAtUtc = null)
    {
        try
        {
            using var doc = JsonDocument.Parse(handoff.PayloadJson);
            var root = doc.RootElement;

            DateTimeOffset? start = null;
            DateTimeOffset? end = null;
            string? shiftName = null;
            string? machineReference = null;
            string? moldReference = null;
            string? productReference = null;
            decimal? plannedQuantity = null;
            string? plannedQuantityUnit = null;

            if (root.TryGetProperty("scheduledStartAtUtc", out var startProp)
                && startProp.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(startProp.GetString(), out var parsedStart))
            {
                start = parsedStart;
            }

            start ??= fallbackStartAtUtc;

            if (root.TryGetProperty("scheduledEndAtUtc", out var endProp)
                && endProp.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(endProp.GetString(), out var parsedEnd))
            {
                end = parsedEnd;
            }

            end ??= fallbackEndAtUtc;

            if (root.TryGetProperty("shiftName", out var shiftProp) && shiftProp.ValueKind == JsonValueKind.String)
            {
                shiftName = shiftProp.GetString()?.Trim();
                if (string.IsNullOrEmpty(shiftName))
                {
                    shiftName = null;
                }
            }

            if (root.TryGetProperty("machineId", out var machProp) && machProp.ValueKind == JsonValueKind.String)
            {
                machineReference = machProp.GetString()?.Trim();
                if (string.IsNullOrEmpty(machineReference))
                {
                    machineReference = null;
                }
            }

            if (root.TryGetProperty("moldId", out var moldProp) && moldProp.ValueKind == JsonValueKind.String)
            {
                moldReference = moldProp.GetString()?.Trim();
                if (string.IsNullOrEmpty(moldReference))
                {
                    moldReference = null;
                }
            }

            if (root.TryGetProperty("demands", out var demandsProp)
                && demandsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var demand in demandsProp.EnumerateArray())
                {
                    if (productReference is null
                        && demand.TryGetProperty("productCode", out var pcProp)
                        && pcProp.ValueKind == JsonValueKind.String)
                    {
                        var pc = pcProp.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(pc))
                        {
                            productReference = pc;
                        }
                    }

                    // Extract planned quantity from first demand that has one.
                    // Accept both JSON number (canonical) and JSON string (legacy handoffs stored quantity as GetRawText()).
                    if (plannedQuantity is null)
                    {
                        if (demand.TryGetProperty("quantity", out var qtyProp))
                        {
                            if (qtyProp.ValueKind == JsonValueKind.Number
                                && qtyProp.TryGetDecimal(out var qtyNum)
                                && qtyNum > 0)
                            {
                                plannedQuantity = qtyNum;
                            }
                            else if (qtyProp.ValueKind == JsonValueKind.String
                                && decimal.TryParse(qtyProp.GetString(), System.Globalization.NumberStyles.Number,
                                    System.Globalization.CultureInfo.InvariantCulture, out var qtyStr)
                                && qtyStr > 0)
                            {
                                plannedQuantity = qtyStr;
                            }
                        }
                    }

                    if (plannedQuantityUnit is null
                        && demand.TryGetProperty("unit", out var unitProp)
                        && unitProp.ValueKind == JsonValueKind.String)
                    {
                        plannedQuantityUnit = unitProp.GetString()?.Trim();
                        if (string.IsNullOrEmpty(plannedQuantityUnit))
                        {
                            plannedQuantityUnit = null;
                        }
                    }
                }
            }

            if (start is null || end is null)
            {
                return new HandoffMapResult(
                    false, null, null, null, null, null, null, null, null, null,
                    "Handoff payload is missing scheduled start or end times.");
            }

            var title = $"{handoff.ReferenceCode} — Üretim Emri";

            return new HandoffMapResult(
                true, title, start, end, shiftName, machineReference, moldReference, productReference,
                plannedQuantity, plannedQuantityUnit ?? (plannedQuantity.HasValue ? "piece" : null), null);
        }
        catch
        {
            return new HandoffMapResult(
                false, null, null, null, null, null, null, null, null, null,
                "Handoff payload could not be parsed.");
        }
    }

    private static string BuildOrderReferenceCode(string handoffReferenceCode) =>
        $"ORD-{handoffReferenceCode}";

    private static string BuildInitialHistoryJson(DateTimeOffset occurredAtUtc, string actorSubjectId)
    {
        return JsonSerializer.Serialize(
            new[]
            {
                new
                {
                    type = "created",
                    occurredAtUtc = occurredAtUtc.ToString("O"),
                    actorSubjectId,
                    note = (string?)null,
                },
            });
    }

    private static void AppendHistoryEntry(
        ProductionOrder order,
        string type,
        DateTimeOffset occurredAtUtc,
        string actorSubjectId,
        string? note)
    {
        try
        {
            using var doc = JsonDocument.Parse(order.HistoryJson);
            var entries = doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement.EnumerateArray()
                    .Select(e => e.GetRawText())
                    .ToList()
                : new List<string>();

            var newEntry = JsonSerializer.Serialize(new
            {
                type,
                occurredAtUtc = occurredAtUtc.ToString("O"),
                actorSubjectId,
                note,
            });

            entries.Add(newEntry);
            order.HistoryJson = $"[{string.Join(",", entries)}]";
        }
        catch
        {
            // Don't fail on history append errors
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("23505", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("ix_production_orders_handoff", StringComparison.OrdinalIgnoreCase) == true;
    }
}

public sealed class UnavailableProductionOrderWriteRepository : IProductionOrderWriteRepository
{
    private static readonly ProductionOrderWriteResult UnavailableResult =
        ProductionOrderWriteResult.Fail(
            ProductionOrderFailureCodes.SourceUnavailable,
            "Production order service is unavailable because persistence is disabled.");

    public Task<ProductionOrderWriteResult> ConsumeHandoffAsync(
        ProductionOrderOperationContext context,
        ConsumeHandoffCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableResult);

    public Task<ProductionOrderWriteResult> StartAsync(
        ProductionOrderOperationContext context,
        StartProductionOrderCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableResult);

    public Task<ProductionOrderWriteResult> PauseAsync(
        ProductionOrderOperationContext context,
        PauseProductionOrderCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableResult);

    public Task<ProductionOrderWriteResult> ResumeAsync(
        ProductionOrderOperationContext context,
        ResumeProductionOrderCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableResult);

    public Task<ProductionOrderWriteResult> CompleteAsync(
        ProductionOrderOperationContext context,
        CompleteProductionOrderCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableResult);
}
