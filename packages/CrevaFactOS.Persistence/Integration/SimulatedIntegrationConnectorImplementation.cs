namespace CrevaFactOS.Persistence.Integration;

public sealed class SimulatedIntegrationConnectorImplementation : IIntegrationConnectorImplementation
{
    public string ConnectorKind => IntegrationConnectorKinds.FileImport;

    public Task<IntegrationConnectionTestResult> TestConnectionAsync(
        IntegrationConnectorContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.Connector.Environment == IntegrationEnvironments.Production)
        {
            return Task.FromResult(new IntegrationConnectionTestResult(
                false,
                IntegrationHealthStatuses.Misconfigured,
                "Simulated connector cannot be used in production environment."));
        }

        return Task.FromResult(new IntegrationConnectionTestResult(
            true,
            IntegrationHealthStatuses.Healthy,
            "Simulated connection verified.",
            12));
    }

    public Task<IntegrationDiscoverResult> DiscoverAsync(
        IntegrationConnectorContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new IntegrationDiscoverResult(
            true,
            ["production-orders", "materials", "machines"],
            null));

    public Task<IntegrationFetchResult> FetchAsync(
        IntegrationConnectorContext context,
        string? checkpointJson,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new IntegrationFetchResult(
            true,
            0,
            checkpointJson ?? "{}",
            null,
            null));

    public Task<IntegrationNormalizeResult> NormalizeAsync(
        IntegrationConnectorContext context,
        IntegrationMappingVersion mappingVersion,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new IntegrationNormalizeResult(true, 0, 0, 0, null));

    public Task<IntegrationReconcileResult> ReconcileAsync(
        IntegrationConnectorContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new IntegrationReconcileResult(true, 0, 0, 0, null));
}

public sealed class IntegrationConnectorImplementationResolver(
    IEnumerable<IIntegrationConnectorImplementation> implementations) : IIntegrationConnectorImplementationResolver
{
    private readonly Dictionary<string, IIntegrationConnectorImplementation> _byKind =
        implementations
            .GroupBy(i => i.ConnectorKind, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

    public IIntegrationConnectorImplementation? Resolve(string connectorKind) =>
        _byKind.TryGetValue(connectorKind, out var implementation) ? implementation : null;
}
