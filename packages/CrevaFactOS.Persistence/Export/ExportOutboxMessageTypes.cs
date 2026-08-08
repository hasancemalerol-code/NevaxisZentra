namespace CrevaFactOS.Persistence.Export;

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
}
