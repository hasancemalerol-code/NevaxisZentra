namespace NevaxisFactOS.Persistence.BulkDataOnboarding;

public interface IBulkDataSourceProvider
{
    string ProviderKind { get; }
    string SourceKind { get; }
    Task<BulkSourceDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<BulkMappedRecord> ReadRecordsAsync(
        BulkMappingProfileDefinition mapping,
        BulkImportCheckpoint? resumeFrom,
        CancellationToken cancellationToken = default);
}

public interface IBulkDataSourceProviderResolver
{
    IBulkDataSourceProvider Resolve(string providerKind);
}

public sealed class BulkDataSourceProviderResolver(IEnumerable<IBulkDataSourceProvider> providers)
    : IBulkDataSourceProviderResolver
{
    private readonly Dictionary<string, IBulkDataSourceProvider> _providers =
        providers.ToDictionary(p => p.ProviderKind, StringComparer.OrdinalIgnoreCase);

    public IBulkDataSourceProvider Resolve(string providerKind) =>
        _providers.TryGetValue(providerKind, out var provider)
            ? provider
            : throw new InvalidOperationException($"Unknown bulk source provider '{providerKind}'.");
}

public interface IFutureBulkDatabaseSourceProvider : IBulkDataSourceProvider;

public interface IFutureBulkJsonExportSourceProvider : IBulkDataSourceProvider;

public sealed class SyntheticStructuredSourceProvider : IBulkDataSourceProvider
{
    public string ProviderKind => BulkDataSourceProviderKinds.SyntheticStructured;
    public string SourceKind => BulkDataSourceKinds.Database;

    public Task<BulkSourceDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(BulkDataOnboardingSyntheticFixture.BuildDiscovery());
    }

    public async IAsyncEnumerable<BulkMappedRecord> ReadRecordsAsync(
        BulkMappingProfileDefinition mapping,
        BulkImportCheckpoint? resumeFrom,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var records = BulkDataOnboardingSyntheticFixture.BuildMappedRecords(mapping);
        var startIndex = resumeFrom?.ProcessedRows ?? 0;
        for (var i = startIndex; i < records.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return records[i];
            if (i % 100 == 0)
            {
                await Task.Yield();
            }
        }
    }
}
