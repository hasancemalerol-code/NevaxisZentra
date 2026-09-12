using System.Text.Json;
using System.Text.Json.Serialization;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Outbox;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Production;

public sealed class UnavailableProductionOperatorRepository : IProductionOperatorRepository
{
    private static readonly ProductionAssignmentWriteResult Unavailable =
        ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.SourceUnavailable, "Service unavailable.");

    public Task<ProductionAssignmentWriteResult> AssignOperatorAsync(ProductionOrderOperationContext context, AssignOperatorCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable);
    public Task<ProductionAssignmentWriteResult> ReleaseOperatorAsync(ProductionOrderOperationContext context, ReleaseOperatorCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Unavailable);
    public Task<ProductionOperationsListResult<OperatorAssignmentReadModel>> ListOperatorAssignmentsAsync(ProductionOrderOperationContext context, Guid productionOrderId, int limit, CancellationToken cancellationToken = default) => Task.FromResult(ProductionOperationsListResult.Fail<OperatorAssignmentReadModel>(ProductionOrderFailureCodes.SourceUnavailable, "Unavailable."));
    public Task<OperatorExecutionSummaryResult> GetOperatorExecutionSummaryAsync(ProductionOrderOperationContext context, string subjectId, CancellationToken cancellationToken = default) => Task.FromResult(OperatorExecutionSummaryResult.Fail(ProductionOrderFailureCodes.SourceUnavailable, "Unavailable."));
}

