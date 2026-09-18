namespace NevaxisZentra.Persistence.IotPlc;

public sealed class IotPlcQualityProfile
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ProfileCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int ExpectedObservationIntervalSeconds { get; set; }
    public int FreshnessToleranceSeconds { get; set; }
    public int StaleThresholdSeconds { get; set; }
    public int MaximumClockSkewToleranceSeconds { get; set; }
    public int MinimumExpectedObservationCount { get; set; }
    public decimal MinimumGoodQualityRatio { get; set; }
    public decimal MaximumBadQualityRatio { get; set; }
    public decimal MaximumUncertainQualityRatio { get; set; }
    public decimal MaximumSourceConflictRatio { get; set; }
    public bool SequenceGapEvaluationEnabled { get; set; }
    public string Status { get; set; } = IotPlcQualityProfileStatuses.Approved;
    public string SourceProvenance { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

public sealed class IotPlcConnectivityAssessment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string TargetType { get; set; } = IotPlcIntelligenceTargetTypes.Endpoint;
    public Guid TargetId { get; set; }
    public DateTimeOffset EvaluationWindowStartUtc { get; set; }
    public DateTimeOffset EvaluationWindowEndUtc { get; set; }
    public Guid? QualityProfileId { get; set; }
    public string EvidenceMode { get; set; } = IotPlcIntelligenceEvidenceModes.Simulated;
    public string Outcome { get; set; } = IotPlcIntelligenceOutcomes.InsufficientData;
    public string? LeadingDriverCode { get; set; }
    public Guid? LeadingDriverId { get; set; }
    public string? LatestHealthState { get; set; }
    public decimal? HealthObservationAvailabilityRatio { get; set; }
    public int? FreshnessSeconds { get; set; }
    public string RuleSetVersion { get; set; } = IotPlcIntelligenceConstants.RuleSetVersion;
    public string SourceProvenance { get; set; } = string.Empty;
    public string? EvaluateIdempotencyKey { get; set; }
    public string? EvaluateIdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public sealed class IotPlcSignalQualityAssessment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string TargetType { get; set; } = IotPlcIntelligenceTargetTypes.Signal;
    public Guid TargetId { get; set; }
    public Guid? SignalDefinitionId { get; set; }
    public Guid? EndpointId { get; set; }
    public DateTimeOffset EvaluationWindowStartUtc { get; set; }
    public DateTimeOffset EvaluationWindowEndUtc { get; set; }
    public Guid? QualityProfileId { get; set; }
    public string EvidenceMode { get; set; } = IotPlcIntelligenceEvidenceModes.Simulated;
    public string Outcome { get; set; } = IotPlcIntelligenceOutcomes.InsufficientData;
    public string? LeadingDriverCode { get; set; }
    public Guid? LeadingDriverId { get; set; }
    public int ActualObservationCount { get; set; }
    public int? ExpectedObservationCount { get; set; }
    public decimal? CompletenessRatio { get; set; }
    public int GoodCount { get; set; }
    public int UncertainCount { get; set; }
    public int BadCount { get; set; }
    public int StaleCount { get; set; }
    public int SourceConflictCount { get; set; }
    public decimal? GoodRatio { get; set; }
    public decimal? UncertainRatio { get; set; }
    public decimal? BadRatio { get; set; }
    public decimal? StaleRatio { get; set; }
    public decimal? SourceConflictRatio { get; set; }
    public DateTimeOffset? LatestValidObservedAtUtc { get; set; }
    public int? FreshnessSeconds { get; set; }
    public int? MaxClockSkewSeconds { get; set; }
    public string RuleSetVersion { get; set; } = IotPlcIntelligenceConstants.RuleSetVersion;
    public string SourceProvenance { get; set; } = string.Empty;
    public string? EvaluateIdempotencyKey { get; set; }
    public string? EvaluateIdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public sealed class IotPlcIntelligenceDriver
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string AssessmentKind { get; set; } = string.Empty;
    public Guid AssessmentId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public int OutcomeSeverity { get; set; }
    public decimal ThresholdVariance { get; set; }
    public int AffectedObservationCount { get; set; }
    public string StructuredEvidenceJson { get; set; } = "{}";
    public string Explanation { get; set; } = string.Empty;
    public bool IsLeadingDriver { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
