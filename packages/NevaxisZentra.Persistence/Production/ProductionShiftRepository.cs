using System.Text.Json;
using System.Text.Json.Serialization;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Outbox;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Production;

public sealed class UnavailableProductionShiftRepository : IProductionShiftRepository
{
    private static readonly ProductionAssignmentWriteResult Unavailable =
        ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.SourceUnavailable, "Service unavailable.");

    public Task<ProductionAssignmentWriteResult> AssignShiftAsync(ProductionOrderOperationContext context, AssignShiftCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable);
    public Task<ProductionAssignmentWriteResult> ReassignShiftAsync(ProductionOrderOperationContext context, ReassignShiftCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable);
    public Task<ProductionOperationsListResult<ShiftAssignmentReadModel>> ListShiftAssignmentsAsync(ProductionOrderOperationContext context, Guid productionOrderId, int limit, CancellationToken cancellationToken = default) => Task.FromResult(ProductionOperationsListResult.Fail<ShiftAssignmentReadModel>(ProductionOrderFailureCodes.SourceUnavailable, "Unavailable."));
    public Task<ShiftExecutionSummaryResult> GetShiftExecutionSummaryAsync(ProductionOrderOperationContext context, Guid productionShiftId, DateOnly shiftDate, CancellationToken cancellationToken = default) => Task.FromResult(ShiftExecutionSummaryResult.Fail(ProductionOrderFailureCodes.SourceUnavailable, "Unavailable."));
}

