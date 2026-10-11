namespace NevaxisZentra.Persistence.Audit;

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
    public const string FactoryCopilotQueryAnswered = "factorycopilot.query.answered";
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
    public const string ExportCaseCreated = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportCaseCreated;
    public const string ExportCaseUpdated = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportCaseUpdated;
    public const string ExportDocumentRegistered = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportDocumentRegistered;
    public const string ExportDocumentVerified = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportDocumentVerified;
    public const string ExportDocumentRejected = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportDocumentRejected;
    public const string ExportReadinessChanged = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportReadinessChanged;

    // Export 2L
    public const string ExportPackingUnitCreated = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportPackingUnitCreated;
    public const string ExportPackingUnitUpdated = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportPackingUnitUpdated;
    public const string ExportPackingItemAdded = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportPackingItemAdded;
    public const string ExportPackingCompleted = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportPackingCompleted;
    public const string ExportPackingVerified = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportPackingVerified;
    public const string ExportShipmentCreated = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportShipmentCreated;
    public const string ExportShipmentUpdated = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportShipmentUpdated;
    public const string ExportShipmentStatusChanged = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportShipmentStatusChanged;
    public const string ExportBrokerWorkspaceCreated = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportBrokerWorkspaceCreated;
    public const string ExportBrokerPackagePrepared = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportBrokerPackagePrepared;
    public const string ExportBrokerPackageSent = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportBrokerPackageSent;
    public const string ExportBrokerWorkspaceAcknowledged = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportBrokerWorkspaceAcknowledged;
    public const string ExportBrokerCorrectionCreated = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportBrokerCorrectionCreated;
    public const string ExportBrokerCorrectionResolved = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportBrokerCorrectionResolved;

    // Export 3L
    public const string ExportComplianceEvaluated = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportComplianceEvaluated;
    public const string ExportComplianceRevalidated = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportComplianceRevalidated;
    public const string ExportComplianceRequirementSatisfied = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportComplianceRequirementSatisfied;
    public const string ExportCountryRulePackCreated = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportCountryRulePackCreated;
    public const string ExportCountryRuleAdded = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportCountryRuleAdded;
    public const string ExportCountryRulePackVerified = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportCountryRulePackVerified;
    public const string ExportCountryRulePackSuperseded = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportCountryRulePackSuperseded;
    public const string ExportCountryResearchCreated = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportCountryResearchCreated;
    public const string ExportCountryResearchFindingAdded = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportCountryResearchFindingAdded;
    public const string ExportCountryResearchReviewed = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportCountryResearchReviewed;
    public const string ExportCountryResearchCompleted = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportCountryResearchCompleted;
    public const string ExportCountryResearchLinked = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportCountryResearchLinked;
    public const string ExportCountryKnowledgeCreated = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportCountryKnowledgeCreated;
    public const string ExportCountryKnowledgeVerified = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportCountryKnowledgeVerified;
    public const string ExportCountryKnowledgeSuperseded = NevaxisZentra.Persistence.Export.ExportAuditEventTypes.ExportCountryKnowledgeSuperseded;

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
        FactoryCopilotQueryAnswered,
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
