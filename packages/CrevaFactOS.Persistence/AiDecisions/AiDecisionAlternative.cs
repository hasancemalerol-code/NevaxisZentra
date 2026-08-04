using System.Text.Json.Serialization;

namespace CrevaFactOS.Persistence.AiDecisions;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AiDecisionAlternative(
    string Code,
    string Summary,
    AiDecisionRiskLevel RiskLevel,
    decimal? Confidence = null,
    string? RejectionReasonCode = null);
