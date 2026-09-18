namespace NevaxisZentra.Persistence.Maintenance;

public static class MaintenanceAuditActions
{
    public const string RequestCreated = "maintenance.request.created";
    public const string RequestUpdated = "maintenance.request.updated";
    public const string WorkOrderCreated = "maintenance.workorder.created";
    public const string WorkOrderAssigned = "maintenance.workorder.assigned";
    public const string WorkOrderStarted = "maintenance.workorder.started";
    public const string WorkOrderWaiting = "maintenance.workorder.waiting";
    public const string WorkOrderResumed = "maintenance.workorder.resumed";
    public const string WorkOrderCompleted = "maintenance.workorder.completed";
    public const string WorkOrderCancelled = "maintenance.workorder.cancelled";
    public const string PreventivePlanCreated = "maintenance.preventiveplan.created";
    public const string PreventivePlanUpdated = "maintenance.preventiveplan.updated";
    public const string WorkNoteCreated = "maintenance.worknote.created";
    public const string PreventiveDueGenerated = "maintenance.preventive.due.generated";
    public const string PreventiveDueWorkOrderCreated = "maintenance.preventive.due.workordercreated";
    public const string PartUsageRecorded = "maintenance.partusage.recorded";
}

public static class MaintenanceAuditEventTypes
{
    public const string RequestCreated = "maintenance.request.created";
    public const string RequestUpdated = "maintenance.request.updated";
    public const string WorkOrderCreated = "maintenance.workorder.created";
    public const string WorkOrderAssigned = "maintenance.workorder.assigned";
    public const string WorkOrderStarted = "maintenance.workorder.started";
    public const string WorkOrderWaiting = "maintenance.workorder.waiting";
    public const string WorkOrderResumed = "maintenance.workorder.resumed";
    public const string WorkOrderCompleted = "maintenance.workorder.completed";
    public const string WorkOrderCancelled = "maintenance.workorder.cancelled";
    public const string PreventivePlanCreated = "maintenance.preventiveplan.created";
    public const string PreventivePlanUpdated = "maintenance.preventiveplan.updated";
    public const string WorkNoteCreated = "maintenance.worknote.created";
    public const string PreventiveDueGenerated = "maintenance.preventive.due.generated";
    public const string PreventiveDueWorkOrderCreated = "maintenance.preventive.due.workordercreated";
    public const string PartUsageRecorded = "maintenance.partusage.recorded";
}

public static class MaintenanceOutboxMessageTypes
{
    public const string RequestCreated = "maintenance-request-created";
    public const string WorkOrderCreated = "maintenance-work-order-created";
    public const string WorkOrderStarted = "maintenance-work-order-started";
    public const string WorkOrderCompleted = "maintenance-work-order-completed";
    public const string PreventiveDueChanged = "maintenance-preventive-due-changed";
    public const string WorkNoteCreated = "maintenance-worknote-created";
    public const string PreventiveDueGenerated = "maintenance-preventive-due-generated";
    public const string PreventiveWorkOrderCreated = "maintenance-preventive-workorder-created";
    public const string PartUsageRecorded = "maintenance-partusage-recorded";
}
