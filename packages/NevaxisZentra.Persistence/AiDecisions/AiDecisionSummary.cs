namespace NevaxisZentra.Persistence.AiDecisions;

public sealed record AiDecisionSummary(
    Guid Id,
    string DecisionType,
    string CapabilityKey,
    string RecommendationCode,
    string RecommendationSummary,
    decimal? ConfidenceScore,
    AiDecisionRiskLevel RiskLevel,
    bool HumanReviewRequired,
    AiDecisionStatus Status,
    Guid CorrelationId,
    DateTimeOffset CreatedAtUtc);
