namespace NevaxisFactOS.Persistence.Mold;

/// <summary>
/// Mold registry → AI Task Center integration boundary.
/// Source entity: <see cref="MoldRegistryEntry"/> / molds.
/// Eligible: maintenance lifecycle or counter at/above persisted nominal shot life.
/// Excluded: retired/scrapped lifecycle; resolved/dismissed follow-up terminal states.
/// Permissions: mold.read (read), mold.manage (follow-up transitions), ai-task.create (task draft).
/// Audit: append-only mold_follow_up_audit_events.
/// </summary>
public static class MoldFollowUpConstants
{
    public const string SourceModule = "mold";
    public const string TaskPurposeHumanReviewDraft = "human-review-draft";
    public const string TaskPurposeDataReviewDraft = "data-review-draft";
    public const string AutomationDisclaimer =
        "Otomatik bakım emri, kalıp yaşam döngüsü veya üretim kararı uygulanmaz.";
}

public static class MoldFollowUpStatuses
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

public static class MoldFollowUpAuditEventTypes
{
    public const string TaskLinked = "task-linked";
    public const string FollowUpTransitioned = "follow-up-transitioned";
}
