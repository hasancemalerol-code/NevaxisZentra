namespace CrevaFactOS.Persistence.Production;

/// <summary>
/// Audit action constants for the Production Execution bounded context.
/// Values must be registered in the DB audit_records action taxonomy check constraint.
/// </summary>
public static class ProductionAuditActions
{
    public const string ProductionOrderCreate = "production.order.create";
    public const string ProductionOrderStart = "production.order.start";
    public const string ProductionOrderPause = "production.order.pause";
    public const string ProductionOrderResume = "production.order.resume";
    public const string ProductionOrderComplete = "production.order.complete";
    public const string ProductionOrderCompleteShort = "production.order.complete.short";
    public const string ProductionPlanHandoffConsume = "production.plan.handoff.consume";
    public const string ProductionQuantityGoodRecord = "production.order.quantity.good.record";
    public const string ProductionQuantityScrapRecord = "production.order.quantity.scrap.record";
    public const string ProductionQuantityReverse = "production.order.quantity.reverse";

    // 3L — downtime
    public const string ProductionDowntimeStart = "production.order.downtime.start";
    public const string ProductionDowntimeEnd = "production.order.downtime.end";

    // 3L — setup
    public const string ProductionSetupStart = "production.order.setup.start";
    public const string ProductionSetupComplete = "production.order.setup.complete";

    // 3L — manual cycle
    public const string ProductionCycleManualRecord = "production.order.cycle.manual.record";
}

/// <summary>
/// Audit event type constants for the Production Execution bounded context.
/// Values must be registered in the DB audit_records event taxonomy check constraint.
/// </summary>
public static class ProductionAuditEventTypes
{
    public const string ProductionOrderCreated = "production.order.created";
    public const string ProductionOrderStarted = "production.order.started";
    public const string ProductionOrderPaused = "production.order.paused";
    public const string ProductionOrderResumed = "production.order.resumed";
    public const string ProductionOrderCompleted = "production.order.completed";
    public const string ProductionOrderCompletedShort = "production.order.completed.short";
    public const string ProductionPlanHandoffConsumed = "production.plan.handoff.consumed";
    public const string ProductionQuantityGoodRecorded = "production.order.quantity.good.recorded";
    public const string ProductionQuantityScrapRecorded = "production.order.quantity.scrap.recorded";
    public const string ProductionQuantityReversed = "production.order.quantity.reversed";

    // 3L — downtime
    public const string ProductionDowntimeStarted = "production.order.downtime.started";
    public const string ProductionDowntimeEnded = "production.order.downtime.ended";

    // 3L — setup
    public const string ProductionSetupStarted = "production.order.setup.started";
    public const string ProductionSetupCompleted = "production.order.setup.completed";

    // 3L — manual cycle
    public const string ProductionCycleManualRecorded = "production.order.cycle.manual.recorded";
}

/// <summary>
/// Outbox message type constants for the Production Execution bounded context.
/// Values must be registered in the DB outbox_messages type check constraint.
/// </summary>
public static class ProductionOutboxMessageTypes
{
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
}
