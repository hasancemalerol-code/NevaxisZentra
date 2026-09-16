using NevaxisFactOS.Persistence.MixedProduction;

namespace NevaxisFactOS.Persistence.ThroughputIntelligence;

public sealed record ThroughputMeasurementResult(
    decimal GoodUnitsPerHour,
    decimal GrossUnitsPerHour,
    decimal RejectUnitsPerHour,
    decimal ReworkUnitsPerHour,
    decimal? TargetGoodUnitsPerHour,
    decimal? ActualGoodUnitsPerHour,
    decimal? ThroughputGapPerHour,
    int GoodCount,
    int GrossCount,
    int RejectCount,
    int ReworkCount,
    double WindowHours,
    string DataQuality,
    string WindowLabel);

public sealed record TaktDemandResult(
    decimal? TaktTimeSeconds,
    decimal? TargetCycleSeconds,
    decimal? ActualEffectiveCycleSeconds,
    decimal? RequiredGoodQuantity,
    decimal? AvailableProductionHours,
    string DataQuality,
    string? InsufficientDataReason);

public sealed record StationCycleTimeResult(
    Guid StationId,
    string StationCode,
    string StationName,
    decimal? MedianCycleSeconds,
    decimal? VariationSeconds,
    decimal? TargetCycleSeconds,
    decimal? CycleGapSeconds,
    int SampleCount,
    string WindowLabel,
    string Freshness,
    string DataQuality);

public sealed record StationCapacityResult(
    Guid StationId,
    string StationCode,
    string StationName,
    string CapacityKind,
    decimal TheoreticalGoodUnitsPerHour,
    decimal AvailableGoodUnitsPerHour,
    decimal EffectiveGoodUnitsPerHour,
    decimal UtilizationPercent,
    string Explanation,
    string DataQuality);

public sealed record ConstraintEvidence(
    string EvidenceType,
    string Description,
    decimal? Value,
    string? Unit);

public sealed record ConstraintResult(
    string Role,
    Guid? StationId,
    string? StationCode,
    string? StationName,
    Guid? ResourceId,
    string? ResourceCode,
    string ConstraintType,
    IReadOnlyList<ConstraintEvidence> Evidence,
    decimal ImpactOnGoodThroughputPerHour,
    string Confidence,
    string DataQuality,
    string Explanation);

public sealed record LossTreeNode(
    string Category,
    string Label,
    decimal LostGoodUnitsPerHour,
    string AttributionQuality,
    Guid? StationId,
    string? StationCode,
    string? Explanation);

public sealed record LossTreeResult(
    IReadOnlyList<LossTreeNode> Nodes,
    decimal TotalGapPerHour,
    decimal AttributedLossPerHour,
    decimal UnattributedLossPerHour,
    string ReconciliationQuality,
    string DataQuality);

public sealed record MicrostopSummary(
    Guid StationId,
    string StationCode,
    int Count,
    double TotalLostSeconds,
    decimal EstimatedGoodThroughputImpactPerHour,
    string DataQuality);

public sealed record StarvedBlockedSummary(
    Guid StationId,
    string StationCode,
    string State,
    string? RelatedStationCode,
    double DurationSeconds,
    decimal EstimatedGoodThroughputImpactPerHour,
    string? BufferState,
    string Explanation,
    string DataQuality);

public sealed record BufferIntelligenceResult(
    Guid ResourceId,
    string BufferCode,
    int Capacity,
    int CurrentWip,
    int MinObserved,
    int MaxObserved,
    decimal UtilizationPercent,
    string StarvationExposure,
    string BlockingExposure,
    string FlowImbalance,
    string? UpstreamStationCode,
    string? DownstreamStationCode,
    string Assessment,
    string DataQuality);

public sealed record ProductionGapResult(
    decimal RequiredGoodPerHour,
    decimal CurrentGoodPerHour,
    decimal GapPerHour,
    IReadOnlyList<LossTreeNode> AttributedLosses,
    string AttributionQuality,
    string DataQuality);

public sealed record OperatorSkillEligibility(
    string OperatorCode,
    string DisplayName,
    int SkillLevel,
    bool IsQualified,
    bool IsExpired,
    IReadOnlyList<string> EligibleStationCodes,
    IReadOnlyList<string> EligibleOperationCodes);

public sealed record LineBalancingRecommendation(
    string OverloadedStationCode,
    string UnderloadedStationCode,
    IReadOnlyList<string> EligibleOperators,
    string RecommendationType,
    string Explanation,
    bool RespectsQualifications);

