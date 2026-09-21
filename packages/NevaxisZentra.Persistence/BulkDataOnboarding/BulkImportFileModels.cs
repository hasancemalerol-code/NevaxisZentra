namespace NevaxisZentra.Persistence.BulkDataOnboarding;

public sealed record BulkImportStoredFile(
    string FileName,
    string StoredName,
    long SizeBytes,
    string ContentType);

public sealed record BulkImportFileManifest(
    IReadOnlyList<BulkImportStoredFile> Files)
{
    public static BulkImportFileManifest Empty { get; } = new([]);

    public bool HasFiles => Files.Count > 0;
}

public sealed record BulkQuarantineSummary(
    Guid Id,
    string SourceTable,
    string SourceKey,
    string ReasonCode,
    string SafeReason,
    string Severity,
    string Status,
    DateTimeOffset OccurredAtUtc);

public sealed record UpsertBulkMappingProfileRequest(
    string Name,
    string SourceSystem,
    IReadOnlyList<BulkMappingRule> Rules);

public sealed record BulkMappingProfileSummary(
    Guid Id,
    string Name,
    string SourceSystem,
    string Status,
    int RuleCount,
    DateTimeOffset UpdatedAtUtc);

public sealed record UploadBulkImportFilesResult(
    Guid JobId,
    int FileCount,
    IReadOnlyList<BulkImportStoredFile> Files);
