using System.Text.Json;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Outbox;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CrevaFactOS.Persistence.Production;

public sealed class ProductionExecutionEntryRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IProductionExecutionEntryRepository
{
    public Task<ProductionExecutionEntryWriteResult> RecordGoodAsync(
        ProductionOrderOperationContext context,
        RecordGoodProductionCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteEntryWriteAsync(
            context,
            command.ProductionOrderId,
            command.ExpectedVersion,
            order =>
            {
                if (order.Status != ProductionOrderStatus.InProgress)
                {
                    return ProductionExecutionEntryWriteResult.Fail(
                        ProductionOrderFailureCodes.InvalidTransition,
                        "Quantity can only be recorded while the order is in progress.");
                }

                if (command.Quantity <= 0)
                {
                    return ProductionExecutionEntryWriteResult.Fail(
                        ProductionOrderFailureCodes.InvalidQuantity,
                        "Quantity must be greater than zero.");
                }

                return null;
            },
            (order, now, correlationId) =>
            {
                var entry = new ProductionExecutionEntry
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ProductionOrderId = order.Id,
                    EntryType = ProductionExecutionEntryType.GoodProduction,
                    Quantity = command.Quantity,
                    Unit = "piece",
                    ReasonCode = null,
                    Comment = command.Comment,
                    RecordedBySubjectId = context.IdentitySubjectId.ToString(),
                    RecordedAtUtc = now,
                    ReversesEntryId = null,
                    CorrelationId = correlationId,
                    Version = 1,
                };

                order.GoodQuantity += command.Quantity;
                order.LastProductionEntryAtUtc = now;

                return entry;
            },
            ProductionAuditActions.ProductionQuantityGoodRecord,
            ProductionAuditEventTypes.ProductionQuantityGoodRecorded,
            ProductionOutboxMessageTypes.ProductionGoodQuantityRecorded,
            "good-production",
            cancellationToken);

    public Task<ProductionExecutionEntryWriteResult> RecordScrapAsync(
        ProductionOrderOperationContext context,
        RecordScrapCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteEntryWriteAsync(
            context,
            command.ProductionOrderId,
            command.ExpectedVersion,
            order =>
            {
                if (order.Status != ProductionOrderStatus.InProgress)
                {
                    return ProductionExecutionEntryWriteResult.Fail(
                        ProductionOrderFailureCodes.InvalidTransition,
                        "Scrap can only be recorded while the order is in progress.");
                }

                if (command.Quantity <= 0)
                {
                    return ProductionExecutionEntryWriteResult.Fail(
                        ProductionOrderFailureCodes.InvalidQuantity,
                        "Quantity must be greater than zero.");
                }

                if (!ProductionExecutionScrapReasons.All.Contains(command.ReasonCode))
                {
                    return ProductionExecutionEntryWriteResult.Fail(
                        ProductionOrderFailureCodes.InvalidScrapReason,
                        $"Scrap reason '{command.ReasonCode}' is not a recognized reason code.");
                }

                if (ProductionExecutionScrapReasons.Requires(command.ReasonCode)
                    && string.IsNullOrWhiteSpace(command.Comment))
                {
                    return ProductionExecutionEntryWriteResult.Fail(
                        ProductionOrderFailureCodes.CommentRequired,
                        "A comment is required when scrap reason is 'other'.");
                }

                return null;
            },
            (order, now, correlationId) =>
            {
                var entry = new ProductionExecutionEntry
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ProductionOrderId = order.Id,
                    EntryType = ProductionExecutionEntryType.Scrap,
                    Quantity = command.Quantity,
                    Unit = "piece",
                    ReasonCode = command.ReasonCode,
                    Comment = command.Comment,
                    RecordedBySubjectId = context.IdentitySubjectId.ToString(),
                    RecordedAtUtc = now,
                    ReversesEntryId = null,
                    CorrelationId = correlationId,
                    Version = 1,
                };

                order.ScrapQuantity += command.Quantity;
                order.LastProductionEntryAtUtc = now;

                return entry;
            },
            ProductionAuditActions.ProductionQuantityScrapRecord,
            ProductionAuditEventTypes.ProductionQuantityScrapRecorded,
            ProductionOutboxMessageTypes.ProductionScrapRecorded,
            "scrap",
            cancellationToken);

