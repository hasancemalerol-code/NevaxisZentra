namespace NevaxisZentra.Persistence.Mold;

public sealed class MoldMaintenanceRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MoldId { get; set; }
    public MoldMaintenanceType MaintenanceType { get; set; } = MoldMaintenanceType.Preventive;
    public MoldMaintenanceStatus Status { get; set; } = MoldMaintenanceStatus.Planned;
    public DateTimeOffset OpenedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? Findings { get; set; }
    public string? ActionsTaken { get; set; }
    public string? PerformedByDisplayName { get; set; }
    public string? ExternalMaintenanceRequestReference { get; set; }
    public string? ExternalWorkOrderReference { get; set; }
    public MoldSourceType SourceType { get; set; } = MoldSourceType.Manual;
    public string? SourceReference { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? IdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public string UpdatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;
}

public sealed class MoldRevisionRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MoldId { get; set; }
    public string RevisionNumber { get; set; } = string.Empty;
    public string RevisionNumberNormalized { get; set; } = string.Empty;
    public MoldRevisionType RevisionType { get; set; } = MoldRevisionType.Other;
    public DateOnly RevisionDate { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? TechnicalChange { get; set; }
    public int? PreviousCavityCount { get; set; }
    public int? NewCavityCount { get; set; }
    public string? PerformedByDisplayName { get; set; }
    public string? ExternalReference { get; set; }
    public MoldSourceType SourceType { get; set; } = MoldSourceType.Manual;
    public string? SourceReference { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? IdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
}

public sealed class MoldTrialRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MoldId { get; set; }
    public string TrialNumber { get; set; } = string.Empty;
    public string TrialNumberNormalized { get; set; } = string.Empty;
    public DateOnly TrialDate { get; set; }
    public MoldTrialResult Result { get; set; } = MoldTrialResult.Pending;
    public string? MachineReference { get; set; }
    public string? ProductionOrderReference { get; set; }
    public string? ProductReference { get; set; }
    public string? MaterialReference { get; set; }
    public decimal? CycleTimeSeconds { get; set; }
    public int? ShotCount { get; set; }
    public int? GoodPartCount { get; set; }
    public int? ScrapPartCount { get; set; }
    public string? Findings { get; set; }
    public string? Decision { get; set; }
    public string? PerformedByDisplayName { get; set; }
    public MoldSourceType SourceType { get; set; } = MoldSourceType.Manual;
    public string? SourceReference { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? IdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
}

public enum MoldMaintenanceType
{
    Preventive,
    Corrective,
    Cleaning,
    Inspection,
    Repair,
    Overhaul,
    Other,
}

public enum MoldMaintenanceStatus
{
    Planned,
    InProgress,
    Completed,
    Cancelled,
}

public enum MoldRevisionType
{
    DesignChange,
    CavityChange,
    MaterialChange,
    CoolingChange,
    RunnerChange,
    RepairImprovement,
    CustomerChange,
    Other,
}

public enum MoldTrialResult
{
    Pending,
    Successful,
    ConditionallyApproved,
    Failed,
    Cancelled,
}
