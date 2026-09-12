namespace NevaxisFactOS.Persistence.Energy;

public sealed record EnergyIntelligenceWorkspaceReadModel(
    int BaselineCount,
    int ActiveTargetCount,
    int RecentAssessmentCount,
    string RuleSetVersion,
    string ProvenanceDisclaimer,
    bool HasDevelopmentPilotData);

public sealed record EnergyBaselineProfileReadModel(
    Guid Id,
    string BaselineReference,
    string ComparisonDimension,
    string? MeasurementPointCode,
    Guid? MachineAssetId,
    string? ShiftReference,
    string? ProductionContextReference,
    int ReportingWindowDurationSeconds,
    int SampleCount,
    decimal BaselineConsumedKwh,
    decimal? BaselineMaximumDemandKw,
    decimal MedianCenter,
    decimal MadDispersion,
    decimal NormalizedDispersion,
    decimal RelativeTolerance,
    decimal UpperThresholdConsumedKwh,
    decimal? UpperThresholdDemandKw,
    string CalculationMethod,
    string EvidenceStatus,
    string RuleSetVersion,
    string Provenance,
    uint Version,
    bool IsPilotData);

public sealed record EnergyBaselineListReadModel(
    IReadOnlyList<EnergyBaselineProfileReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record EnergyBaselineListFilters(
    string? ComparisonDimension,
    string? MeasurementPointReference,
    int Page,
    int PageSize);

public sealed record EnergyTargetReadModel(
    Guid Id,
    string TargetReference,
    string TargetScope,
    string ScopeReference,
    string TargetMetric,
    decimal TargetValue,
    string Unit,
    decimal Tolerance,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset EffectiveToUtc,
    string SourceProvenance,
    string Status,
    uint Version,
    bool IsPilotData);

public sealed record EnergyTargetListReadModel(
    IReadOnlyList<EnergyTargetReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record EnergyTargetListFilters(
    string? TargetScope,
    string? TargetMetric,
    string? Status,
    int Page,
    int PageSize);

public sealed record EnergyIntelligenceAssessmentSummaryReadModel(
    Guid Id,
    string AssessmentReference,
    string ComparisonDimension,
    string? MeasurementPointCode,
    DateTimeOffset EvaluationWindowStartUtc,
    DateTimeOffset EvaluationWindowEndUtc,
    decimal? ActualConsumedKwh,
    decimal? ActualMaximumDemandKw,
    decimal? VarianceConsumedKwh,
    decimal? VariancePercent,
    string Outcome,
    string? LeadingDriverCode,
    decimal? PotentialReducibleKwh,
    decimal? EstimatedCostImpact,
    string? CostCurrencyCode,
    string? OpportunityLabel,
    string EvidenceStatus,
    DateTimeOffset EvaluatedAtUtc,
    bool IsPilotData);

public sealed record EnergyIntelligenceAssessmentDetailReadModel(
    EnergyIntelligenceAssessmentSummaryReadModel Summary,
    string? BaselineReference,
    string? TargetReference,
    IReadOnlyList<EnergyIntelligenceDriverReadModel> Drivers);

public sealed record EnergyIntelligenceAssessmentListReadModel(
    IReadOnlyList<EnergyIntelligenceAssessmentSummaryReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record EnergyIntelligenceAssessmentListFilters(
    string? ComparisonDimension,
    string? Outcome,
    string? MeasurementPointReference,
    int Page,
    int PageSize);

public sealed record EnergyIntelligenceDriverReadModel(
    Guid Id,
    string ReasonCode,
    string LabelTr,
    string Severity,
    string Category,
    string? ActualValue,
    string? ActualUnit,
    string? ComparisonValue,
    string? ComparisonUnit,
    decimal? NormalizedVariance,
    string Explanation,
    string EvidenceSource,
    string? FreshnessState,
    bool IsLeadingDriver);

public sealed record EvaluateEnergyBaselineCommand(
    string ComparisonDimension,
    string MeasurementPointReference,
    int ReportingWindowDurationSeconds,
    string? ShiftReference,
    string? ProductionContextReference,
    decimal? RelativeTolerance,
    string? IdempotencyKey);

public sealed record CreateEnergyTargetCommand(
    string TargetReference,
    string TargetScope,
    string ScopeReference,
    string TargetMetric,
    decimal TargetValue,
    string Unit,
    decimal Tolerance,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset EffectiveToUtc,
    string SourceProvenance,
    string? IdempotencyKey);

public sealed record UpdateEnergyTargetCommand(
    string TargetReference,
    decimal TargetValue,
    decimal Tolerance,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset EffectiveToUtc,
    string SourceProvenance,
    string Status,
    uint ExpectedVersion);

public sealed record EvaluateEnergyIntelligenceAssessmentCommand(
    string AssessmentReference,
    string ComparisonDimension,
    string MeasurementPointReference,
    DateTimeOffset EvaluationWindowStartUtc,
    DateTimeOffset EvaluationWindowEndUtc,
    string? BaselineReference,
    string? ConsumptionTargetReference,
    string? DemandTargetReference,
    string? IdleTargetReference,
    string? IntensityTargetReference,
    string? ProductionContextReference,
    string? IdempotencyKey);
