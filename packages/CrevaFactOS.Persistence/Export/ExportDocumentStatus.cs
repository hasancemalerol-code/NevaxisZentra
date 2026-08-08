namespace CrevaFactOS.Persistence.Export;

/// <summary>
/// Document readiness/status values. <see cref="Missing"/> is derived for readiness only;
/// physical document rows store draft/received/verified/rejected.
/// </summary>
public enum ExportDocumentStatus
{
    Missing,
    Draft,
    Received,
    Verified,
    Rejected,
}
