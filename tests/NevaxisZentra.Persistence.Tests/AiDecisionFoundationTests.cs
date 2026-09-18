using System.Text.Json;
using NevaxisZentra.Persistence.AiDecisions;

namespace NevaxisZentra.Persistence.Tests;

public sealed class AiDecisionFoundationTests
{
    [Theory]
    [InlineData(AiDecisionRiskLevel.High)]
    [InlineData(AiDecisionRiskLevel.Critical)]
    public void HighRiskRequiresHumanReview(AiDecisionRiskLevel risk)
    {
        var accepted = TrySerialize(CreateRequest() with
        {
            RiskLevel = risk,
            HumanReviewRequired = false,
        }, out var failure);

        Assert.False(accepted);
        Assert.Equal("ai_decision.human_review.required", failure);
    }

    [Theory]
    [InlineData(-0.0001)]
    [InlineData(1.0001)]
    public void ConfidenceMustBeWithinInclusiveUnitInterval(double confidence)
    {
        var accepted = TrySerialize(CreateRequest() with
        {
            ConfidenceScore = (decimal)confidence,
        }, out var failure);

        Assert.False(accepted);
        Assert.Equal("ai_decision.confidence.invalid", failure);
    }

    [Fact]
    public void EvidenceAlternativesAndRecommendationAreBounded()
    {
        var tooMuchEvidence = Enumerable.Repeat(CreateEvidence(), 21).ToArray();
        var tooManyAlternatives = Enumerable.Repeat(
            new AiDecisionAlternative("alternative.safe", "Safe option", AiDecisionRiskLevel.Low), 11).ToArray();

        Assert.False(TrySerialize(CreateRequest() with { Evidence = tooMuchEvidence }, out var evidenceFailure));
        Assert.Equal("ai_decision.evidence.invalid", evidenceFailure);
        Assert.False(TrySerialize(CreateRequest() with { Alternatives = tooManyAlternatives }, out var alternativesFailure));
        Assert.Equal("ai_decision.alternatives.invalid", alternativesFailure);
        Assert.False(TrySerialize(CreateRequest() with { RecommendationSummary = new string('x', 501) }, out var summaryFailure));
        Assert.Equal("ai_decision.recommendation_summary.invalid", summaryFailure);
    }

    [Theory]
    [InlineData("prompt")]
    [InlineData("completion")]
    [InlineData("reasoning")]
    [InlineData("chainOfThought")]
    public void ProhibitedAndUnknownPayloadFieldsAreRejected(string field)
    {
        var json = JsonSerializer.Serialize(CreateRequest());
        json = json.Insert(json.Length - 1, $",\"{field}\":\"must not persist\"");

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<AiDecisionWriteRequest>(json));
    }

    [Fact]
    public void EvidenceRequiresUtcAndStructuredPolicyContract()
    {
        var localEvidence = CreateEvidence() with
        {
            ObservedAtUtc = new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.FromHours(3)),
        };
        var arbitraryPolicy = new AiDecisionPolicySnapshot(
            "safety.review", "1", "Any", "system.ai-decision.write", "Execute");

        Assert.False(TrySerialize(CreateRequest() with { Evidence = [localEvidence] }, out var timestampFailure));
        Assert.Equal("ai_decision.evidence.invalid", timestampFailure);
        Assert.False(TrySerialize(CreateRequest() with { PolicySnapshot = arbitraryPolicy }, out var policyFailure));
        Assert.Equal("ai_decision.policy.invalid", policyFailure);
    }

    private static bool TrySerialize(AiDecisionWriteRequest request, out string? failure) =>
        AiDecisionPayloadValidator.TrySerialize(
            request,
            new DateTimeOffset(2026, 8, 4, 12, 0, 0, TimeSpan.Zero),
            out _, out _, out _, out failure);

    private static AiDecisionWriteRequest CreateRequest() => new(
        "diagnostic.recommendation",
        "foundation.diagnostic",
        "test-provider",
        "test-model",
        "recommendation.inspect",
        "Inspect the referenced evidence.",
        AiDecisionRiskLevel.Low,
        false,
        AiDecisionStatus.Proposed,
        [CreateEvidence()],
        ConfidenceScore: 0.5m);

    private static AiDecisionEvidence CreateEvidence() => new(
        "diagnostic.reference",
        "reference-001",
        "test.fixture",
        new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.Zero),
        "1");
}
