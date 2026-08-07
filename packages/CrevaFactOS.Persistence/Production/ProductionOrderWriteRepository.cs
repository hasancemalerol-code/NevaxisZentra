using System.Text.Json;
using System.Text.Json.Serialization;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Outbox;
using CrevaFactOS.Persistence.ProductionPlanning;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Production;

public sealed class ProductionOrderWriteRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IProductionOrderWriteRepository
{
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
        return await ExecuteLifecycleAsync(
            context,
            command.ProductionOrderId,
            command.ExpectedVersion,
            order =>
            {
                if (order.Status != ProductionOrderStatus.Ready)
                {
                    return ProductionOrderWriteResult.Fail(
                        ProductionOrderFailureCodes.InvalidTransition,
                        $"Only Ready orders can be started. Current status: '{ProductionOrderValueConversions.ToStorage(order.Status)}'.");
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
        return await ExecuteLifecycleAsync(
            context,
            command.ProductionOrderId,
            command.ExpectedVersion,
            order =>
            {
                if (order.Status != ProductionOrderStatus.Paused)
                {
                    return ProductionOrderWriteResult.Fail(
                        ProductionOrderFailureCodes.InvalidTransition,
                        $"Only Paused orders can be resumed. Current status: '{ProductionOrderValueConversions.ToStorage(order.Status)}'.");
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
                        $"Only InProgress orders can be completed. Current status: '{ProductionOrderValueConversions.ToStorage(order.Status)}'.");
                }

                return null;
            },
            (order, now) =>
            {
                order.Status = ProductionOrderStatus.Completed;
                order.ActualEndAtUtc = now;
            },
            ProductionAuditActions.ProductionOrderComplete,
            ProductionAuditEventTypes.ProductionOrderCompleted,
            ProductionOutboxMessageTypes.ProductionOrderCompleted,
            cancellationToken);
    }

    // ─────────────────────────── Shared lifecycle helper ───────────────────────────

    private async Task<ProductionOrderWriteResult> ExecuteLifecycleAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        uint expectedVersion,
        Func<ProductionOrder, ProductionOrderWriteResult?> validateTransition,
        Action<ProductionOrder, DateTimeOffset> applyTransition,
        string auditAction,
        string auditEventType,
        string outboxMessageType,
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

                    var validationFailure = validateTransition(order);
                    if (validationFailure is not null)
                    {
                        return validationFailure;
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var previousStatus = ProductionOrderValueConversions.ToStorage(order.Status);

                    applyTransition(order, now);

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
                    if (demand.TryGetProperty("productCode", out var pcProp)
                        && pcProp.ValueKind == JsonValueKind.String)
                    {
                        var pc = pcProp.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(pc))
                        {
                            productReference = pc;
                            break;
                        }
                    }
                }
            }

            if (start is null || end is null)
            {
                return new HandoffMapResult(
                    false, null, null, null, null, null, null, null,
                    "Handoff payload is missing scheduled start or end times.");
            }

            var title = $"{handoff.ReferenceCode} — Üretim Emri";

            return new HandoffMapResult(
                true, title, start, end, shiftName, machineReference, moldReference, productReference, null);
        }
        catch
        {
            return new HandoffMapResult(
                false, null, null, null, null, null, null, null,
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
