namespace NevaxisFactOS.Persistence.Integration;

public sealed record IntegrationConnectorContext(
    IntegrationConnector Connector,
    string TriggeredBySubjectId,
    Guid? CorrelationId = null);

public sealed record IntegrationConnectionTestResult(
    bool Success,
    string HealthStatus,
    string? SafeMessage,
    int? LatencyMs = null);

public sealed record IntegrationDiscoverResult(
    bool Success,
    IReadOnlyList<string> DiscoveredEntityTypes,
    string? SafeMessage);

public sealed record IntegrationFetchResult(
    bool Success,
    int RecordsRead,
    string? CheckpointJson,
    string? FailureCategory,
    string? SafeMessage);

public sealed record IntegrationNormalizeResult(
    bool Success,
    int RecordsAccepted,
    int RecordsRejected,
    int RecordsQuarantined,
    string? SafeMessage);

public sealed record IntegrationReconcileResult(
    bool Success,
    int MissingRecords,
    int DuplicateIdentities,
    int ConflictingVersions,
    string? SafeMessage);

public interface IIntegrationConnectorImplementation
{
    string ConnectorKind { get; }

    Task<IntegrationConnectionTestResult> TestConnectionAsync(
        IntegrationConnectorContext context,
        CancellationToken cancellationToken = default);

    Task<IntegrationDiscoverResult> DiscoverAsync(
        IntegrationConnectorContext context,
        CancellationToken cancellationToken = default);

    Task<IntegrationFetchResult> FetchAsync(
        IntegrationConnectorContext context,
        string? checkpointJson,
        CancellationToken cancellationToken = default);

    Task<IntegrationNormalizeResult> NormalizeAsync(
        IntegrationConnectorContext context,
        IntegrationMappingVersion mappingVersion,
        CancellationToken cancellationToken = default);

    Task<IntegrationReconcileResult> ReconcileAsync(
        IntegrationConnectorContext context,
        CancellationToken cancellationToken = default);
}

public interface IIntegrationConnectorImplementationResolver
{
    IIntegrationConnectorImplementation? Resolve(string connectorKind);
}
