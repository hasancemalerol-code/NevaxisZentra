using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NevaxisFactOS.Persistence.AiDecisions;

public static partial class AiDecisionPayloadValidator
{
    public const int MaximumEvidenceCount = 20;
    public const int MaximumAlternativeCount = 10;
    public const int MaximumRecommendationSummaryLength = 500;
    public const int MaximumEvidenceBytes = 16_384;
    public const int MaximumAlternativesBytes = 8_192;
    public const int MaximumPolicyBytes = 4_096;

    private static readonly HashSet<string> PolicyRuleOutcomes =
        new HashSet<string>(["Pass", "Fail", "Review"], StringComparer.Ordinal);
    private static readonly HashSet<string> ReviewLevels =
        new HashSet<string>(["None", "Human", "Supervisor"], StringComparer.Ordinal);

    public static bool TrySerialize(
        AiDecisionWriteRequest request,
        DateTimeOffset createdAtUtc,
        out string evidenceJson,
        out string? alternativesJson,
        out string? policyJson,
        out string? failureCode)
    {
        evidenceJson = "[]";
        alternativesJson = null;
        policyJson = null;
        failureCode = ValidateCore(request, createdAtUtc);
        if (failureCode is not null)
        {
            return false;
        }

        evidenceJson = JsonSerializer.Serialize(request.Evidence);
        alternativesJson = request.Alternatives is null
            ? null
            : JsonSerializer.Serialize(request.Alternatives);
        policyJson = request.PolicySnapshot is null
            ? null
            : JsonSerializer.Serialize(request.PolicySnapshot);
        if (Encoding.UTF8.GetByteCount(evidenceJson) > MaximumEvidenceBytes)
        {
            failureCode = "ai_decision.evidence.too_large";
            return false;
        }

        if (alternativesJson is not null
            && Encoding.UTF8.GetByteCount(alternativesJson) > MaximumAlternativesBytes)
        {
            failureCode = "ai_decision.alternatives.too_large";
            return false;
        }

        if (policyJson is not null && Encoding.UTF8.GetByteCount(policyJson) > MaximumPolicyBytes)
        {
            failureCode = "ai_decision.policy.too_large";
            return false;
        }

        failureCode = null;
        return true;
    }

    private static string? ValidateCore(AiDecisionWriteRequest request, DateTimeOffset createdAtUtc)
    {
        if (!IsStableKey(request.DecisionType, 150)
            || !IsStableKey(request.CapabilityKey, 150)
            || !IsStableKey(request.RecommendationCode, 100))
        {
            return "ai_decision.contract_key.invalid";
        }

        if (!IsBoundedIdentifier(request.ModelProvider, 100)
            || !IsBoundedIdentifier(request.ModelName, 150)
            || request.ModelVersion?.Length > 100
            || request.ConfigurationFingerprint?.Length > 128)
        {
            return "ai_decision.provenance.invalid";
        }

        if (string.IsNullOrWhiteSpace(request.RecommendationSummary)
            || request.RecommendationSummary.Length > MaximumRecommendationSummaryLength)
        {
            return "ai_decision.recommendation_summary.invalid";
        }

        if (!IsConfidence(request.ConfidenceScore))
        {
            return "ai_decision.confidence.invalid";
        }

        if (request.RiskLevel is AiDecisionRiskLevel.High or AiDecisionRiskLevel.Critical
            && !request.HumanReviewRequired
            || request.Status == AiDecisionStatus.ReviewRequired && !request.HumanReviewRequired)
        {
            return "ai_decision.human_review.required";
        }

        if (request.Evidence.Count is < 1 or > MaximumEvidenceCount
            || request.Evidence.Any(ValidateEvidence))
        {
            return "ai_decision.evidence.invalid";
        }

        if (request.Alternatives is { Count: > MaximumAlternativeCount }
            || request.Alternatives?.Any(ValidateAlternative) == true)
        {
            return "ai_decision.alternatives.invalid";
        }

        if (request.PolicySnapshot is { } policy && ValidatePolicy(policy))
        {
            return "ai_decision.policy.invalid";
        }

        if (createdAtUtc.Offset != TimeSpan.Zero
            || request.ExpiresAtUtc is { } expiresWithOffset
                && expiresWithOffset.Offset != TimeSpan.Zero)
        {
            return "ai_decision.timestamp.not_utc";
        }

        if (request.ExpiresAtUtc is { } expiresAt && expiresAt <= createdAtUtc)
        {
            return "ai_decision.expiry.invalid";
        }

        if (request.FailureCode is { Length: > 100 }
            || request.FailureCode is not null && !FailureCodePattern().IsMatch(request.FailureCode))
        {
            return "ai_decision.failure_code.invalid";
        }

        return null;
    }

    private static bool ValidateEvidence(AiDecisionEvidence evidence) =>
        !IsStableKey(evidence.EvidenceType, 100)
        || !IsBounded(evidence.ReferenceId, 200)
        || !IsStableKey(evidence.SourceSystem, 100)
        || evidence.ObservedAtUtc.Offset != TimeSpan.Zero
        || !IsBounded(evidence.Version, 100)
        || evidence.Checksum?.Length > 128
        || evidence.Summary?.Length > 300;

    private static bool ValidateAlternative(AiDecisionAlternative alternative) =>
        !IsStableKey(alternative.Code, 100)
        || !IsBounded(alternative.Summary, 300)
        || !IsConfidence(alternative.Confidence)
        || alternative.RejectionReasonCode is not null
            && !IsStableKey(alternative.RejectionReasonCode, 100);

    private static bool ValidatePolicy(AiDecisionPolicySnapshot policy) =>
        !IsStableKey(policy.PolicyKey, 150)
        || !IsBounded(policy.PolicyVersion, 100)
        || !ReviewLevels.Contains(policy.RequiredReviewLevel)
        || !IsStableKey(policy.PermissionKey, 150)
        || !PolicyRuleOutcomes.Contains(policy.RuleOutcome);

    private static bool IsConfidence(decimal? value) => value is null or >= 0m and <= 1m;

    private static bool IsStableKey(string value, int maximumLength) =>
        IsBounded(value, maximumLength) && StableKeyPattern().IsMatch(value);

    private static bool IsBoundedIdentifier(string value, int maximumLength) =>
        IsBounded(value, maximumLength) && IdentifierPattern().IsMatch(value);

    private static bool IsBounded(string value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength;

    [GeneratedRegex("^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableKeyPattern();

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._:-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierPattern();

    [GeneratedRegex("^[a-z0-9][a-z0-9._-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex FailureCodePattern();
}
