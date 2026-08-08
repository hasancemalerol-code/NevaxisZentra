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
}
