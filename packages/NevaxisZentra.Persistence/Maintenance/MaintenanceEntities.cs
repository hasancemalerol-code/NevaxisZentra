namespace NevaxisZentra.Persistence.Maintenance;

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
    public MaintenanceFailureCategory FailureCategory { get; set; } = MaintenanceFailureCategory.Other;
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
    public Guid? PreventivePlanId { get; set; }
    public Guid? PreventiveDueId { get; set; }
    public decimal? ExternalServiceCost { get; set; }
    public decimal? OtherCost { get; set; }
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

public enum MaintenanceFailureCategory
{
    Mechanical,
    Electrical,
    Hydraulic,
    Pneumatic,
    Cooling,
    Heating,
    Control,
    Mold,
    Robot,
    Auxiliary,
    Other,
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

public sealed class MaintenanceWorkNote
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid WorkOrderId { get; set; }
    public MaintenanceWorkNoteType NoteType { get; set; } = MaintenanceWorkNoteType.General;
    public string Text { get; set; } = string.Empty;
    public bool IsImportant { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;

    public MaintenanceWorkOrder? WorkOrder { get; set; }
}

public sealed class PreventiveMaintenanceDue
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid PreventivePlanId { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string AssetReference { get; set; } = string.Empty;
    public DateTimeOffset DueAtUtc { get; set; }
    public PreventiveMaintenanceDueStatus Status { get; set; } = PreventiveMaintenanceDueStatus.Due;
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public Guid? WorkOrderId { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public uint Version { get; set; } = 1;

    public PreventiveMaintenancePlan? PreventivePlan { get; set; }
}

public sealed class MaintenancePartUsage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid WorkOrderId { get; set; }
    public string PartReference { get; set; } = string.Empty;
    public string? PartName { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? SourceSystem { get; set; }
    public string? SourceReference { get; set; }
    public decimal? EstimatedUnitCost { get; set; }
    public decimal? ActualUnitCost { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public string RecordedBySubjectId { get; set; } = string.Empty;

    public MaintenanceWorkOrder? WorkOrder { get; set; }
}

public enum MaintenanceWorkNoteType
{
    General,
    Diagnosis,
    Action,
    Observation,
    Handover,
}

public enum PreventiveMaintenanceDueStatus
{
    Due,
    WorkOrderCreated,
    Completed,
    Skipped,
    Cancelled,
}
