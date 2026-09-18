using System.Text.Json;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Outbox;
using NevaxisZentra.Persistence.Production;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Maintenance;

internal static class MaintenanceRepositorySupport
{
    internal static async Task<string?> ValidateProductionOrderAsync(
        NevaxisZentraDbContext dbContext,
        MaintenanceOperationContext context,
        Guid? productionOrderId,
        CancellationToken ct)
    {
        if (!productionOrderId.HasValue)
        {
            return null;
        }

        var exists = await dbContext.ProductionOrders.AsNoTracking()
            .AnyAsync(
                o => o.TenantId == context.TenantId
                    && o.FacilityId == context.FacilityId
                    && o.Id == productionOrderId.Value,
                ct);

        return exists ? null : MaintenanceFailureCodes.ProductionOrderNotFound;
    }

    internal static async Task<(string? Reference, string? Status)> ReadProductionOrderContextAsync(
        NevaxisZentraDbContext dbContext,
        MaintenanceOperationContext context,
        Guid? productionOrderId,
        CancellationToken ct)
    {
        if (!productionOrderId.HasValue)
        {
            return (null, null);
        }

        var order = await dbContext.ProductionOrders.AsNoTracking()
            .SingleOrDefaultAsync(
                o => o.TenantId == context.TenantId
                    && o.FacilityId == context.FacilityId
                    && o.Id == productionOrderId.Value,
                ct);

        return order is null
            ? (null, null)
            : (order.ReferenceCode, ProductionOrderValueConversions.ToStorage(order.Status));
    }

    internal static void AddAudit(
        NevaxisZentraDbContext dbContext,
        MaintenanceOperationContext context,
        string action,
        string eventType,
        string resourceType,
        Guid resourceId,
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
            ResourceId = resourceId.ToString(),
            Outcome = AuditOutcome.Success,
            PermissionKey = context.PermissionKey,
            CorrelationId = context.CorrelationId ?? Guid.NewGuid(),
            Source = AuditSource.Api,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            MetadataJson = JsonSerializer.Serialize(metadata),
        });
    }

    internal static void AddOutbox(
        NevaxisZentraDbContext dbContext,
        MaintenanceOperationContext context,
        string aggregateType,
        Guid aggregateId,
        uint version,
        string messageType,
        DateTimeOffset now,
        object payload)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            MessageType = messageType,
            SchemaVersion = OutboxMessageTypes.SchemaVersion,
            AggregateType = aggregateType,
            AggregateId = aggregateId.ToString(),
            CorrelationId = context.CorrelationId ?? Guid.NewGuid(),
            CausationId = null,
            IdempotencyKey = $"{aggregateId}:{version}:{messageType}",
            PayloadJson = JsonSerializer.Serialize(payload),
            HeadersJson = "{}",
            Status = OutboxMessageStatus.Pending,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            AvailableAtUtc = now,
            AttemptCount = 0,
        });
    }

    internal static async Task<IReadOnlyList<MaintenanceActivityReadModel>> LoadActivityAsync(
        NevaxisZentraDbContext dbContext,
        MaintenanceOperationContext context,
        string resourceType,
        Guid resourceId,
        CancellationToken ct)
    {
        var records = await dbContext.AuditRecords.AsNoTracking()
            .Where(a => a.TenantId == context.TenantId
                && a.ResourceType == resourceType
                && a.ResourceId == resourceId.ToString())
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(25)
            .ToListAsync(ct);

        return records.Select(a => new MaintenanceActivityReadModel(
            a.Action,
            a.EventType,
            a.OccurredAtUtc,
            a.IdentitySubjectId.ToString(),
            a.Action)).ToList();
    }

    internal static MaintenanceWriteResult ConcurrencyFail(uint actual, uint expected) =>
        MaintenanceWriteResult.Fail(
            MaintenanceFailureCodes.ConcurrencyConflict,
            $"The record was modified (have {actual}, expected {expected}).");

    internal static async Task<MaintenanceWriteResult> ExecuteWriteAsync(
        TenantDatabaseSession tenantDatabaseSession,
        MaintenanceOperationContext context,
        Func<CancellationToken, Task<MaintenanceWriteResult>> action,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.InvalidScope, "Tenant and facility are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(action, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return MaintenanceWriteResult.Fail(
                MaintenanceFailureCodes.ConcurrencyConflict,
                "The record was modified by another user. Please reload and try again.");
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.SourceUnavailable, "Maintenance service is unavailable.");
        }
    }

    internal static async Task<string> NextRequestNumberAsync(
        NevaxisZentraDbContext dbContext,
        MaintenanceOperationContext context,
        CancellationToken ct)
    {
        var year = DateTimeOffset.UtcNow.Year;
        var count = await dbContext.MaintenanceRequests
            .CountAsync(
                r => r.TenantId == context.TenantId
                    && r.FacilityId == context.FacilityId
                    && r.RequestNumber.StartsWith($"MR-{year}-"),
                ct);
        return $"MR-{year}-{(count + 1):D6}";
    }

    internal static async Task<string> NextWorkOrderNumberAsync(
        NevaxisZentraDbContext dbContext,
        MaintenanceOperationContext context,
        CancellationToken ct)
    {
        var year = DateTimeOffset.UtcNow.Year;
        var count = await dbContext.MaintenanceWorkOrders
            .CountAsync(
                w => w.TenantId == context.TenantId
                    && w.FacilityId == context.FacilityId
                    && w.WorkOrderNumber.StartsWith($"MWO-{year}-"),
                ct);
        return $"MWO-{year}-{(count + 1):D6}";
    }
}
