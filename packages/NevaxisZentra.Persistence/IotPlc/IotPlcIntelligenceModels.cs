namespace NevaxisZentra.Persistence.IotPlc;

public sealed record IotPlcIntelligenceWorkspaceReadModel(
    int ApprovedProfileCount,
    int RecentConnectivityAssessmentCount,
    int RecentSignalAssessmentCount,
    string RuleSetVersion,
    string ProvenanceDisclaimer,
    bool HasDevelopmentPilotData);

public sealed record IotPlcQualityProfileReadModel(
    Guid Id,
    string ProfileCode,
    string DisplayName,
    int ExpectedObservationIntervalSeconds,
    int FreshnessToleranceSeconds,
    int StaleThresholdSeconds,
    int MaximumClockSkewToleranceSeconds,
    int MinimumExpectedObservationCount,
    decimal MinimumGoodQualityRatio,
    decimal MaximumBadQualityRatio,
    decimal MaximumUncertainQualityRatio,
    decimal MaximumSourceConflictRatio,
    bool SequenceGapEvaluationEnabled,
    string Status,
    string SourceProvenance,
    uint Version,
    bool IsPilotData);

public sealed record IotPlcQualityProfileListReadModel(
    IReadOnlyList<IotPlcQualityProfileReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record IotPlcConnectivityAssessmentReadModel(
    Guid Id,
    string TargetType,
    Guid TargetId,
    DateTimeOffset EvaluationWindowStartUtc,
    DateTimeOffset EvaluationWindowEndUtc,
    Guid? QualityProfileId,
    string EvidenceMode,
    string Outcome,
    string? LeadingDriverCode,
    string? LatestHealthState,
    decimal? HealthObservationAvailabilityRatio,
    int? FreshnessSeconds,
    DateTimeOffset EvaluatedAtUtc,
    string SourceProvenance,
    bool IsPilotData);

public sealed record IotPlcSignalQualityAssessmentReadModel(
    Guid Id,
    string TargetType,
    Guid TargetId,
    Guid? SignalDefinitionId,
    DateTimeOffset EvaluationWindowStartUtc,
    DateTimeOffset EvaluationWindowEndUtc,
    Guid? QualityProfileId,
    string EvidenceMode,
    string Outcome,
    string? LeadingDriverCode,
    int ActualObservationCount,
    int? ExpectedObservationCount,
    decimal? CompletenessRatio,
    decimal? GoodRatio,
    decimal? UncertainRatio,
    decimal? BadRatio,
    decimal? StaleRatio,
    decimal? SourceConflictRatio,
    int? FreshnessSeconds,
    int? MaxClockSkewSeconds,
    DateTimeOffset EvaluatedAtUtc,
    string SourceProvenance,
    bool IsPilotData);

public sealed record IotPlcIntelligenceDriverReadModel(
    Guid Id,
    string ReasonCode,
    int OutcomeSeverity,
    decimal ThresholdVariance,
    int AffectedObservationCount,
    string Explanation,
    bool IsLeadingDriver,
    int SortOrder);

public sealed record IotPlcAssessmentDetailReadModel<TAssessment>(
    TAssessment Assessment,
    IReadOnlyList<IotPlcIntelligenceDriverReadModel> Drivers);

public sealed record CreateIotPlcQualityProfileCommand(
    string ProfileCode,
    string DisplayName,
    int ExpectedObservationIntervalSeconds,
    int FreshnessToleranceSeconds,
    int StaleThresholdSeconds,
    int MaximumClockSkewToleranceSeconds,
    int MinimumExpectedObservationCount,
    decimal MinimumGoodQualityRatio,
    decimal MaximumBadQualityRatio,
    decimal MaximumUncertainQualityRatio,
    decimal MaximumSourceConflictRatio,
    bool SequenceGapEvaluationEnabled,
    string Status,
    string SourceProvenance);

public sealed record UpdateIotPlcQualityProfileCommand(
    string DisplayName,
    int ExpectedObservationIntervalSeconds,
    int FreshnessToleranceSeconds,
    int StaleThresholdSeconds,
    int MaximumClockSkewToleranceSeconds,
    int MinimumExpectedObservationCount,
    decimal MinimumGoodQualityRatio,
    decimal MaximumBadQualityRatio,
    decimal MaximumUncertainQualityRatio,
    decimal MaximumSourceConflictRatio,
    bool SequenceGapEvaluationEnabled,
    string Status,
    string SourceProvenance,
    uint ExpectedVersion);

public sealed record EvaluateIotPlcConnectivityCommand(
    string EndpointReference,
    DateTimeOffset EvaluationWindowStartUtc,
    DateTimeOffset EvaluationWindowEndUtc,
    DateTimeOffset EvaluationTimeUtc,
    string? ProfileReference,
    string EvidenceMode,
    string SourceProvenance,
    string? IdempotencyKey);

public sealed record EvaluateIotPlcSignalQualityCommand(
    string SignalReference,
    DateTimeOffset EvaluationWindowStartUtc,
    DateTimeOffset EvaluationWindowEndUtc,
    DateTimeOffset EvaluationTimeUtc,
    string? ProfileReference,
    string EvidenceMode,
    string SourceProvenance,
    string? IdempotencyKey);

public sealed class IotPlcIntelligenceWriteResult
{
    public bool Succeeded { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public bool IdempotentReplay { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static IotPlcIntelligenceWriteResult Success(Guid entityId, uint? version = null, bool idempotentReplay = false) =>
        new() { Succeeded = true, EntityId = entityId, Version = version, IdempotentReplay = idempotentReplay };

    public static IotPlcIntelligenceWriteResult Failure(string failureCode, string? failureReason) =>
        new() { Succeeded = false, FailureCode = failureCode, FailureReason = failureReason };
}
