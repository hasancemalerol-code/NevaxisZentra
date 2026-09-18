namespace NevaxisZentra.Persistence.OperationalRisk;

public sealed class OperationalRiskCase
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string CaseReference { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ScopeType { get; set; } = OperationalRiskScopeTypes.Facility;
    public string? ScopeReference { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EvidenceState { get; set; } = OperationalRiskEvidenceStates.InsufficientData;
    public int? LikelihoodLevel { get; set; }
    public int? ImpactLevel { get; set; }
    public int? RiskScore { get; set; }
    public string? SeverityBand { get; set; }
    public string AssessmentOutcome { get; set; } = OperationalRiskAssessmentOutcomes.InsufficientData;
    public string LifecycleStatus { get; set; } = OperationalRiskLifecycleStatuses.Detected;
    public string RuleSetVersion { get; set; } = OperationalRiskConstants.RuleSetVersion;
    public string? SourceModule { get; set; }
    public string? SourceReference { get; set; }
    public string SourceProvenance { get; set; } = string.Empty;
    public string? DetectionKey { get; set; }
    public int OccurrenceSequence { get; set; } = 1;
    public string? DetectionRuleCode { get; set; }
    public DateTimeOffset FirstDetectedAtUtc { get; set; }
    public DateTimeOffset LastObservedAtUtc { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? IdempotencyPayloadFingerprint { get; set; }
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    public ICollection<OperationalRiskEvidence> Evidence { get; set; } = [];
    public ICollection<OperationalRiskStatusHistory> StatusHistory { get; set; } = [];
}

public sealed class OperationalRiskEvidence
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid CaseId { get; set; }
    public string EvidenceTypeCode { get; set; } = string.Empty;
    public string EvidenceState { get; set; } = string.Empty;
    public DateTimeOffset ObservedAtUtc { get; set; }
    public string? SourceModule { get; set; }
    public string? SourceReference { get; set; }
    public string Summary { get; set; } = string.Empty;
    public decimal? NumericValue { get; set; }
    public string? Unit { get; set; }
    public string? ComparisonReference { get; set; }
    public string SourceProvenance { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public string? IdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public OperationalRiskCase? Case { get; set; }
}

public sealed class OperationalRiskStatusHistory
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid CaseId { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? ReasonNote { get; set; }
    public string ActorSubjectId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public uint ExpectedCaseVersion { get; set; }
    public string SourceProvenance { get; set; } = string.Empty;

    public OperationalRiskCase? Case { get; set; }
}

public sealed class OperationalRiskAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string ActorSubjectId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid? CorrelationId { get; set; }
}
