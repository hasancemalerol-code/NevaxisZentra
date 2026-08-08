namespace CrevaFactOS.Persistence.Export;

/// <summary>
/// Audit action constants for the Export bounded context.
/// Values must be registered in the DB audit_records action taxonomy check constraint.
/// </summary>
public static class ExportAuditActions
{
    public const string ExportCaseCreated = "export.case.created";
    public const string ExportCaseUpdated = "export.case.updated";
    public const string ExportDocumentRegistered = "export.document.registered";
    public const string ExportDocumentVerified = "export.document.verified";
    public const string ExportDocumentRejected = "export.document.rejected";
    public const string ExportReadinessChanged = "export.readiness.changed";

    // EXPORT-2L
    public const string ExportPackingUnitCreated = "export.packing.unit.created";
    public const string ExportPackingUnitUpdated = "export.packing.unit.updated";
    public const string ExportPackingItemAdded = "export.packing.item.added";
    public const string ExportPackingCompleted = "export.packing.completed";
    public const string ExportPackingVerified = "export.packing.verified";
    public const string ExportShipmentCreated = "export.shipment.created";
    public const string ExportShipmentUpdated = "export.shipment.updated";
    public const string ExportShipmentStatusChanged = "export.shipment.status.changed";
    public const string ExportBrokerWorkspaceCreated = "export.broker.workspace.created";
    public const string ExportBrokerPackagePrepared = "export.broker.package.prepared";
    public const string ExportBrokerPackageSent = "export.broker.package.sent";
    public const string ExportBrokerWorkspaceAcknowledged = "export.broker.workspace.acknowledged";
    public const string ExportBrokerCorrectionCreated = "export.broker.correction.created";
    public const string ExportBrokerCorrectionResolved = "export.broker.correction.resolved";

    // EXPORT-3L
    public const string ExportComplianceEvaluated = "export.compliance.evaluated";
    public const string ExportComplianceRevalidated = "export.compliance.revalidated";
    public const string ExportComplianceRequirementSatisfied = "export.compliance.requirement.satisfied";
    public const string ExportCountryRulePackCreated = "export.country.rule.pack.created";
    public const string ExportCountryRuleAdded = "export.country.rule.added";
    public const string ExportCountryRulePackVerified = "export.country.rule.pack.verified";
    public const string ExportCountryRulePackSuperseded = "export.country.rule.pack.superseded";
    public const string ExportCountryResearchCreated = "export.country.research.created";
    public const string ExportCountryResearchFindingAdded = "export.country.research.finding.added";
    public const string ExportCountryResearchReviewed = "export.country.research.reviewed";
    public const string ExportCountryResearchCompleted = "export.country.research.completed";
    public const string ExportCountryResearchLinked = "export.country.research.linked";
    public const string ExportCountryKnowledgeCreated = "export.country.knowledge.created";
    public const string ExportCountryKnowledgeVerified = "export.country.knowledge.verified";
    public const string ExportCountryKnowledgeSuperseded = "export.country.knowledge.superseded";
}

/// <summary>
/// Audit event type constants for the Export bounded context.
/// Values must be registered in the DB audit_records event taxonomy check constraint.
/// </summary>
public static class ExportAuditEventTypes
{
    public const string ExportCaseCreated = "export.case.created";
    public const string ExportCaseUpdated = "export.case.updated";
    public const string ExportDocumentRegistered = "export.document.registered";
    public const string ExportDocumentVerified = "export.document.verified";
    public const string ExportDocumentRejected = "export.document.rejected";
    public const string ExportReadinessChanged = "export.readiness.changed";

