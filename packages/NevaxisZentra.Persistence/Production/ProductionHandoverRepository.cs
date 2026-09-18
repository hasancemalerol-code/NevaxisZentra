using System.Text.Json;
using System.Text.Json.Serialization;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Outbox;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Production;

public sealed class UnavailableProductionHandoverRepository : IProductionHandoverRepository
{
    public Task<ProductionHandoverWriteResult> CreateHandoverAsync(ProductionOrderOperationContext context, CreateHandoverCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(ProductionHandoverWriteResult.Fail(ProductionOrderFailureCodes.SourceUnavailable, "Service unavailable."));

    public Task<ProductionOperationsListResult<HandoverReadModel>> ListHandoversAsync(ProductionOrderOperationContext context, Guid productionOrderId, int limit, CancellationToken cancellationToken = default) =>
        Task.FromResult(ProductionOperationsListResult.Fail<HandoverReadModel>(ProductionOrderFailureCodes.SourceUnavailable, "Unavailable."));
}

/// <summary>
/// Creates immutable shift handover notes.
/// Requires active shift assignment. Note is mandatory.
/// No UPDATE or DELETE — handovers are permanent provenance records.
/// </summary>
public sealed class ProductionHandoverRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IProductionHandoverRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private const int MaxLimit = 30;
    private const int MaxNoteLength = 2000;

    public async Task<ProductionHandoverWriteResult> CreateHandoverAsync(
        ProductionOrderOperationContext context,
        CreateHandoverCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Note))
            return ProductionHandoverWriteResult.Fail(ProductionOrderFailureCodes.HandoverNoteRequired, "Handover note is required.");

        if (command.Note.Trim().Length > MaxNoteLength)
            return ProductionHandoverWriteResult.Fail(ProductionOrderFailureCodes.MessageTooLong, $"Note must not exceed {MaxNoteLength} characters.");

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(async ct =>
            {
                var order = await dbContext.ProductionOrders
                    .SingleOrDefaultAsync(o => o.TenantId == context.TenantId && o.FacilityId == context.FacilityId && o.Id == command.ProductionOrderId, ct);

                if (order is null) return ProductionHandoverWriteResult.Fail(ProductionOrderFailureCodes.NotFound, "Production order not found.");

                if (order.Status is ProductionOrderStatus.Completed or ProductionOrderStatus.Cancelled)
                    return ProductionHandoverWriteResult.Fail(ProductionOrderFailureCodes.InvalidTransition, "Cannot create handover for completed or cancelled order.");

                if (order.Version != command.ExpectedVersion)
                    return ProductionHandoverWriteResult.Fail(ProductionOrderFailureCodes.ConcurrencyConflict, "Order was modified. Please reload.");

                // Active shift assignment required
                var activeAssignment = await dbContext.ProductionOrderShiftAssignments
                    .FirstOrDefaultAsync(sa => sa.TenantId == context.TenantId && sa.ProductionOrderId == order.Id && sa.ReleasedAtUtc == null, ct);

                if (activeAssignment is null)
                    return ProductionHandoverWriteResult.Fail(ProductionOrderFailureCodes.HandoverActiveShiftRequired, "An active shift assignment is required to create a handover.");

                var nowUtc = DateTimeOffset.UtcNow;
                var actorId = context.IdentitySubjectId.ToString();
                var correlationId = context.CorrelationId ?? Guid.NewGuid();

                var handover = new ProductionShiftHandover
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ProductionOrderId = order.Id,
                    FromShiftAssignmentId = activeAssignment.Id,
                    ToShiftAssignmentId = null,
                    Note = command.Note.Trim(),
                    OpenIssuesSummary = command.OpenIssuesSummary?.Trim(),
                    CreatedAtUtc = nowUtc,
                    CreatedBySubjectId = actorId,
                    CorrelationId = correlationId,
                };

                dbContext.ProductionShiftHandovers.Add(handover);

                dbContext.AuditRecords.Add(new AuditRecord
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    IdentitySubjectId = context.IdentitySubjectId,
                    TenantMembershipId = context.MembershipId,
                    Action = ProductionAuditActions.ProductionHandoverCreate,
                    EventType = ProductionAuditEventTypes.ProductionHandoverCreated,
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
                        handoverId = handover.Id.ToString(),
                        fromAssignmentId = activeAssignment.Id.ToString(),
                    }, JsonOptions),
                });

                dbContext.OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    MessageType = ProductionOutboxMessageTypes.ProductionHandoverCreated,
                    SchemaVersion = OutboxMessageTypes.SchemaVersion,
                    AggregateType = "production-order",
                    AggregateId = order.Id.ToString(),
                    CorrelationId = correlationId,
                    IdempotencyKey = $"{handover.Id}:1:{ProductionOutboxMessageTypes.ProductionHandoverCreated}",
                    PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string?>
                    {
                        ["tenantId"] = context.TenantId.ToString(),
                        ["facilityId"] = context.FacilityId.ToString(),
                        ["productionOrderId"] = order.Id.ToString(),
                        ["handoverId"] = handover.Id.ToString(),
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
                return ProductionHandoverWriteResult.Success(handover.Id);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProductionHandoverWriteResult.Fail(ProductionOrderFailureCodes.ConcurrencyConflict, "Concurrent modification detected.");
        }
    }

    public async Task<ProductionOperationsListResult<HandoverReadModel>> ListHandoversAsync(
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
                var items = await dbContext.ProductionShiftHandovers
                    .AsNoTracking()
                    .Where(h => h.TenantId == context.TenantId && h.FacilityId == context.FacilityId && h.ProductionOrderId == productionOrderId)
                    .OrderByDescending(h => h.CreatedAtUtc)
                    .Take(clampedLimit)
                    .Select(h => new HandoverReadModel(
                        h.Id, h.FromShiftAssignmentId, h.ToShiftAssignmentId,
                        h.Note, h.OpenIssuesSummary, h.CreatedAtUtc, h.CreatedBySubjectId))
                    .ToListAsync(ct);

                return ProductionOperationsListResult.Success<HandoverReadModel>(items);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            return ProductionOperationsListResult.Fail<HandoverReadModel>(ProductionOrderFailureCodes.SourceUnavailable, ex.Message);
        }
    }
}
