namespace CrevaFactOS.Persistence.Maintenance;

public static class MaintenanceLifecycleEngine
{
    private static readonly HashSet<(MaintenanceWorkOrderStatus From, MaintenanceWorkOrderStatus To)> ValidWorkOrderTransitions =
    [
        (MaintenanceWorkOrderStatus.Draft, MaintenanceWorkOrderStatus.Planned),
        (MaintenanceWorkOrderStatus.Draft, MaintenanceWorkOrderStatus.Cancelled),
        (MaintenanceWorkOrderStatus.Planned, MaintenanceWorkOrderStatus.Assigned),
        (MaintenanceWorkOrderStatus.Planned, MaintenanceWorkOrderStatus.Cancelled),
        (MaintenanceWorkOrderStatus.Assigned, MaintenanceWorkOrderStatus.InProgress),
        (MaintenanceWorkOrderStatus.Assigned, MaintenanceWorkOrderStatus.Cancelled),
        (MaintenanceWorkOrderStatus.InProgress, MaintenanceWorkOrderStatus.Waiting),
        (MaintenanceWorkOrderStatus.InProgress, MaintenanceWorkOrderStatus.Completed),
        (MaintenanceWorkOrderStatus.InProgress, MaintenanceWorkOrderStatus.Cancelled),
        (MaintenanceWorkOrderStatus.Waiting, MaintenanceWorkOrderStatus.InProgress),
        (MaintenanceWorkOrderStatus.Waiting, MaintenanceWorkOrderStatus.Cancelled),
    ];

    public static bool CanTransitionWorkOrder(MaintenanceWorkOrderStatus from, MaintenanceWorkOrderStatus to) =>
        ValidWorkOrderTransitions.Contains((from, to));

    public static bool IsTerminalWorkOrderStatus(MaintenanceWorkOrderStatus status) =>
        status is MaintenanceWorkOrderStatus.Completed or MaintenanceWorkOrderStatus.Cancelled;

    public static bool CanAssignTechnician(MaintenanceWorkOrderStatus status) =>
        status is MaintenanceWorkOrderStatus.Draft
            or MaintenanceWorkOrderStatus.Planned
            or MaintenanceWorkOrderStatus.Assigned
            or MaintenanceWorkOrderStatus.InProgress
            or MaintenanceWorkOrderStatus.Waiting;

    public static bool CanRemoveTechnician(MaintenanceWorkOrderStatus status) =>
        status is MaintenanceWorkOrderStatus.Assigned or MaintenanceWorkOrderStatus.Planned;

    public static bool CanConvertRequest(MaintenanceRequestStatus status) =>
        status is MaintenanceRequestStatus.Open or MaintenanceRequestStatus.Triaged;

    public static bool CanUpdateRequest(MaintenanceRequestStatus status) =>
        status is not MaintenanceRequestStatus.ConvertedToWorkOrder
            and not MaintenanceRequestStatus.Closed
            and not MaintenanceRequestStatus.Cancelled;
}
