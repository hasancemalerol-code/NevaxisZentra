namespace NevaxisZentra.Persistence.CommercialIntelligence;

public static class CommercialProfitabilityAlertConstants
{
    public const string RuleSetVersion = "COMMERCIAL-PROFITABILITY-6.0";
    public const string SourceModule = "commercial-intelligence";
    public const string TaskPurposeHumanReviewDraft = "human-review-draft";
}

public static class CommercialProfitabilityAlertCategories
{
    public const string ProductContributionDeterioration = "product-contribution-deterioration";
    public const string CustomerContributionDeterioration = "customer-contribution-deterioration";
    public const string MaterialContributionDeterioration = "material-contribution-deterioration";
    public const string CostMixDeterioration = "cost-mix-deterioration";
    public const string DataReview = "data-review";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        ProductContributionDeterioration,
        CustomerContributionDeterioration,
        MaterialContributionDeterioration,
        CostMixDeterioration,
        DataReview,
    };
}

public static class CommercialProfitabilityAlertSeverities
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Low, Medium, High, Critical,
    };

    public static readonly IReadOnlySet<string> HighOrCritical = new HashSet<string>(StringComparer.Ordinal)
    {
        High, Critical,
    };
}

public static class CommercialProfitabilityAlertLifecycleStatuses
{
    public const string Active = "active";
    public const string Acknowledged = "acknowledged";
    public const string TaskCreated = "task-created";
    public const string Dismissed = "dismissed";
    public const string Resolved = "resolved";

    public static readonly IReadOnlySet<string> Open = new HashSet<string>(StringComparer.Ordinal)
    {
        Active, Acknowledged, TaskCreated,
    };

    public static readonly IReadOnlySet<string> Terminal = new HashSet<string>(StringComparer.Ordinal)
    {
        Dismissed, Resolved,
    };

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Active, Acknowledged, TaskCreated, Dismissed, Resolved,
    };
}

public static class CommercialProfitabilityAlertAuditEventTypes
{
    public const string Materialized = "materialized";
    public const string Refreshed = "refreshed";
    public const string Acknowledged = "acknowledged";
    public const string Dismissed = "dismissed";
    public const string TaskLinked = "task-linked";
    public const string Resolved = "resolved";
}
