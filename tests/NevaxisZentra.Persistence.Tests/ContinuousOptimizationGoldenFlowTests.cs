using NevaxisZentra.Persistence.ContinuousOptimization;
using NevaxisZentra.Persistence.FactoryCopilot;

namespace NevaxisZentra.Persistence.Tests;

public sealed class ContinuousOptimizationGoldenFlowTests
{
    [Theory]
    [InlineData("Zentra neden bu optimizasyonu önerdi?", FactoryCopilotIntents.OptimizationExplanation)]
    [InlineData("Ne kazandırması bekleniyor?", FactoryCopilotIntents.OptimizationExplanation)]
    [InlineData("Neden uygulanmadı?", FactoryCopilotIntents.OptimizationExplanation)]
    [InlineData("Sonuç doğrulandı mı?", FactoryCopilotIntents.VerificationStatus)]
    public void GoldenCopilotQueriesClassifyWithExpectedIntent(string query, string expectedIntent)
    {
        var result = FactoryCopilotIntentClassifier.Classify(
            query,
            new FactoryCopilotContextInput(FactoryCopilotEntryPoints.Fabrika, LineCode: "MIX-LINE-01"));

        Assert.Equal(expectedIntent, result.Intent);
    }

    [Fact]
    public void GoldenNoOpTriggerUsesInsignificantVariation()
    {
        var trigger = new ContinuousOptimizationTriggerRequest(
            ContinuousOptimizationTriggerTypes.InsignificantVariation,
            ContinuousOptimizationTypes.Throughput,
            "line",
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            0.1m,
            100m,
            100m,
            "golden no-op",
            DateTimeOffset.UtcNow);

        var result = ContinuousOptimizationQualificationEngine.QualifyTrigger(
            trigger,
            new ContinuousOptimizationPolicyThresholds(),
            true,
            false,
            null,
            null,
            0);

        Assert.False(result.Qualified);
        Assert.Equal(ContinuousOptimizationOutcomes.NoSignificantOpportunity, result.ReasonCode);
    }

    [Fact]
    public void GoldenContinuousTriggerUsesMeaningfulThroughputDeviation()
    {
        var trigger = new ContinuousOptimizationTriggerRequest(
            ContinuousOptimizationTriggerTypes.ThroughputDeviationChanged,
            ContinuousOptimizationTypes.Throughput,
            "line",
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            25m,
            75m,
            100m,
            "assembly cycle drift + buffer starvation",
            DateTimeOffset.UtcNow);

        var result = ContinuousOptimizationQualificationEngine.QualifyTrigger(
            trigger,
            new ContinuousOptimizationPolicyThresholds(),
            true,
            false,
            null,
            null,
            0);

        Assert.True(result.Qualified);
    }
}
