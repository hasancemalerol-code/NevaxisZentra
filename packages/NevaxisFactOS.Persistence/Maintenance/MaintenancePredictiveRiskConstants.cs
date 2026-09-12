namespace NevaxisFactOS.Persistence.Maintenance;

public static class MaintenancePredictiveRiskConstants
{
    public const string SourceModule = "maintenance";
    public const string TaskPurposeHumanReviewDraft = "human-review-draft";
    public const string TaskPurposeDataReviewDraft = "data-review-draft";
    public const string AutomationDisclaimer =
        "Bu görev taslağı otomatik bakım emri, duruş veya makine müdahalesi başlatmaz.";
}

public static class MaintenancePredictiveRiskFollowUpStatuses
{
    public const string Active = "active";
    public const string DataReviewNeeded = "data-review-needed";
    public const string Monitoring = "monitoring";
    public const string TaskDraftCreated = "task-draft-created";
    public const string HumanReviewed = "human-reviewed";
    public const string Resolved = "resolved";

    public static readonly IReadOnlySet<string> Open = new HashSet<string>(StringComparer.Ordinal)
    {
        Active,
        DataReviewNeeded,
        Monitoring,
        TaskDraftCreated,
        HumanReviewed,
    };

    public static readonly IReadOnlySet<string> Terminal = new HashSet<string>(StringComparer.Ordinal)
    {
        Resolved,
    };

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Active,
        DataReviewNeeded,
        Monitoring,
        TaskDraftCreated,
        HumanReviewed,
        Resolved,
    };
}

public static class MaintenancePredictiveRiskAuditEventTypes
{
    public const string TaskLinked = "task-linked";
    public const string FollowUpTransitioned = "follow-up-transitioned";
}
