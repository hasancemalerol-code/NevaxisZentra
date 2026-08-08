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
}
