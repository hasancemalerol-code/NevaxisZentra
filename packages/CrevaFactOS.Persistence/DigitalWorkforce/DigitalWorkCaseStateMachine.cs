namespace CrevaFactOS.Persistence.DigitalWorkforce;

public static class DigitalWorkCaseStateMachine
{
    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new(StringComparer.OrdinalIgnoreCase)
    {
        [DigitalWorkCaseStatuses.Detected] = new(StringComparer.OrdinalIgnoreCase)
        {
            DigitalWorkCaseStatuses.Analyzing,
            DigitalWorkCaseStatuses.Escalated,
            DigitalWorkCaseStatuses.Closed,
        },
        [DigitalWorkCaseStatuses.Analyzing] = new(StringComparer.OrdinalIgnoreCase)
        {
            DigitalWorkCaseStatuses.Deciding,
            DigitalWorkCaseStatuses.Escalated,
        },
        [DigitalWorkCaseStatuses.Deciding] = new(StringComparer.OrdinalIgnoreCase)
        {
            DigitalWorkCaseStatuses.Planning,
            DigitalWorkCaseStatuses.AwaitingApproval,
            DigitalWorkCaseStatuses.Escalated,
        },
        [DigitalWorkCaseStatuses.Planning] = new(StringComparer.OrdinalIgnoreCase)
        {
            DigitalWorkCaseStatuses.AwaitingApproval,
            DigitalWorkCaseStatuses.Executing,
            DigitalWorkCaseStatuses.Escalated,
        },
        [DigitalWorkCaseStatuses.AwaitingApproval] = new(StringComparer.OrdinalIgnoreCase)
        {
            DigitalWorkCaseStatuses.Executing,
            DigitalWorkCaseStatuses.Escalated,
            DigitalWorkCaseStatuses.Closed,
        },
        [DigitalWorkCaseStatuses.Executing] = new(StringComparer.OrdinalIgnoreCase)
        {
            DigitalWorkCaseStatuses.Verifying,
            DigitalWorkCaseStatuses.Escalated,
        },
        [DigitalWorkCaseStatuses.Verifying] = new(StringComparer.OrdinalIgnoreCase)
        {
            DigitalWorkCaseStatuses.Closed,
            DigitalWorkCaseStatuses.Executing,
            DigitalWorkCaseStatuses.Escalated,
        },
        [DigitalWorkCaseStatuses.Escalated] = new(StringComparer.OrdinalIgnoreCase)
        {
            DigitalWorkCaseStatuses.Analyzing,
            DigitalWorkCaseStatuses.Planning,
            DigitalWorkCaseStatuses.Executing,
            DigitalWorkCaseStatuses.Closed,
        },
        [DigitalWorkCaseStatuses.Closed] = new(StringComparer.OrdinalIgnoreCase)
        {
            DigitalWorkCaseStatuses.Analyzing,
        },
    };

    public static bool CanTransition(string fromStatus, string toStatus, bool allowEmergencyClose = false)
    {
        if (string.Equals(fromStatus, toStatus, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (allowEmergencyClose &&
            string.Equals(toStatus, DigitalWorkCaseStatuses.Closed, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(fromStatus, DigitalWorkCaseStatuses.Detected, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return AllowedTransitions.TryGetValue(fromStatus, out var targets) &&
               targets.Contains(toStatus);
    }

    public static bool RequiresVerificationForClosure(string fromStatus) =>
        string.Equals(fromStatus, DigitalWorkCaseStatuses.Executing, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(fromStatus, DigitalWorkCaseStatuses.Verifying, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(fromStatus, DigitalWorkCaseStatuses.Escalated, StringComparison.OrdinalIgnoreCase);

    public static string MapStatusToStage(string status) => status switch
    {
        DigitalWorkCaseStatuses.Detected => DigitalWorkCaseStages.Detect,
        DigitalWorkCaseStatuses.Analyzing => DigitalWorkCaseStages.Analyze,
        DigitalWorkCaseStatuses.Deciding => DigitalWorkCaseStages.Decide,
        DigitalWorkCaseStatuses.Planning => DigitalWorkCaseStages.Plan,
        DigitalWorkCaseStatuses.AwaitingApproval => DigitalWorkCaseStages.Approve,
        DigitalWorkCaseStatuses.Executing => DigitalWorkCaseStages.Act,
        DigitalWorkCaseStatuses.Verifying => DigitalWorkCaseStages.Verify,
        DigitalWorkCaseStatuses.Closed => DigitalWorkCaseStages.Close,
        DigitalWorkCaseStatuses.Escalated => DigitalWorkCaseStages.Act,
        _ => DigitalWorkCaseStages.Detect,
    };
}
