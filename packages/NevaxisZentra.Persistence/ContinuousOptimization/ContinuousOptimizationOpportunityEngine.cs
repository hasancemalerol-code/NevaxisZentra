namespace NevaxisZentra.Persistence.ContinuousOptimization;

public static class ContinuousOptimizationOpportunityEngine
{
    public static List<OptimizationOpportunity> Prioritize(IEnumerable<OptimizationOpportunity> opportunities) =>
        opportunities
            .OrderByDescending(Score)
            .ThenByDescending(o => o.ExpiresAtUtc)
            .ToList();

    public static bool MeetsMinimumValueThreshold(
        OptimizationOpportunity opportunity,
        ContinuousOptimizationPolicyThresholds thresholds) =>
        opportunity.EstimatedBenefit >= thresholds.MinimumValueThreshold;

    private static decimal Score(OptimizationOpportunity opportunity)
    {
        var riskPenalty = opportunity.RiskClass switch
        {
            ContinuousOptimizationActionRiskClasses.Low => 0m,
            ContinuousOptimizationActionRiskClasses.Medium => 5m,
            ContinuousOptimizationActionRiskClasses.High => 15m,
            ContinuousOptimizationActionRiskClasses.SafetyCritical => 100m,
            _ => 10m,
        };

        return opportunity.EstimatedBenefit * opportunity.Confidence - riskPenalty;
    }
}
