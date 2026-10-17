using NevaxisZentra.Persistence.ProductionManager;

namespace NevaxisZentra.Persistence.Tests;

/// <summary>
/// A Production tenant must never receive the canned production-manager scenario (PO-1842, IM-07, fixed counts).
/// </summary>
public sealed class ProductionManagerScenarioGateTests
{
    private static ProductionManagerService ClosedService() =>
        new(null!, null!, null!, null!, null!, new ProductionManagerScenarioGate(false));

    [Fact]
    public async Task ClosedGateReturnsAnHonestEmptyWorkspaceWithoutTouchingTheDatabase()
    {
        var workspace = await ClosedService().GetWorkspaceAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(workspace.Predictions);
        Assert.Empty(workspace.Opportunities);
        Assert.Empty(workspace.PendingDecisions);
        Assert.Empty(workspace.RecentDecisions);
        Assert.Empty(workspace.Bottlenecks);
        Assert.Equal(0, workspace.Executive.ActiveRiskCount);
        Assert.Equal(0, workspace.Executive.PredictedIssueCount);
        Assert.NotEqual("Stabil", workspace.Executive.FactoryStatusLabel);
    }

    [Fact]
    public async Task ClosedGateRefusesDecisionCreationAndSimulation()
    {
        var service = ClosedService();

        var create = await Assert.ThrowsAsync<ProductionManagerException>(
            () => service.CreateDecisionAsync(Guid.NewGuid(), "order-delay", null, "subject", CancellationToken.None));
        Assert.Equal(ProductionManagerFailureCodes.ScenarioUnavailable, create.FailureCode);
    }

    [Fact]
    public void OnlyDevelopmentOpensTheScenarioGate()
    {
        var program = File.ReadAllText(Path.Combine(CanonicalMetricSourceTests.FindRepoRoot(), "apps", "api", "Program.cs"));
        Assert.Contains("AddNevaxisZentraProductionManager(scenarioEnginesEnabled: builder.Environment.IsDevelopment())", program, StringComparison.Ordinal);
    }
}
