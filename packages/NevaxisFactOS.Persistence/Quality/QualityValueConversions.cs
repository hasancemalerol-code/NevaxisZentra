namespace NevaxisFactOS.Persistence.Quality;

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

public enum QualityNcrStatus
{
    Open,
    UnderReview,
    ActionRequired,
    Resolved,
    Closed,
    Cancelled,
}

public enum QualityNcrSeverity
{
    Minor,
    Major,
    Critical,
}

public enum QualityNcrDispositionStatus
{
    Pending,
    Decided,
}

public enum QualityNcrDispositionType
{
    UseAsIs,
    Rework,
    Scrap,
    ReturnToSupplier,
    SortOrScreen,
    Pending,
}

public enum QualityNcrHoldStatus
{
    None,
    Active,
    Released,
}

public enum QualityHoldRecordStatus
{
    Active,
    Released,
    Cancelled,
}

public enum QualityCorrectiveActionStatus
{
    Open,
    InProgress,
    Completed,
    Cancelled,
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

    private static readonly Dictionary<QualityNcrStatus, string> NcrStatuses = new()
    {
        [QualityNcrStatus.Open] = "open",
        [QualityNcrStatus.UnderReview] = "under-review",
        [QualityNcrStatus.ActionRequired] = "action-required",
        [QualityNcrStatus.Resolved] = "resolved",
        [QualityNcrStatus.Closed] = "closed",
        [QualityNcrStatus.Cancelled] = "cancelled",
    };

    private static readonly Dictionary<QualityNcrSeverity, string> NcrSeverities = new()
    {
        [QualityNcrSeverity.Minor] = "minor",
        [QualityNcrSeverity.Major] = "major",
        [QualityNcrSeverity.Critical] = "critical",
    };

    private static readonly Dictionary<QualityNcrDispositionStatus, string> DispositionStatuses = new()
    {
        [QualityNcrDispositionStatus.Pending] = "pending",
        [QualityNcrDispositionStatus.Decided] = "decided",
    };

    private static readonly Dictionary<QualityNcrDispositionType, string> DispositionTypes = new()
    {
        [QualityNcrDispositionType.UseAsIs] = "use-as-is",
        [QualityNcrDispositionType.Rework] = "rework",
        [QualityNcrDispositionType.Scrap] = "scrap",
        [QualityNcrDispositionType.ReturnToSupplier] = "return-to-supplier",
        [QualityNcrDispositionType.SortOrScreen] = "sort-or-screen",
        [QualityNcrDispositionType.Pending] = "pending",
    };

    private static readonly Dictionary<QualityNcrHoldStatus, string> NcrHoldStatuses = new()
    {
        [QualityNcrHoldStatus.None] = "none",
        [QualityNcrHoldStatus.Active] = "active",
        [QualityNcrHoldStatus.Released] = "released",
    };

    private static readonly Dictionary<QualityHoldRecordStatus, string> HoldRecordStatuses = new()
    {
        [QualityHoldRecordStatus.Active] = "active",
        [QualityHoldRecordStatus.Released] = "released",
        [QualityHoldRecordStatus.Cancelled] = "cancelled",
    };

    private static readonly Dictionary<QualityCorrectiveActionStatus, string> CorrectiveActionStatuses = new()
    {
        [QualityCorrectiveActionStatus.Open] = "open",
        [QualityCorrectiveActionStatus.InProgress] = "in-progress",
        [QualityCorrectiveActionStatus.Completed] = "completed",
        [QualityCorrectiveActionStatus.Cancelled] = "cancelled",
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

    public static string ToStorage(QualityNcrStatus value) => NcrStatuses[value];
    public static QualityNcrStatus NcrStatusFromStorage(string value) =>
        NcrStatuses.First(p => p.Value == value).Key;

    public static string ToStorage(QualityNcrSeverity value) => NcrSeverities[value];
    public static QualityNcrSeverity NcrSeverityFromStorage(string value) =>
        NcrSeverities.First(p => p.Value == value).Key;

    public static string ToStorage(QualityNcrDispositionStatus value) => DispositionStatuses[value];
    public static QualityNcrDispositionStatus DispositionStatusFromStorage(string value) =>
        DispositionStatuses.First(p => p.Value == value).Key;

    public static string ToStorage(QualityNcrDispositionType value) => DispositionTypes[value];
    public static QualityNcrDispositionType DispositionTypeFromStorage(string value) =>
        DispositionTypes.First(p => p.Value == value).Key;

    public static string ToStorage(QualityNcrHoldStatus value) => NcrHoldStatuses[value];
    public static QualityNcrHoldStatus NcrHoldStatusFromStorage(string value) =>
        NcrHoldStatuses.First(p => p.Value == value).Key;

    public static string ToStorage(QualityHoldRecordStatus value) => HoldRecordStatuses[value];
    public static QualityHoldRecordStatus HoldRecordStatusFromStorage(string value) =>
        HoldRecordStatuses.First(p => p.Value == value).Key;

    public static string ToStorage(QualityCorrectiveActionStatus value) => CorrectiveActionStatuses[value];
    public static QualityCorrectiveActionStatus CorrectiveActionStatusFromStorage(string value) =>
        CorrectiveActionStatuses.First(p => p.Value == value).Key;
}
