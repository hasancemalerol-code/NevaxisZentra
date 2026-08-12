namespace CrevaFactOS.Persistence.Material;

public static class MaterialLotValueConversions
{
    public static string ToStorage(MaterialLotStatus value) => value switch
    {
        MaterialLotStatus.Draft => "draft",
        MaterialLotStatus.Received => "received",
        MaterialLotStatus.Available => "available",
        MaterialLotStatus.QualityHold => "quality-hold",
        MaterialLotStatus.Blocked => "blocked",
        MaterialLotStatus.Closed => "closed",
        _ => "draft",
    };

    public static MaterialLotStatus LotStatusFromStorage(string value) => value switch
    {
        "draft" => MaterialLotStatus.Draft,
        "received" => MaterialLotStatus.Received,
        "available" => MaterialLotStatus.Available,
        "quality-hold" => MaterialLotStatus.QualityHold,
        "blocked" => MaterialLotStatus.Blocked,
        "closed" => MaterialLotStatus.Closed,
        _ => MaterialLotStatus.Draft,
    };

    public static string ToStorage(MaterialLotQualityStatus value) => value switch
    {
        MaterialLotQualityStatus.Pending => "pending",
        MaterialLotQualityStatus.Accepted => "accepted",
        MaterialLotQualityStatus.ConditionallyAccepted => "conditionally-accepted",
        MaterialLotQualityStatus.Rejected => "rejected",
        MaterialLotQualityStatus.NotRequired => "not-required",
        _ => "unknown",
    };

    public static MaterialLotQualityStatus LotQualityStatusFromStorage(string value) => value switch
    {
        "pending" => MaterialLotQualityStatus.Pending,
        "accepted" => MaterialLotQualityStatus.Accepted,
        "conditionally-accepted" => MaterialLotQualityStatus.ConditionallyAccepted,
        "rejected" => MaterialLotQualityStatus.Rejected,
        "not-required" => MaterialLotQualityStatus.NotRequired,
        _ => MaterialLotQualityStatus.Unknown,
    };

    public static string ToStorage(MaterialLotEventType value) => value switch
    {
        MaterialLotEventType.LotCreated => "lot-created",
        MaterialLotEventType.ReceiptObserved => "receipt-observed",
        MaterialLotEventType.LocationTransfer => "location-transfer",
        MaterialLotEventType.StatusChanged => "status-changed",
        MaterialLotEventType.QualityStatusRecorded => "quality-status-recorded",
        MaterialLotEventType.CertificateLinked => "certificate-linked",
        MaterialLotEventType.NoteRecorded => "note-recorded",
        _ => "lot-created",
    };

    public static MaterialLotEventType LotEventTypeFromStorage(string value) => value switch
    {
        "lot-created" => MaterialLotEventType.LotCreated,
        "receipt-observed" => MaterialLotEventType.ReceiptObserved,
        "location-transfer" => MaterialLotEventType.LocationTransfer,
        "status-changed" => MaterialLotEventType.StatusChanged,
        "quality-status-recorded" => MaterialLotEventType.QualityStatusRecorded,
        "certificate-linked" => MaterialLotEventType.CertificateLinked,
        "note-recorded" => MaterialLotEventType.NoteRecorded,
        _ => MaterialLotEventType.LotCreated,
    };

    public static string ToStorage(MaterialLotCertificateType value) => value switch
    {
        MaterialLotCertificateType.CertificateOfAnalysis => "certificate-of-analysis",
        MaterialLotCertificateType.MaterialCertificate => "material-certificate",
        MaterialLotCertificateType.SafetyDataSheet => "safety-data-sheet",
        MaterialLotCertificateType.TechnicalDataSheet => "technical-data-sheet",
        MaterialLotCertificateType.ConformityCertificate => "conformity-certificate",
        _ => "other",
    };

    public static MaterialLotCertificateType LotCertificateTypeFromStorage(string value) => value switch
    {
        "certificate-of-analysis" => MaterialLotCertificateType.CertificateOfAnalysis,
        "material-certificate" => MaterialLotCertificateType.MaterialCertificate,
        "safety-data-sheet" => MaterialLotCertificateType.SafetyDataSheet,
        "technical-data-sheet" => MaterialLotCertificateType.TechnicalDataSheet,
        "conformity-certificate" => MaterialLotCertificateType.ConformityCertificate,
        _ => MaterialLotCertificateType.Other,
    };

    public static int LotStatusSortPriority(MaterialLotStatus status) => status switch
    {
        MaterialLotStatus.Available => 0,
        MaterialLotStatus.Received => 1,
        MaterialLotStatus.QualityHold => 2,
        MaterialLotStatus.Draft => 3,
        MaterialLotStatus.Blocked => 4,
        MaterialLotStatus.Closed => 5,
        _ => 6,
    };
}
