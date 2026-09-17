namespace NevaxisFactOS.Persistence.RealFactoryPilot;

public sealed record FactoryFieldMappingRule(
    string SourceField,
    string SourceType,
    string TargetEntity,
    string? TargetEntityId,
    string CanonicalEventType,
    string? Transformation,
    string? Unit,
    decimal? Scale,
    string? QualityRule,
    string TimestampRule);

public sealed record FactoryDiscoveryField(
    string SourceField,
    string SourceType,
    string? SampleValue,
    string? InferredCanonicalEventType);

public sealed record FactoryDiscoveryResult(
    Guid ConnectorId,
    string ConnectorName,
    IReadOnlyList<FactoryDiscoveryField> Fields,
    DateTimeOffset DiscoveredAtUtc);

public sealed record FactoryDataQualityAssessmentResult(
    string OverallState,
    decimal CompletenessScore,
    decimal FreshnessScore,
    decimal ConsistencyScore,
    decimal ValidityScore,
    decimal SequenceIntegrityScore,
    decimal ClockIntegrityScore,
    decimal MappingConfidenceScore,
    IReadOnlyList<string> Issues,
    bool BlocksAutonomousAction);

public sealed record FactoryShadowRecommendation(
    string RecommendationType,
    string Summary,
    string? PredictedConstraintStation,
    decimal? PredictedGoodThroughputPerHour,
    decimal? PredictedDeltaPercent,
    IReadOnlyList<string> EvidenceRefs,
    DateTimeOffset GeneratedAtUtc);

public sealed record FactoryShadowAccuracySnapshot(
    string PredictedConstraintStation,
    string? ObservedConstraintStation,
    decimal? PredictedGoodThroughputPerHour,
    decimal? ActualGoodThroughputPerHour,
    decimal? PredictedDeltaPercent,
    decimal? ActualDeltaPercent,
    bool ConstraintMatched,
    bool ThroughputWithinTolerance);

public sealed record RealFactoryPilotWorkspace(
    Guid PilotRunId,
    Guid LineId,
    string LineCode,
    string CurrentPhase,
    string AcceptanceKind,
    IReadOnlyList<ConnectorWorkspaceSnapshot> Connectors,
    FactoryDataQualityAssessmentResult? DataQuality,
    FactoryShadowSessionSnapshot? Shadow,
    Guid? ActiveCaseId,
    FactoryShadowAccuracySnapshot? ShadowAccuracy,
    PilotEvidencePackSnapshot? EvidencePack,
    DateTimeOffset UpdatedAtUtc);

public sealed record ConnectorWorkspaceSnapshot(
    Guid ConnectorId,
    string Name,
    string ConnectorKind,
    string SourceSystem,
    string HealthStatus,
    string MappingStatus,
    DateTimeOffset? LastEventReceivedAtUtc,
    int EventsIngested);

public sealed record FactoryShadowSessionSnapshot(
    Guid Id,
    Guid LineId,
    string Mode,
    bool IsActive,
    bool BlocksPhysicalActions,
    DateTimeOffset StartedAtUtc);

public sealed record PilotEvidencePackSnapshot(
    Guid Id,
    string FacilityCode,
    string LineCode,
    DateTimeOffset PilotPeriodStartUtc,
    DateTimeOffset PilotPeriodEndUtc,
    string DataQualityState,
    decimal BaselineGoodThroughputPerHour,
    decimal? PostInterventionGoodThroughputPerHour,
    string? IdentifiedConstraint,
    string? VerificationOutcome,
    bool KnowledgePromoted,
    string SummaryJson);

public sealed record RealFactoryGoldenPilotResult(
    Guid PilotRunId,
    Guid LineId,
    string FinalPhase,
    string AcceptanceKind,
    string VerificationOutcome,
    bool KnowledgePromoted,
    bool EvidencePackGenerated,
    string Message);
