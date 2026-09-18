namespace NevaxisZentra.Persistence.Outbox;

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
    public const string PlanningLogisticsRiskHigh = "planning-logistics-risk-high";
    public const string PlanningLogisticsDeadlineBreached = "planning-logistics-deadline-breached";
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

    // 5L — shift
    public const string ProductionShiftAssigned = "production-order-shift-assigned";
    public const string ProductionShiftReassigned = "production-order-shift-reassigned";

    // 5L — operator
    public const string ProductionOperatorAssigned = "production-order-operator-assigned";
    public const string ProductionOperatorReleased = "production-order-operator-released";

    // 5L — handover
    public const string ProductionHandoverCreated = "production-order-shift-handover-created";

    // 5L — exception routing
    public const string ProductionExceptionAssigned = "production-order-exception-assigned";
    public const string ProductionExceptionUnassigned = "production-order-exception-unassigned";

    // Export
    public const string ExportCaseCreated = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportCaseCreated;
    public const string ExportDocumentRegistered = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportDocumentRegistered;
    public const string ExportDocumentVerified = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportDocumentVerified;
    public const string ExportReadinessChanged = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportReadinessChanged;
    public const string ExportReady = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportReady;
    public const string ExportBlocked = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportBlocked;

    // Export 2L
    public const string ExportPackingCompleted = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportPackingCompleted;
    public const string ExportPackingVerified = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportPackingVerified;
    public const string ExportShipmentCreated = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportShipmentCreated;
    public const string ExportShipmentStatusChanged = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportShipmentStatusChanged;
    public const string ExportBrokerPackageSent = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportBrokerPackageSent;
    public const string ExportBrokerWorkspaceAcknowledged = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportBrokerWorkspaceAcknowledged;

    // Export 3L
    public const string ExportComplianceBlocked = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportComplianceBlocked;
    public const string ExportComplianceReady = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportComplianceReady;
    public const string ExportComplianceRevalidationRequired = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportComplianceRevalidationRequired;
    public const string ExportCountryResearchCompleted = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportCountryResearchCompleted;
    public const string ExportCountryRulePackVerified = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportCountryRulePackVerified;
    public const string ExportCountryKnowledgeUpdated = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportCountryKnowledgeUpdated;

    // Export 4L
    public const string ExportAiAnalysisCompleted = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportAiAnalysisCompleted;
    public const string ExportAiHighRiskFound = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportAiHighRiskFound;
    public const string ExportAiRecommendationAccepted = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportAiRecommendationAccepted;
    public const string ExportAiTaskRequested = NevaxisZentra.Persistence.Export.ExportOutboxMessageTypes.ExportAiTaskRequested;

    // ERP-1L / ERP-2L
    public const string ErpConnectionCreated = NevaxisZentra.Persistence.Erp.ErpOutboxMessageTypes.ConnectionCreated;
    public const string ErpSyncCompleted = NevaxisZentra.Persistence.Erp.ErpOutboxMessageTypes.SyncCompleted;
    public const string ErpSyncFailed = NevaxisZentra.Persistence.Erp.ErpOutboxMessageTypes.SyncFailed;
    public const string ErpMappingConfirmed = NevaxisZentra.Persistence.Erp.ErpOutboxMessageTypes.MappingConfirmed;
    public const string ErpSyncOrdersCompleted = NevaxisZentra.Persistence.Erp.ErpOutboxMessageTypes.SyncOrdersCompleted;
    public const string ErpSyncOrdersFailed = NevaxisZentra.Persistence.Erp.ErpOutboxMessageTypes.SyncOrdersFailed;

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
            PlanningLogisticsRiskHigh,
            PlanningLogisticsDeadlineBreached,
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
            ExportDocumentRegistered,
            ExportDocumentVerified,
            ExportReadinessChanged,
            ExportReady,
            ExportBlocked,
            ExportPackingCompleted,
            ExportPackingVerified,
            ExportShipmentCreated,
            ExportShipmentStatusChanged,
            ExportBrokerPackageSent,
            ExportBrokerWorkspaceAcknowledged,
            ExportComplianceBlocked,
            ExportComplianceReady,
            ExportComplianceRevalidationRequired,
            ExportCountryResearchCompleted,
            ExportCountryRulePackVerified,
            ExportCountryKnowledgeUpdated,
            ExportAiAnalysisCompleted,
            ExportAiHighRiskFound,
            ExportAiRecommendationAccepted,
            ExportAiTaskRequested,
            ErpConnectionCreated,
            ErpSyncCompleted,
            ErpSyncFailed,
            ErpMappingConfirmed,
            ErpSyncOrdersCompleted,
            ErpSyncOrdersFailed,
        ],
        StringComparer.Ordinal);
}
