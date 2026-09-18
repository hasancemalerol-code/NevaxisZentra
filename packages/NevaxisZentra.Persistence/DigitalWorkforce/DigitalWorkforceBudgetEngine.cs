namespace NevaxisZentra.Persistence.DigitalWorkforce;

public sealed record DigitalWorkforceBudgetEvaluation(
    string State,
    bool AllowAutonomousAi,
    bool ShouldWarn,
    bool HardLimitReached);

public static class DigitalWorkforceBudgetEngine
{
    public static DigitalWorkforceBudgetEvaluation Evaluate(DigitalWorkerBudget budget)
    {
        var dailyRatio = budget.DailyLimit <= 0 ? 0 : budget.DailyConsumed / budget.DailyLimit;
        var monthlyRatio = budget.MonthlyLimit <= 0 ? 0 : budget.MonthlyConsumed / budget.MonthlyLimit;
        var ratio = Math.Max(dailyRatio, monthlyRatio);

        if (ratio >= 1m)
        {
            return new(DigitalWorkforceBudgetStates.LimitReached, false, true, true);
        }

        if (ratio >= budget.WarningThreshold)
        {
            return new(DigitalWorkforceBudgetStates.NearLimit, true, true, false);
        }

        return new(DigitalWorkforceBudgetStates.Normal, true, false, false);
    }

    public static void ApplyUsage(DigitalWorkerBudget budget, decimal cost)
    {
        budget.DailyConsumed += cost;
        budget.MonthlyConsumed += cost;
        budget.State = Evaluate(budget).State;
        budget.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
