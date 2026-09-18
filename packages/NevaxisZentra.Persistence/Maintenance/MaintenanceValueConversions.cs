namespace NevaxisZentra.Persistence.Maintenance;

public static class MaintenanceValueConversions
{
    public static string ToStorage(MaintenanceRequestType value) => value switch
    {
        MaintenanceRequestType.Fault => "fault",
        MaintenanceRequestType.Corrective => "corrective",
        MaintenanceRequestType.Preventive => "preventive",
        MaintenanceRequestType.Inspection => "inspection",
        _ => "other",
    };

    public static MaintenanceRequestType RequestTypeFromStorage(string value) => value switch
    {
        "fault" => MaintenanceRequestType.Fault,
        "corrective" => MaintenanceRequestType.Corrective,
        "preventive" => MaintenanceRequestType.Preventive,
        "inspection" => MaintenanceRequestType.Inspection,
        _ => MaintenanceRequestType.Other,
    };

    public static string ToStorage(MaintenanceWorkOrderType value) => value switch
    {
        MaintenanceWorkOrderType.Fault => "fault",
        MaintenanceWorkOrderType.Corrective => "corrective",
        MaintenanceWorkOrderType.Preventive => "preventive",
        MaintenanceWorkOrderType.Inspection => "inspection",
        _ => "other",
    };

    public static MaintenanceWorkOrderType WorkOrderTypeFromStorage(string value) => value switch
    {
        "fault" => MaintenanceWorkOrderType.Fault,
        "corrective" => MaintenanceWorkOrderType.Corrective,
        "preventive" => MaintenanceWorkOrderType.Preventive,
        "inspection" => MaintenanceWorkOrderType.Inspection,
        _ => MaintenanceWorkOrderType.Other,
    };

    public static string ToStorage(MaintenancePriority value) => value switch
    {
        MaintenancePriority.Low => "low",
        MaintenancePriority.Normal => "normal",
        MaintenancePriority.High => "high",
        MaintenancePriority.Critical => "critical",
        _ => "normal",
    };

    public static MaintenancePriority PriorityFromStorage(string value) => value switch
    {
        "low" => MaintenancePriority.Low,
        "high" => MaintenancePriority.High,
        "critical" => MaintenancePriority.Critical,
        _ => MaintenancePriority.Normal,
    };

    public static string ToStorage(MaintenanceRequestStatus value) => value switch
    {
        MaintenanceRequestStatus.Open => "open",
        MaintenanceRequestStatus.Triaged => "triaged",
        MaintenanceRequestStatus.ConvertedToWorkOrder => "converted-to-work-order",
        MaintenanceRequestStatus.Closed => "closed",
        MaintenanceRequestStatus.Cancelled => "cancelled",
        _ => "open",
    };

    public static MaintenanceRequestStatus RequestStatusFromStorage(string value) => value switch
    {
        "triaged" => MaintenanceRequestStatus.Triaged,
        "converted-to-work-order" => MaintenanceRequestStatus.ConvertedToWorkOrder,
        "closed" => MaintenanceRequestStatus.Closed,
        "cancelled" => MaintenanceRequestStatus.Cancelled,
        _ => MaintenanceRequestStatus.Open,
    };

    public static string ToStorage(MaintenanceWorkOrderStatus value) => value switch
    {
        MaintenanceWorkOrderStatus.Draft => "draft",
        MaintenanceWorkOrderStatus.Planned => "planned",
        MaintenanceWorkOrderStatus.Assigned => "assigned",
        MaintenanceWorkOrderStatus.InProgress => "in-progress",
        MaintenanceWorkOrderStatus.Waiting => "waiting",
        MaintenanceWorkOrderStatus.Completed => "completed",
        MaintenanceWorkOrderStatus.Cancelled => "cancelled",
        _ => "draft",
    };

    public static MaintenanceWorkOrderStatus WorkOrderStatusFromStorage(string value) => value switch
    {
        "planned" => MaintenanceWorkOrderStatus.Planned,
        "assigned" => MaintenanceWorkOrderStatus.Assigned,
        "in-progress" => MaintenanceWorkOrderStatus.InProgress,
        "waiting" => MaintenanceWorkOrderStatus.Waiting,
        "completed" => MaintenanceWorkOrderStatus.Completed,
        "cancelled" => MaintenanceWorkOrderStatus.Cancelled,
        _ => MaintenanceWorkOrderStatus.Draft,
    };

    public static string ToStorage(MaintenanceResolutionCode value) => value switch
    {
        MaintenanceResolutionCode.Repaired => "repaired",
        MaintenanceResolutionCode.Adjusted => "adjusted",
        MaintenanceResolutionCode.Cleaned => "cleaned",
        MaintenanceResolutionCode.ReplacedPart => "replaced-part",
        MaintenanceResolutionCode.TemporaryFix => "temporary-fix",
        MaintenanceResolutionCode.NoFaultFound => "no-fault-found",
        _ => "other",
    };

