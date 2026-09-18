namespace NevaxisZentra.Persistence.Procurement;

/// <summary>
/// Procurement delivery risk assessment → AI Task Center integration boundary.
/// Permissions: erp.read (read), erp.manage (follow-up transitions), ai-task.create (task draft).
/// </summary>
public static class ProcurementDeliveryRiskFollowUpConstants
{
    public const string SourceModule = "procurement";
    public const string TaskPurposeHumanReviewDraft = "human-review-draft";
    public const string TaskPurposeDataReviewDraft = "data-review-draft";
    public const string AutomationDisclaimer =
        "Otomatik satın alma, tedarikçi iletişimi, stok veya üretim kararı uygulanmaz.";
}

public static class ProcurementDeliveryRiskFollowUpStatuses
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

public static class ProcurementDeliveryRiskFollowUpAuditEventTypes
{
    public const string TaskLinked = "task-linked";
    public const string FollowUpTransitioned = "follow-up-transitioned";
}
