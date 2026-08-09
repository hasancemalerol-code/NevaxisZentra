namespace CrevaFactOS.Persistence.Quality;

public static class QualityAuditActions
{
    public const string PlanCreated = "quality.plan.created";
    public const string PlanUpdated = "quality.plan.updated";
    public const string InspectionStarted = "quality.inspection.started";
    public const string ResultRecorded = "quality.result.recorded";
    public const string InspectionCompleted = "quality.inspection.completed";
    public const string ReleaseReleased = "quality.release.released";
    public const string ReleaseRejected = "quality.release.rejected";
}

public static class QualityAuditEventTypes
{
    public const string PlanCreated = "quality.plan.created";
    public const string PlanUpdated = "quality.plan.updated";
    public const string InspectionStarted = "quality.inspection.started";
    public const string ResultRecorded = "quality.result.recorded";
    public const string InspectionCompleted = "quality.inspection.completed";
    public const string ReleaseReleased = "quality.release.released";
    public const string ReleaseRejected = "quality.release.rejected";
}

public static class QualityOutboxMessageTypes
{
    public const string InspectionCompleted = "quality-inspection-completed";
    public const string ReleaseReleased = "quality-release-released";
    public const string ReleaseRejected = "quality-release-rejected";
}

public static class QualityNcrAuditActions
{
    public const string Created = "quality.ncr.created";
    public const string Updated = "quality.ncr.updated";
    public const string Resolved = "quality.ncr.resolved";
    public const string Closed = "quality.ncr.closed";
    public const string HoldPlaced = "quality.hold.placed";
    public const string HoldReleased = "quality.hold.released";
    public const string DispositionDecided = "quality.disposition.decided";
    public const string CorrectiveActionCreated = "quality.corrective.action.created";
    public const string CorrectiveActionCompleted = "quality.corrective.action.completed";
}

public static class QualityNcrAuditEventTypes
{
    public const string Created = "quality.ncr.created";
    public const string Updated = "quality.ncr.updated";
    public const string Resolved = "quality.ncr.resolved";
    public const string Closed = "quality.ncr.closed";
    public const string HoldPlaced = "quality.hold.placed";
    public const string HoldReleased = "quality.hold.released";
    public const string DispositionDecided = "quality.disposition.decided";
    public const string CorrectiveActionCreated = "quality.corrective.action.created";
    public const string CorrectiveActionCompleted = "quality.corrective.action.completed";
}

public static class QualityNcrOutboxMessageTypes
{
    public const string Created = "quality-ncr-created";
    public const string HoldPlaced = "quality-hold-placed";
    public const string HoldReleased = "quality-hold-released";
    public const string Resolved = "quality-ncr-resolved";
    public const string Closed = "quality-ncr-closed";
    public const string CorrectiveActionCompleted = "quality-corrective-action-completed";
}
