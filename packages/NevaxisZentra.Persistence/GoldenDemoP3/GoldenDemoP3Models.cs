namespace NevaxisZentra.Persistence.GoldenDemoP3;

public sealed record OutcomeSnapshot(
    Guid ProductionEventId,
    string MachineCode,
    string InterventionStatus,
    string OutcomeStatus,
    string VerificationConfidence,
    ValidationWindowModel ValidationWindow,
    MeasurementWindowModel BaselineWindow,
    MeasurementWindowModel? ValidationMeasurementWindow,
    OutcomeHeroModel Hero,
    IReadOnlyList<MetricComparisonModel> MetricComparisons,
    IReadOnlyList<VerificationCheckModel> VerificationChecks,
    IReadOnlyList<string> InsufficientEvidenceReasons,
    EnergyImpactModel EnergyImpact,
    BusinessImpactClosureModel BusinessImpact,
    OutcomeTraceabilityModel Traceability);

public sealed record ValidationWindowModel(
    string Status,
    string StatusLabel,
    DateTimeOffset? ValidationStartedAtUtc,
    int? SamplesCollected,
    int? MinimumSamplesRequired,
    string EvidenceCompleteness,
    string Summary,
    bool ReadyForVerification);

public sealed record MeasurementWindowModel(
    string WindowKind,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    int GoodParts,
    int RejectParts,
    string SourceLabel,
    string ProductionContext,
    decimal? CycleSeconds,
    decimal? GoodPartsPerHour,
    decimal? AvailabilityPercent,
    int MicroStopCount,
    decimal? KwhPerGoodPart);

public sealed record OutcomeHeroModel(
    string Headline,
    string Subheadline,
    int RecoveredGoodPartsPerShift,
    string RecoveredGoodPartsClassification,
    string VerificationResult,
    bool ShowSuccessHero,
    IReadOnlyList<OutcomeHeroMetricModel> Highlights);

public sealed record OutcomeHeroMetricModel(
    string Label,
    string BeforeValue,
    string AfterValue,
    string DeltaLabel,
    string Status);

public sealed record MetricComparisonModel(
    string MetricCode,
    string Label,
    string? BeforeValue,
    string? AfterValue,
    string? DeltaLabel,
    string Status,
    string Classification);

public sealed record VerificationCheckModel(
    string Code,
    string Label,
    bool Passed,
    string? MeasuredValue,
    string? ReferenceValue,
    string Status,
    string Explanation,
    string Classification);

public sealed record EnergyImpactModel(
    bool HasData,
    string Classification,
    decimal? BeforeKwhPerGoodPart,
    decimal? AfterKwhPerGoodPart,
    decimal? ChangePercent,
    string Summary,
    string RelationshipNote);

public sealed record BusinessImpactClosureModel(
    int OperationalGoodPartsPerShift,
    string OperationalClassification,
    decimal CapacityImpactPercent,
    decimal? EstimatedBusinessImpactTry,
    string EstimatedBusinessClassification,
    decimal? ModeledVerifiedBusinessImpactTry,
    string ModeledVerifiedBusinessClassification,
    IReadOnlyList<BusinessImpactCategoryModel> Categories);

public sealed record BusinessImpactCategoryModel(
    string CategoryCode,
    string Label,
    string BeforeLabel,
    string AfterLabel,
    string ProtectedOrRecoveredLabel,
    string Classification);

public sealed record OutcomeTraceabilityModel(
    string? PlanReferenceId,
    string? VerificationReferenceId,
    string? InterventionReferenceId,
    IReadOnlyList<string> SourceAgentReferences,
    string WorkflowSummary);
