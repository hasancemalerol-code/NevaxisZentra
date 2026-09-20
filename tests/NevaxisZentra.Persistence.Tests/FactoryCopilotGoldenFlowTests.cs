using NevaxisZentra.Persistence.FactoryCopilot;

namespace NevaxisZentra.Persistence.Tests;

public sealed class FactoryCopilotGoldenFlowTests
{
    private static readonly FactoryCopilotContextInput MixLineContext =
        new(FactoryCopilotEntryPoints.Fabrika, LineCode: "MIX-LINE-01");

    [Theory]
    [InlineData("Bu hat neden hedefin altında?", FactoryCopilotIntents.CauseAnalysis, false)]
    [InlineData("Ne yapabiliriz?", FactoryCopilotIntents.RecoveryOptions, false)]
    [InlineData("+1 operatör koyarsak ne olur?", FactoryCopilotIntents.RecoveryOptions, false)]
    [InlineData("Neden Option B önerildi?", FactoryCopilotIntents.ScenarioExplanation, false)]
    [InlineData("Benzer doğrulanmış vaka var mı?", FactoryCopilotIntents.SimilarCases, false)]
    [InlineData("Bu değişiklik gerçekten işe yaradı mı?", FactoryCopilotIntents.VerificationStatus, false)]
    public void GoldenQueriesClassifyWithExpectedIntent(string query, string expectedIntent, bool requiresModel)
    {
        var result = FactoryCopilotIntentClassifier.Classify(query, MixLineContext);
        Assert.Equal(expectedIntent, result.Intent);
        Assert.Equal(requiresModel, result.RequiresModel);
    }

    [Fact]
    public void GoldenCauseAnalysisUsesDeterministicToolSet()
    {
        var result = FactoryCopilotIntentClassifier.Classify(
            "Bu hat neden hedefin altında?",
            MixLineContext);

        Assert.Equal(FactoryCopilotRoutingClasses.Analytic, result.RoutingClass);
        Assert.Contains(FactoryCopilotToolIds.GetThroughputOverview, result.RequiredTools);
        Assert.Contains(FactoryCopilotToolIds.GetLossTree, result.RequiredTools);
    }

    [Fact]
    public void GoldenSimulationUsesScenarioSimulationTool()
    {
        var result = FactoryCopilotIntentClassifier.Classify(
            "+1 operatör koyarsak ne olur?",
            MixLineContext);

        Assert.Equal(FactoryCopilotRoutingClasses.Simulation, result.RoutingClass);
        Assert.Contains(FactoryCopilotToolIds.RunScenarioSimulation, result.RequiredTools);
    }
}
