namespace NevaxisFactOS.Persistence.CommercialIntelligence;

public static class CommercialProfitabilityAlertLifecycle
{
    private static readonly Dictionary<string, IReadOnlySet<string>> AllowedTransitions =
        new(StringComparer.Ordinal)
        {
            [CommercialProfitabilityAlertLifecycleStatuses.Active] = new HashSet<string>(StringComparer.Ordinal)
            {
                CommercialProfitabilityAlertLifecycleStatuses.Acknowledged,
                CommercialProfitabilityAlertLifecycleStatuses.Dismissed,
                CommercialProfitabilityAlertLifecycleStatuses.TaskCreated,
            },
            [CommercialProfitabilityAlertLifecycleStatuses.Acknowledged] = new HashSet<string>(StringComparer.Ordinal)
            {
                CommercialProfitabilityAlertLifecycleStatuses.Dismissed,
                CommercialProfitabilityAlertLifecycleStatuses.TaskCreated,
                CommercialProfitabilityAlertLifecycleStatuses.Resolved,
            },
            [CommercialProfitabilityAlertLifecycleStatuses.TaskCreated] = new HashSet<string>(StringComparer.Ordinal)
            {
                CommercialProfitabilityAlertLifecycleStatuses.Resolved,
                CommercialProfitabilityAlertLifecycleStatuses.Dismissed,
            },
        };

    public static bool IsTerminal(string status) =>
        CommercialProfitabilityAlertLifecycleStatuses.Terminal.Contains(status);

    public static bool CanTransition(string currentStatus, string targetStatus)
    {
        if (IsTerminal(currentStatus))
        {
            return false;
        }

        return AllowedTransitions.TryGetValue(currentStatus, out var targets)
               && targets.Contains(targetStatus);
    }

    public static string? ValidateTransitionNote(string targetStatus, string? note)
    {
        if (string.Equals(targetStatus, CommercialProfitabilityAlertLifecycleStatuses.Dismissed, StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(note))
        {
            return "Reddetme için Türkçe bir gerekçe gereklidir.";
        }

        return null;
    }
}
