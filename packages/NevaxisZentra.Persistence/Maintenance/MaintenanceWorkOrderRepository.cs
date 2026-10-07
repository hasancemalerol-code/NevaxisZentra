using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Maintenance;

public interface IMaintenanceWorkOrderRepository
{
    Task<MaintenanceWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceWorkOrderSummaryReadModel>> ListAsync(
        MaintenanceOperationContext context,
        string? status = null,
        string? priority = null,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWorkOrderDetailReadModel?> GetByIdAsync(
        MaintenanceOperationContext context,
        Guid workOrderId,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> CreateAsync(
        MaintenanceOperationContext context,
        CreateMaintenanceWorkOrderCommand command,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> AssignAsync(
        MaintenanceOperationContext context,
        AssignMaintenanceWorkOrderCommand command,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> StartAsync(
        MaintenanceOperationContext context,
        MaintenanceWorkOrderLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> WaitAsync(
        MaintenanceOperationContext context,
        MaintenanceWorkOrderLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> ResumeAsync(
        MaintenanceOperationContext context,
        MaintenanceWorkOrderLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> CompleteAsync(
        MaintenanceOperationContext context,
        CompleteMaintenanceWorkOrderCommand command,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> CancelAsync(
        MaintenanceOperationContext context,
        MaintenanceWorkOrderLifecycleCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class MaintenanceWorkOrderRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IMaintenanceWorkOrderRepository
{
    public Task<MaintenanceWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var requests = await dbContext.MaintenanceRequests.AsNoTracking()
                .Where(r => r.TenantId == context.TenantId && r.FacilityId == context.FacilityId)
                .Select(r => r.Status)
                .ToListAsync(ct);

            var workOrders = await dbContext.MaintenanceWorkOrders.AsNoTracking()
                .Where(w => w.TenantId == context.TenantId && w.FacilityId == context.FacilityId)
                .Select(w => new { w.Status, w.Priority, w.CompletedAtUtc })
                .ToListAsync(ct);

            var plans = await dbContext.PreventiveMaintenancePlans.AsNoTracking()
                .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
                .Select(p => new { p.Status, p.NextDueAtUtc })
                .ToListAsync(ct);

            var today = DateTime.UtcNow.Date;
            return new MaintenanceWorkspaceSummaryReadModel(
                requests.Count(s => s is MaintenanceRequestStatus.Open or MaintenanceRequestStatus.Triaged),
                workOrders.Count(w => w.Status is not MaintenanceWorkOrderStatus.Completed and not MaintenanceWorkOrderStatus.Cancelled),
                workOrders.Count(w => w.Priority == MaintenancePriority.Critical
                    && w.Status is not MaintenanceWorkOrderStatus.Completed and not MaintenanceWorkOrderStatus.Cancelled),
                workOrders.Count(w => w.Status == MaintenanceWorkOrderStatus.InProgress),
                workOrders.Count(w => w.Status == MaintenanceWorkOrderStatus.Waiting),
                workOrders.Count(w => w.Status == MaintenanceWorkOrderStatus.Completed
                    && w.CompletedAtUtc.HasValue
                    && w.CompletedAtUtc.Value.UtcDateTime.Date == today),
                plans.Count(p => p.Status == PreventivePlanStatus.Active
                    && p.NextDueAtUtc.HasValue
                    && p.NextDueAtUtc.Value < DateTimeOffset.UtcNow));
        }, cancellationToken);

    public Task<IReadOnlyList<MaintenanceWorkOrderSummaryReadModel>> ListAsync(
        MaintenanceOperationContext context,
        string? status = null,
        string? priority = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.MaintenanceWorkOrders.AsNoTracking()
                .Where(w => w.TenantId == context.TenantId && w.FacilityId == context.FacilityId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(w => w.Status == MaintenanceValueConversions.WorkOrderStatusFromStorage(status));
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(w => w.Priority == MaintenanceValueConversions.PriorityFromStorage(priority));
            }

            var items = await query.OrderByDescending(w => w.UpdatedAtUtc).ToListAsync(ct);
            var requestIds = items.Where(w => w.MaintenanceRequestId.HasValue).Select(w => w.MaintenanceRequestId!.Value).Distinct().ToList();
            var requestNumbers = requestIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await dbContext.MaintenanceRequests.AsNoTracking()
                    .Where(r => requestIds.Contains(r.Id))
                    .ToDictionaryAsync(r => r.Id, r => r.RequestNumber, ct);

            return (IReadOnlyList<MaintenanceWorkOrderSummaryReadModel>)items.Select(w => MapSummary(w, requestNumbers)).ToList();
        }, cancellationToken);

    public Task<MaintenanceWorkOrderDetailReadModel?> GetByIdAsync(
        MaintenanceOperationContext context,
        Guid workOrderId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var workOrder = await dbContext.MaintenanceWorkOrders.AsNoTracking()
                .SingleOrDefaultAsync(
                    w => w.TenantId == context.TenantId
                        && w.FacilityId == context.FacilityId
                        && w.Id == workOrderId,
                    ct);

            if (workOrder is null)
            {
                return null;
            }

            string? requestNumber = null;
            if (workOrder.MaintenanceRequestId.HasValue)
            {
                requestNumber = await dbContext.MaintenanceRequests.AsNoTracking()
                    .Where(r => r.Id == workOrder.MaintenanceRequestId.Value)
                    .Select(r => r.RequestNumber)
                    .SingleOrDefaultAsync(ct);
            }

            var (orderRef, orderStatus) = await MaintenanceRepositorySupport.ReadProductionOrderContextAsync(
                dbContext, context, workOrder.ProductionOrderId, ct);
            var activity = await MaintenanceRepositorySupport.LoadActivityAsync(
                dbContext, context, "maintenance.workorder", workOrder.Id, ct);

            string? planCode = null;
            if (workOrder.PreventivePlanId.HasValue)
            {
                planCode = await dbContext.PreventiveMaintenancePlans.AsNoTracking()
                    .Where(p => p.Id == workOrder.PreventivePlanId.Value)
                    .Select(p => p.Code)
                    .SingleOrDefaultAsync(ct);
            }

            return MapDetail(workOrder, requestNumber, orderRef, orderStatus, planCode, activity);
        }, cancellationToken);

    public Task<MaintenanceWriteResult> CreateAsync(
        MaintenanceOperationContext context,
        CreateMaintenanceWorkOrderCommand command,
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

                var initialStatus = MaintenanceValueConversions.WorkOrderStatusFromStorage(command.InitialStatus);
                if (initialStatus is not (MaintenanceWorkOrderStatus.Draft or MaintenanceWorkOrderStatus.Planned))
                {
                    initialStatus = MaintenanceWorkOrderStatus.Draft;
                }

                if (!string.IsNullOrWhiteSpace(command.AssignedTechnicianSubjectId)
                    && initialStatus == MaintenanceWorkOrderStatus.Planned)
                {
                    initialStatus = MaintenanceWorkOrderStatus.Assigned;
                }

                var now = DateTimeOffset.UtcNow;
                var workOrder = new MaintenanceWorkOrder
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    WorkOrderNumber = await MaintenanceRepositorySupport.NextWorkOrderNumberAsync(dbContext, context, ct),
                    MaintenanceRequestId = command.MaintenanceRequestId,
                    AssetType = command.AssetType,
                    AssetId = command.AssetId,
                    AssetReference = command.AssetReference,
                    AssetName = command.AssetName,
                    ProductionOrderId = command.ProductionOrderId,
                    Type = MaintenanceValueConversions.WorkOrderTypeFromStorage(command.Type),
                    Priority = MaintenanceValueConversions.PriorityFromStorage(command.Priority),
                    Status = initialStatus,
                    Title = command.Title.Trim(),
                    Description = command.Description?.Trim() ?? string.Empty,
                    AssignedTechnicianSubjectId = command.AssignedTechnicianSubjectId,
                    PlannedStartUtc = command.PlannedStartUtc,
                    PlannedEndUtc = command.PlannedEndUtc,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBySubjectId = context.IdentitySubjectId.ToString(),
                    Version = 1,
                };

                dbContext.MaintenanceWorkOrders.Add(workOrder);
                MaintenanceRepositorySupport.AddAudit(
                    dbContext, context,
                    MaintenanceAuditActions.WorkOrderCreated,
                    MaintenanceAuditEventTypes.WorkOrderCreated,
                    "maintenance.workorder",
                    workOrder.Id,
                    now,
                    new { workOrderId = workOrder.Id, workOrderNumber = workOrder.WorkOrderNumber });
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

    public Task<MaintenanceWriteResult> AssignAsync(
        MaintenanceOperationContext context,
        AssignMaintenanceWorkOrderCommand command,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            context,
            command.WorkOrderId,
            command.ExpectedVersion,
            workOrder =>
            {
                if (!MaintenanceLifecycleEngine.CanAssignTechnician(workOrder.Status))
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.InvalidTransition, "Technician cannot be assigned in current status.");
                }

                workOrder.AssignedTechnicianSubjectId = command.AssignedTechnicianSubjectId;

                if (workOrder.Status == MaintenanceWorkOrderStatus.Draft)
                {
                    workOrder.Status = MaintenanceWorkOrderStatus.Planned;
                }

                if (!string.IsNullOrWhiteSpace(command.AssignedTechnicianSubjectId)
                    && workOrder.Status is MaintenanceWorkOrderStatus.Planned or MaintenanceWorkOrderStatus.Draft)
                {
                    workOrder.Status = MaintenanceWorkOrderStatus.Assigned;
                }

                if (string.IsNullOrWhiteSpace(command.AssignedTechnicianSubjectId)
                    && MaintenanceLifecycleEngine.CanRemoveTechnician(workOrder.Status))
                {
                    workOrder.Status = MaintenanceWorkOrderStatus.Planned;
                }

                return null;
            },
            MaintenanceAuditActions.WorkOrderAssigned,
            MaintenanceAuditEventTypes.WorkOrderAssigned,
            null,
            cancellationToken);

    public Task<MaintenanceWriteResult> StartAsync(
        MaintenanceOperationContext context,
        MaintenanceWorkOrderLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            context,
            command.WorkOrderId,
            command.ExpectedVersion,
            workOrder =>
            {
                if (!MaintenanceLifecycleEngine.CanTransitionWorkOrder(workOrder.Status, MaintenanceWorkOrderStatus.InProgress))
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.InvalidTransition, "Work order cannot be started from current status.");
                }

                if (string.IsNullOrWhiteSpace(workOrder.AssignedTechnicianSubjectId))
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.ValidationFailed, "Technician must be assigned before starting.");
                }

                workOrder.Status = MaintenanceWorkOrderStatus.InProgress;
                workOrder.StartedAtUtc ??= DateTimeOffset.UtcNow;
                return null;
            },
            MaintenanceAuditActions.WorkOrderStarted,
            MaintenanceAuditEventTypes.WorkOrderStarted,
            MaintenanceOutboxMessageTypes.WorkOrderStarted,
            cancellationToken);

