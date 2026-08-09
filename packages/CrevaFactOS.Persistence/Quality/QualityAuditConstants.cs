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