    // EXPORT-2L
    public const string ExportPackingUnitCreated = "export.packing.unit.created";
    public const string ExportPackingUnitUpdated = "export.packing.unit.updated";
    public const string ExportPackingItemAdded = "export.packing.item.added";
    public const string ExportPackingCompleted = "export.packing.completed";
    public const string ExportPackingVerified = "export.packing.verified";
    public const string ExportShipmentCreated = "export.shipment.created";
    public const string ExportShipmentUpdated = "export.shipment.updated";
    public const string ExportShipmentStatusChanged = "export.shipment.status.changed";
    public const string ExportBrokerWorkspaceCreated = "export.broker.workspace.created";
    public const string ExportBrokerPackagePrepared = "export.broker.package.prepared";
    public const string ExportBrokerPackageSent = "export.broker.package.sent";
    public const string ExportBrokerWorkspaceAcknowledged = "export.broker.workspace.acknowledged";
    public const string ExportBrokerCorrectionCreated = "export.broker.correction.created";
    public const string ExportBrokerCorrectionResolved = "export.broker.correction.resolved";

    // EXPORT-3L
    public const string ExportComplianceEvaluated = "export.compliance.evaluated";
    public const string ExportComplianceRevalidated = "export.compliance.revalidated";
    public const string ExportComplianceRequirementSatisfied = "export.compliance.requirement.satisfied";
    public const string ExportCountryRulePackCreated = "export.country.rule.pack.created";
    public const string ExportCountryRuleAdded = "export.country.rule.added";
    public const string ExportCountryRulePackVerified = "export.country.rule.pack.verified";
    public const string ExportCountryRulePackSuperseded = "export.country.rule.pack.superseded";
    public const string ExportCountryResearchCreated = "export.country.research.created";
    public const string ExportCountryResearchFindingAdded = "export.country.research.finding.added";
    public const string ExportCountryResearchReviewed = "export.country.research.reviewed";
    public const string ExportCountryResearchCompleted = "export.country.research.completed";
    public const string ExportCountryResearchLinked = "export.country.research.linked";
    public const string ExportCountryKnowledgeCreated = "export.country.knowledge.created";
    public const string ExportCountryKnowledgeVerified = "export.country.knowledge.verified";
    public const string ExportCountryKnowledgeSuperseded = "export.country.knowledge.superseded";
}

/// <summary>Activity event types appended to export_case_activities (avoid spam).</summary>
public static class ExportActivityEventTypes
{
    public const string ExportCaseCreated = "export-case-created";
    public const string DestinationCountrySet = "destination-country-set";
    public const string DocumentRegistered = "document-registered";
    public const string DocumentVerified = "document-verified";
    public const string DocumentRejected = "document-rejected";
    public const string ReadinessChanged = "readiness-changed";
    public const string ExportReady = "export-ready";
    public const string ExportBlocked = "export-blocked";

    // EXPORT-2L
    public const string PackingUnitCreated = "packing-unit-created";
    public const string PackingUnitUpdated = "packing-unit-updated";
    public const string PackingItemAdded = "packing-item-added";
    public const string PackingCompleted = "packing-completed";
    public const string PackingVerified = "packing-verified";
    public const string ShipmentCreated = "shipment-created";
    public const string ShipmentUpdated = "shipment-updated";
    public const string ShipmentStatusChanged = "shipment-status-changed";
    public const string BrokerWorkspaceCreated = "broker-workspace-created";
    public const string BrokerPackagePrepared = "broker-package-prepared";
    public const string BrokerPackageSent = "broker-package-sent";
    public const string BrokerWorkspaceAcknowledged = "broker-workspace-acknowledged";
    public const string BrokerCorrectionCreated = "broker-correction-created";
    public const string BrokerCorrectionResolved = "broker-correction-resolved";

    // EXPORT-3L
    public const string ComplianceEvaluated = "compliance-evaluated";
    public const string ComplianceBlocked = "compliance-blocked";
    public const string ComplianceCleared = "compliance-cleared";
    public const string ComplianceRevalidationRequired = "compliance-revalidation-required";
    public const string CountryResearchCreated = "country-research-created";
    public const string CountryResearchVerified = "country-research-verified";
    public const string CountryResearchLinked = "country-research-linked";
    public const string CountryRulePackUpdated = "country-rule-pack-updated";
}
