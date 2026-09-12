namespace NevaxisFactOS.Persistence.Audit;

public static class AuditEventTypes
{
    public const string AuthenticationSucceeded = "authentication.succeeded";
    public const string AuthenticationFailed = "authentication.failed";
    public const string TenantAccessGranted = "tenant.access.granted";
    public const string TenantAccessDenied = "tenant.access.denied";
    public const string AuthorizationGranted = "authorization.granted";
    public const string AuthorizationDenied = "authorization.denied";
    public const string AuditWriteFailed = "audit.write.failed";
    public const string InfrastructureConfigurationRejected = "infrastructure.configuration.rejected";
    public const string AiDecisionRecorded = "ai.decision.recorded";
    public const string ProductionPlanDraftCreated = "production.plan.draft.created";
    public const string ProductionPlanDraftUpdated = "production.plan.draft.updated";
    public const string ProductionPlanSubmittedForReview = "production.plan.submitted.for.review";
    public const string ProductionPlanRevisionRequested = "production.plan.revision.requested";
    public const string ProductionPlanApproved = "production.plan.approved";
    public const string ProductionPlanRejected = "production.plan.rejected";
    public const string ProductionPlanScheduled = "production.plan.scheduled";
    public const string ProductionPlanTransferRequested = "production.plan.transfer.requested";
    public const string ProductionPlanTransferStarted = "production.plan.transfer.started";
    public const string ProductionPlanTransferSucceeded = "production.plan.transfer.succeeded";
    public const string ProductionPlanTransferFailed = "production.plan.transfer.failed";
    public const string ProductionPlanTransferUncertain = "production.plan.transfer.uncertain";
    public const string ProductionPlanHandoffPrepared = "production.plan.handoff.prepared";
    public const string PlanningLogisticsConstraintsEvaluated = "planning.logistics.constraints.evaluated";
    public const string PlanningLogisticsRiskChanged = "planning.logistics.risk.changed";
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

    // 5L — shift
    public const string ProductionShiftAssigned = "production.order.shift.assigned";
    public const string ProductionShiftReassigned = "production.order.shift.reassigned";

    // 5L — operator
    public const string ProductionOperatorAssigned = "production.order.operator.assigned";
    public const string ProductionOperatorReleased = "production.order.operator.released";

    // 5L — handover
    public const string ProductionHandoverCreated = "production.order.handover.created";

    // 5L — exception routing
    public const string ProductionExceptionAssigned = "production.order.exception.assigned";
    public const string ProductionExceptionUnassigned = "production.order.exception.unassigned";

    // Export
    public const string ExportCaseCreated = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportCaseCreated;
    public const string ExportCaseUpdated = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportCaseUpdated;
    public const string ExportDocumentRegistered = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportDocumentRegistered;
    public const string ExportDocumentVerified = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportDocumentVerified;
    public const string ExportDocumentRejected = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportDocumentRejected;
    public const string ExportReadinessChanged = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportReadinessChanged;

    // Export 2L
    public const string ExportPackingUnitCreated = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportPackingUnitCreated;
    public const string ExportPackingUnitUpdated = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportPackingUnitUpdated;
    public const string ExportPackingItemAdded = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportPackingItemAdded;
    public const string ExportPackingCompleted = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportPackingCompleted;
    public const string ExportPackingVerified = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportPackingVerified;
    public const string ExportShipmentCreated = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportShipmentCreated;
    public const string ExportShipmentUpdated = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportShipmentUpdated;
    public const string ExportShipmentStatusChanged = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportShipmentStatusChanged;
    public const string ExportBrokerWorkspaceCreated = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportBrokerWorkspaceCreated;
    public const string ExportBrokerPackagePrepared = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportBrokerPackagePrepared;
    public const string ExportBrokerPackageSent = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportBrokerPackageSent;
    public const string ExportBrokerWorkspaceAcknowledged = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportBrokerWorkspaceAcknowledged;
    public const string ExportBrokerCorrectionCreated = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportBrokerCorrectionCreated;
    public const string ExportBrokerCorrectionResolved = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportBrokerCorrectionResolved;

