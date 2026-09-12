namespace NevaxisFactOS.Persistence.ShiftHandover;

public static class ShiftHandoverStatuses
{
    public const string Draft = "draft";
    public const string ReadyForHandover = "ready-for-handover";
    public const string HandedOver = "handed-over";
    public const string Acknowledged = "acknowledged";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";

    public static readonly HashSet<string> Terminal =
    [
        Completed,
        Cancelled,
    ];

    public static readonly HashSet<string> PostAcknowledgment =
    [
        Acknowledged,
        Completed,
    ];
}

public static class ShiftHandoverShiftPeriods
{
    public const string Morning = "morning";
    public const string Evening = "evening";
    public const string Night = "night";
    public const string Custom = "custom";
}

public static class ShiftHandoverItemCategories
{
    public const string Production = "production";
    public const string Machine = "machine";
    public const string Maintenance = "maintenance";
    public const string Quality = "quality";
    public const string Material = "material";
    public const string Order = "order";
    public const string Safety = "safety";
    public const string General = "general";
}

public static class ShiftHandoverItemSeverities
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";
}

public static class ShiftHandoverItemStatuses
{
    public const string Open = "open";
    public const string Resolved = "resolved";
    public const string Escalated = "escalated";
}

public static class ShiftHandoverAuditEventTypes
{
    public const string Created = "created";
    public const string Updated = "updated";
    public const string ItemAdded = "item-added";
    public const string ItemUpdated = "item-updated";
    public const string ReadyForHandover = "ready-for-handover";
    public const string RevertedToDraft = "reverted-to-draft";
    public const string HandedOver = "handed-over";
    public const string Acknowledged = "acknowledged";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
    public const string FollowUpTaskLinked = "follow-up-task-linked";
}

public static class ShiftHandoverFailureCodes
{
    public const string NotFound = "shift-handover.not-found";
    public const string ValidationFailed = "shift-handover.validation-failed";
    public const string AccessDenied = "shift-handover.access-denied";
    public const string InvalidTransition = "shift-handover.invalid-transition";
    public const string ConcurrencyConflict = "shift-handover.concurrency-conflict";
    public const string ItemNotFound = "shift-handover.item-not-found";
    public const string TaskAlreadyLinked = "shift-handover.task-already-linked";
}

public static class ShiftHandoverConstants
{
    public const string SourceModule = "shift-handover";
    public const int MaxSummaryLength = 4000;
    public const int MaxDepartmentLength = 120;
    public const int MaxItemTitleLength = 200;
    public const int MaxItemDescriptionLength = 4000;
    public const int MaxPageSize = 100;
    public const int MaxEvidenceMetadataLength = 2000;

    public const string AutomationDisclaimer =
        "Vardiya devir teslimi yaln─▒zca operasyonel bilgi aktar─▒m─▒ sa─şlar; otomatik ├╝retim, bak─▒m veya kalite karar─▒ uygulanmaz.";
}
