namespace CrevaFactOS.Persistence.Outbox;

public static class OutboxMessageTypes
{
    public const string AuditRecorded = "infrastructure.audit.recorded";
    public const string AiDecisionRecorded = "infrastructure.ai-decision.recorded";
    public const string Probe = "infrastructure.outbox.probe";
    public const string ProductionPlanSubmittedForReview = "production-plan-submitted-for-review";
    public const string ProductionPlanRevisionRequested = "production-plan-revision-requested";
    public const string ProductionPlanApproved = "production-plan-approved";
    public const string ProductionPlanRejected = "production-plan-rejected";
    public const string ProductionPlanScheduled = "production-plan-scheduled";
    public const string ProductionPlanTransferRequested = "production-plan-transfer-requested";
    public const string ProductionPlanTransferStarted = "production-plan-transfer-started";
    public const string ProductionPlanTransferSucceeded = "production-plan-transfer-succeeded";
    public const string ProductionPlanTransferFailed = "production-plan-transfer-failed";
    public const string ProductionPlanTransferUncertain = "production-plan-transfer-uncertain";

    public const int SchemaVersion = 1;

    public static readonly HashSet<string> All = new(
        [
            AuditRecorded,
            AiDecisionRecorded,
            Probe,
            ProductionPlanSubmittedForReview,
            ProductionPlanRevisionRequested,
            ProductionPlanApproved,
            ProductionPlanRejected,
            ProductionPlanScheduled,
            ProductionPlanTransferRequested,
            ProductionPlanTransferStarted,
            ProductionPlanTransferSucceeded,
            ProductionPlanTransferFailed,
            ProductionPlanTransferUncertain,
        ],
        StringComparer.Ordinal);
}
