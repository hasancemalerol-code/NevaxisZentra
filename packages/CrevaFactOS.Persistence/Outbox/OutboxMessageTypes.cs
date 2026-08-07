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
    public const string ProductionPlanHandoffPrepared = "production-plan-handoff-prepared";
    public const string ProductionOrderCreated = "production-order-created";
    public const string ProductionOrderStarted = "production-order-started";
    public const string ProductionOrderPaused = "production-order-paused";
    public const string ProductionOrderResumed = "production-order-resumed";
    public const string ProductionOrderCompleted = "production-order-completed";
    public const string ProductionOrderCompletedShort = "production-order-completed-short";
    public const string ProductionPlanHandoffConsumed = "production-plan-handoff-consumed";
    public const string ProductionGoodQuantityRecorded = "production-order-good-quantity-recorded";
    public const string ProductionScrapRecorded = "production-order-scrap-recorded";
    public const string ProductionQuantityReversed = "production-order-quantity-reversed";

    // 3L — downtime
    public const string ProductionDowntimeStarted = "production-order-downtime-started";
    public const string ProductionDowntimeEnded = "production-order-downtime-ended";

    // 3L — setup
    public const string ProductionSetupStarted = "production-order-setup-started";
    public const string ProductionSetupCompleted = "production-order-setup-completed";

    // 3L — manual cycle
    public const string ProductionManualCycleRecorded = "production-order-manual-cycle-recorded";

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
            ProductionPlanHandoffPrepared,
            ProductionOrderCreated,
            ProductionOrderStarted,
            ProductionOrderPaused,
            ProductionOrderResumed,
            ProductionOrderCompleted,
            ProductionOrderCompletedShort,
            ProductionPlanHandoffConsumed,
            ProductionGoodQuantityRecorded,
            ProductionScrapRecorded,
            ProductionQuantityReversed,
            ProductionDowntimeStarted,
            ProductionDowntimeEnded,
            ProductionSetupStarted,
            ProductionSetupCompleted,
            ProductionManualCycleRecorded,
        ],
        StringComparer.Ordinal);
}
