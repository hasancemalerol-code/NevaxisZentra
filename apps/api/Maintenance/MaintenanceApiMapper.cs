using CrevaFactOS.Persistence.Maintenance;

namespace CrevaFactOS.Api.Maintenance;

internal static class MaintenanceApiMapper
{
    public static object MapRequestSummary(MaintenanceRequestSummaryReadModel model) => new
    {
        id = model.Id,
        requestNumber = model.RequestNumber,
        assetType = model.AssetType,
        assetId = model.AssetId,
        assetReference = model.AssetReference,
        assetName = model.AssetName,
        title = model.Title,
        type = model.Type,
        priority = model.Priority,
        status = model.Status,
        reportedBySubjectId = model.ReportedBySubjectId,
        reportedAtUtc = model.ReportedAtUtc,
        version = model.Version,
        updatedAtUtc = model.UpdatedAtUtc,
    };

    public static object MapRequestDetail(MaintenanceRequestDetailReadModel model) => new
    {
        id = model.Id,
        requestNumber = model.RequestNumber,
        assetType = model.AssetType,
        assetId = model.AssetId,
        assetReference = model.AssetReference,
        assetName = model.AssetName,
        productionOrderId = model.ProductionOrderId,
        productionOrderReference = model.ProductionOrderReference,
        productionOrderStatus = model.ProductionOrderStatus,
        type = model.Type,
        priority = model.Priority,
        status = model.Status,
        title = model.Title,
        description = model.Description,
        source = model.Source,
        reportedBySubjectId = model.ReportedBySubjectId,
        reportedAtUtc = model.ReportedAtUtc,
        version = model.Version,
        createdAtUtc = model.CreatedAtUtc,
        updatedAtUtc = model.UpdatedAtUtc,
        activity = model.Activity.Select(MapActivity).ToList(),
    };

    public static object MapWorkOrderSummary(MaintenanceWorkOrderSummaryReadModel model) => new
    {
        id = model.Id,
        workOrderNumber = model.WorkOrderNumber,
        maintenanceRequestId = model.MaintenanceRequestId,
        maintenanceRequestNumber = model.MaintenanceRequestNumber,
        assetType = model.AssetType,
        assetId = model.AssetId,
        assetReference = model.AssetReference,
        assetName = model.AssetName,
        type = model.Type,
        priority = model.Priority,
        status = model.Status,
        assignedTechnicianSubjectId = model.AssignedTechnicianSubjectId,
        plannedStartUtc = model.PlannedStartUtc,
        plannedEndUtc = model.PlannedEndUtc,
        startedAtUtc = model.StartedAtUtc,
        version = model.Version,
        updatedAtUtc = model.UpdatedAtUtc,
    };

    public static object MapWorkOrderDetail(MaintenanceWorkOrderDetailReadModel model) => new
    {
        id = model.Id,
        workOrderNumber = model.WorkOrderNumber,
        maintenanceRequestId = model.MaintenanceRequestId,
        maintenanceRequestNumber = model.MaintenanceRequestNumber,
        assetType = model.AssetType,
        assetId = model.AssetId,
        assetReference = model.AssetReference,
        assetName = model.AssetName,
        productionOrderId = model.ProductionOrderId,
        productionOrderReference = model.ProductionOrderReference,
        productionOrderStatus = model.ProductionOrderStatus,
        type = model.Type,
        priority = model.Priority,
        status = model.Status,
        title = model.Title,
        description = model.Description,
        assignedTechnicianSubjectId = model.AssignedTechnicianSubjectId,
        plannedStartUtc = model.PlannedStartUtc,
        plannedEndUtc = model.PlannedEndUtc,
        startedAtUtc = model.StartedAtUtc,
        completedAtUtc = model.CompletedAtUtc,
        completionNote = model.CompletionNote,
        resolutionCode = model.ResolutionCode,
        createdBySubjectId = model.CreatedBySubjectId,
        version = model.Version,
        createdAtUtc = model.CreatedAtUtc,
        updatedAtUtc = model.UpdatedAtUtc,
        activity = model.Activity.Select(MapActivity).ToList(),
    };

    public static object MapPreventivePlanSummary(PreventiveMaintenancePlanSummaryReadModel model) => new
    {
        id = model.Id,
        code = model.Code,
        name = model.Name,
        assetType = model.AssetType,
        assetId = model.AssetId,
        assetReference = model.AssetReference,
        intervalType = model.IntervalType,
        intervalValue = model.IntervalValue,
        nextDueAtUtc = model.NextDueAtUtc,
        status = model.Status,
        version = model.Version,
        updatedAtUtc = model.UpdatedAtUtc,
    };

    public static object MapPreventivePlanDetail(PreventiveMaintenancePlanDetailReadModel model) => new
    {
        id = model.Id,
        code = model.Code,
        name = model.Name,
        assetType = model.AssetType,
        assetId = model.AssetId,
        assetReference = model.AssetReference,
        intervalType = model.IntervalType,
        intervalValue = model.IntervalValue,
        nextDueAtUtc = model.NextDueAtUtc,
        status = model.Status,
        description = model.Description,
        version = model.Version,
        createdAtUtc = model.CreatedAtUtc,
        updatedAtUtc = model.UpdatedAtUtc,
    };

    public static object MapWorkspaceSummary(MaintenanceWorkspaceSummaryReadModel model) => new
    {
        openRequestCount = model.OpenRequestCount,
        openWorkOrderCount = model.OpenWorkOrderCount,
        criticalWorkOrderCount = model.CriticalWorkOrderCount,
        inProgressCount = model.InProgressCount,
        waitingCount = model.WaitingCount,
        completedTodayCount = model.CompletedTodayCount,
        overduePreventiveCount = model.OverduePreventiveCount,
    };

    private static object MapActivity(MaintenanceActivityReadModel model) => new
    {
        action = model.Action,
        eventType = model.EventType,
        occurredAtUtc = model.OccurredAtUtc,
        actorSubjectId = model.ActorSubjectId,
        summary = model.Summary,
    };
}
