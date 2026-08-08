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
}