    public Task<MaintenanceWriteResult> WaitAsync(
        MaintenanceOperationContext context,
        MaintenanceWorkOrderLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            context,
            command.WorkOrderId,
            command.ExpectedVersion,
            workOrder =>
            {
                if (!MaintenanceLifecycleEngine.CanTransitionWorkOrder(workOrder.Status, MaintenanceWorkOrderStatus.Waiting))
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.InvalidTransition, "Work order cannot be paused from current status.");
                }

                workOrder.Status = MaintenanceWorkOrderStatus.Waiting;
                return null;
            },
            MaintenanceAuditActions.WorkOrderWaiting,
            MaintenanceAuditEventTypes.WorkOrderWaiting,
            null,
            cancellationToken);

    public Task<MaintenanceWriteResult> ResumeAsync(
        MaintenanceOperationContext context,
        MaintenanceWorkOrderLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            context,
            command.WorkOrderId,
            command.ExpectedVersion,
            workOrder =>
            {
                if (!MaintenanceLifecycleEngine.CanTransitionWorkOrder(workOrder.Status, MaintenanceWorkOrderStatus.InProgress))
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.InvalidTransition, "Work order cannot be resumed from current status.");
                }

                workOrder.Status = MaintenanceWorkOrderStatus.InProgress;
                return null;
            },
            MaintenanceAuditActions.WorkOrderResumed,
            MaintenanceAuditEventTypes.WorkOrderResumed,
            null,
            cancellationToken);

    public Task<MaintenanceWriteResult> CompleteAsync(
        MaintenanceOperationContext context,
        CompleteMaintenanceWorkOrderCommand command,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            context,
            command.WorkOrderId,
            command.ExpectedVersion,
            workOrder =>
            {
                if (string.IsNullOrWhiteSpace(command.CompletionNote))
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.CompletionNoteRequired, "Completion note is required.");
                }

                if (!MaintenanceLifecycleEngine.CanTransitionWorkOrder(workOrder.Status, MaintenanceWorkOrderStatus.Completed))
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.InvalidTransition, "Work order cannot be completed from current status.");
                }

                workOrder.Status = MaintenanceWorkOrderStatus.Completed;
                workOrder.CompletedAtUtc = DateTimeOffset.UtcNow;
                workOrder.CompletionNote = command.CompletionNote.Trim();
                workOrder.ResolutionCode = string.IsNullOrWhiteSpace(command.ResolutionCode)
                    ? null
                    : MaintenanceValueConversions.ResolutionCodeFromStorage(command.ResolutionCode);
                return null;
            },
            MaintenanceAuditActions.WorkOrderCompleted,
            MaintenanceAuditEventTypes.WorkOrderCompleted,
            MaintenanceOutboxMessageTypes.WorkOrderCompleted,
            cancellationToken);

    public Task<MaintenanceWriteResult> CancelAsync(
        MaintenanceOperationContext context,
        MaintenanceWorkOrderLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            context,
            command.WorkOrderId,
            command.ExpectedVersion,
            workOrder =>
            {
                if (!MaintenanceLifecycleEngine.CanTransitionWorkOrder(workOrder.Status, MaintenanceWorkOrderStatus.Cancelled))
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.InvalidTransition, "Work order cannot be cancelled from current status.");
                }

                workOrder.Status = MaintenanceWorkOrderStatus.Cancelled;
                return null;
            },
            MaintenanceAuditActions.WorkOrderCancelled,
            MaintenanceAuditEventTypes.WorkOrderCancelled,
            null,
            cancellationToken);

    private Task<MaintenanceWriteResult> TransitionAsync(
        MaintenanceOperationContext context,
        Guid workOrderId,
        uint expectedVersion,
        Func<MaintenanceWorkOrder, MaintenanceWriteResult?> mutate,
        string auditAction,
        string auditEventType,
        string? outboxType,
        CancellationToken cancellationToken) =>
        MaintenanceRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                var workOrder = await dbContext.MaintenanceWorkOrders
                    .SingleOrDefaultAsync(
                        w => w.TenantId == context.TenantId
                            && w.FacilityId == context.FacilityId
                            && w.Id == workOrderId,
                        ct);

                if (workOrder is null)
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.NotFound, "Maintenance work order not found.");
                }

                if (workOrder.Version != expectedVersion)
                {
                    return MaintenanceRepositorySupport.ConcurrencyFail(workOrder.Version, expectedVersion);
                }

                var validationError = mutate(workOrder);
                if (validationError is not null)
                {
                    return validationError;
                }

                var now = DateTimeOffset.UtcNow;
                workOrder.UpdatedAtUtc = now;
                workOrder.Version++;

                MaintenanceRepositorySupport.AddAudit(
                    dbContext, context,
                    auditAction,
                    auditEventType,
                    "maintenance.workorder",
                    workOrder.Id,
                    now,
                    new { workOrderId = workOrder.Id, status = MaintenanceValueConversions.ToStorage(workOrder.Status) });

                if (outboxType is not null)
                {
                    MaintenanceRepositorySupport.AddOutbox(
                        dbContext, context,
                        "maintenance-work-order",
                        workOrder.Id,
                        workOrder.Version,
                        outboxType,
                        now,
                        new { workOrderId = workOrder.Id.ToString(), workOrderNumber = workOrder.WorkOrderNumber });
                }

                if (auditAction == MaintenanceAuditActions.WorkOrderCompleted && workOrder.PreventiveDueId.HasValue)
                {
                    var due = await dbContext.PreventiveMaintenanceDues
                        .SingleOrDefaultAsync(
                            d => d.TenantId == context.TenantId
                                && d.FacilityId == context.FacilityId
                                && d.Id == workOrder.PreventiveDueId.Value,
                            ct);

                    if (due is not null && due.Status == PreventiveMaintenanceDueStatus.WorkOrderCreated)
                    {
                        due.Status = PreventiveMaintenanceDueStatus.Completed;
                        due.CompletedAtUtc = now;
                        due.Version++;
                    }
                }

                await dbContext.SaveChangesAsync(ct);
                return MaintenanceWriteResult.Success(workOrder.Id, workOrder.Version);
            },
            cancellationToken);

    private static MaintenanceWorkOrderSummaryReadModel MapSummary(
        MaintenanceWorkOrder workOrder,
        Dictionary<Guid, string> requestNumbers) =>
        new(
            workOrder.Id,
            workOrder.WorkOrderNumber,
            workOrder.MaintenanceRequestId,
            workOrder.MaintenanceRequestId.HasValue && requestNumbers.TryGetValue(workOrder.MaintenanceRequestId.Value, out var num)
                ? num
                : null,
            workOrder.AssetType,
            workOrder.AssetId,
            workOrder.AssetReference,
            workOrder.AssetName,
            MaintenanceValueConversions.ToStorage(workOrder.Type),
            MaintenanceValueConversions.ToStorage(workOrder.Priority),
            MaintenanceValueConversions.ToStorage(workOrder.Status),
            workOrder.AssignedTechnicianSubjectId,
            workOrder.PlannedStartUtc,
            workOrder.PlannedEndUtc,
            workOrder.StartedAtUtc,
            workOrder.Version,
            workOrder.UpdatedAtUtc);

    private static MaintenanceWorkOrderDetailReadModel MapDetail(
        MaintenanceWorkOrder workOrder,
        string? requestNumber,
        string? orderRef,
        string? orderStatus,
        string? planCode,
        IReadOnlyList<MaintenanceActivityReadModel> activity) =>
        new(
            workOrder.Id,
            workOrder.WorkOrderNumber,
            workOrder.MaintenanceRequestId,
            requestNumber,
            workOrder.AssetType,
            workOrder.AssetId,
            workOrder.AssetReference,
            workOrder.AssetName,
            workOrder.ProductionOrderId,
            orderRef,
            orderStatus,
            workOrder.PreventivePlanId,
            planCode,
            workOrder.PreventiveDueId,
            workOrder.ExternalServiceCost,
            workOrder.OtherCost,
            MaintenanceValueConversions.ToStorage(workOrder.Type),
            MaintenanceValueConversions.ToStorage(workOrder.Priority),
            MaintenanceValueConversions.ToStorage(workOrder.Status),
            workOrder.Title,
            workOrder.Description,
            workOrder.AssignedTechnicianSubjectId,
            workOrder.PlannedStartUtc,
            workOrder.PlannedEndUtc,
            workOrder.StartedAtUtc,
            workOrder.CompletedAtUtc,
            workOrder.CompletionNote,
            workOrder.ResolutionCode.HasValue
                ? MaintenanceValueConversions.ToStorage(workOrder.ResolutionCode.Value)
                : null,
            workOrder.CreatedBySubjectId,
            workOrder.Version,
            workOrder.CreatedAtUtc,
            workOrder.UpdatedAtUtc,
            activity);
}

