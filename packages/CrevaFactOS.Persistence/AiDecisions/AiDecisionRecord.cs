namespace CrevaFactOS.Persistence.AiDecisions;

public sealed class AiDecisionRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? IdentitySubjectId { get; set; }
    public Guid? TenantMembershipId { get; set; }
    public required string DecisionType { get; set; }
    public required string CapabilityKey { get; set; }
    public required string ModelProvider { get; set; }
    public required string ModelName { get; set; }
    public string? ModelVersion { get; set; }
    public string? ConfigurationFingerprint { get; set; }
    public required string RecommendationCode { get; set; }
    public required string RecommendationSummary { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public AiDecisionRiskLevel RiskLevel { get; set; }
    public bool HumanReviewRequired { get; set; }
    public AiDecisionStatus Status { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid? CausationId { get; set; }
    public Guid AuditRecordId { get; set; }
    public required string InputEvidenceJson { get; set; }
    public string? AlternativesJson { get; set; }
    public string? PolicySnapshotJson { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public Guid? SupersedesDecisionId { get; set; }
    public string? FailureCode { get; set; }
}
