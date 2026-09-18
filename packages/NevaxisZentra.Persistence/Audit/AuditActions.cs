namespace NevaxisZentra.Persistence.Audit;

public static class AuditActions
{
    public const string Authenticate = "authenticate";
    public const string AccessTenant = "access.tenant";
    public const string Authorize = "authorize";
    public const string WriteAudit = "write.audit";
    public const string ValidateConfiguration = "validate.configuration";
    public const string AppendAiDecision = "ai.decision.append";
    public const string ProductionPlanDraftCreate = "production.plan.draft.create";
    public const string ProductionPlanDraftUpdate = "production.plan.draft.update";
    public const string ProductionPlanSubmitForReview = "production.plan.workflow.submit.for.review";
    public const string ProductionPlanRequestRevision = "production.plan.workflow.request.revision";
    public const string ProductionPlanApprove = "production.plan.workflow.approve";
    public const string ProductionPlanReject = "production.plan.workflow.reject";
    public const string ProductionPlanSchedule = "production.plan.schedule";
    public const string ProductionPlanTransferRequest = "production.plan.transfer.request";
    public const string ProductionPlanTransferStart = "production.plan.transfer.start";
    public const string ProductionPlanTransferSucceed = "production.plan.transfer.succeed";
    public const string ProductionPlanTransferFail = "production.plan.transfer.fail";
    public const string ProductionPlanTransferMarkUncertain = "production.plan.transfer.mark.uncertain";
    public const string ProductionPlanHandoffPrepare = "production.plan.handoff.prepare";
    public const string PlanningLogisticsConstraintsEvaluate = "planning.logistics.constraints.evaluate";
    public const string PlanningLogisticsRiskChange = "planning.logistics.risk.change";
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

    // 5L — shift
    public const string ProductionShiftAssign = "production.order.shift.assign";
    public const string ProductionShiftReassign = "production.order.shift.reassign";

    // 5L — operator
    public const string ProductionOperatorAssign = "production.order.operator.assign";
    public const string ProductionOperatorRelease = "production.order.operator.release";

    // 5L — handover
    public const string ProductionHandoverCreate = "production.order.handover.create";

    // 5L — exception routing
    public const string ProductionExceptionAssign = "production.order.exception.assign";
    public const string ProductionExceptionUnassign = "production.order.exception.unassign";

    // Export
    public const string ExportCaseCreated = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportCaseCreated;
    public const string ExportCaseUpdated = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportCaseUpdated;
    public const string ExportDocumentRegistered = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportDocumentRegistered;
    public const string ExportDocumentVerified = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportDocumentVerified;
    public const string ExportDocumentRejected = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportDocumentRejected;
    public const string ExportReadinessChanged = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportReadinessChanged;

    // Export 2L
    public const string ExportPackingUnitCreated = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportPackingUnitCreated;
    public const string ExportPackingUnitUpdated = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportPackingUnitUpdated;
    public const string ExportPackingItemAdded = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportPackingItemAdded;
    public const string ExportPackingCompleted = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportPackingCompleted;
    public const string ExportPackingVerified = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportPackingVerified;
    public const string ExportShipmentCreated = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportShipmentCreated;
    public const string ExportShipmentUpdated = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportShipmentUpdated;
    public const string ExportShipmentStatusChanged = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportShipmentStatusChanged;
    public const string ExportBrokerWorkspaceCreated = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportBrokerWorkspaceCreated;
    public const string ExportBrokerPackagePrepared = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportBrokerPackagePrepared;
    public const string ExportBrokerPackageSent = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportBrokerPackageSent;
    public const string ExportBrokerWorkspaceAcknowledged = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportBrokerWorkspaceAcknowledged;
    public const string ExportBrokerCorrectionCreated = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportBrokerCorrectionCreated;
    public const string ExportBrokerCorrectionResolved = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportBrokerCorrectionResolved;

    // Export 3L
    public const string ExportComplianceEvaluated = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportComplianceEvaluated;
    public const string ExportComplianceRevalidated = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportComplianceRevalidated;
    public const string ExportComplianceRequirementSatisfied = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportComplianceRequirementSatisfied;
    public const string ExportCountryRulePackCreated = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportCountryRulePackCreated;
    public const string ExportCountryRuleAdded = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportCountryRuleAdded;
    public const string ExportCountryRulePackVerified = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportCountryRulePackVerified;
    public const string ExportCountryRulePackSuperseded = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportCountryRulePackSuperseded;
    public const string ExportCountryResearchCreated = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportCountryResearchCreated;
    public const string ExportCountryResearchFindingAdded = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportCountryResearchFindingAdded;
    public const string ExportCountryResearchReviewed = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportCountryResearchReviewed;
    public const string ExportCountryResearchCompleted = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportCountryResearchCompleted;
    public const string ExportCountryResearchLinked = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportCountryResearchLinked;
    public const string ExportCountryKnowledgeCreated = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportCountryKnowledgeCreated;
    public const string ExportCountryKnowledgeVerified = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportCountryKnowledgeVerified;
    public const string ExportCountryKnowledgeSuperseded = NevaxisZentra.Persistence.Export.ExportAuditActions.ExportCountryKnowledgeSuperseded;

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Authenticate,
        AccessTenant,
        Authorize,
        WriteAudit,
        ValidateConfiguration,
        AppendAiDecision,
        ProductionPlanDraftCreate,
        ProductionPlanDraftUpdate,
        ProductionPlanSubmitForReview,
        ProductionPlanRequestRevision,
        ProductionPlanApprove,
        ProductionPlanReject,
        ProductionPlanSchedule,
        ProductionPlanTransferRequest,
        ProductionPlanTransferStart,
        ProductionPlanTransferSucceed,
        ProductionPlanTransferFail,
        ProductionPlanTransferMarkUncertain,
        ProductionPlanHandoffPrepare,
        PlanningLogisticsConstraintsEvaluate,
        PlanningLogisticsRiskChange,
        ProductionOrderCreate,
        ProductionOrderStart,
        ProductionOrderPause,
        ProductionOrderResume,
        ProductionOrderComplete,
        ProductionOrderCompleteShort,
        ProductionPlanHandoffConsume,
        ProductionQuantityGoodRecord,
        ProductionQuantityScrapRecord,
        ProductionQuantityReverse,
        ProductionDowntimeStart,
        ProductionDowntimeEnd,
        ProductionSetupStart,
        ProductionSetupComplete,
        ProductionCycleManualRecord,
        ProductionExceptionRaise,
        ProductionExceptionAcknowledge,
        ProductionExceptionResolve,
        ProductionDowntimeRecover,
        ProductionSetupRecover,
        ProductionShiftAssign,
        ProductionShiftReassign,
        ProductionOperatorAssign,
        ProductionOperatorRelease,
        ProductionHandoverCreate,
        ProductionExceptionAssign,
        ProductionExceptionUnassign,
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
