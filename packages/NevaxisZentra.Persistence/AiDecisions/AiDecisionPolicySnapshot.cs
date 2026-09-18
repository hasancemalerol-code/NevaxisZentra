using System.Text.Json.Serialization;

namespace NevaxisZentra.Persistence.AiDecisions;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AiDecisionPolicySnapshot(
    string PolicyKey,
    string PolicyVersion,
    string RequiredReviewLevel,
    string PermissionKey,
    string RuleOutcome,
    decimal? ThresholdValue = null);