/// <summary>
/// Handles operator assignment, primary operator uniqueness, and explicit release.
/// Primary operator replacement is transactional: release old + create new + version bump.
/// </summary>
public sealed class ProductionOperatorRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IProductionOperatorRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private const int MaxLimit = 50;

    public async Task<ProductionAssignmentWriteResult> AssignOperatorAsync(
        ProductionOrderOperationContext context,
        AssignOperatorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.SubjectId))
            return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.AssignmentTargetEmpty, "SubjectId is required.");

        if (!ProductionOperatorRoles.All.Contains(command.AssignmentRole))
            return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.InvalidOperatorRole, $"Unknown assignment role: '{command.AssignmentRole}'.");

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(async ct =>
            {
                var order = await dbContext.ProductionOrders
                    .SingleOrDefaultAsync(o => o.TenantId == context.TenantId && o.FacilityId == context.FacilityId && o.Id == command.ProductionOrderId, ct);

                if (order is null) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.NotFound, "Production order not found.");
                if (order.Status == ProductionOrderStatus.Completed) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.OrderAlreadyCompleted, "Cannot assign operator to a completed order.");
                if (order.Version != command.ExpectedVersion) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.ConcurrencyConflict, "Order was modified. Please reload.");

                // Validate facility membership
                var isMember = await IsFacilityMemberAsync(context.TenantId, context.FacilityId, command.SubjectId.Trim(), ct);
                if (!isMember)
                    return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.OperatorNotFacilityMember, $"Subject '{command.SubjectId}' is not an active member of this facility.");

                var nowUtc = DateTimeOffset.UtcNow;
                var actorId = context.IdentitySubjectId.ToString();
                var correlationId = context.CorrelationId ?? Guid.NewGuid();

                // If assigning primary-operator, atomically release the existing one.
                // existingPrimary is hoisted so the recount below can exclude it (its ReleasedAtUtc is set
                // in-memory but not yet flushed, so a DB CountAsync would still see it as active).
                ProductionOperatorAssignment? existingPrimary = null;
                if (command.AssignmentRole == ProductionOperatorRoles.PrimaryOperator)
                {
                    existingPrimary = await dbContext.ProductionOperatorAssignments
                        .FirstOrDefaultAsync(oa => oa.TenantId == context.TenantId
                            && oa.ProductionOrderId == order.Id
                            && oa.AssignmentRole == ProductionOperatorRoles.PrimaryOperator
                            && oa.ReleasedAtUtc == null, ct);

                    if (existingPrimary is not null)
                    {
                        existingPrimary.ReleasedAtUtc = nowUtc;
                        existingPrimary.ReleasedBySubjectId = actorId;
                        existingPrimary.ReleaseReason = "Replaced by new primary operator assignment.";
                        existingPrimary.Version++;
                    }
                }

                var assignment = new ProductionOperatorAssignment
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ProductionOrderId = order.Id,
                    SubjectId = command.SubjectId.Trim(),
                    AssignmentRole = command.AssignmentRole,
                    AssignedAtUtc = nowUtc,
                    AssignedBySubjectId = actorId,
                    CorrelationId = correlationId,
                    Version = 1,
                };

                dbContext.ProductionOperatorAssignments.Add(assignment);

                // Recount active assignments (excluding the primary being replaced in this TX, which is released in-memory but not yet flushed)
                var activeCount = await dbContext.ProductionOperatorAssignments
                    .CountAsync(oa => oa.TenantId == context.TenantId && oa.ProductionOrderId == order.Id && oa.ReleasedAtUtc == null
                        && (existingPrimary == null || oa.Id != existingPrimary.Id), ct);

                order.ActiveOperatorCount = activeCount + 1;
                if (command.AssignmentRole == ProductionOperatorRoles.PrimaryOperator)
                    order.PrimaryOperatorSubjectId = command.SubjectId.Trim();
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
                    Action = ProductionAuditActions.ProductionOperatorAssign,
                    EventType = ProductionAuditEventTypes.ProductionOperatorAssigned,
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
                        subjectId = command.SubjectId,
                        assignmentRole = command.AssignmentRole,
                    }, JsonOptions),
                });

                dbContext.OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    MessageType = ProductionOutboxMessageTypes.ProductionOperatorAssigned,
                    SchemaVersion = OutboxMessageTypes.SchemaVersion,
                    AggregateType = "production-order",
                    AggregateId = order.Id.ToString(),
                    CorrelationId = correlationId,
                    IdempotencyKey = $"{assignment.Id}:1:{ProductionOutboxMessageTypes.ProductionOperatorAssigned}",
                    PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                    {
                        ["tenantId"] = context.TenantId.ToString(),
                        ["facilityId"] = context.FacilityId.ToString(),
                        ["productionOrderId"] = order.Id.ToString(),
                        ["assignmentId"] = assignment.Id.ToString(),
                        ["subjectId"] = command.SubjectId,
                        ["assignmentRole"] = command.AssignmentRole,
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
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.ConcurrencyConflict, "Concurrent modification detected.");
        }
    }

    public async Task<ProductionAssignmentWriteResult> ReleaseOperatorAsync(
        ProductionOrderOperationContext context,
        ReleaseOperatorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Reason))
            return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.CommentRequired, "Release reason is required.");

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(async ct =>
            {
                var order = await dbContext.ProductionOrders
                    .SingleOrDefaultAsync(o => o.TenantId == context.TenantId && o.FacilityId == context.FacilityId && o.Id == command.ProductionOrderId, ct);

                if (order is null) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.NotFound, "Production order not found.");
                if (order.Version != command.ExpectedVersion) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.ConcurrencyConflict, "Order was modified. Please reload.");

                var assignment = await dbContext.ProductionOperatorAssignments
                    .SingleOrDefaultAsync(oa => oa.TenantId == context.TenantId && oa.ProductionOrderId == order.Id && oa.Id == command.AssignmentId, ct);

                if (assignment is null) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.OperatorAssignmentNotFound, "Operator assignment not found.");
                if (assignment.ReleasedAtUtc is not null) return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.OperatorAssignmentNotActive, "Assignment is already released.");

                var nowUtc = DateTimeOffset.UtcNow;
                var actorId = context.IdentitySubjectId.ToString();
                var correlationId = context.CorrelationId ?? Guid.NewGuid();

                assignment.ReleasedAtUtc = nowUtc;
                assignment.ReleasedBySubjectId = actorId;
                assignment.ReleaseReason = command.Reason.Trim();
                assignment.Version++;

                var activeCount = await dbContext.ProductionOperatorAssignments
                    .CountAsync(oa => oa.TenantId == context.TenantId && oa.ProductionOrderId == order.Id && oa.ReleasedAtUtc == null && oa.Id != command.AssignmentId, ct);

                order.ActiveOperatorCount = activeCount;
                if (assignment.AssignmentRole == ProductionOperatorRoles.PrimaryOperator)
                    order.PrimaryOperatorSubjectId = null;
                order.LastModifiedBySubjectId = actorId;
                order.Version++;
                order.UpdatedAtUtc = nowUtc;

                dbContext.AuditRecords.Add(new AuditRecord
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    IdentitySubjectId = context.IdentitySubjectId,
                    TenantMembershipId = context.MembershipId,
                    Action = ProductionAuditActions.ProductionOperatorRelease,
                    EventType = ProductionAuditEventTypes.ProductionOperatorReleased,
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
                        subjectId = assignment.SubjectId,
                    }, JsonOptions),
                });

                dbContext.OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    MessageType = ProductionOutboxMessageTypes.ProductionOperatorReleased,
                    SchemaVersion = OutboxMessageTypes.SchemaVersion,
                    AggregateType = "production-order",
                    AggregateId = order.Id.ToString(),
                    CorrelationId = correlationId,
                    IdempotencyKey = $"{assignment.Id}:{assignment.Version}:{ProductionOutboxMessageTypes.ProductionOperatorReleased}",
                    PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                    {
                        ["tenantId"] = context.TenantId.ToString(),
                        ["facilityId"] = context.FacilityId.ToString(),
                        ["productionOrderId"] = order.Id.ToString(),
                        ["assignmentId"] = assignment.Id.ToString(),
                        ["subjectId"] = assignment.SubjectId,
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
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionAssignmentWriteResult.Fail(ProductionOrderFailureCodes.ConcurrencyConflict, "Concurrent modification detected.");
        }
    }

    public async Task<ProductionOperationsListResult<OperatorAssignmentReadModel>> ListOperatorAssignmentsAsync(
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
                var items = await dbContext.ProductionOperatorAssignments
                    .AsNoTracking()
                    .Where(oa => oa.TenantId == context.TenantId && oa.FacilityId == context.FacilityId && oa.ProductionOrderId == productionOrderId)
                    .OrderByDescending(oa => oa.AssignedAtUtc)
                    .Take(clampedLimit)
                    .Select(oa => new OperatorAssignmentReadModel(
                        oa.Id, oa.SubjectId, oa.AssignmentRole,
                        oa.AssignedAtUtc, oa.AssignedBySubjectId,
                        oa.ReleasedAtUtc, oa.ReleasedBySubjectId, oa.ReleaseReason,
                        oa.ReleasedAtUtc == null))
                    .ToListAsync(ct);

                return ProductionOperationsListResult.Success<OperatorAssignmentReadModel>(items);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            return ProductionOperationsListResult.Fail<OperatorAssignmentReadModel>(ProductionOrderFailureCodes.SourceUnavailable, ex.Message);
        }
    }

    public async Task<OperatorExecutionSummaryResult> GetOperatorExecutionSummaryAsync(
        ProductionOrderOperationContext context,
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(subjectId))
            return OperatorExecutionSummaryResult.Fail(ProductionOrderFailureCodes.AssignmentTargetEmpty, "SubjectId is required.");

        try
        {
            return await tenantDatabaseSession.ExecuteReadAsync(async ct =>
            {
                var activeOrderIds = await dbContext.ProductionOperatorAssignments.AsNoTracking()
                    .Where(oa => oa.TenantId == context.TenantId && oa.FacilityId == context.FacilityId
                        && oa.SubjectId == subjectId && oa.ReleasedAtUtc == null)
                    .Select(oa => oa.ProductionOrderId).Distinct().ToListAsync(ct);

                var allOrderIds = await dbContext.ProductionOperatorAssignments.AsNoTracking()
                    .Where(oa => oa.TenantId == context.TenantId && oa.FacilityId == context.FacilityId && oa.SubjectId == subjectId)
                    .Select(oa => oa.ProductionOrderId).Distinct().ToListAsync(ct);

                var orders = await dbContext.ProductionOrders.AsNoTracking()
                    .Where(o => allOrderIds.Contains(o.Id)).ToListAsync(ct);

                var activeCount = orders.Count(o => activeOrderIds.Contains(o.Id)
                    && o.Status != ProductionOrderStatus.Completed && o.Status != ProductionOrderStatus.Cancelled);
                var completedCount = orders.Count(o => o.Status == ProductionOrderStatus.Completed);
                var goodQty = orders.Sum(o => o.GoodQuantity);
                var scrapQty = orders.Sum(o => o.ScrapQuantity);

                var assignedExceptions = await dbContext.ProductionExecutionExceptions.AsNoTracking()
                    .CountAsync(ex => ex.TenantId == context.TenantId && ex.FacilityId == context.FacilityId
                        && ex.AssignedToSubjectId == subjectId
                        && (ex.Status == ProductionExceptionStatuses.Open || ex.Status == ProductionExceptionStatuses.Acknowledged), ct);

                var lastActivity = orders.Count > 0 ? orders.Max(o => (DateTimeOffset?)o.UpdatedAtUtc) : null;

                return OperatorExecutionSummaryResult.Success(new OperatorExecutionSummary(
                    subjectId, activeCount, completedCount, goodQty, scrapQty, assignedExceptions, lastActivity));
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            return OperatorExecutionSummaryResult.Fail(ProductionOrderFailureCodes.SourceUnavailable, ex.Message);
        }
    }

    private async Task<bool> IsFacilityMemberAsync(Guid tenantId, Guid facilityId, string subjectId, CancellationToken ct)
    {
        // Validate via FacilityMembership → TenantMembership → IdentitySubject chain using ExternalSubject.
        return await dbContext.FacilityMemberships
            .AsNoTracking()
            .Join(dbContext.TenantMemberships.AsNoTracking(),
                fm => fm.TenantMembershipId,
                tm => tm.Id,
                (fm, tm) => new { fm, tm })
            .Join(dbContext.IdentitySubjects.AsNoTracking(),
                x => x.tm.IdentitySubjectId,
                is2 => is2.Id,
                (x, is2) => new { x.fm, x.tm, is2 })
            .AnyAsync(x =>
                x.fm.TenantId == tenantId
                && x.fm.FacilityId == facilityId
                && x.fm.Status == FacilityMembershipStatus.Active
                && x.is2.ExternalSubject == subjectId,
            ct);
    }
}
