using System.Text.Json.Serialization;

namespace CrevaFactOS.Persistence.AiDecisions;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AiDecisionWriteRequest(
    string DecisionType,
    string CapabilityKey,
    string ModelProvider,
    string ModelName,
    string RecommendationCode,
    string RecommendationSummary,
    AiDecisionRiskLevel RiskLevel,
    bool HumanReviewRequired,
    AiDecisionStatus Status,
    IReadOnlyList<AiDecisionEvidence> Evidence,
    string? ModelVersion = null,
    decimal? ConfidenceScore = null,
    IReadOnlyList<AiDecisionAlternative>? Alternatives = null,
    AiDecisionPolicySnapshot? PolicySnapshot = null,
    Guid? CausationId = null,
    DateTimeOffset? ExpiresAtUtc = null,
    Guid? SupersedesDecisionId = null,
    string? FailureCode = null,
    string? ConfigurationFingerprint = null);
