namespace NevaxisFactOS.Persistence.Material;

public sealed class MaterialRegistryEntry
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string CodeNormalized { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MaterialCategory MaterialCategory { get; set; } = MaterialCategory.Other;
    public PolymerFamily PolymerFamily { get; set; } = PolymerFamily.Unknown;
    public string? Manufacturer { get; set; }
    public string? TradeNameOrGrade { get; set; }
    public string? ColorName { get; set; }
    public string? ColorCode { get; set; }
    public PhysicalForm PhysicalForm { get; set; } = PhysicalForm.Granule;
    public MaterialLifecycleStatus LifecycleStatus { get; set; } = MaterialLifecycleStatus.Draft;
    public string UnitOfMeasure { get; set; } = "kg";
    public bool IsActive { get; set; } = true;
    public MaterialSourceType SourceType { get; set; } = MaterialSourceType.Manual;
    public string? SourceSystem { get; set; }
    public string? SourceReference { get; set; }
    public string? ExternalMaterialCode { get; set; }
    public DateTimeOffset? DataRecordedAtUtc { get; set; }
    public string? CreateIdempotencyKey { get; set; }
    public string? CreateIdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public string UpdatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;

    public MaterialTechnicalSpecification? TechnicalSpecification { get; set; }
}

public sealed class MaterialTechnicalSpecification
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal? DensityGPerCm3 { get; set; }
    public decimal? MeltFlowRate { get; set; }
    public string? MeltFlowUnit { get; set; }
    public decimal? MeltFlowTestTemperatureC { get; set; }
    public decimal? MeltFlowTestLoadKg { get; set; }
    public decimal? MoistureLimitPercent { get; set; }
    public bool? DryingRequired { get; set; }
    public decimal? DryingTemperatureMinC { get; set; }
    public decimal? DryingTemperatureMaxC { get; set; }
    public decimal? DryingTimeMinHours { get; set; }
    public decimal? DryingTimeMaxHours { get; set; }
    public decimal? MeltTemperatureMinC { get; set; }
    public decimal? MeltTemperatureMaxC { get; set; }
    public decimal? MoldTemperatureMinC { get; set; }
    public decimal? MoldTemperatureMaxC { get; set; }
    public decimal? StorageTemperatureMinC { get; set; }
    public decimal? StorageTemperatureMaxC { get; set; }
    public decimal? MaximumRegrindPercent { get; set; }
    public string? TechnicalNotes { get; set; }
    public string? SafetyDataSheetReference { get; set; }
    public string? TechnicalDataSheetReference { get; set; }
    public MaterialSpecificationSource SpecificationSource { get; set; } = MaterialSpecificationSource.Unknown;
    public string? SpecificationSourceReference { get; set; }
    public DateTimeOffset? SpecificationRecordedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public string UpdatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;

    public MaterialRegistryEntry? Material { get; set; }
}

public sealed class MaterialLifecycleEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MaterialId { get; set; }
    public MaterialLifecycleStatus PreviousStatus { get; set; }
    public MaterialLifecycleStatus NewStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ActorSubjectId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? SourceReference { get; set; }
    public uint Version { get; set; } = 1;
}

public enum MaterialCategory
{
    VirginPolymer,
    Compound,
    Masterbatch,
    Additive,
    Filler,
    Regrind,
    Other,
}

public enum PolymerFamily
{
    Pp,
    Pe,
    Hdpe,
    Ldpe,
    Abs,
    Pa,
    Pom,
    Pc,
    Pmma,
    Pet,
    Pbt,
    Ps,
    Tpe,
    Tpu,
    Pvc,
    Other,
    Unknown,
}

public enum PhysicalForm
{
    Granule,
    Powder,
    Liquid,
    Flake,
    Pellet,
    Other,
}

public enum MaterialLifecycleStatus
{
    Draft,
    Active,
    Blocked,
    Obsolete,
}

public enum MaterialSourceType
{
    Manual,
    Imported,
    Erp,
    DevelopmentPilot,
    Unknown,
}

public enum MaterialSpecificationSource
{
    Manual,
    Imported,
    DevelopmentPilot,
    Unknown,
}
