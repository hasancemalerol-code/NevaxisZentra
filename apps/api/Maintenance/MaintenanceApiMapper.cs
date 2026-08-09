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
        preventivePlanId = model.PreventivePlanId,
        preventivePlanCode = model.PreventivePlanCode,
        preventiveDueId = model.PreventiveDueId,
        externalServiceCost = model.ExternalServiceCost,
        otherCost = model.OtherCost,
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

    public static object MapWorkNote(MaintenanceWorkNoteReadModel model) => new
    {
        id = model.Id,
        workOrderId = model.WorkOrderId,
        noteType = model.NoteType,
        text = model.Text,
        isImportant = model.IsImportant,
        createdAtUtc = model.CreatedAtUtc,
        createdBySubjectId = model.CreatedBySubjectId,
    };

    public static object MapTimelineEntry(MaintenanceTimelineEntryReadModel model) => new
    {
        entryType = model.EntryType,
        title = model.Title,
        detail = model.Detail,
        occurredAtUtc = model.OccurredAtUtc,
        actorSubjectId = model.ActorSubjectId,
    };

    public static object MapPartUsage(MaintenancePartUsageReadModel model) => new
    {
        id = model.Id,
        workOrderId = model.WorkOrderId,
        partReference = model.PartReference,
        partName = model.PartName,
        quantity = model.Quantity,
        unit = model.Unit,
        sourceSystem = model.SourceSystem,
        sourceReference = model.SourceReference,
        estimatedUnitCost = model.EstimatedUnitCost,
        actualUnitCost = model.ActualUnitCost,
        estimatedLineCost = model.EstimatedLineCost,
        actualLineCost = model.ActualLineCost,
        recordedAtUtc = model.RecordedAtUtc,
        recordedBySubjectId = model.RecordedBySubjectId,
    };

    public static object MapCostSummary(MaintenanceCostSummaryReadModel model) => new
    {
        workOrderId = model.WorkOrderId,
        estimatedPartsCost = model.EstimatedPartsCost,
        actualPartsCost = model.ActualPartsCost,
        externalServiceCost = model.ExternalServiceCost,
        otherCost = model.OtherCost,
        totalMaintenanceCost = model.TotalMaintenanceCost,
    };

    public static object MapPreventiveDueSummary(PreventiveMaintenanceDueSummaryReadModel model) => new
    {
        id = model.Id,
        preventivePlanId = model.PreventivePlanId,
        preventivePlanCode = model.PreventivePlanCode,
        preventivePlanName = model.PreventivePlanName,
        assetType = model.AssetType,
        assetReference = model.AssetReference,
        dueAtUtc = model.DueAtUtc,
        status = model.Status,
        generatedAtUtc = model.GeneratedAtUtc,
        workOrderId = model.WorkOrderId,
        workOrderNumber = model.WorkOrderNumber,
        completedAtUtc = model.CompletedAtUtc,
        version = model.Version,
    };

    public static object MapPreventiveDueDetail(PreventiveMaintenanceDueDetailReadModel model) => new
    {
        id = model.Id,
        preventivePlanId = model.PreventivePlanId,
        preventivePlanCode = model.PreventivePlanCode,
        preventivePlanName = model.PreventivePlanName,
        assetType = model.AssetType,
        assetReference = model.AssetReference,
        dueAtUtc = model.DueAtUtc,
        status = model.Status,
        generatedAtUtc = model.GeneratedAtUtc,
        workOrderId = model.WorkOrderId,
        workOrderNumber = model.WorkOrderNumber,
        completedAtUtc = model.CompletedAtUtc,
        version = model.Version,
    };

    public static object MapAssetHistory(MaintenanceAssetHistoryReadModel model) => new
    {
        assetReference = model.AssetReference,
        requests = model.Requests.Select(MapRequestSummary).ToList(),
        workOrders = model.WorkOrders.Select(MapWorkOrderSummary).ToList(),
        preventivePlans = model.PreventivePlans.Select(MapPreventivePlanSummary).ToList(),
        preventiveDues = model.PreventiveDues.Select(MapPreventiveDueSummary).ToList(),
        workNotes = model.WorkNotes.Select(MapWorkNote).ToList(),
        partUsages = model.PartUsages.Select(MapPartUsage).ToList(),
        costSummaries = model.CostSummaries.Select(MapCostSummary).ToList(),
    };
}
