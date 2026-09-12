namespace NevaxisFactOS.Persistence.AiTasks;

public static class AiTaskLifecycle
{
    private static readonly Dictionary<string, IReadOnlySet<string>> AllowedTransitions =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [AiTaskStatuses.Draft] = new HashSet<string>(StringComparer.Ordinal)
            {
                AiTaskStatuses.AwaitingReview,
                AiTaskStatuses.Cancelled,
            },
            [AiTaskStatuses.AwaitingReview] = new HashSet<string>(StringComparer.Ordinal)
            {
                AiTaskStatuses.Draft,
                AiTaskStatuses.AwaitingApproval,
                AiTaskStatuses.Approved,
                AiTaskStatuses.Deferred,
                AiTaskStatuses.Rejected,
                AiTaskStatuses.Cancelled,
            },
            [AiTaskStatuses.AwaitingApproval] = new HashSet<string>(StringComparer.Ordinal)
            {
                AiTaskStatuses.Approved,
                AiTaskStatuses.Rejected,
                AiTaskStatuses.Deferred,
                AiTaskStatuses.Cancelled,
            },
            [AiTaskStatuses.Approved] = new HashSet<string>(StringComparer.Ordinal)
            {
                AiTaskStatuses.InProgress,
                AiTaskStatuses.Cancelled,
            },
            [AiTaskStatuses.InProgress] = new HashSet<string>(StringComparer.Ordinal)
            {
                AiTaskStatuses.Completed,
                AiTaskStatuses.Failed,
                AiTaskStatuses.Deferred,
                AiTaskStatuses.Cancelled,
            },
            [AiTaskStatuses.Deferred] = new HashSet<string>(StringComparer.Ordinal)
            {
                AiTaskStatuses.AwaitingReview,
                AiTaskStatuses.AwaitingApproval,
                AiTaskStatuses.Cancelled,
            },
            [AiTaskStatuses.Failed] = new HashSet<string>(StringComparer.Ordinal)
            {
                AiTaskStatuses.AwaitingReview,
                AiTaskStatuses.Approved,
                AiTaskStatuses.Cancelled,
            },
        };

    public static bool IsFinalStatus(string status) =>
        AiTaskStatuses.Final.Contains(status);

    public static IReadOnlySet<string> GetAllowedTransitions(string status) =>
        AllowedTransitions.TryGetValue(status, out var targets)
            ? targets
            : Array.Empty<string>().ToHashSet(StringComparer.Ordinal);

    public static bool CanTransition(string currentStatus, string targetStatus)
    {
        if (string.Equals(currentStatus, targetStatus, StringComparison.Ordinal))
        {
            return false;
        }

        if (IsFinalStatus(currentStatus))
        {
            return false;
        }

        return AllowedTransitions.TryGetValue(currentStatus, out var targets)
               && targets.Contains(targetStatus);
    }

    public static string? ValidateTransitionNote(string targetStatus, string? note)
    {
        if (string.Equals(targetStatus, AiTaskStatuses.Rejected, StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(note))
        {
            return "Rejection requires a non-empty reason.";
        }

        return null;
    }

    public static string ResolveInitialApprovalState(string actionType, bool explicitlyRequiresApproval)
    {
        if (explicitlyRequiresApproval || AiTaskActionTypes.DoesActionRequireApproval(actionType))
        {
            return AiTaskApprovalStates.Required;
        }

        return AiTaskApprovalStates.NotRequired;
    }

    public static string ResolveTransitionAuditEventType(string targetStatus) =>
        targetStatus switch
        {
            AiTaskStatuses.AwaitingApproval => AiTaskAuditEventTypes.ApprovalRequested,
            AiTaskStatuses.Approved => AiTaskAuditEventTypes.Approved,
            AiTaskStatuses.Rejected => AiTaskAuditEventTypes.Rejected,
            AiTaskStatuses.Deferred => AiTaskAuditEventTypes.Deferred,
            AiTaskStatuses.InProgress => AiTaskAuditEventTypes.ExecutionStarted,
            AiTaskStatuses.Completed => AiTaskAuditEventTypes.ExecutionCompleted,
            AiTaskStatuses.Failed => AiTaskAuditEventTypes.ExecutionFailed,
            AiTaskStatuses.Cancelled => AiTaskAuditEventTypes.Cancelled,
            AiTaskStatuses.Draft => AiTaskAuditEventTypes.AnalysisUpdated,
            AiTaskStatuses.AwaitingReview => AiTaskAuditEventTypes.AnalysisUpdated,
            _ => AiTaskAuditEventTypes.StatusChanged,
        };

    public static string? ResolveApprovalStateForTransition(string currentApprovalState, string targetStatus)
    {
        return targetStatus switch
        {
            AiTaskStatuses.AwaitingApproval => AiTaskApprovalStates.Pending,
            AiTaskStatuses.Approved => AiTaskApprovalStates.Approved,
            AiTaskStatuses.Rejected => AiTaskApprovalStates.Rejected,
            _ => null,
        };
    }
}