public sealed class ProductionShiftRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IProductionShiftRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private const int MaxLimit = 50;

    public async Task<ProductionAssignmentWriteResult> AssignShiftAsync(
        ProductionOrderOperationContext context,
        AssignShiftCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
            return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.InvalidScope, "Tenant and facility are required.");

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(async ct =>
            {
                var order = await dbContext.ProductionOrders
                    .SingleOrDefaultAsync(o => o.TenantId == context.TenantId && o.FacilityId == context.FacilityId && o.Id == command.ProductionOrderId, ct);

                if (order is null) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.NotFound, "Production order not found.");
                if (order.Status == ProductionOrderStatus.Completed) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.OrderAlreadyCompleted, "Cannot assign shift to a completed order.");
                if (order.Version != command.ExpectedVersion) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.ConcurrencyConflict, "Order was modified. Please reload.");

                var existingActive = await dbContext.ProductionOrderShiftAssignments
                    .AnyAsync(sa => sa.TenantId == context.TenantId && sa.ProductionOrderId == order.Id && sa.ReleasedAtUtc == null, ct);

                if (existingActive) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.ShiftAssignmentNotActive, "An active shift assignment already exists. Use reassign to change it.");

                return await CreateShiftAssignmentCoreAsync(context, order, command.ProductionShiftId, command.ShiftDate,
                    ProductionAuditActions.ProductionShiftAssign, ProductionAuditEventTypes.ProductionShiftAssigned,
                    ProductionOutboxMessageTypes.ProductionShiftAssigned, ct);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.ConcurrencyConflict, "Concurrent modification detected.");
        }
    }

    public async Task<ProductionAssignmentWriteResult> ReassignShiftAsync(
        ProductionOrderOperationContext context,
        ReassignShiftCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Reason))
            return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.CommentRequired, "Reassignment reason is required.");

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(async ct =>
            {
                var order = await dbContext.ProductionOrders
                    .SingleOrDefaultAsync(o => o.TenantId == context.TenantId && o.FacilityId == context.FacilityId && o.Id == command.ProductionOrderId, ct);

                if (order is null) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.NotFound, "Production order not found.");
                if (order.Status == ProductionOrderStatus.Completed) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.OrderAlreadyCompleted, "Cannot reassign shift on completed order.");
                if (order.Version != command.ExpectedVersion) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.ConcurrencyConflict, "Order was modified. Please reload.");

                var nowUtc = DateTimeOffset.UtcNow;
                var actorId = context.IdentitySubjectId.ToString();

                // Release existing active assignment
                var existing = await dbContext.ProductionOrderShiftAssignments
                    .FirstOrDefaultAsync(sa => sa.TenantId == context.TenantId && sa.ProductionOrderId == order.Id && sa.ReleasedAtUtc == null, ct);

                if (existing is not null)
                {
                    existing.ReleasedAtUtc = nowUtc;
                    existing.ReleasedBySubjectId = actorId;
                    existing.ReleaseReason = command.Reason.Trim();
                    existing.Version++;
                }

                return await CreateShiftAssignmentCoreAsync(context, order, command.ProductionShiftId, command.ShiftDate,
                    ProductionAuditActions.ProductionShiftReassign, ProductionAuditEventTypes.ProductionShiftReassigned,
                    ProductionOutboxMessageTypes.ProductionShiftReassigned, ct);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.ConcurrencyConflict, "Concurrent modification detected.");
        }
    }

    private async Task<ProductionAssignmentWriteResult> CreateShiftAssignmentCoreAsync(
        ProductionOrderOperationContext context,
        ProductionOrder order,
        Guid productionShiftId,
        DateOnly shiftDate,
        string auditAction,
        string auditEventType,
        string outboxType,
        CancellationToken ct)
    {
        var shift = await dbContext.ProductionShifts
            .SingleOrDefaultAsync(s => s.TenantId == context.TenantId && s.FacilityId == context.FacilityId && s.Id == productionShiftId, ct);

        if (shift is null) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.ShiftNotFound, "Shift not found in this facility.");
        if (!shift.IsActive) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.ShiftNotActive, "Shift is not active.");

        var facility = await dbContext.Facilities.SingleOrDefaultAsync(f => f.TenantId == context.TenantId && f.Id == context.FacilityId, ct);
        if (facility is null) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.InvalidScope, "Facility not found.");

        DateTimeOffset shiftStartUtc;
        DateTimeOffset shiftEndUtc;

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(facility.TimeZoneId);
            var localDate = shiftDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            shiftStartUtc = TimeZoneInfo.ConvertTimeToUtc(localDate + shift.StartLocalTime, tz);
            bool isOvernight = shift.EndLocalTime <= shift.StartLocalTime;
            var endBase = isOvernight ? localDate.AddDays(1) : localDate;
            shiftEndUtc = TimeZoneInfo.ConvertTimeToUtc(endBase + shift.EndLocalTime, tz);
        }
        catch
        {
            var localDate = shiftDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            shiftStartUtc = new DateTimeOffset(localDate + shift.StartLocalTime, TimeSpan.Zero);
            bool isOvernight = shift.EndLocalTime <= shift.StartLocalTime;
            var endBase = isOvernight ? localDate.AddDays(1) : localDate;
            shiftEndUtc = new DateTimeOffset(endBase + shift.EndLocalTime, TimeSpan.Zero);
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var actorId = context.IdentitySubjectId.ToString();
        var correlationId = context.CorrelationId ?? Guid.NewGuid();

        var assignment = new ProductionOrderShiftAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ProductionOrderId = order.Id,
            ProductionShiftId = productionShiftId,
            ShiftCode = shift.Code,
            ShiftDate = shiftDate,
            ShiftStartAtUtc = shiftStartUtc,
            ShiftEndAtUtc = shiftEndUtc,
            AssignedAtUtc = nowUtc,
            AssignedBySubjectId = actorId,
            CorrelationId = correlationId,
            Version = 1,
        };

        dbContext.ProductionOrderShiftAssignments.Add(assignment);

        order.CurrentShiftId = productionShiftId;
        order.CurrentShiftCode = shift.Code;
        order.CurrentShiftStartAtUtc = shiftStartUtc;
        order.CurrentShiftEndAtUtc = shiftEndUtc;
        order.LastAssignmentAtUtc = nowUtc;
        order.LastModifiedBySubjectId = actorId;
        order.Version++;
        order.UpdatedAtUtc = nowUtc;

        dbContext.AuditRecords.Add(new AuditRecord
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            IdentitySubjectId = context.IdentitySubjectId,
            TenantMembershipId = context.MembershipId,
            Action = auditAction,
            EventType = auditEventType,
            ResourceType = "production.order",
            ResourceId = order.Id.ToString(),
            Outcome = AuditOutcome.Success,
            PermissionKey = context.PermissionKey,
            CorrelationId = correlationId,
            Source = AuditSource.Api,
            OccurredAtUtc = nowUtc,
            RecordedAtUtc = nowUtc,
            MetadataJson = JsonSerializer.Serialize(new
            {
                productionOrderId = order.Id.ToString(),
                assignmentId = assignment.Id.ToString(),
                shiftCode = shift.Code,
                shiftDate = shiftDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            }, JsonOptions),
        });

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            MessageType = outboxType,
            SchemaVersion = OutboxMessageTypes.SchemaVersion,
            AggregateType = "production-order",
            AggregateId = order.Id.ToString(),
            CorrelationId = correlationId,
            IdempotencyKey = $"{assignment.Id}:1:{outboxType}",
            PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
            {
                ["tenantId"] = context.TenantId.ToString(),
                ["facilityId"] = context.FacilityId.ToString(),
                ["productionOrderId"] = order.Id.ToString(),
                ["assignmentId"] = assignment.Id.ToString(),
                ["shiftCode"] = shift.Code,
                ["shiftDate"] = shiftDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                ["occurredAtUtc"] = nowUtc.ToString("O"),
            }),
            HeadersJson = "{}",
            Status = OutboxMessageStatus.Pending,
            OccurredAtUtc = nowUtc,
            RecordedAtUtc = nowUtc,
            AvailableAtUtc = nowUtc,
            AttemptCount = 0,
        });

        await dbContext.SaveChangesAsync(ct);
        return ProductionAssignmentWriteResult.Success(assignment.Id, order.Version);
    }

    public async Task<ProductionOperationsListResult<ShiftAssignmentReadModel>> ListShiftAssignmentsAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var clampedLimit = Math.Min(Math.Max(1, limit), MaxLimit);

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(async ct =>
            {
                var items = await dbContext.ProductionOrderShiftAssignments
                    .AsNoTracking()
                    .Where(sa => sa.TenantId == context.TenantId && sa.FacilityId == context.FacilityId && sa.ProductionOrderId == productionOrderId)
                    .OrderByDescending(sa => sa.AssignedAtUtc)
                    .Take(clampedLimit)
                    .Select(sa => new ShiftAssignmentReadModel(
                        sa.Id, sa.ProductionShiftId, sa.ShiftCode, sa.ShiftDate,
                        sa.ShiftStartAtUtc, sa.ShiftEndAtUtc,
                        sa.AssignedAtUtc, sa.AssignedBySubjectId,
                        sa.ReleasedAtUtc, sa.ReleasedBySubjectId, sa.ReleaseReason,
                        sa.ReleasedAtUtc == null))
                    .ToListAsync(ct);

                return ProductionOperationsListResult.Success<ShiftAssignmentReadModel>(items);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            return ProductionOperationsListResult.Fail<ShiftAssignmentReadModel>(ProductionOrderFailureCodes.SourceUnavailable, ex.Message);
        }
    }

    public async Task<ShiftExecutionSummaryResult> GetShiftExecutionSummaryAsync(
        ProductionOrderOperationContext context,
        Guid productionShiftId,
        DateOnly shiftDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(async ct =>
            {
                var shift = await dbContext.ProductionShifts.AsNoTracking()
                    .SingleOrDefaultAsync(s => s.TenantId == context.TenantId && s.FacilityId == context.FacilityId && s.Id == productionShiftId, ct);

                if (shift is null) return ShiftExecutionSummaryResult.Fail(ProductionOrderFailureCodes.ShiftNotFound, "Shift not found.");

                var assignmentOrderIds = await dbContext.ProductionOrderShiftAssignments.AsNoTracking()
                    .Where(sa => sa.TenantId == context.TenantId && sa.FacilityId == context.FacilityId
                        && sa.ProductionShiftId == productionShiftId && sa.ShiftDate == shiftDate)
                    .Select(sa => sa.ProductionOrderId)
                    .Distinct()
                    .ToListAsync(ct);

                // Determine shift UTC window
                var firstAssignment = await dbContext.ProductionOrderShiftAssignments.AsNoTracking()
                    .Where(sa => sa.TenantId == context.TenantId && sa.ProductionShiftId == productionShiftId && sa.ShiftDate == shiftDate)
                    .OrderBy(sa => sa.AssignedAtUtc)
                    .FirstOrDefaultAsync(ct);

                DateTimeOffset shiftStartUtc = firstAssignment?.ShiftStartAtUtc ?? DateTimeOffset.UtcNow;
                DateTimeOffset shiftEndUtc = firstAssignment?.ShiftEndAtUtc ?? DateTimeOffset.UtcNow;

                if (firstAssignment is null)
                {
                    var facility = await dbContext.Facilities.AsNoTracking()
                        .SingleOrDefaultAsync(f => f.TenantId == context.TenantId && f.Id == context.FacilityId, ct);
                    if (facility is not null)
                    {
                        try
                        {
                            var tz = TimeZoneInfo.FindSystemTimeZoneById(facility.TimeZoneId);
                            var localDate = shiftDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
                            shiftStartUtc = TimeZoneInfo.ConvertTimeToUtc(localDate + shift.StartLocalTime, tz);
                            bool overnight = shift.EndLocalTime <= shift.StartLocalTime;
                            var endBase = overnight ? localDate.AddDays(1) : localDate;
                            shiftEndUtc = TimeZoneInfo.ConvertTimeToUtc(endBase + shift.EndLocalTime, tz);
                        }
                        catch { /* use defaults */ }
                    }
                }

                if (assignmentOrderIds.Count == 0)
                    return ShiftExecutionSummaryResult.Success(new ShiftExecutionSummary(productionShiftId, shift.Code, shiftStartUtc, shiftEndUtc, 0, 0, 0, 0, 0, 0, 0, 0, 0));

                var orders = await dbContext.ProductionOrders.AsNoTracking()
                    .Where(o => assignmentOrderIds.Contains(o.Id)).ToListAsync(ct);

                var activeCount = orders.Count(o => o.Status != ProductionOrderStatus.Completed && o.Status != ProductionOrderStatus.Cancelled);
                var completedCount = orders.Count(o => o.Status == ProductionOrderStatus.Completed);
                var goodQty = orders.Sum(o => o.GoodQuantity);
                var scrapQty = orders.Sum(o => o.ScrapQuantity);
                var downtimeSeconds = orders.Sum(o => o.TotalDowntimeSeconds);
                var setupSeconds = orders.Sum(o => o.TotalSetupSeconds);
                var cycleCount = orders.Sum(o => o.ManualCycleCount);

                var openExceptions = await dbContext.ProductionExecutionExceptions.AsNoTracking()
                    .CountAsync(ex => assignmentOrderIds.Contains(ex.ProductionOrderId)
                        && (ex.Status == ProductionExceptionStatuses.Open || ex.Status == ProductionExceptionStatuses.Acknowledged), ct);

                var criticalExceptions = await dbContext.ProductionExecutionExceptions.AsNoTracking()
                    .CountAsync(ex => assignmentOrderIds.Contains(ex.ProductionOrderId)
                        && ex.Severity == ProductionExceptionSeverities.Critical
                        && (ex.Status == ProductionExceptionStatuses.Open || ex.Status == ProductionExceptionStatuses.Acknowledged), ct);

                return ShiftExecutionSummaryResult.Success(new ShiftExecutionSummary(
                    productionShiftId, shift.Code, shiftStartUtc, shiftEndUtc,
                    activeCount, completedCount, goodQty, scrapQty, downtimeSeconds, setupSeconds, cycleCount, openExceptions, criticalExceptions));
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            return ShiftExecutionSummaryResult.Fail(ProductionOrderFailureCodes.SourceUnavailable, ex.Message);
        }
    }
}
