namespace NevaxisFactOS.Persistence.CommercialIntelligence;

public static class CommercialPricingScenarioLifecycle
{
    private static readonly Dictionary<string, IReadOnlySet<string>> AllowedTransitions =
        new(StringComparer.Ordinal)
        {
            [CommercialPricingScenarioLifecycleStatuses.Draft] = new HashSet<string>(StringComparer.Ordinal)
            {
                CommercialPricingScenarioLifecycleStatuses.Evaluated,
            },
            [CommercialPricingScenarioLifecycleStatuses.Evaluated] = new HashSet<string>(StringComparer.Ordinal)
            {
                CommercialPricingScenarioLifecycleStatuses.NeedsReview,
                CommercialPricingScenarioLifecycleStatuses.EligibleForApproval,
                CommercialPricingScenarioLifecycleStatuses.Blocked,
                CommercialPricingScenarioLifecycleStatuses.InsufficientData,
            },
            [CommercialPricingScenarioLifecycleStatuses.NeedsReview] = new HashSet<string>(StringComparer.Ordinal)
            {
                CommercialPricingScenarioLifecycleStatuses.SubmittedForApproval,
                CommercialPricingScenarioLifecycleStatuses.Draft,
            },
            [CommercialPricingScenarioLifecycleStatuses.EligibleForApproval] = new HashSet<string>(StringComparer.Ordinal)
            {
                CommercialPricingScenarioLifecycleStatuses.SubmittedForApproval,
                CommercialPricingScenarioLifecycleStatuses.Draft,
            },
            [CommercialPricingScenarioLifecycleStatuses.Blocked] = new HashSet<string>(StringComparer.Ordinal)
            {
                CommercialPricingScenarioLifecycleStatuses.Draft,
            },
            [CommercialPricingScenarioLifecycleStatuses.InsufficientData] = new HashSet<string>(StringComparer.Ordinal)
            {
                CommercialPricingScenarioLifecycleStatuses.Draft,
            },
            [CommercialPricingScenarioLifecycleStatuses.SubmittedForApproval] = new HashSet<string>(StringComparer.Ordinal)
            {
                CommercialPricingScenarioLifecycleStatuses.Approved,
                CommercialPricingScenarioLifecycleStatuses.Rejected,
                CommercialPricingScenarioLifecycleStatuses.Withdrawn,
            },
        };

    public static bool IsTerminal(string status) =>
        CommercialPricingScenarioLifecycleStatuses.Terminal.Contains(status);

    public static bool CanTransition(string currentStatus, string targetStatus)
    {
        if (IsTerminal(currentStatus))
        {
            return false;
        }

        return AllowedTransitions.TryGetValue(currentStatus, out var targets)
               && targets.Contains(targetStatus);
    }

    public static string MapGuardrailToLifecycle(string guardrailResult) =>
        guardrailResult switch
        {
            CommercialPricingScenarioGuardrailResults.Passes =>
                CommercialPricingScenarioLifecycleStatuses.EligibleForApproval,
            CommercialPricingScenarioGuardrailResults.NeedsReview =>
                CommercialPricingScenarioLifecycleStatuses.NeedsReview,
            CommercialPricingScenarioGuardrailResults.Blocked =>
                CommercialPricingScenarioLifecycleStatuses.Blocked,
            CommercialPricingScenarioGuardrailResults.InsufficientData =>
                CommercialPricingScenarioLifecycleStatuses.InsufficientData,
            _ => CommercialPricingScenarioLifecycleStatuses.InsufficientData,
        };

    public static string? ValidateTransitionNote(string targetStatus, string? note)
    {
        if (string.Equals(targetStatus, CommercialPricingScenarioLifecycleStatuses.Rejected, StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(note))
        {
            return "Reddetme için Türkçe bir gerekçe gereklidir.";
        }

        return null;
    }
}
