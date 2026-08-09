namespace CrevaFactOS.Persistence.Maintenance;

public sealed class MaintenanceRequest
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string? AssetId { get; set; }
    public string AssetReference { get; set; } = string.Empty;
    public string? AssetName { get; set; }
    public Guid? ProductionOrderId { get; set; }
    public MaintenanceRequestType Type { get; set; } = MaintenanceRequestType.Fault;
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Normal;
    public MaintenanceRequestStatus Status { get; set; } = MaintenanceRequestStatus.Open;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset ReportedAtUtc { get; set; }
    public string ReportedBySubjectId { get; set; } = string.Empty;
    public string Source { get; set; } = "manual";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;
}

public sealed class MaintenanceWorkOrder
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public Guid? MaintenanceRequestId { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string? AssetId { get; set; }
    public string AssetReference { get; set; } = string.Empty;
    public string? AssetName { get; set; }
    public Guid? ProductionOrderId { get; set; }
    public MaintenanceWorkOrderType Type { get; set; } = MaintenanceWorkOrderType.Corrective;
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Normal;
    public MaintenanceWorkOrderStatus Status { get; set; } = MaintenanceWorkOrderStatus.Draft;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AssignedTechnicianSubjectId { get; set; }
    public DateTimeOffset? PlannedStartUtc { get; set; }
    public DateTimeOffset? PlannedEndUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? CompletionNote { get; set; }
    public MaintenanceResolutionCode? ResolutionCode { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;

    public MaintenanceRequest? MaintenanceRequest { get; set; }
}

public sealed class PreventiveMaintenancePlan
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string? AssetId { get; set; }
    public string AssetReference { get; set; } = string.Empty;
    public PreventiveIntervalType IntervalType { get; set; } = PreventiveIntervalType.Months;
    public int IntervalValue { get; set; } = 1;
    public DateTimeOffset? NextDueAtUtc { get; set; }
    public PreventivePlanStatus Status { get; set; } = PreventivePlanStatus.Active;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;
}

public enum MaintenanceRequestType
{
    Fault,
    Corrective,
    Preventive,
    Inspection,
    Other,
}

public enum MaintenanceWorkOrderType
{
    Fault,
    Corrective,
    Preventive,
    Inspection,
    Other,
}

public enum MaintenancePriority
{
    Low,
    Normal,
    High,
    Critical,
}

public enum MaintenanceRequestStatus
{
    Open,
    Triaged,
    ConvertedToWorkOrder,
    Closed,
    Cancelled,
}

public enum MaintenanceWorkOrderStatus
{
    Draft,
    Planned,
    Assigned,
    InProgress,
    Waiting,
    Completed,
    Cancelled,
}

public enum MaintenanceResolutionCode
{
    Repaired,
    Adjusted,
    Cleaned,
    ReplacedPart,
    TemporaryFix,
    NoFaultFound,
    Other,
}

public enum PreventiveIntervalType
{
    Days,
    Weeks,
    Months,
}

public enum PreventivePlanStatus
{
    Active,
    Inactive,
}
