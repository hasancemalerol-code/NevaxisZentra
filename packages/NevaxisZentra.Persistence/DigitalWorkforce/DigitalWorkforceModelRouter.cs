namespace NevaxisZentra.Persistence.DigitalWorkforce;

public static class DigitalWorkforceModelTiers
{
    public const string Fast = "fast";
    public const string Standard = "standard";
    public const string Advanced = "advanced";
}

public sealed record DigitalWorkforceModelRoute(
    string Tier,
    string ModelName,
    bool RequiresAiInvocation);

public static class DigitalWorkforceModelRouter
{
    public static DigitalWorkforceModelRoute Route(DigitalWorkforceRoutingPurpose purpose, string? budgetState = null)
    {
        if (string.Equals(budgetState, DigitalWorkforceBudgetStates.LimitReached, StringComparison.OrdinalIgnoreCase))
        {
            return new(DigitalWorkforceModelTiers.Fast, "budget-fast", false);
        }

        return purpose switch
        {
            DigitalWorkforceRoutingPurpose.PermissionCheck or
            DigitalWorkforceRoutingPurpose.StateTransition or
            DigitalWorkforceRoutingPurpose.RetryTimer or
            DigitalWorkforceRoutingPurpose.Idempotency or
            DigitalWorkforceRoutingPurpose.Validation or
            DigitalWorkforceRoutingPurpose.BudgetCheck =>
                new(DigitalWorkforceModelTiers.Fast, "deterministic", false),

            DigitalWorkforceRoutingPurpose.EventTriage or
            DigitalWorkforceRoutingPurpose.PlanDraft =>
                new(DigitalWorkforceModelTiers.Standard, "standard-reasoning", true),

            DigitalWorkforceRoutingPurpose.ComplexDecision or
            DigitalWorkforceRoutingPurpose.CoordinatorOrchestration =>
                new(DigitalWorkforceModelTiers.Advanced, "advanced-reasoning", true),

            _ => new(DigitalWorkforceModelTiers.Standard, "standard-reasoning", true),
        };
    }
}

public enum DigitalWorkforceRoutingPurpose
{
    PermissionCheck,
    StateTransition,
    RetryTimer,
    Idempotency,
    Validation,
    BudgetCheck,
    EventTriage,
    PlanDraft,
    ComplexDecision,
    CoordinatorOrchestration,
}
