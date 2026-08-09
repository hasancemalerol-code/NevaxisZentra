namespace CrevaFactOS.Persistence.Maintenance;

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
}

public static class MaintenanceOutboxMessageTypes
{
    public const string RequestCreated = "maintenance-request-created";
    public const string WorkOrderCreated = "maintenance-work-order-created";
    public const string WorkOrderStarted = "maintenance-work-order-started";
    public const string WorkOrderCompleted = "maintenance-work-order-completed";
    public const string PreventiveDueChanged = "maintenance-preventive-due-changed";
}
