using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Maintenance;

public interface IMaintenanceRequestRepository
{
    Task<IReadOnlyList<MaintenanceRequestSummaryReadModel>> ListAsync(
        MaintenanceOperationContext context,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<MaintenanceRequestDetailReadModel?> GetByIdAsync(
        MaintenanceOperationContext context,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> CreateAsync(
        MaintenanceOperationContext context,
        CreateMaintenanceRequestCommand command,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> UpdateAsync(
        MaintenanceOperationContext context,
        UpdateMaintenanceRequestCommand command,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> ConvertToWorkOrderAsync(
        MaintenanceOperationContext context,
        ConvertMaintenanceRequestCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class MaintenanceRequestRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IMaintenanceRequestRepository
{
    public Task<IReadOnlyList<MaintenanceRequestSummaryReadModel>> ListAsync(
        MaintenanceOperationContext context,
        string? status = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.MaintenanceRequests.AsNoTracking()
                .Where(r => r.TenantId == context.TenantId && r.FacilityId == context.FacilityId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var parsed = MaintenanceValueConversions.RequestStatusFromStorage(status);
                query = query.Where(r => r.Status == parsed);
            }

            var items = await query.OrderByDescending(r => r.UpdatedAtUtc).ToListAsync(ct);
            return (IReadOnlyList<MaintenanceRequestSummaryReadModel>)items.Select(MapSummary).ToList();
        }, cancellationToken);

    public Task<MaintenanceRequestDetailReadModel?> GetByIdAsync(
        MaintenanceOperationContext context,
        Guid requestId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var request = await dbContext.MaintenanceRequests.AsNoTracking()
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && r.Id == requestId,
                    ct);

            if (request is null)
            {
                return null;
            }

            var (orderRef, orderStatus) = await MaintenanceRepositorySupport.ReadProductionOrderContextAsync(
                dbContext, context, request.ProductionOrderId, ct);
            var activity = await MaintenanceRepositorySupport.LoadActivityAsync(
                dbContext, context, "maintenance.request", request.Id, ct);

            return MapDetail(request, orderRef, orderStatus, activity);
        }, cancellationToken);

    public Task<MaintenanceWriteResult> CreateAsync(
        MaintenanceOperationContext context,
        CreateMaintenanceRequestCommand command,
        CancellationToken cancellationToken = default) =>
        MaintenanceRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(command.Title) || string.IsNullOrWhiteSpace(command.AssetReference))
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.ValidationFailed, "Title and asset reference are required.");
                }

                var orderError = await MaintenanceRepositorySupport.ValidateProductionOrderAsync(
                    dbContext, context, command.ProductionOrderId, ct);
                if (orderError is not null)
                {
                    return MaintenanceWriteResult.Fail(orderError, "Production order not found in facility scope.");
                }

