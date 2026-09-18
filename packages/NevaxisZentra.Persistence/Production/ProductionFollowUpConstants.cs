namespace NevaxisZentra.Persistence.Production;

/// <summary>
/// Production execution exception → AI Task Center integration boundary.
/// Source entity: <see cref="ProductionExecutionExceptionRecord"/> / production_execution_exceptions.
/// Eligible: unresolved exception (open/acknowledged) with non-terminal follow-up status.
/// Excluded: resolved exceptions; resolved/dismissed follow-up terminal states.
/// Permissions: production.execution.read (read), production.execution.exception (follow-up transitions), ai-task.create (task draft).
/// Audit: append-only production_follow_up_audit_events.
/// </summary>
public static class ProductionFollowUpConstants
{
    public const string SourceModule = "production";
    public const string TaskPurposeHumanReviewDraft = "human-review-draft";
    public const string TaskPurposeDataReviewDraft = "data-review-draft";
    public const string AutomationDisclaimer =
        "Otomatik üretim emri, makine, stok veya kalite kararı uygulanmaz.";
}

public static class ProductionFollowUpStatuses
{
    public const string RequiresReview = "requires-review";
    public const string Acknowledged = "acknowledged";
    public const string TaskCreated = "task-created";
    public const string Resolved = "resolved";
    public const string Dismissed = "dismissed";

    public static readonly IReadOnlySet<string> Open = new HashSet<string>(StringComparer.Ordinal)
    {
        RequiresReview,
        Acknowledged,
        TaskCreated,
    };

    public static readonly IReadOnlySet<string> Terminal = new HashSet<string>(StringComparer.Ordinal)
    {
        Resolved,
        Dismissed,
    };

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        RequiresReview,
        Acknowledged,
        TaskCreated,
        Resolved,
        Dismissed,
    };
}

public static class ProductionFollowUpAuditEventTypes
{
    public const string TaskLinked = "task-linked";
    public const string FollowUpTransitioned = "follow-up-transitioned";
}
