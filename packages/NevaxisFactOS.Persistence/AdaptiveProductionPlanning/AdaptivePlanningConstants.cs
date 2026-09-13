namespace NevaxisFactOS.Persistence.AdaptiveProductionPlanning;

public static class AdaptivePlanLifecycleStatuses
{
    public const string Draft = "draft";
    public const string Simulating = "simulating";
    public const string Ready = "ready";
    public const string ApprovalPending = "approval-pending";
    public const string Published = "published";
    public const string Active = "active";
    public const string Superseded = "superseded";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
}

public static class PlanningHorizons
{
    public const string Tactical = "tactical";
    public const string Operational = "operational";
    public const string RealTimeRecovery = "realtime-recovery";
}

public static class ReplanActions
{
    public const string KeepCurrentPlan = "keep-current-plan";
    public const string LocalAdjustment = "local-adjustment";
    public const string PartialReplan = "partial-replan";
    public const string FullReplan = "full-replan";
}

public static class PlanningAutonomyModes
{
    public const string Observe = "observe";
    public const string Draft = "draft";
    public const string Limited = "limited";
    public const string Full = "full";
}

public static class PlanningDataClassifications
{
    public const string Measured = "measured";
    public const string Derived = "derived";
    public const string Estimated = "estimated";
    public const string Predicted = "predicted";
    public const string Unknown = "unknown";
}

public static class AdaptivePlanningFailureCodes
{
    public const string NotFound = "adaptive-planning.not-found";
    public const string ValidationFailed = "adaptive-planning.validation-failed";
    public const string Forbidden = "adaptive-planning.forbidden";
    public const string ConcurrencyConflict = "adaptive-planning.concurrency-conflict";
    public const string Infeasible = "adaptive-planning.infeasible";
    public const string FrozenWindow = "adaptive-planning.frozen-window";
    public const string NervousnessBlocked = "adaptive-planning.nervousness-blocked";
    public const string StaleData = "adaptive-planning.stale-data";
    public const string Conflict = "adaptive-planning.conflict";
    public const string ApprovalRequired = "adaptive-planning.approval-required";
    public const string DuplicatePublish = "adaptive-planning.duplicate-publish";
    public const string PolicyViolation = "adaptive-planning.policy-violation";
}

public sealed class AdaptivePlanningException : Exception
{
    public string FailureCode { get; }

    public AdaptivePlanningException(string failureCode, string message) : base(message) =>
        FailureCode = failureCode;
}
