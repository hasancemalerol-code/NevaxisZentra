namespace CrevaFactOS.Persistence.CommercialIntelligence;

public static class CommercialPricingScenarioConstants
{
    public const string RuleSetVersion = "COMMERCIAL-PROFITABILITY-7.0";
    public const string SourceModule = "commercial-intelligence";
    public const string TaskPurposeHumanReviewDraft = "human-review-draft";
    public const string AutomationDisclaimer = "Bu onay fiyatı otomatik olarak uygulamaz.";
}

public static class CommercialPricingScenarioGuardrailResults
{
    public const string Passes = "passes";
    public const string NeedsReview = "needs-review";
    public const string Blocked = "blocked";
    public const string InsufficientData = "insufficient-data";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Passes, NeedsReview, Blocked, InsufficientData,
    };

    public static readonly IReadOnlySet<string> Submittable = new HashSet<string>(StringComparer.Ordinal)
    {
        Passes, NeedsReview,
    };
}

public static class CommercialPricingScenarioLifecycleStatuses
{
    public const string Draft = "draft";
    public const string Evaluated = "evaluated";
    public const string NeedsReview = "needs-review";
    public const string EligibleForApproval = "eligible-for-approval";
    public const string Blocked = "blocked";
    public const string InsufficientData = "insufficient-data";
    public const string SubmittedForApproval = "submitted-for-approval";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Withdrawn = "withdrawn";

    public static readonly IReadOnlySet<string> Terminal = new HashSet<string>(StringComparer.Ordinal)
    {
        Approved, Rejected, Withdrawn,
    };

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Draft,
        Evaluated,
        NeedsReview,
        EligibleForApproval,
        Blocked,
        InsufficientData,
        SubmittedForApproval,
        Approved,
        Rejected,
        Withdrawn,
    };
}

public static class CommercialPricingScenarioAuditEventTypes
{
    public const string Created = "created";
    public const string Updated = "updated";
    public const string Evaluated = "evaluated";
    public const string SubmittedForApproval = "submitted-for-approval";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Withdrawn = "withdrawn";
    public const string TaskLinked = "task-linked";
    public const string ReturnedToDraft = "returned-to-draft";
}