public sealed record ScenarioIntervention(
    string InterventionType,
    Guid? StationId,
    string? StationCode,
    string? OperatorCode,
    string? TargetStationCode,
    int? BufferCapacityDelta,
    decimal? CycleTimeDeltaSeconds,
    decimal? OperationRedistributionPercent,
    string? ResourceCode);

public sealed record ScenarioInputContract(
    Guid TenantId,
    Guid FacilityId,
    Guid LineId,
    string AnalysisWindow,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    IReadOnlyList<ScenarioIntervention> Interventions,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Guardrails,
    IReadOnlyList<string> RequestedOutputs);

public sealed record ScenarioMetricSnapshot(
    decimal PredictedGoodThroughputPerHour,
    decimal BaselineGoodThroughputPerHour,
    decimal DeltaGoodThroughputPerHour,
    decimal PredictedScrapRatePercent,
    decimal BaselineScrapRatePercent,
    decimal PredictedWipAverage,
    decimal BaselineWipAverage,
    decimal UtilizationPercent,
    ConstraintResult? ConstraintBefore,
    ConstraintResult? ConstraintAfter,
    decimal QualityImpactScore,
    decimal OperationalRiskScore,
    string Eligibility,
    string DataQuality,
    string Confidence,
    IReadOnlyList<string> LimitingFactors,
    IReadOnlyList<string> Assumptions);

public sealed record ScenarioComparisonRow(
    string ScenarioCode,
    string ScenarioType,
    string Title,
    decimal PredictedGoodThroughputPerHour,
    decimal DeltaGoodThroughputPerHour,
    string? ConstraintBeforeCode,
    string? ConstraintAfterCode,
    decimal QualityImpactScore,
    decimal OperationalRiskScore,
    string Eligibility,
    string DataQuality);

public sealed record ThroughputIntelligenceOverview(
    Guid LineId,
    string LineCode,
    string LineName,
    ThroughputMeasurementResult Measurement,
    TaktDemandResult Takt,
    ProductionGapResult ProductionGap,
    IReadOnlyList<ConstraintResult> Constraints,
    LossTreeResult LossTree,
    IReadOnlyList<StationCycleTimeResult> StationCycles,
    IReadOnlyList<StationCapacityResult> StationCapacities,
    IReadOnlyList<BufferIntelligenceResult> BufferIntelligence,
    IReadOnlyList<MicrostopSummary> Microstops,
    IReadOnlyList<StarvedBlockedSummary> StarvedBlocked,
    IReadOnlyList<ThroughputTopologyStationSnapshot> Topology,
    IReadOnlyList<OperatorSkillEligibility> OperatorSkills,
    IReadOnlyList<LineBalancingRecommendation> BalancingRecommendations,
    string DataQuality,
    long AnalysisDurationMs,
    int EventsConsidered,
    int StationsEvaluated,
    string EngineVersion);

public sealed record ThroughputTopologyStationSnapshot(
    Guid StationId,
    string Code,
    string Name,
    string State,
    decimal? EffectiveCycleSeconds,
    decimal? EffectiveGoodCapacityPerHour,
    decimal? TaktRelationPercent,
    int? Wip,
    string? OperatorCode,
    int GoodCount,
    int RejectCount,
    bool IsCurrentConstraint,
    bool IsSecondaryConstraint,
    string? ConstraintType);

public sealed record ThroughputAnalysisContext(
    ProductionLine Line,
    IReadOnlyList<ProductionStation> Stations,
    IReadOnlyDictionary<Guid, ManufacturingResource> ResourcesByStationId,
    IReadOnlyDictionary<Guid, BufferResourceState> BuffersByResourceId,
    IReadOnlyList<CanonicalProductionEvent> Events,
    LineThroughputTarget? Target,
    IReadOnlyList<OperatorSkillProfile> OperatorSkills,
    IReadOnlyList<OperatorStationQualification> Qualifications,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    string AnalysisWindow,
    decimal MicrostopThresholdSeconds = 30m);

public sealed record SimulationStationState(
    Guid StationId,
    string StationCode,
    string StationName,
    string ResourceType,
    decimal EffectiveCycleSeconds,
    decimal TargetCycleSeconds,
    int OperatorCount,
    int BufferCapacity,
    decimal YieldPercent,
    decimal MicrostopLossSecondsPerHour,
    decimal OperatorWaitSecondsPerHour,
    decimal MaterialWaitSecondsPerHour,
    decimal StarvedSecondsPerHour,
    decimal BlockedSecondsPerHour);
