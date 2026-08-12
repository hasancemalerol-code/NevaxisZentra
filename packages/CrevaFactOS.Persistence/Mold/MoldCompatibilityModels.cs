namespace CrevaFactOS.Persistence.Mold;

public sealed record MoldCompatibilityRequirementReadModel(
    Guid Id,
    Guid MoldId,
    decimal? RequiredClampForceTon,
    decimal? MoldWidthMm,
    decimal? MoldHeightMm,
    decimal? MoldDepthMm,
    decimal? RequiredTieBarWidthMm,
    decimal? RequiredTieBarHeightMm,
    decimal? MinimumShotCapacityGrams,
    string? RequiredInjectionUnit,
    string? SupportedMaterialGroup,
    decimal? NominalCycleTimeSeconds,
    int? EffectiveCavityCount,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record UpsertMoldCompatibilityRequirementCommand(
    Guid MoldId,
    decimal? RequiredClampForceTon,
    decimal? MoldWidthMm,
    decimal? MoldHeightMm,
    decimal? MoldDepthMm,
    decimal? RequiredTieBarWidthMm,
    decimal? RequiredTieBarHeightMm,
    decimal? MinimumShotCapacityGrams,
    string? RequiredInjectionUnit,
    string? SupportedMaterialGroup,
    decimal? NominalCycleTimeSeconds,
    int? EffectiveCavityCount,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    uint? ExpectedVersion);

public sealed record MachineCompatibilityCapabilityReadModel(
    Guid Id,
    Guid MachineAssetId,
    string MachineCode,
    string MachineDisplayName,
    decimal? ClampForceTon,
    decimal? MaximumMoldWidthMm,
    decimal? MaximumMoldHeightMm,
    decimal? MinimumMoldDepthMm,
    decimal? MaximumMoldDepthMm,
    decimal? TieBarWidthMm,
    decimal? TieBarHeightMm,
    decimal? MaximumShotCapacityGrams,
    string? InjectionUnit,
    IReadOnlyList<string> SupportedMaterialGroups,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record UpsertMachineCompatibilityCapabilityCommand(
    Guid MachineAssetId,
    string MachineCode,
    string MachineDisplayName,
    decimal? ClampForceTon,
    decimal? MaximumMoldWidthMm,
    decimal? MaximumMoldHeightMm,
    decimal? MinimumMoldDepthMm,
    decimal? MaximumMoldDepthMm,
    decimal? TieBarWidthMm,
    decimal? TieBarHeightMm,
    decimal? MaximumShotCapacityGrams,
    string? InjectionUnit,
    IReadOnlyList<string>? SupportedMaterialGroups,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    uint? ExpectedVersion);

public sealed record MoldCompatibilityRuleResultReadModel(
    string RuleCode,
    string LabelTr,
    string Status,
    string? RequiredValue,
    string? AvailableValue,
    string Source,
    DateTimeOffset? DataTimestampUtc,
    bool IsCritical);

public sealed record MoldCompatibilityAssessmentReadModel(
    Guid Id,
    Guid MoldId,
    Guid MachineAssetId,
    string MachineCode,
    string MachineDisplayName,
    string Status,
    string RuleSetVersion,
    uint RequirementVersion,
    uint MachineProfileVersion,
    DateTimeOffset EvaluatedAtUtc,
    IReadOnlyList<MoldCompatibilityRuleResultReadModel> Reasons,
    IReadOnlyDictionary<string, string> Evidence,
    decimal? TheoreticalPartsPerHour,
    bool RequirementStale,
    bool MachineProfileStale,
    IReadOnlyList<string> MissingFields,
    bool IdempotentReplay);

public sealed record MoldCompatibilityOverviewReadModel(
    Guid MoldId,
    MoldCompatibilityRequirementReadModel? Requirement,
    int CompatibleCount,
    int ConditionalCount,
    int IncompatibleCount,
    int InsufficientDataCount,
    IReadOnlyList<MoldCompatibilityAssessmentSummaryReadModel> Machines);

public sealed record MoldCompatibilityAssessmentSummaryReadModel(
    Guid AssessmentId,
    Guid MachineAssetId,
    string MachineCode,
    string MachineDisplayName,
    string Status,
    string? CriticalConstraintLabel,
    bool RequirementStale,
    bool MachineProfileStale,
    decimal? TheoreticalPartsPerHour,
    DateTimeOffset EvaluatedAtUtc);

public sealed record EvaluateMoldMachineCompatibilityCommand(
    Guid MoldId,
    Guid MachineAssetId);

public sealed record EvaluateAllMoldMachineCompatibilityCommand(
    Guid MoldId);

public sealed record MoldCompatibilityEvaluationResult(
    bool Succeeded,
    string? FailureCode,
    string? FailureReason,
    MoldCompatibilityAssessmentReadModel? Assessment,
    bool IdempotentReplay);

public static class MoldCompatibilityFailureCodes
{
    public const string NotFound = "not-found";
    public const string ValidationFailed = "validation-failed";
    public const string ConcurrencyConflict = "concurrency-conflict";
    public const string MachineNotVisible = "machine-not-visible";
}
