namespace NevaxisFactOS.Persistence.Mold;

public sealed class MoldRegistryEntry
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string CodeNormalized { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }
    public int? ManufactureYear { get; set; }
    public DateOnly? ManufactureDate { get; set; }
    public DateOnly? CommissioningDate { get; set; }
    public MoldSourceType SourceType { get; set; } = MoldSourceType.Manual;
    public string? ExternalSourceReference { get; set; }
    public MoldOwnershipType OwnershipType { get; set; } = MoldOwnershipType.CompanyOwned;
    public string? OwnerReference { get; set; }
    public MoldLocationType LocationType { get; set; } = MoldLocationType.Unknown;
    public string? LocationLabel { get; set; }
    public DateTimeOffset? LocationChangedAtUtc { get; set; }
    public string? LocationChangedBySubjectId { get; set; }
    public MoldLifecycleState LifecycleState { get; set; } = MoldLifecycleState.Draft;
    public string? MoldType { get; set; }
    public int? CavityCount { get; set; }
    public long? NominalShotLife { get; set; }
    public long? CurrentShotCount { get; set; }
    public DateTimeOffset? CounterRecordedAtUtc { get; set; }
    public MoldCounterSourceType? CounterSource { get; set; }
    public string? CounterSourceReference { get; set; }
    public string? CounterRecordedBySubjectId { get; set; }
    public string? CounterNote { get; set; }
    public string? PrimaryProductReference { get; set; }
    public string? MaterialFamilyReference { get; set; }
    public string? RunnerType { get; set; }
    public bool? HotRunnerPresent { get; set; }
    public decimal? MoldWeightKg { get; set; }
    public decimal? DimensionLengthMm { get; set; }
    public decimal? DimensionWidthMm { get; set; }
    public decimal? DimensionHeightMm { get; set; }
    public string? Notes { get; set; }
    public string? CreateIdempotencyKey { get; set; }
    public string? CreateIdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public string UpdatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;
    public Guid? LinkedAiTaskId { get; set; }
    public string FollowUpStatus { get; set; } = MoldFollowUpStatuses.RequiresReview;
    public string? FollowUpUpdatedBy { get; set; }
}

public sealed class MoldFollowUpAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MoldId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public MoldRegistryEntry? Mold { get; set; }
}

public sealed class MoldLifecycleEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MoldId { get; set; }
    public MoldLifecycleState PreviousState { get; set; }
    public MoldLifecycleState NewState { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ActorSubjectId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? SourceReference { get; set; }
    public uint Version { get; set; } = 1;
}

public sealed class MoldCounterSnapshot
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MoldId { get; set; }
    public long ShotCount { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public MoldCounterSourceType SourceType { get; set; } = MoldCounterSourceType.Manual;
    public string? SourceReference { get; set; }
    public string? RecordedBySubjectId { get; set; }
    public string? Note { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;
}

public enum MoldOwnershipType
{
    CompanyOwned,
    CustomerOwned,
    SupplierOwned,
    Leased,
    Unknown,
}

public enum MoldLocationType
{
    Warehouse,
    ProductionArea,
    MaintenanceArea,
    TrialArea,
    ExternalCustomer,
    ExternalSupplier,
    Transit,
    Unknown,
}

public enum MoldSourceType
{
    Manual,
    Imported,
    Erp,
    Unknown,
}

public enum MoldCounterSourceType
{
    Manual,
    Production,
    Edge,
    Erp,
    Imported,
    Unknown,
}

public enum MoldLifecycleState
{
    Draft,
    Active,
    Maintenance,
    Trial,
    Quarantined,
    Retired,
    Scrapped,
}
