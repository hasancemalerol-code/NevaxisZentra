namespace NevaxisFactOS.Persistence.Quality;

/// <summary>
/// Quality NCR → AI Task Center integration boundary.
/// Source entity: <see cref="QualityNonconformance"/> / quality_nonconformances.
/// Eligible NCR statuses: open, under-review, action-required (not closed/cancelled/resolved).
/// Excluded: closed, cancelled, resolved NCR lifecycle; dismissed/resolved follow-up terminal states.
/// Permissions: quality.read (read), quality.ncr.manage (follow-up transitions), ai-task.create (task draft).
/// Audit: append-only quality_ncr_follow_up_audit_events (existing quality audit stream unchanged).
/// </summary>
public static class QualityFollowUpConstants
{
    public const string SourceModule = "quality";
    public const string TaskPurposeHumanReviewDraft = "human-review-draft";
    public const string TaskPurposeDataReviewDraft = "data-review-draft";
    public const string AutomationDisclaimer =
        "Otomatik üretim, stok veya kalite kararı uygulanmaz.";
}

public static class QualityNcrFollowUpStatuses
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

public static class QualityNcrFollowUpAuditEventTypes
{
    public const string TaskLinked = "task-linked";
    public const string FollowUpTransitioned = "follow-up-transitioned";
}
