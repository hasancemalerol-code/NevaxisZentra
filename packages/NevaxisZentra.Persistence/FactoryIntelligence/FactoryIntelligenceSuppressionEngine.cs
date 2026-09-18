namespace NevaxisZentra.Persistence.FactoryIntelligence;

public static class FactoryIntelligenceSuppressionEngine
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(20);

    public static bool ShouldSuppressDuplicate(
        FactoryPredictedRisk? existing,
        FactoryRiskDetectionResult detection,
        DateTimeOffset nowUtc)
    {
        if (existing is null || !existing.IsActive)
        {
            return false;
        }

        if (!string.Equals(existing.CorrelationKey, detection.CorrelationKey, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(existing.RiskState, detection.RiskState, StringComparison.OrdinalIgnoreCase)
            && existing.UpdatedAtUtc > nowUtc.Subtract(Cooldown);
    }

    public static string ResolveEscalatedState(string? currentState, string proposedState)
    {
        if (string.IsNullOrWhiteSpace(currentState)
            || currentState is FactoryIntelligenceRiskStates.Resolved
            or FactoryIntelligenceRiskStates.Averted)
        {
            return proposedState;
        }

        return Rank(proposedState) > Rank(currentState) ? proposedState : currentState;
    }

    public static string ResolveDeEscalatedState(string currentState, string proposedState)
    {
        if (currentState is FactoryIntelligenceRiskStates.Resolved or FactoryIntelligenceRiskStates.Averted)
        {
            return currentState;
        }

        return Rank(proposedState) < Rank(currentState) ? proposedState : currentState;
    }

    public static bool ShouldOpenEarlyCase(string riskState) =>
        riskState is FactoryIntelligenceRiskStates.Warning or FactoryIntelligenceRiskStates.Critical;

    private static int Rank(string state) => state switch
    {
        FactoryIntelligenceRiskStates.Critical => 3,
        FactoryIntelligenceRiskStates.Warning => 2,
        FactoryIntelligenceRiskStates.Watch => 1,
        _ => 0,
    };
}
