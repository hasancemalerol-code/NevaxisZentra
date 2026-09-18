namespace NevaxisZentra.Persistence.ProductionPlanning;

public enum ProductionPlanStatus
{
    Draft,
    NeedsReview,
    ReadyForApproval,
    Approved,
    Rejected,
    Scheduled,
    InProgress,
    Completed,
    Cancelled,
}
