namespace NevaxisZentra.Persistence.FactoryIntelligence;

public sealed record FactorySignalWindowInput(
    Guid TenantId,
    Guid FacilityId,
    string ResourceType,
    string ResourceId,
    string SignalType,
    decimal MeasuredValue,
    DateTimeOffset ObservedAtUtc,
    string DataQuality = FactoryIntelligenceDataQualityStates.Good,
    IReadOnlyList<string>? SourceReferences = null);

public sealed record FactoryBaselineResult(
    string SignalType,
    decimal Reference,
    decimal Current,
    decimal DeviationPercent,
    string Trend,
    string DataQuality,
    string BaselineSource);

public sealed record FactoryRiskDetectionInput(
    Guid TenantId,
    Guid FacilityId,
    string MachineCode,
    string? OrderReference,
    string? MaterialCode,
    IReadOnlyList<FactorySignalWindowInput> Signals,
    IReadOnlyList<FactoryBaselineResult> Baselines,
    DateTimeOffset EvaluatedAtUtc);

public sealed record FactoryRiskDetectionResult(
    string RiskType,
    string RiskState,
    string AffectedOutcome,
    string ConfidenceBand,
    string Horizon,
    string Severity,
    string? TimeToImpactLabel,
    int? ProjectedGapQuantity,
    IReadOnlyList<string> ContributingSignals,
    IReadOnlyList<string> Explanation,
    IReadOnlyList<string> Assumptions,
    string DataQuality,
    string CorrelationKey);

public sealed record FactoryIntelligenceEvaluationRequest(
    Guid TenantId,
    Guid FacilityId,
    string MachineCode,
    string? OrderReference,
    string? MaterialCode,
    IReadOnlyList<FactorySignalWindowInput> Signals,
    DateTimeOffset EvaluatedAtUtc);

public sealed record FactoryIntelligenceEvaluationResult(
    IReadOnlyList<FactoryPredictiveSignal> Signals,
    IReadOnlyList<FactoryOperationalBaseline> Baselines,
    IReadOnlyList<FactoryPredictedRisk> Risks,
    IReadOnlyList<FactoryRiskDetectionResult> Detections,
    bool CaseCreatedOrUpdated,
    Guid? CaseId);

public sealed record FactoryIntelligenceOverview(
    int ActivePredictions,
    int WatchCount,
    int WarningCount,
    int CriticalCount,
    int AvertedLast24h,
    int ConfirmedLast24h,
    int FalseAlarmsLast24h,
    double AverageLeadTimeMinutes,
    string DataQualitySummary);

public sealed record FactoryIntelligenceRiskCard(
    Guid RiskId,
    Guid? LinkedCaseId,
    string RiskType,
    string RiskState,
    string MachineCode,
    string? OrderReference,
    string ObservedSummary,
    string PredictedSummary,
    string TimeToImpactLabel,
    string ConfidenceBand,
    string Horizon,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> RecommendedActions,
    string DataQuality,
    DateTimeOffset GeneratedAtUtc);

public sealed record FactoryIntelligenceRiskDetail(
    Guid RiskId,
    string RiskType,
    string RiskState,
    string MachineCode,
    string? OrderReference,
    IReadOnlyList<string> ObservedFacts,
    IReadOnlyList<string> PredictedOutcomes,
    IReadOnlyList<string> Why,
    IReadOnlyList<string> AtRiskOutcomes,
    string TimeToImpactLabel,
    string ConfidenceBand,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> RecommendedActions,
    IReadOnlyList<string> SimilarVerifiedCases,
    IReadOnlyList<string> Assumptions,
    string DataQuality,
    string Horizon,
    Guid? LinkedCaseId,
    DateTimeOffset GeneratedAtUtc);

public sealed record FactoryIntelligenceCalibrationSummary(
    string RiskType,
    int PredictionCount,
    int ConfirmedRisks,
    int AvertedRisks,
    int FalseAlarms,
    int MissedRisks,
    double AverageLeadTimeMinutes,
    string AverageConfidenceBand);
