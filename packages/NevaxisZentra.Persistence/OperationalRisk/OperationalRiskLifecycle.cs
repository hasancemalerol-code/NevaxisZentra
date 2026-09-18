namespace NevaxisZentra.Persistence.OperationalRisk;

public static class OperationalRiskLifecycle
{
    private static readonly Dictionary<string, IReadOnlySet<string>> AllowedTransitions =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [OperationalRiskLifecycleStatuses.Detected] = new HashSet<string>(StringComparer.Ordinal)
            {
                OperationalRiskLifecycleStatuses.UnderReview,
                OperationalRiskLifecycleStatuses.Accepted,
                OperationalRiskLifecycleStatuses.Dismissed,
            },
            [OperationalRiskLifecycleStatuses.UnderReview] = new HashSet<string>(StringComparer.Ordinal)
            {
                OperationalRiskLifecycleStatuses.Accepted,
                OperationalRiskLifecycleStatuses.MitigationPlanned,
                OperationalRiskLifecycleStatuses.Monitoring,
                OperationalRiskLifecycleStatuses.Dismissed,
            },
            [OperationalRiskLifecycleStatuses.Accepted] = new HashSet<string>(StringComparer.Ordinal)
            {
                OperationalRiskLifecycleStatuses.MitigationPlanned,
                OperationalRiskLifecycleStatuses.Monitoring,
                OperationalRiskLifecycleStatuses.Dismissed,
            },
            [OperationalRiskLifecycleStatuses.MitigationPlanned] = new HashSet<string>(StringComparer.Ordinal)
            {
                OperationalRiskLifecycleStatuses.Monitoring,
                OperationalRiskLifecycleStatuses.Resolved,
                OperationalRiskLifecycleStatuses.Dismissed,
            },
            [OperationalRiskLifecycleStatuses.Monitoring] = new HashSet<string>(StringComparer.Ordinal)
            {
                OperationalRiskLifecycleStatuses.Resolved,
                OperationalRiskLifecycleStatuses.Dismissed,
            },
        };

    public static bool IsTerminal(string status) =>
        OperationalRiskLifecycleStatuses.Terminal.Contains(status);

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
        if (string.Equals(targetStatus, OperationalRiskLifecycleStatuses.Dismissed, StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(note))
        {
            return "Dismissal requires a non-empty reason.";
        }

        if (string.Equals(targetStatus, OperationalRiskLifecycleStatuses.Resolved, StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(note))
        {
            return "Resolution requires a non-empty resolution note.";
        }

        return null;
    }
}
