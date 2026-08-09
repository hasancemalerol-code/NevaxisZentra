namespace CrevaFactOS.Persistence.Quality;

public enum QualityControlPlanStatus
{
    Draft,
    Active,
    Inactive,
    Archived,
}

public enum QualityInspectionType
{
    Measurement,
    Visual,
    Boolean,
    Text,
}

public enum QualityInspectionStatus
{
    Draft,
    InProgress,
    Completed,
    Cancelled,
}

public enum QualityInspectionOverallResult
{
    Pending,
    Pass,
    Fail,
    Conditional,
}

public enum QualityInspectionReleaseStatus
{
    NotReady,
    ReadyForReview,
    Released,
    Rejected,
}

public enum QualityInspectionItemResult
{
    Pending,
    Pass,
    Fail,
    NotApplicable,
}

public static class QualityValueConversions
{
    private static readonly Dictionary<QualityControlPlanStatus, string> PlanStatuses = new()
    {
        [QualityControlPlanStatus.Draft] = "draft",
        [QualityControlPlanStatus.Active] = "active",
        [QualityControlPlanStatus.Inactive] = "inactive",
        [QualityControlPlanStatus.Archived] = "archived",
    };

    private static readonly Dictionary<QualityInspectionType, string> InspectionTypes = new()
    {
        [QualityInspectionType.Measurement] = "measurement",
        [QualityInspectionType.Visual] = "visual",
        [QualityInspectionType.Boolean] = "boolean",
        [QualityInspectionType.Text] = "text",
    };

    private static readonly Dictionary<QualityInspectionStatus, string> InspectionStatuses = new()
    {
        [QualityInspectionStatus.Draft] = "draft",
        [QualityInspectionStatus.InProgress] = "in-progress",
        [QualityInspectionStatus.Completed] = "completed",
        [QualityInspectionStatus.Cancelled] = "cancelled",
    };

    private static readonly Dictionary<QualityInspectionOverallResult, string> OverallResults = new()
    {
        [QualityInspectionOverallResult.Pending] = "pending",
        [QualityInspectionOverallResult.Pass] = "pass",
        [QualityInspectionOverallResult.Fail] = "fail",
        [QualityInspectionOverallResult.Conditional] = "conditional",
    };

    private static readonly Dictionary<QualityInspectionReleaseStatus, string> ReleaseStatuses = new()
    {
        [QualityInspectionReleaseStatus.NotReady] = "not-ready",
        [QualityInspectionReleaseStatus.ReadyForReview] = "ready-for-review",
        [QualityInspectionReleaseStatus.Released] = "released",
        [QualityInspectionReleaseStatus.Rejected] = "rejected",
    };

    private static readonly Dictionary<QualityInspectionItemResult, string> ItemResults = new()
    {
        [QualityInspectionItemResult.Pending] = "pending",
        [QualityInspectionItemResult.Pass] = "pass",
        [QualityInspectionItemResult.Fail] = "fail",
        [QualityInspectionItemResult.NotApplicable] = "not-applicable",
    };

    public static string ToStorage(QualityControlPlanStatus value) => PlanStatuses[value];
    public static QualityControlPlanStatus PlanStatusFromStorage(string value) =>
        PlanStatuses.First(p => p.Value == value).Key;

    public static string ToStorage(QualityInspectionType value) => InspectionTypes[value];
    public static QualityInspectionType InspectionTypeFromStorage(string value) =>
        InspectionTypes.First(p => p.Value == value).Key;

    public static string ToStorage(QualityInspectionStatus value) => InspectionStatuses[value];
    public static QualityInspectionStatus InspectionStatusFromStorage(string value) =>
        InspectionStatuses.First(p => p.Value == value).Key;

    public static string ToStorage(QualityInspectionOverallResult value) => OverallResults[value];
    public static QualityInspectionOverallResult OverallResultFromStorage(string value) =>
        OverallResults.First(p => p.Value == value).Key;

    public static string ToStorage(QualityInspectionReleaseStatus value) => ReleaseStatuses[value];
    public static QualityInspectionReleaseStatus ReleaseStatusFromStorage(string value) =>
        ReleaseStatuses.First(p => p.Value == value).Key;

    public static string ToStorage(QualityInspectionItemResult value) => ItemResults[value];
    public static QualityInspectionItemResult ItemResultFromStorage(string value) =>
        ItemResults.First(p => p.Value == value).Key;
}
