namespace CrevaFactOS.Persistence.Export;

public static class ExportValueConversions
{
    private static readonly Dictionary<ExportCaseStatus, string> CaseStatusToStorage =
        new()
        {
            [ExportCaseStatus.Open] = "open",
            [ExportCaseStatus.Preparing] = "preparing",
            [ExportCaseStatus.Blocked] = "blocked",
            [ExportCaseStatus.Ready] = "ready",
            [ExportCaseStatus.Closed] = "closed",
        };

    private static readonly Dictionary<string, ExportCaseStatus> StorageToCaseStatus =
        CaseStatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<ExportReadinessStatus, string> ReadinessStatusToStorage =
        new()
        {
            [ExportReadinessStatus.NotReady] = "not-ready",
            [ExportReadinessStatus.Attention] = "attention",
            [ExportReadinessStatus.Ready] = "ready",
        };

    private static readonly Dictionary<string, ExportReadinessStatus> StorageToReadinessStatus =
        ReadinessStatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<ExportDocumentStatus, string> DocumentStatusToStorage =
        new()
        {
            [ExportDocumentStatus.Missing] = "missing",
            [ExportDocumentStatus.Draft] = "draft",
            [ExportDocumentStatus.Received] = "received",
            [ExportDocumentStatus.Verified] = "verified",
            [ExportDocumentStatus.Rejected] = "rejected",
        };

    private static readonly Dictionary<string, ExportDocumentStatus> StorageToDocumentStatus =
        DocumentStatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<ExportActivitySourceType, string> ActivitySourceToStorage =
        new()
        {
            [ExportActivitySourceType.System] = "system",
            [ExportActivitySourceType.User] = "user",
            [ExportActivitySourceType.Integration] = "integration",
            [ExportActivitySourceType.Ai] = "ai",
        };

    private static readonly Dictionary<string, ExportActivitySourceType> StorageToActivitySource =
        ActivitySourceToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    // EXPORT-2L enums
    private static readonly Dictionary<ExportPackingStatus, string> PackingStatusToStorage =
        new()
        {
            [ExportPackingStatus.Draft] = "draft",
            [ExportPackingStatus.Packing] = "packing",
            [ExportPackingStatus.Complete] = "complete",
            [ExportPackingStatus.Verified] = "verified",
        };

    private static readonly Dictionary<string, ExportPackingStatus> StorageToPackingStatus =
        PackingStatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<ExportShipmentStatus, string> ShipmentStatusToStorage =
        new()
        {
            [ExportShipmentStatus.Draft] = "draft",
            [ExportShipmentStatus.Preparing] = "preparing",
            [ExportShipmentStatus.Ready] = "ready",
            [ExportShipmentStatus.HandedToBroker] = "handed-to-broker",
            [ExportShipmentStatus.ClearedForDispatch] = "cleared-for-dispatch",
            [ExportShipmentStatus.Dispatched] = "dispatched",
            [ExportShipmentStatus.Closed] = "closed",
            [ExportShipmentStatus.Cancelled] = "cancelled",
        };

    private static readonly Dictionary<string, ExportShipmentStatus> StorageToShipmentStatus =
        ShipmentStatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<ExportBrokerWorkspaceStatus, string> BrokerWorkspaceStatusToStorage =
        new()
        {
            [ExportBrokerWorkspaceStatus.Draft] = "draft",
            [ExportBrokerWorkspaceStatus.PackageReady] = "package-ready",
            [ExportBrokerWorkspaceStatus.Sent] = "sent",
            [ExportBrokerWorkspaceStatus.Acknowledged] = "acknowledged",
            [ExportBrokerWorkspaceStatus.CorrectionRequested] = "correction-requested",
            [ExportBrokerWorkspaceStatus.Ready] = "ready",
            [ExportBrokerWorkspaceStatus.Closed] = "closed",
            [ExportBrokerWorkspaceStatus.Cancelled] = "cancelled",
        };

    private static readonly Dictionary<string, ExportBrokerWorkspaceStatus> StorageToBrokerWorkspaceStatus =
        BrokerWorkspaceStatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<ExportBrokerPackageVersionStatus, string> PackageVersionStatusToStorage =
        new()
        {
            [ExportBrokerPackageVersionStatus.Draft] = "draft",
            [ExportBrokerPackageVersionStatus.Sent] = "sent",
            [ExportBrokerPackageVersionStatus.Stale] = "stale",
        };

    private static readonly Dictionary<string, ExportBrokerPackageVersionStatus> StorageToPackageVersionStatus =
        PackageVersionStatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<ExportBrokerCorrectionSeverity, string> CorrectionSeverityToStorage =
        new()
        {
            [ExportBrokerCorrectionSeverity.Info] = "info",
            [ExportBrokerCorrectionSeverity.Warning] = "warning",
            [ExportBrokerCorrectionSeverity.Blocking] = "blocking",
        };