    // Export 3L
    public const string ExportComplianceEvaluated = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportComplianceEvaluated;
    public const string ExportComplianceRevalidated = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportComplianceRevalidated;
    public const string ExportComplianceRequirementSatisfied = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportComplianceRequirementSatisfied;
    public const string ExportCountryRulePackCreated = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportCountryRulePackCreated;
    public const string ExportCountryRuleAdded = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportCountryRuleAdded;
    public const string ExportCountryRulePackVerified = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportCountryRulePackVerified;
    public const string ExportCountryRulePackSuperseded = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportCountryRulePackSuperseded;
    public const string ExportCountryResearchCreated = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportCountryResearchCreated;
    public const string ExportCountryResearchFindingAdded = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportCountryResearchFindingAdded;
    public const string ExportCountryResearchReviewed = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportCountryResearchReviewed;
    public const string ExportCountryResearchCompleted = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportCountryResearchCompleted;
    public const string ExportCountryResearchLinked = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportCountryResearchLinked;
    public const string ExportCountryKnowledgeCreated = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportCountryKnowledgeCreated;
    public const string ExportCountryKnowledgeVerified = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportCountryKnowledgeVerified;
    public const string ExportCountryKnowledgeSuperseded = NevaxisFactOS.Persistence.Export.ExportAuditEventTypes.ExportCountryKnowledgeSuperseded;

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        AuthenticationSucceeded,
        AuthenticationFailed,
        TenantAccessGranted,
        TenantAccessDenied,
        AuthorizationGranted,
        AuthorizationDenied,
        AuditWriteFailed,
        InfrastructureConfigurationRejected,
        AiDecisionRecorded,
        ProductionPlanDraftCreated,
        ProductionPlanDraftUpdated,
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
        PlanningLogisticsConstraintsEvaluated,
        PlanningLogisticsRiskChanged,
        ProductionOrderCreated,
        ProductionOrderStarted,
        ProductionOrderPaused,
        ProductionOrderResumed,
        ProductionOrderCompleted,
        ProductionOrderCompletedShort,
        ProductionPlanHandoffConsumed,
        ProductionQuantityGoodRecorded,
        ProductionQuantityScrapRecorded,
        ProductionQuantityReversed,
        ProductionDowntimeStarted,
        ProductionDowntimeEnded,
        ProductionSetupStarted,
        ProductionSetupCompleted,
        ProductionCycleManualRecorded,
        ProductionExceptionRaised,
        ProductionExceptionAcknowledged,
        ProductionExceptionResolved,
        ProductionDowntimeRecovered,
        ProductionSetupRecovered,
        ProductionShiftAssigned,
        ProductionShiftReassigned,
        ProductionOperatorAssigned,
        ProductionOperatorReleased,
        ProductionHandoverCreated,
        ProductionExceptionAssigned,
        ProductionExceptionUnassigned,
        ExportCaseCreated,
        ExportCaseUpdated,
        ExportDocumentRegistered,
        ExportDocumentVerified,
        ExportDocumentRejected,
        ExportReadinessChanged,
        ExportPackingUnitCreated,
        ExportPackingUnitUpdated,
        ExportPackingItemAdded,
        ExportPackingCompleted,
        ExportPackingVerified,
        ExportShipmentCreated,
        ExportShipmentUpdated,
        ExportShipmentStatusChanged,
        ExportBrokerWorkspaceCreated,
        ExportBrokerPackagePrepared,
        ExportBrokerPackageSent,
        ExportBrokerWorkspaceAcknowledged,
        ExportBrokerCorrectionCreated,
        ExportBrokerCorrectionResolved,
        ExportComplianceEvaluated,
        ExportComplianceRevalidated,
        ExportComplianceRequirementSatisfied,
        ExportCountryRulePackCreated,
        ExportCountryRuleAdded,
        ExportCountryRulePackVerified,
        ExportCountryRulePackSuperseded,
        ExportCountryResearchCreated,
        ExportCountryResearchFindingAdded,
        ExportCountryResearchReviewed,
        ExportCountryResearchCompleted,
        ExportCountryResearchLinked,
        ExportCountryKnowledgeCreated,
        ExportCountryKnowledgeVerified,
        ExportCountryKnowledgeSuperseded,
    };
}
