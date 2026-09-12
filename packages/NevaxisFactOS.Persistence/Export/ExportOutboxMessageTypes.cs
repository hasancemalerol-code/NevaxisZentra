namespace NevaxisFactOS.Persistence.Export;

/// <summary>
/// Outbox message type constants for the Export bounded context.
/// Values must be registered in the DB outbox_messages type check constraint.
/// </summary>
public static class ExportOutboxMessageTypes
{
    public const string ExportCaseCreated = "export-case-created";
    public const string ExportDocumentRegistered = "export-document-registered";
    public const string ExportDocumentVerified = "export-document-verified";
    public const string ExportReadinessChanged = "export-readiness-changed";
    public const string ExportReady = "export-ready";
    public const string ExportBlocked = "export-blocked";

    // EXPORT-2L
    public const string ExportPackingCompleted = "export-packing-completed";
    public const string ExportPackingVerified = "export-packing-verified";
    public const string ExportShipmentCreated = "export-shipment-created";
    public const string ExportShipmentStatusChanged = "export-shipment-status-changed";
    public const string ExportBrokerPackageSent = "export-broker-package-sent";
    public const string ExportBrokerWorkspaceAcknowledged = "export-broker-workspace-acknowledged";

    // EXPORT-3L
    public const string ExportComplianceBlocked = "export-compliance-blocked";
    public const string ExportComplianceReady = "export-compliance-ready";
    public const string ExportComplianceRevalidationRequired = "export-compliance-revalidation-required";
    public const string ExportCountryResearchCompleted = "export-country-research-completed";
    public const string ExportCountryRulePackVerified = "export-country-rule-pack-verified";
    public const string ExportCountryKnowledgeUpdated = "export-country-knowledge-updated";

    // EXPORT-4L
    public const string ExportAiAnalysisCompleted = ExportAiOutboxMessageTypes.ExportAiAnalysisCompleted;
    public const string ExportAiHighRiskFound = ExportAiOutboxMessageTypes.ExportAiHighRiskFound;
    public const string ExportAiRecommendationAccepted = ExportAiOutboxMessageTypes.ExportAiRecommendationAccepted;
    public const string ExportAiTaskRequested = ExportAiOutboxMessageTypes.ExportAiTaskRequested;
}
