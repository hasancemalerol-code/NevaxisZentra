using NevaxisZentra.Persistence.AdaptiveProductionPlanning;

namespace NevaxisZentra.Persistence.Tests;

/// <summary>Planning metrics come from the tenant's plan, never from constants (no fabricated "1 order at risk").</summary>
public sealed class AdaptivePlanningMetricsTruthTests
{
    [Fact]
    public void StabilityMetricsReportTheSuppliedRiskCountsNotAConstant()
    {
        var empty = PlanStabilityMetricsEngine.Build([], [], 0m, 0m, ordersAtRisk: 0, lateOrders: 0);
        Assert.Equal(0, empty.OrdersAtRisk);
        Assert.Equal(0, empty.LateOrders);
        Assert.Equal(0m, empty.PlanAdherencePercent);

        var risky = PlanStabilityMetricsEngine.Build([], [], 80m, 72m, ordersAtRisk: 3, lateOrders: 2);
        Assert.Equal(3, risky.OrdersAtRisk);
        Assert.Equal(2, risky.LateOrders);
    }

    [Fact]
    public void WorkspaceDoesNotInventCapacityOverloadsOrAtRiskOrders()
    {
        var service = File.ReadAllText(Path.Combine(
            CanonicalMetricSourceTests.FindRepoRoot(), "packages", "NevaxisZentra.Persistence", "AdaptiveProductionPlanning", "AdaptivePlanningService.cs"));
        Assert.DoesNotContain("Line-2 overload", service, StringComparison.Ordinal);
        Assert.Contains("operations.Count == 0 ? 0m", service, StringComparison.Ordinal);
    }
}
