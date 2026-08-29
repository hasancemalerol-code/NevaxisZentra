namespace CrevaFactOS.Persistence.Material;

/// <summary>
/// Material availability risk assessment → AI Task Center integration boundary.
/// Source entity: <see cref="MaterialRequirementAssessment"/> with availability-risk type.
/// Eligibility uses persisted status, ERP quantities, drivers and evidence only.
/// Permissions: material.read (read), material.manage (follow-up transitions), ai-task.create (task draft).
/// </summary>
public static class MaterialAvailabilityRiskFollowUpConstants
{
    public const string SourceModule = "material";
    public const string TaskPurposeHumanReviewDraft = "human-review-draft";
    public const string TaskPurposeDataReviewDraft = "data-review-draft";
    public const string AutomationDisclaimer =
        "Bu ekran yalnızca malzeme kullanılabilirliği riskini gösterir; otomatik stok, satın alma veya üretim işlemi yapmaz.";
}

public static class MaterialAvailabilityRiskFollowUpStatuses
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

public static class MaterialAvailabilityRiskFollowUpAuditEventTypes
{
    public const string TaskLinked = "task-linked";
    public const string FollowUpTransitioned = "follow-up-transitioned";
}
