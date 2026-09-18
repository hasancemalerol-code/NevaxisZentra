namespace NevaxisZentra.Persistence.ShiftOperations;

public static class ShiftInstanceStatuses
{
    public const string Planned = "planned";
    public const string Ready = "ready";
    public const string Active = "active";
    public const string Closing = "closing";
    public const string HandoverPending = "handover-pending";
    public const string HandedOver = "handed-over";
    public const string Closed = "closed";
    public const string Exception = "exception";
}

public static class ShiftOpenItemCategories
{
    public const string Production = "production";
    public const string Machine = "machine";
    public const string Maintenance = "maintenance";
    public const string Quality = "quality";
    public const string Material = "material";
    public const string Supplier = "supplier";
    public const string Delivery = "delivery";
    public const string Mold = "mold";
    public const string Approval = "approval";
    public const string DigitalWorkforce = "digital-workforce";
    public const string Task = "task";
    public const string Other = "other";
}

public static class ShiftOpenItemStatuses
{
    public const string Open = "open";
    public const string Transferred = "transferred";
    public const string Resolved = "resolved";
    public const string CancelledWithReason = "cancelled-with-reason";
    public const string AcceptedRisk = "accepted-risk";
    public const string Escalated = "escalated";
}

public static class ShiftHandoverPriorities
{
    public const string Critical = "critical";
    public const string High = "high";
    public const string Normal = "normal";
    public const string Info = "info";
}

public static class ShiftHandoverDispositions
{
    public const string Transfer = "transfer";
    public const string Resolved = "resolved";
    public const string AcceptedRisk = "accepted-risk";
    public const string Cancelled = "cancelled";
    public const string Escalated = "escalated";
}

public static class ShiftOperationsFailureCodes
{
    public const string NotFound = "shift-operations.not-found";
    public const string ValidationFailed = "shift-operations.validation-failed";
    public const string Forbidden = "shift-operations.forbidden";
    public const string InvalidTransition = "shift-operations.invalid-transition";
    public const string ConcurrencyConflict = "shift-operations.concurrency-conflict";
    public const string SilentDropBlocked = "shift-operations.silent-drop-blocked";
    public const string ConflictDetected = "shift-operations.conflict-detected";
    public const string PreviewMutationDenied = "shift-operations.preview-mutation-denied";
}

public sealed class ShiftOperationsException : Exception
{
    public string FailureCode { get; }

    public ShiftOperationsException(string failureCode, string message) : base(message) =>
        FailureCode = failureCode;
}
