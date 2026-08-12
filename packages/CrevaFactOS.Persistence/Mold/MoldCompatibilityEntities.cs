namespace CrevaFactOS.Persistence.Mold;

public sealed class MoldCompatibilityRequirement
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MoldId { get; set; }
    public decimal? RequiredClampForceTon { get; set; }
    public decimal? MoldWidthMm { get; set; }
    public decimal? MoldHeightMm { get; set; }
    public decimal? MoldDepthMm { get; set; }
    public decimal? RequiredTieBarWidthMm { get; set; }
    public decimal? RequiredTieBarHeightMm { get; set; }
    public decimal? MinimumShotCapacityGrams { get; set; }
    public string? RequiredInjectionUnit { get; set; }
    public string? SupportedMaterialGroup { get; set; }
    public decimal? NominalCycleTimeSeconds { get; set; }
    public int? EffectiveCavityCount { get; set; }
    public string SourceType { get; set; } = MoldSourceTypes.Manual;
    public string? SourceReference { get; set; }
    public DateTimeOffset? DataRecordedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string UpdatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; }
}

public sealed class MachineCompatibilityCapabilityProfile
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MachineAssetId { get; set; }
    public string MachineCode { get; set; } = string.Empty;
    public string MachineDisplayName { get; set; } = string.Empty;
    public decimal? ClampForceTon { get; set; }
    public decimal? MaximumMoldWidthMm { get; set; }
    public decimal? MaximumMoldHeightMm { get; set; }
    public decimal? MinimumMoldDepthMm { get; set; }
    public decimal? MaximumMoldDepthMm { get; set; }
    public decimal? TieBarWidthMm { get; set; }
    public decimal? TieBarHeightMm { get; set; }
    public decimal? MaximumShotCapacityGrams { get; set; }
    public string? InjectionUnit { get; set; }
    public string? SupportedMaterialGroups { get; set; }
    public string SourceType { get; set; } = MoldSourceTypes.Manual;
    public string? SourceReference { get; set; }
    public DateTimeOffset? DataRecordedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string UpdatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; }
}

public sealed class MoldMachineCompatibilityAssessment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MoldId { get; set; }
    public Guid MachineAssetId { get; set; }
    public string Status { get; set; } = MoldCompatibilityStatuses.InsufficientData;
    public string RuleSetVersion { get; set; } = MoldCompatibilityConstants.RuleSetVersion;
    public uint RequirementVersion { get; set; }
    public uint MachineProfileVersion { get; set; }
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public string StructuredReasonsJson { get; set; } = "[]";
    public string StructuredEvidenceJson { get; set; } = "{}";
    public decimal? TheoreticalPartsPerHour { get; set; }
    public string InputFingerprint { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
}

public static class MoldCompatibilityConstants
{
    public const string RuleSetVersion = "MOLD-MACHINE-COMPAT-V1";
    public const int ManualProfileStaleAfterDays = 365;
}

public static class MoldCompatibilityStatuses
{
    public const string Compatible = "compatible";
    public const string Conditional = "conditional";
    public const string Incompatible = "incompatible";
    public const string InsufficientData = "insufficient-data";
}

public static class MoldCompatibilityRuleStatuses
{
    public const string Pass = "pass";
    public const string Warn = "warn";
    public const string Fail = "fail";
    public const string Unknown = "unknown";
}

public static class MoldSourceTypes
{
    public const string Manual = "manual";
    public const string Imported = "imported";
    public const string Erp = "erp";
    public const string Unknown = "unknown";
}