                var now = DateTimeOffset.UtcNow;
                var request = new MaintenanceRequest
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    RequestNumber = await MaintenanceRepositorySupport.NextRequestNumberAsync(dbContext, context, ct),
                    AssetType = command.AssetType,
                    AssetId = command.AssetId,
                    AssetReference = command.AssetReference,
                    AssetName = command.AssetName,
                    ProductionOrderId = command.ProductionOrderId,
                    Type = MaintenanceValueConversions.RequestTypeFromStorage(command.Type),
                    Priority = MaintenanceValueConversions.PriorityFromStorage(command.Priority),
                    Status = MaintenanceRequestStatus.Open,
                    Title = command.Title.Trim(),
                    Description = command.Description?.Trim() ?? string.Empty,
                    ReportedAtUtc = now,
                    ReportedBySubjectId = context.IdentitySubjectId.ToString(),
                    Source = string.IsNullOrWhiteSpace(command.Source) ? "manual" : command.Source.Trim(),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Version = 1,
                };

                dbContext.MaintenanceRequests.Add(request);
                MaintenanceRepositorySupport.AddAudit(
                    dbContext, context,
                    MaintenanceAuditActions.RequestCreated,
                    MaintenanceAuditEventTypes.RequestCreated,
                    "maintenance.request",
                    request.Id,
                    now,
                    new { requestId = request.Id, request.RequestNumber });
                MaintenanceRepositorySupport.AddOutbox(
                    dbContext, context,
                    "maintenance-request",
                    request.Id,
                    request.Version,
                    MaintenanceOutboxMessageTypes.RequestCreated,
                    now,
                    new { requestId = request.Id.ToString(), requestNumber = request.RequestNumber });

                await dbContext.SaveChangesAsync(ct);
                return MaintenanceWriteResult.Success(request.Id, request.Version);
            },
            cancellationToken);

    public Task<MaintenanceWriteResult> UpdateAsync(
        MaintenanceOperationContext context,
        UpdateMaintenanceRequestCommand command,
        CancellationToken cancellationToken = default) =>
        MaintenanceRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                var request = await dbContext.MaintenanceRequests
                    .SingleOrDefaultAsync(
                        r => r.TenantId == context.TenantId
                            && r.FacilityId == context.FacilityId
                            && r.Id == command.RequestId,
                        ct);

                if (request is null)
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.NotFound, "Maintenance request not found.");
                }

                if (request.Version != command.ExpectedVersion)
                {
                    return MaintenanceRepositorySupport.ConcurrencyFail(request.Version, command.ExpectedVersion);
                }

                if (!MaintenanceLifecycleEngine.CanUpdateRequest(request.Status))
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.InvalidTransition, "Request cannot be updated in current status.");
                }

                if (!string.IsNullOrWhiteSpace(command.Title))
                {
                    request.Title = command.Title.Trim();
                }

                if (command.Description is not null)
                {
                    request.Description = command.Description.Trim();
                }

                if (!string.IsNullOrWhiteSpace(command.Priority))
                {
                    request.Priority = MaintenanceValueConversions.PriorityFromStorage(command.Priority);
                }

                if (!string.IsNullOrWhiteSpace(command.Status))
                {
                    var next = MaintenanceValueConversions.RequestStatusFromStorage(command.Status);
                    if (next == MaintenanceRequestStatus.ConvertedToWorkOrder)
                    {
                        return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.InvalidTransition, "Use convert-to-work-order endpoint.");
                    }

                    request.Status = next;
                }

                var now = DateTimeOffset.UtcNow;
                request.UpdatedAtUtc = now;
                request.Version++;

                MaintenanceRepositorySupport.AddAudit(
                    dbContext, context,
                    MaintenanceAuditActions.RequestUpdated,
                    MaintenanceAuditEventTypes.RequestUpdated,
                    "maintenance.request",
                    request.Id,
                    now,
                    new { requestId = request.Id, status = MaintenanceValueConversions.ToStorage(request.Status) });

                await dbContext.SaveChangesAsync(ct);
                return MaintenanceWriteResult.Success(request.Id, request.Version);
            },
            cancellationToken);

    public Task<MaintenanceWriteResult> ConvertToWorkOrderAsync(
        MaintenanceOperationContext context,
        ConvertMaintenanceRequestCommand command,
        CancellationToken cancellationToken = default) =>
        MaintenanceRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                var request = await dbContext.MaintenanceRequests
                    .SingleOrDefaultAsync(
                        r => r.TenantId == context.TenantId
                            && r.FacilityId == context.FacilityId
                            && r.Id == command.RequestId,
                        ct);

                if (request is null)
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.NotFound, "Maintenance request not found.");
                }

                if (request.Version != command.ExpectedVersion)
                {
                    return MaintenanceRepositorySupport.ConcurrencyFail(request.Version, command.ExpectedVersion);
                }

                if (!MaintenanceLifecycleEngine.CanConvertRequest(request.Status))
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.InvalidTransition, "Request cannot be converted in current status.");
                }

                var now = DateTimeOffset.UtcNow;
                var workOrder = new MaintenanceWorkOrder
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    WorkOrderNumber = await MaintenanceRepositorySupport.NextWorkOrderNumberAsync(dbContext, context, ct),
                    MaintenanceRequestId = request.Id,
                    AssetType = request.AssetType,
                    AssetId = request.AssetId,
                    AssetReference = request.AssetReference,
                    AssetName = request.AssetName,
                    ProductionOrderId = request.ProductionOrderId,
                    Type = request.Type switch
                    {
                        MaintenanceRequestType.Fault => MaintenanceWorkOrderType.Fault,
                        MaintenanceRequestType.Corrective => MaintenanceWorkOrderType.Corrective,
                        MaintenanceRequestType.Preventive => MaintenanceWorkOrderType.Preventive,
                        MaintenanceRequestType.Inspection => MaintenanceWorkOrderType.Inspection,
                        _ => MaintenanceWorkOrderType.Other,
                    },
                    Priority = request.Priority,
                    Status = string.IsNullOrWhiteSpace(command.AssignedTechnicianSubjectId)
                        ? MaintenanceWorkOrderStatus.Planned
                        : MaintenanceWorkOrderStatus.Assigned,
                    Title = request.Title,
                    Description = request.Description,
                    AssignedTechnicianSubjectId = command.AssignedTechnicianSubjectId,
                    PlannedStartUtc = command.PlannedStartUtc,
                    PlannedEndUtc = command.PlannedEndUtc,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBySubjectId = context.IdentitySubjectId.ToString(),
                    Version = 1,
                };

                dbContext.MaintenanceWorkOrders.Add(workOrder);
                request.Status = MaintenanceRequestStatus.ConvertedToWorkOrder;
                request.UpdatedAtUtc = now;
                request.Version++;

                MaintenanceRepositorySupport.AddAudit(
                    dbContext, context,
                    MaintenanceAuditActions.WorkOrderCreated,
                    MaintenanceAuditEventTypes.WorkOrderCreated,
                    "maintenance.workorder",
                    workOrder.Id,
                    now,
                    new { workOrderId = workOrder.Id, workOrderNumber = workOrder.WorkOrderNumber, requestId = request.Id });
                MaintenanceRepositorySupport.AddOutbox(
                    dbContext, context,
                    "maintenance-work-order",
                    workOrder.Id,
                    workOrder.Version,
                    MaintenanceOutboxMessageTypes.WorkOrderCreated,
                    now,
                    new { workOrderId = workOrder.Id.ToString(), workOrderNumber = workOrder.WorkOrderNumber });

                await dbContext.SaveChangesAsync(ct);
                return MaintenanceWriteResult.Success(workOrder.Id, workOrder.Version);
            },
            cancellationToken);

    private static MaintenanceRequestSummaryReadModel MapSummary(MaintenanceRequest request) =>
        new(
            request.Id,
            request.RequestNumber,
            request.AssetType,
            request.AssetId,
            request.AssetReference,
            request.AssetName,
            request.Title,
            MaintenanceValueConversions.ToStorage(request.Type),
            MaintenanceValueConversions.ToStorage(request.Priority),
            MaintenanceValueConversions.ToStorage(request.Status),
            request.ReportedBySubjectId,
            request.ReportedAtUtc,
            request.Version,
            request.UpdatedAtUtc);

    private static MaintenanceRequestDetailReadModel MapDetail(
        MaintenanceRequest request,
        string? orderRef,
        string? orderStatus,
        IReadOnlyList<MaintenanceActivityReadModel> activity) =>
        new(
            request.Id,
            request.RequestNumber,
            request.AssetType,
            request.AssetId,
            request.AssetReference,
            request.AssetName,
            request.ProductionOrderId,
            orderRef,
            orderStatus,
            MaintenanceValueConversions.ToStorage(request.Type),
            MaintenanceValueConversions.ToStorage(request.Priority),
            MaintenanceValueConversions.ToStorage(request.Status),
            request.Title,
            request.Description,
            request.Source,
            request.ReportedBySubjectId,
            request.ReportedAtUtc,
            request.Version,
            request.CreatedAtUtc,
            request.UpdatedAtUtc,
            activity);
}

public sealed class UnavailableMaintenanceRequestRepository : IMaintenanceRequestRepository
{
    private static MaintenanceWriteResult Unavailable() =>
        MaintenanceWriteResult.Fail(MaintenanceFailureCodes.SourceUnavailable, "Maintenance request service is unavailable.");

    public Task<IReadOnlyList<MaintenanceRequestSummaryReadModel>> ListAsync(
        MaintenanceOperationContext context,
        string? status = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MaintenanceRequestSummaryReadModel>>([]);

    public Task<MaintenanceRequestDetailReadModel?> GetByIdAsync(
        MaintenanceOperationContext context,
        Guid requestId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<MaintenanceRequestDetailReadModel?>(null);

    public Task<MaintenanceWriteResult> CreateAsync(
        MaintenanceOperationContext context,
        CreateMaintenanceRequestCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaintenanceWriteResult> UpdateAsync(
        MaintenanceOperationContext context,
        UpdateMaintenanceRequestCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaintenanceWriteResult> ConvertToWorkOrderAsync(
        MaintenanceOperationContext context,
        ConvertMaintenanceRequestCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
