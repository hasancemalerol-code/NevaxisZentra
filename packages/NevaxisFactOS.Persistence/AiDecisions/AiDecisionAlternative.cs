using System.Text.Json.Serialization;

namespace NevaxisFactOS.Persistence.AiDecisions;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AiDecisionAlternative(
    string Code,
    string Summary,
    AiDecisionRiskLevel RiskLevel,
    decimal? Confidence = null,
    string? RejectionReasonCode = null);
