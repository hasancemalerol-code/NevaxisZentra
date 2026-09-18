namespace NevaxisZentra.Persistence.Production;

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

    // 4L — exceptions
    public const string ProductionExceptionRaise = "production.order.exception.raise";
    public const string ProductionExceptionAcknowledge = "production.order.exception.acknowledge";
    public const string ProductionExceptionResolve = "production.order.exception.resolve";

    // 4L — recovery
    public const string ProductionDowntimeRecover = "production.order.downtime.recover";
    public const string ProductionSetupRecover = "production.order.setup.recover";

    // 5L — shift assignment
    public const string ProductionShiftAssign = "production.order.shift.assign";
    public const string ProductionShiftReassign = "production.order.shift.reassign";

    // 5L — operator assignment
    public const string ProductionOperatorAssign = "production.order.operator.assign";
    public const string ProductionOperatorRelease = "production.order.operator.release";

    // 5L — handover
    public const string ProductionHandoverCreate = "production.order.handover.create";

    // 5L — exception routing
    public const string ProductionExceptionAssign = "production.order.exception.assign";
    public const string ProductionExceptionUnassign = "production.order.exception.unassign";
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

    // 4L — exceptions
    public const string ProductionExceptionRaised = "production.order.exception.raised";
    public const string ProductionExceptionAcknowledged = "production.order.exception.acknowledged";
    public const string ProductionExceptionResolved = "production.order.exception.resolved";

    // 4L — recovery
    public const string ProductionDowntimeRecovered = "production.order.downtime.recovered";
    public const string ProductionSetupRecovered = "production.order.setup.recovered";

    // 5L — shift assignment
    public const string ProductionShiftAssigned = "production.order.shift.assigned";
    public const string ProductionShiftReassigned = "production.order.shift.reassigned";

    // 5L — operator assignment
    public const string ProductionOperatorAssigned = "production.order.operator.assigned";
    public const string ProductionOperatorReleased = "production.order.operator.released";

    // 5L — handover
    public const string ProductionHandoverCreated = "production.order.handover.created";

    // 5L — exception routing
    public const string ProductionExceptionAssigned = "production.order.exception.assigned";
    public const string ProductionExceptionUnassigned = "production.order.exception.unassigned";
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

    // 4L — exceptions
    public const string ProductionExceptionRaised = "production-order-exception-raised";
    public const string ProductionExceptionAcknowledged = "production-order-exception-acknowledged";
    public const string ProductionExceptionResolved = "production-order-exception-resolved";

    // 4L — recovery
    public const string ProductionDowntimeRecovered = "production-order-downtime-recovered";
    public const string ProductionSetupRecovered = "production-order-setup-recovered";

    // 5L — shift assignment
    public const string ProductionShiftAssigned = "production-order-shift-assigned";
    public const string ProductionShiftReassigned = "production-order-shift-reassigned";

    // 5L — operator assignment
    public const string ProductionOperatorAssigned = "production-order-operator-assigned";
    public const string ProductionOperatorReleased = "production-order-operator-released";

    // 5L — handover
    public const string ProductionHandoverCreated = "production-order-shift-handover-created";

    // 5L — exception routing
    public const string ProductionExceptionAssigned = "production-order-exception-assigned";
    public const string ProductionExceptionUnassigned = "production-order-exception-unassigned";
}
