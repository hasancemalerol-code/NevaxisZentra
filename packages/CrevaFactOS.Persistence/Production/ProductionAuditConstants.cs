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
    public const string ProductionPlanHandoffConsume = "production.plan.handoff.consume";
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
    public const string ProductionPlanHandoffConsumed = "production.plan.handoff.consumed";
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
    public const string ProductionPlanHandoffConsumed = "production-plan-handoff-consumed";
}