    public static MaintenanceResolutionCode ResolutionCodeFromStorage(string value) => value switch
    {
        "repaired" => MaintenanceResolutionCode.Repaired,
        "adjusted" => MaintenanceResolutionCode.Adjusted,
        "cleaned" => MaintenanceResolutionCode.Cleaned,
        "replaced-part" => MaintenanceResolutionCode.ReplacedPart,
        "temporary-fix" => MaintenanceResolutionCode.TemporaryFix,
        "no-fault-found" => MaintenanceResolutionCode.NoFaultFound,
        _ => MaintenanceResolutionCode.Other,
    };

    public static string ToStorage(PreventiveIntervalType value) => value switch
    {
        PreventiveIntervalType.Days => "days",
        PreventiveIntervalType.Weeks => "weeks",
        _ => "months",
    };

    public static PreventiveIntervalType IntervalTypeFromStorage(string value) => value switch
    {
        "days" => PreventiveIntervalType.Days,
        "weeks" => PreventiveIntervalType.Weeks,
        _ => PreventiveIntervalType.Months,
    };

    public static string ToStorage(PreventivePlanStatus value) => value switch
    {
        PreventivePlanStatus.Inactive => "inactive",
        _ => "active",
    };

    public static PreventivePlanStatus PlanStatusFromStorage(string value) =>
        value == "inactive" ? PreventivePlanStatus.Inactive : PreventivePlanStatus.Active;

    public static string ToStorage(MaintenanceWorkNoteType value) => value switch
    {
        MaintenanceWorkNoteType.Diagnosis => "diagnosis",
        MaintenanceWorkNoteType.Action => "action",
        MaintenanceWorkNoteType.Observation => "observation",
        MaintenanceWorkNoteType.Handover => "handover",
        _ => "general",
    };

    public static MaintenanceWorkNoteType WorkNoteTypeFromStorage(string value) => value switch
    {
        "diagnosis" => MaintenanceWorkNoteType.Diagnosis,
        "action" => MaintenanceWorkNoteType.Action,
        "observation" => MaintenanceWorkNoteType.Observation,
        "handover" => MaintenanceWorkNoteType.Handover,
        _ => MaintenanceWorkNoteType.General,
    };

    public static string ToStorage(PreventiveMaintenanceDueStatus value) => value switch
    {
        PreventiveMaintenanceDueStatus.WorkOrderCreated => "work-order-created",
        PreventiveMaintenanceDueStatus.Completed => "completed",
        PreventiveMaintenanceDueStatus.Skipped => "skipped",
        PreventiveMaintenanceDueStatus.Cancelled => "cancelled",
        _ => "due",
    };

    public static PreventiveMaintenanceDueStatus DueStatusFromStorage(string value) => value switch
    {
        "work-order-created" => PreventiveMaintenanceDueStatus.WorkOrderCreated,
        "completed" => PreventiveMaintenanceDueStatus.Completed,
        "skipped" => PreventiveMaintenanceDueStatus.Skipped,
        "cancelled" => PreventiveMaintenanceDueStatus.Cancelled,
        _ => PreventiveMaintenanceDueStatus.Due,
    };

    public static string ToStorage(MaintenanceFailureCategory value) => value switch
    {
        MaintenanceFailureCategory.Mechanical => "mechanical",
        MaintenanceFailureCategory.Electrical => "electrical",
        MaintenanceFailureCategory.Hydraulic => "hydraulic",
        MaintenanceFailureCategory.Pneumatic => "pneumatic",
        MaintenanceFailureCategory.Cooling => "cooling",
        MaintenanceFailureCategory.Heating => "heating",
        MaintenanceFailureCategory.Control => "control",
        MaintenanceFailureCategory.Mold => "mold",
        MaintenanceFailureCategory.Robot => "robot",
        MaintenanceFailureCategory.Auxiliary => "auxiliary",
        _ => "other",
    };

    public static MaintenanceFailureCategory FailureCategoryFromStorage(string value) => value switch
    {
        "mechanical" => MaintenanceFailureCategory.Mechanical,
        "electrical" => MaintenanceFailureCategory.Electrical,
        "hydraulic" => MaintenanceFailureCategory.Hydraulic,
        "pneumatic" => MaintenanceFailureCategory.Pneumatic,
        "cooling" => MaintenanceFailureCategory.Cooling,
        "heating" => MaintenanceFailureCategory.Heating,
        "control" => MaintenanceFailureCategory.Control,
        "mold" => MaintenanceFailureCategory.Mold,
        "robot" => MaintenanceFailureCategory.Robot,
        "auxiliary" => MaintenanceFailureCategory.Auxiliary,
        _ => MaintenanceFailureCategory.Other,
    };
}