public sealed class UnavailableMaintenanceWorkOrderRepository : IMaintenanceWorkOrderRepository
{
    private static MaintenanceWriteResult Unavailable() =>
        MaintenanceWriteResult.Fail(MaintenanceFailureCodes.SourceUnavailable, "Maintenance work order service is unavailable.");

    public Task<MaintenanceWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new MaintenanceWorkspaceSummaryReadModel(0, 0, 0, 0, 0, 0, 0));

    public Task<IReadOnlyList<MaintenanceWorkOrderSummaryReadModel>> ListAsync(
        MaintenanceOperationContext context,
        string? status = null,
        string? priority = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MaintenanceWorkOrderSummaryReadModel>>([]);

    public Task<MaintenanceWorkOrderDetailReadModel?> GetByIdAsync(
        MaintenanceOperationContext context,
        Guid workOrderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<MaintenanceWorkOrderDetailReadModel?>(null);

    public Task<MaintenanceWriteResult> CreateAsync(
        MaintenanceOperationContext context,
        CreateMaintenanceWorkOrderCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaintenanceWriteResult> AssignAsync(
        MaintenanceOperationContext context,
        AssignMaintenanceWorkOrderCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaintenanceWriteResult> StartAsync(
        MaintenanceOperationContext context,
        MaintenanceWorkOrderLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaintenanceWriteResult> WaitAsync(
        MaintenanceOperationContext context,
        MaintenanceWorkOrderLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaintenanceWriteResult> ResumeAsync(
        MaintenanceOperationContext context,
        MaintenanceWorkOrderLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaintenanceWriteResult> CompleteAsync(
        MaintenanceOperationContext context,
        CompleteMaintenanceWorkOrderCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaintenanceWriteResult> CancelAsync(
        MaintenanceOperationContext context,
        MaintenanceWorkOrderLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
