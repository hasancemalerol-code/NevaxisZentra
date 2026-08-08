namespace CrevaFactOS.Persistence.Audit;

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
    public const string ExportCaseCreated = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportCaseCreated;
    public const string ExportCaseUpdated = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportCaseUpdated;
    public const string ExportDocumentRegistered = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportDocumentRegistered;
    public const string ExportDocumentVerified = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportDocumentVerified;
    public const string ExportDocumentRejected = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportDocumentRejected;
    public const string ExportReadinessChanged = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportReadinessChanged;

    // Export 2L
    public const string ExportPackingUnitCreated = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportPackingUnitCreated;
    public const string ExportPackingUnitUpdated = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportPackingUnitUpdated;
    public const string ExportPackingItemAdded = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportPackingItemAdded;
    public const string ExportPackingCompleted = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportPackingCompleted;
    public const string ExportPackingVerified = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportPackingVerified;
    public const string ExportShipmentCreated = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportShipmentCreated;
    public const string ExportShipmentUpdated = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportShipmentUpdated;
    public const string ExportShipmentStatusChanged = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportShipmentStatusChanged;
    public const string ExportBrokerWorkspaceCreated = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportBrokerWorkspaceCreated;
    public const string ExportBrokerPackagePrepared = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportBrokerPackagePrepared;
    public const string ExportBrokerPackageSent = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportBrokerPackageSent;
    public const string ExportBrokerWorkspaceAcknowledged = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportBrokerWorkspaceAcknowledged;
    public const string ExportBrokerCorrectionCreated = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportBrokerCorrectionCreated;
    public const string ExportBrokerCorrectionResolved = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportBrokerCorrectionResolved;

    // Export 3L
    public const string ExportComplianceEvaluated = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportComplianceEvaluated;
    public const string ExportComplianceRevalidated = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportComplianceRevalidated;
    public const string ExportComplianceRequirementSatisfied = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportComplianceRequirementSatisfied;
    public const string ExportCountryRulePackCreated = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportCountryRulePackCreated;
    public const string ExportCountryRuleAdded = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportCountryRuleAdded;
    public const string ExportCountryRulePackVerified = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportCountryRulePackVerified;
    public const string ExportCountryRulePackSuperseded = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportCountryRulePackSuperseded;
    public const string ExportCountryResearchCreated = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportCountryResearchCreated;
    public const string ExportCountryResearchFindingAdded = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportCountryResearchFindingAdded;
    public const string ExportCountryResearchReviewed = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportCountryResearchReviewed;
    public const string ExportCountryResearchCompleted = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportCountryResearchCompleted;
    public const string ExportCountryResearchLinked = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportCountryResearchLinked;
    public const string ExportCountryKnowledgeCreated = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportCountryKnowledgeCreated;
    public const string ExportCountryKnowledgeVerified = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportCountryKnowledgeVerified;
    public const string ExportCountryKnowledgeSuperseded = CrevaFactOS.Persistence.Export.ExportAuditActions.ExportCountryKnowledgeSuperseded;

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