    public async Task<ProductionExecutionEntryWriteResult> ReverseAsync(
        ProductionOrderOperationContext context,
        ReverseProductionEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionExecutionEntryWriteResult.Fail(
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
                            o => o.TenantId == context.TenantId
                                && o.FacilityId == context.FacilityId
                                && o.Id == command.ProductionOrderId,
                            ct);

                    if (order is null)
                    {
                        return ProductionExecutionEntryWriteResult.Fail(
                            ProductionOrderFailureCodes.NotFound,
                            "Production order not found in the requested scope.");
                    }

                    if (order.Version != command.ExpectedVersion)
                    {
                        return ProductionExecutionEntryWriteResult.Fail(
                            ProductionOrderFailureCodes.ConcurrencyConflict,
                            "The production order has been modified. Please reload and try again.");
                    }

                    if (order.Status == ProductionOrderStatus.Completed)
                    {
                        return ProductionExecutionEntryWriteResult.Fail(
                            ProductionOrderFailureCodes.InvalidTransition,
                            "Completed orders cannot be modified.");
                    }

                    var originalEntry = await dbContext.ProductionExecutionEntries
                        .SingleOrDefaultAsync(
                            e => e.TenantId == context.TenantId
                                && e.FacilityId == context.FacilityId
                                && e.ProductionOrderId == command.ProductionOrderId
                                && e.Id == command.EntryId,
                            ct);

                    if (originalEntry is null)
                    {
                        return ProductionExecutionEntryWriteResult.Fail(
                            ProductionOrderFailureCodes.ProductionEntryNotFound,
                            "Production entry not found.");
                    }

                    if (originalEntry.EntryType == ProductionExecutionEntryType.Reversal)
                    {
                        return ProductionExecutionEntryWriteResult.Fail(
                            ProductionOrderFailureCodes.ProductionEntryAlreadyReversed,
                            "A reversal entry cannot itself be reversed.");
                    }

                    var alreadyReversed = await dbContext.ProductionExecutionEntries
                        .AnyAsync(
                            e => e.TenantId == context.TenantId
                                && e.ProductionOrderId == command.ProductionOrderId
                                && e.ReversesEntryId == command.EntryId,
                            ct);

                    if (alreadyReversed)
                    {
                        return ProductionExecutionEntryWriteResult.Fail(
                            ProductionOrderFailureCodes.ProductionEntryAlreadyReversed,
                            "This entry has already been reversed.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();

                    var reversalEntry = new ProductionExecutionEntry
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ProductionOrderId = order.Id,
                        EntryType = ProductionExecutionEntryType.Reversal,
                        Quantity = originalEntry.Quantity,
                        Unit = originalEntry.Unit,
                        ReasonCode = null,
                        Comment = command.Reason,
                        RecordedBySubjectId = context.IdentitySubjectId.ToString(),
                        RecordedAtUtc = now,
                        ReversesEntryId = originalEntry.Id,
                        CorrelationId = correlationId,
                        Version = 1,
                    };

                    // Undo the original entry's effect on denormalized counters
                    if (originalEntry.EntryType == ProductionExecutionEntryType.GoodProduction)
                    {
                        order.GoodQuantity = Math.Max(0, order.GoodQuantity - originalEntry.Quantity);
                    }
                    else if (originalEntry.EntryType == ProductionExecutionEntryType.Scrap)
                    {
                        order.ScrapQuantity = Math.Max(0, order.ScrapQuantity - originalEntry.Quantity);
                    }

                    order.LastProductionEntryAtUtc = now;
                    order.LastModifiedBySubjectId = context.IdentitySubjectId.ToString();
                    order.Version += 1;
                    order.UpdatedAtUtc = now;

                    dbContext.ProductionExecutionEntries.Add(reversalEntry);

                    dbContext.AuditRecords.Add(new Audit.AuditRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        IdentitySubjectId = context.IdentitySubjectId,
                        TenantMembershipId = context.MembershipId,
                        EventType = ProductionAuditEventTypes.ProductionQuantityReversed,
                        Action = ProductionAuditActions.ProductionQuantityReverse,
                        ResourceType = "production.execution.entry",
                        ResourceId = reversalEntry.Id.ToString(),
                        Outcome = AuditOutcome.Success,
                        PermissionKey = context.PermissionKey,
                        CorrelationId = correlationId,
                        Source = AuditSource.Api,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        MetadataJson = JsonSerializer.Serialize(new
                        {
                            productionOrderId = order.Id.ToString(),
                            originalEntryId = originalEntry.Id.ToString(),
                            reversalEntryId = reversalEntry.Id.ToString(),
                            entryType = originalEntry.EntryType.ToString(),
                            quantity = originalEntry.Quantity,
                            orderVersion = order.Version,
                        }),
                    });

                    dbContext.OutboxMessages.Add(new Outbox.OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        MessageType = ProductionOutboxMessageTypes.ProductionQuantityReversed,
                        SchemaVersion = OutboxMessageTypes.SchemaVersion,
                        AggregateType = "production-execution-entry",
                        AggregateId = reversalEntry.Id.ToString(),
                        CorrelationId = correlationId,
                        CausationId = null,
                        IdempotencyKey = $"{reversalEntry.Id}:1:{ProductionOutboxMessageTypes.ProductionQuantityReversed}",
                        PayloadJson = JsonSerializer.Serialize(new
                        {
                            tenantId = context.TenantId.ToString(),
                            facilityId = context.FacilityId.ToString(),
                            productionOrderId = order.Id.ToString(),
                            originalEntryId = originalEntry.Id.ToString(),
                            reversalEntryId = reversalEntry.Id.ToString(),
                            occurredAtUtc = now.ToString("O"),
                        }),
                        HeadersJson = "{}",
                        Status = OutboxMessageStatus.Pending,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        AvailableAtUtc = now,
                        AttemptCount = 0,
                    });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionExecutionEntryWriteResult.Success(reversalEntry.Id, order.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionExecutionEntryWriteResult.Fail(
                ProductionOrderFailureCodes.ConcurrencyConflict,
                "The production order was modified by another user. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionExecutionEntryWriteResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production execution service is unavailable.");
        }
    }

    public async Task<ProductionExecutionEntryListResult> ListEntriesAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await tenantDatabaseSession.ExecuteReadAsync(
                ct => dbContext.ProductionExecutionEntries
                    .AsNoTracking()
                    .Where(e => e.TenantId == context.TenantId
                        && e.FacilityId == context.FacilityId
                        && e.ProductionOrderId == productionOrderId)
                    .OrderByDescending(e => e.RecordedAtUtc)
                    .Take(limit > 0 ? limit : 50)
                    .ToListAsync(ct),
                cancellationToken);

            // Build a set of reversed entry IDs for marking
            var reversedIds = entries
                .Where(e => e.EntryType == ProductionExecutionEntryType.Reversal
                    && e.ReversesEntryId.HasValue)
                .Select(e => e.ReversesEntryId!.Value)
                .ToHashSet();

            var result = entries.Select(e => new ProductionExecutionEntryReadModel(
                e.Id,
                ToEntryTypeString(e.EntryType),
                e.Quantity,
                e.Unit,
                e.ReasonCode,
                e.Comment,
                e.RecordedAtUtc,
                e.RecordedBySubjectId,
                reversedIds.Contains(e.Id),
                e.ReversesEntryId)).ToList();

            return ProductionExecutionEntryListResult.Success(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionExecutionEntryListResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production execution service is unavailable.");
        }
    }

    public async Task<ProductionQuantitySummary> GetSummaryAsync(
        Guid tenantId,
        Guid facilityId,
        Guid productionOrderId,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.ProductionOrders
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId
                && o.FacilityId == facilityId
                && o.Id == productionOrderId)
            .Select(o => new
            {
                o.PlannedQuantity,
                o.PlannedQuantityUnit,
                o.GoodQuantity,
                o.ScrapQuantity,
                o.LastProductionEntryAtUtc,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            return new ProductionQuantitySummary(null, null, 0, 0, 0, null, null, null);
        }

        var gross = order.GoodQuantity + order.ScrapQuantity;
        decimal? remaining = order.PlannedQuantity.HasValue
            ? Math.Max(order.PlannedQuantity.Value - order.GoodQuantity, 0)
            : null;
        decimal? percentage = order.PlannedQuantity.HasValue && order.PlannedQuantity.Value > 0
            ? order.GoodQuantity / order.PlannedQuantity.Value * 100
            : null;

        return new ProductionQuantitySummary(
            order.PlannedQuantity,
            order.PlannedQuantityUnit,
            order.GoodQuantity,
            order.ScrapQuantity,
            gross,
            remaining,
            percentage,
            order.LastProductionEntryAtUtc);
    }

    // ─────────────────────────── Shared write helper ───────────────────────────

    private async Task<ProductionExecutionEntryWriteResult> ExecuteEntryWriteAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        uint expectedVersion,
        Func<ProductionOrder, ProductionExecutionEntryWriteResult?> validate,
        Func<ProductionOrder, DateTimeOffset, Guid, ProductionExecutionEntry> buildEntry,
        string auditAction,
        string auditEventType,
        string outboxMessageType,
        string entryTypeName,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ProductionExecutionEntryWriteResult.Fail(
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
                            o => o.TenantId == context.TenantId
                                && o.FacilityId == context.FacilityId
                                && o.Id == productionOrderId,
                            ct);

                    if (order is null)
                    {
                        return ProductionExecutionEntryWriteResult.Fail(
                            ProductionOrderFailureCodes.NotFound,
                            "Production order not found in the requested scope.");
                    }

                    if (order.Version != expectedVersion)
                    {
                        return ProductionExecutionEntryWriteResult.Fail(
                            ProductionOrderFailureCodes.ConcurrencyConflict,
                            "The production order has been modified. Please reload and try again.");
                    }

                    var validationFailure = validate(order);
                    if (validationFailure is not null)
                    {
                        return validationFailure;
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();

                    var entry = buildEntry(order, now, correlationId);

                    order.LastModifiedBySubjectId = context.IdentitySubjectId.ToString();
                    order.Version += 1;
                    order.UpdatedAtUtc = now;

                    dbContext.ProductionExecutionEntries.Add(entry);

                    dbContext.AuditRecords.Add(new Audit.AuditRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        IdentitySubjectId = context.IdentitySubjectId,
                        TenantMembershipId = context.MembershipId,
                        EventType = auditEventType,
                        Action = auditAction,
                        ResourceType = "production.execution.entry",
                        ResourceId = entry.Id.ToString(),
                        Outcome = AuditOutcome.Success,
                        PermissionKey = context.PermissionKey,
                        CorrelationId = correlationId,
                        Source = AuditSource.Api,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        MetadataJson = JsonSerializer.Serialize(new
                        {
                            productionOrderId = order.Id.ToString(),
                            entryId = entry.Id.ToString(),
                            entryType = entryTypeName,
                            quantity = entry.Quantity,
                            unit = entry.Unit,
                            reasonCode = entry.ReasonCode,
                            orderVersion = order.Version,
                        }),
                    });

                    dbContext.OutboxMessages.Add(new Outbox.OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        MessageType = outboxMessageType,
                        SchemaVersion = OutboxMessageTypes.SchemaVersion,
                        AggregateType = "production-execution-entry",
                        AggregateId = entry.Id.ToString(),
                        CorrelationId = correlationId,
                        CausationId = null,
                        IdempotencyKey = $"{entry.Id}:1:{outboxMessageType}",
                        PayloadJson = JsonSerializer.Serialize(new
                        {
                            tenantId = context.TenantId.ToString(),
                            facilityId = context.FacilityId.ToString(),
                            productionOrderId = order.Id.ToString(),
                            entryId = entry.Id.ToString(),
                            entryType = entryTypeName,
                            quantity = entry.Quantity,
                            unit = entry.Unit,
                            reasonCode = entry.ReasonCode,
                            occurredAtUtc = now.ToString("O"),
                        }),
                        HeadersJson = "{}",
                        Status = OutboxMessageStatus.Pending,
                        OccurredAtUtc = now,
                        RecordedAtUtc = now,
                        AvailableAtUtc = now,
                        AttemptCount = 0,
                    });

                    await dbContext.SaveChangesAsync(ct);

                    return ProductionExecutionEntryWriteResult.Success(entry.Id, order.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionExecutionEntryWriteResult.Fail(
                ProductionOrderFailureCodes.ConcurrencyConflict,
                "The production order was modified by another user. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProductionExecutionEntryWriteResult.Fail(
                ProductionOrderFailureCodes.SourceUnavailable,
                "Production execution service is unavailable.");
        }
    }

    private static string ToEntryTypeString(ProductionExecutionEntryType type) =>
        type switch
        {
            ProductionExecutionEntryType.GoodProduction => "good-production",
            ProductionExecutionEntryType.Scrap => "scrap",
            ProductionExecutionEntryType.Reversal => "reversal",
            _ => type.ToString().ToLowerInvariant(),
        };
}