    private static readonly Dictionary<string, ExportBrokerCorrectionSeverity> StorageToCorrectionSeverity =
        CorrectionSeverityToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<ExportBrokerCorrectionStatus, string> CorrectionStatusToStorage =
        new()
        {
            [ExportBrokerCorrectionStatus.Open] = "open",
            [ExportBrokerCorrectionStatus.Resolved] = "resolved",
            [ExportBrokerCorrectionStatus.Cancelled] = "cancelled",
        };

    private static readonly Dictionary<string, ExportBrokerCorrectionStatus> StorageToCorrectionStatus =
        CorrectionStatusToStorage.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    public static string ToStorage(ExportCaseStatus status) =>
        CaseStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportCaseStatus: {status}");

    public static ExportCaseStatus CaseStatusFromStorage(string value) =>
        StorageToCaseStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown export case status value: '{value}'");

    public static bool TryCaseStatusFromStorage(string value, out ExportCaseStatus status) =>
        StorageToCaseStatus.TryGetValue(value, out status);

    public static string ToStorage(ExportReadinessStatus status) =>
        ReadinessStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportReadinessStatus: {status}");

    public static ExportReadinessStatus ReadinessStatusFromStorage(string value) =>
        StorageToReadinessStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown export readiness status value: '{value}'");

    public static string ToStorage(ExportDocumentStatus status) =>
        DocumentStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportDocumentStatus: {status}");

    /// <summary>Physical document rows never persist <see cref="ExportDocumentStatus.Missing"/>.</summary>
    public static string ToPhysicalStorage(ExportDocumentStatus status) =>
        status == ExportDocumentStatus.Missing
            ? throw new InvalidOperationException("Missing document status is derived and cannot be persisted.")
            : ToStorage(status);

    public static ExportDocumentStatus DocumentStatusFromStorage(string value) =>
        StorageToDocumentStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown export document status value: '{value}'");

    public static string ToStorage(ExportActivitySourceType sourceType) =>
        ActivitySourceToStorage.TryGetValue(sourceType, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportActivitySourceType: {sourceType}");

    public static ExportActivitySourceType ActivitySourceFromStorage(string value) =>
        StorageToActivitySource.TryGetValue(value, out var sourceType)
            ? sourceType
            : throw new InvalidOperationException($"Unknown export activity source value: '{value}'");

    // EXPORT-2L conversions
    public static string ToStorage(ExportPackingStatus status) =>
        PackingStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportPackingStatus: {status}");

    public static ExportPackingStatus PackingStatusFromStorage(string value) =>
        StorageToPackingStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown export packing status value: '{value}'");

    public static bool TryPackingStatusFromStorage(string value, out ExportPackingStatus status) =>
        StorageToPackingStatus.TryGetValue(value, out status);

    public static string ToStorage(ExportShipmentStatus status) =>
        ShipmentStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportShipmentStatus: {status}");

    public static ExportShipmentStatus ShipmentStatusFromStorage(string value) =>
        StorageToShipmentStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown export shipment status value: '{value}'");

    public static bool TryShipmentStatusFromStorage(string value, out ExportShipmentStatus status) =>
        StorageToShipmentStatus.TryGetValue(value, out status);

    public static string ToStorage(ExportBrokerWorkspaceStatus status) =>
        BrokerWorkspaceStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportBrokerWorkspaceStatus: {status}");

    public static ExportBrokerWorkspaceStatus BrokerWorkspaceStatusFromStorage(string value) =>
        StorageToBrokerWorkspaceStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown export broker workspace status value: '{value}'");

    public static string ToStorage(ExportBrokerPackageVersionStatus status) =>
        PackageVersionStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportBrokerPackageVersionStatus: {status}");

    public static ExportBrokerPackageVersionStatus PackageVersionStatusFromStorage(string value) =>
        StorageToPackageVersionStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown export broker package version status value: '{value}'");

    public static string ToStorage(ExportBrokerCorrectionSeverity severity) =>
        CorrectionSeverityToStorage.TryGetValue(severity, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportBrokerCorrectionSeverity: {severity}");

    public static ExportBrokerCorrectionSeverity CorrectionSeverityFromStorage(string value) =>
        StorageToCorrectionSeverity.TryGetValue(value, out var severity)
            ? severity
            : throw new InvalidOperationException($"Unknown export broker correction severity value: '{value}'");

    public static string ToStorage(ExportBrokerCorrectionStatus status) =>
        CorrectionStatusToStorage.TryGetValue(status, out var value)
            ? value
            : throw new InvalidOperationException($"Unknown ExportBrokerCorrectionStatus: {status}");

    public static ExportBrokerCorrectionStatus CorrectionStatusFromStorage(string value) =>
        StorageToCorrectionStatus.TryGetValue(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown export broker correction status value: '{value}'");

    public static bool TryCorrectionSeverityFromStorage(string value, out ExportBrokerCorrectionSeverity severity) =>
        StorageToCorrectionSeverity.TryGetValue(value, out severity);

    public static bool TryCorrectionStatusFromStorage(string value, out ExportBrokerCorrectionStatus status) =>
        StorageToCorrectionStatus.TryGetValue(value, out status);
}
