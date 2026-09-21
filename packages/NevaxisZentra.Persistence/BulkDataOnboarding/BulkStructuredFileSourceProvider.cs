using NevaxisZentra.Persistence.Integration.FileImport;

namespace NevaxisZentra.Persistence.BulkDataOnboarding;

public sealed class CsvFileSourceProvider(
    IBulkImportFileStore fileStore,
    IBulkImportSourceContextAccessor contextAccessor) : IBulkDataSourceProvider
{
    public string ProviderKind => BulkDataSourceProviderKinds.CsvFile;
    public string SourceKind => BulkDataSourceKinds.File;

    public Task<BulkSourceDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken = default) =>
        StructuredFileSourceProviderHelper.DiscoverAsync(
            fileStore,
            contextAccessor,
            BulkDataSourceProviderKinds.CsvFile,
            cancellationToken);

    public IAsyncEnumerable<BulkMappedRecord> ReadRecordsAsync(
        BulkMappingProfileDefinition mapping,
        BulkImportCheckpoint? resumeFrom,
        CancellationToken cancellationToken = default) =>
        StructuredFileSourceProviderHelper.ReadRecordsAsync(
            fileStore,
            contextAccessor,
            mapping,
            resumeFrom,
            cancellationToken);
}

public sealed class XlsxFileSourceProvider(
    IBulkImportFileStore fileStore,
    IBulkImportSourceContextAccessor contextAccessor) : IBulkDataSourceProvider
{
    public string ProviderKind => BulkDataSourceProviderKinds.XlsxFile;
    public string SourceKind => BulkDataSourceKinds.File;

    public Task<BulkSourceDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken = default) =>
        StructuredFileSourceProviderHelper.DiscoverAsync(
            fileStore,
            contextAccessor,
            BulkDataSourceProviderKinds.XlsxFile,
            cancellationToken);

    public IAsyncEnumerable<BulkMappedRecord> ReadRecordsAsync(
        BulkMappingProfileDefinition mapping,
        BulkImportCheckpoint? resumeFrom,
        CancellationToken cancellationToken = default) =>
        StructuredFileSourceProviderHelper.ReadRecordsAsync(
            fileStore,
            contextAccessor,
            mapping,
            resumeFrom,
            cancellationToken);
}

internal static class StructuredFileSourceProviderHelper
{
    private const string SourceDatabase = "customer_file_import";

    public static async Task<BulkSourceDiscoveryResult> DiscoverAsync(
        IBulkImportFileStore fileStore,
        IBulkImportSourceContextAccessor contextAccessor,
        string expectedProviderKind,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var context = RequireContext(contextAccessor, expectedProviderKind);
        var manifest = fileStore.ReadManifest(context.ObservabilityJson);
        if (!manifest.HasFiles)
        {
            throw new InvalidOperationException(BulkFailureCodes.ValidationFailed);
        }

        var tables = new List<BulkSourceTableDescriptor>();
        foreach (var file in manifest.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!MatchesProvider(file.FileName, expectedProviderKind))
            {
                continue;
            }

            await using var stream = fileStore.OpenRead(context.TenantId, context.FacilityId, context.JobId, file.StoredName);
            var table = ReadTableProfile(file.FileName, stream);
            if (table is null)
            {
                continue;
            }

            tables.Add(table);
        }

        if (tables.Count == 0)
        {
            throw new InvalidOperationException(BulkFailureCodes.ValidationFailed);
        }

        return new BulkSourceDiscoveryResult(
            string.IsNullOrWhiteSpace(context.SourceSystem) ? "CUSTOMER-FILE-IMPORT" : context.SourceSystem,
            "1.0.0",
            tables);
    }

    public static async IAsyncEnumerable<BulkMappedRecord> ReadRecordsAsync(
        IBulkImportFileStore fileStore,
        IBulkImportSourceContextAccessor contextAccessor,
        BulkMappingProfileDefinition mapping,
        BulkImportCheckpoint? resumeFrom,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var context = contextAccessor.Current
            ?? throw new InvalidOperationException("Bulk import source context is not available.");
        var manifest = fileStore.ReadManifest(context.ObservabilityJson);
        var processed = resumeFrom?.ProcessedRows ?? 0;
        var globalIndex = 0;

        foreach (var file in manifest.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!MatchesProvider(file.FileName, context.SourceProviderKind))
            {
                continue;
            }

            await using var stream = fileStore.OpenRead(context.TenantId, context.FacilityId, context.JobId, file.StoredName);
            var parsed = IntegrationFileTableReader.Read(file.FileName, stream);
            if (parsed is null)
            {
                continue;
            }

            var tableName = BulkMappingProfileEngine.NormalizeTableName(file.FileName);
            var entityType = BulkMappingProfileEngine.InferEntityType(tableName);
            for (var rowIndex = 0; rowIndex < parsed.Rows.Count; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (globalIndex < processed)
                {
                    globalIndex++;
                    continue;
                }

                var sourceRow = parsed.Rows[rowIndex]
                    .ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
                var fields = BulkMappingProfileEngine.MapRow(tableName, sourceRow, mapping);
                var sourceKey = BulkMappingProfileEngine.ResolveSourceKey(tableName, fields, rowIndex);
                DateTimeOffset? timestamp = null;
                string? timestampRaw = null;
                if (fields.TryGetValue("occurred_at", out var occurredAt) && !string.IsNullOrWhiteSpace(occurredAt))
                {
                    timestampRaw = occurredAt;
                    var normalized = BulkTimeNormalizationEngine.Normalize(occurredAt);
                    timestamp = normalized.NormalizedUtc;
                }

                yield return new BulkMappedRecord(
                    tableName,
                    sourceKey,
                    entityType,
                    fields,
                    timestampRaw,
                    timestamp,
                    DateTimeOffset.UtcNow);

                globalIndex++;
                if (globalIndex % 100 == 0)
                {
                    await Task.Yield();
                }
            }
        }
    }

    private static BulkImportSourceContext RequireContext(
        IBulkImportSourceContextAccessor contextAccessor,
        string expectedProviderKind)
    {
        var context = contextAccessor.Current
            ?? throw new InvalidOperationException("Bulk import source context is not available.");
        if (!string.Equals(context.SourceProviderKind, expectedProviderKind, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(BulkFailureCodes.InvalidState);
        }

        return context;
    }

    private static bool MatchesProvider(string fileName, string providerKind)
    {
        var extension = Path.GetExtension(fileName);
        return providerKind switch
        {
            BulkDataSourceProviderKinds.CsvFile =>
                string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase),
            BulkDataSourceProviderKinds.XlsxFile =>
                string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }

    private static BulkSourceTableDescriptor? ReadTableProfile(string fileName, Stream content)
    {
        var parsed = IntegrationFileTableReader.Read(fileName, content);
        if (parsed is null)
        {
            return null;
        }

        var tableName = BulkMappingProfileEngine.NormalizeTableName(fileName);
        var columns = parsed.Headers
            .Select(header => new BulkColumnDescriptor(
                header,
                header.Contains("at", StringComparison.OrdinalIgnoreCase) ? "timestamp" : "text",
                true,
                header.EndsWith("_code", StringComparison.OrdinalIgnoreCase) || header.Equals("code", StringComparison.OrdinalIgnoreCase),
                null))
            .ToList();

        return new BulkSourceTableDescriptor(
            SourceDatabase,
            "file",
            tableName,
            columns,
            parsed.Rows.Count,
            [columns.FirstOrDefault()?.Name ?? "code"],
            columns.Where(c => c.Name.Contains("at", StringComparison.OrdinalIgnoreCase)).Select(c => c.Name).ToList());
    }
}
