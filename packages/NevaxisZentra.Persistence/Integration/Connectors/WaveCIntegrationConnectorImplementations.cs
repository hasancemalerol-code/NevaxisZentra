using NevaxisZentra.Persistence.Integration.Common;
using NevaxisZentra.Persistence.Integration.Database;
using NevaxisZentra.Persistence.Integration.Erp;
using NevaxisZentra.Persistence.Integration.Rest;

namespace NevaxisZentra.Persistence.Integration.Connectors;

public sealed class RestApiIntegrationConnectorImplementation(
    IIntegrationRestRecordFetcher restRecordFetcher) : IIntegrationConnectorImplementation
{
    public string ConnectorKind => IntegrationConnectorKinds.RestApi;

    public async Task<IntegrationConnectionTestResult> TestConnectionAsync(
        IntegrationConnectorContext context,
        CancellationToken cancellationToken = default)
    {
        var fetch = await restRecordFetcher.FetchAsync(context.Connector, null, null, cancellationToken);
        return new IntegrationConnectionTestResult(
            fetch.Success,
            fetch.Success ? IntegrationHealthStatuses.Healthy : IntegrationHealthStatuses.SyncFailed,
            fetch.SafeMessage ?? (fetch.Success ? "REST connection verified." : "REST connection failed."),
            fetch.Success ? 25 : null);
    }

    public Task<IntegrationDiscoverResult> DiscoverAsync(
        IntegrationConnectorContext context,
        CancellationToken cancellationToken = default)
    {
        var config = IntegrationRestConnectorConfiguration.TryParse(context.Connector.ConfigurationJson);
        return Task.FromResult(new IntegrationDiscoverResult(
            config is not null,
            config is null ? [] : ["records"],
            config is null ? "REST configuration is invalid." : null));
    }

    public Task<IntegrationFetchResult> FetchAsync(
        IntegrationConnectorContext context,
        string? checkpointJson,
        CancellationToken cancellationToken = default) =>
        restRecordFetcher.FetchAsync(context.Connector, checkpointJson, null, cancellationToken)
            .ContinueWith(
                task => new IntegrationFetchResult(
                    task.Result.Success,
                    task.Result.Table?.Rows.Count ?? 0,
                    task.Result.CheckpointJson,
                    task.Result.FailureCategory,
                    task.Result.SafeMessage),
                cancellationToken);

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

public sealed class ReadOnlyDatabaseIntegrationConnectorImplementation(
    IIntegrationReadOnlyDatabaseFetcher databaseFetcher) : IIntegrationConnectorImplementation
{
    public string ConnectorKind => IntegrationConnectorKinds.ReadOnlyDatabase;

    public async Task<IntegrationConnectionTestResult> TestConnectionAsync(
        IntegrationConnectorContext context,
        CancellationToken cancellationToken = default)
    {
        var fetch = await databaseFetcher.FetchAsync(context.Connector, null, cancellationToken);
        return new IntegrationConnectionTestResult(
            fetch.Success,
            fetch.Success ? IntegrationHealthStatuses.Healthy : IntegrationHealthStatuses.SyncFailed,
            fetch.SafeMessage ?? (fetch.Success ? "Database read verified." : "Database read failed."));
    }

    public Task<IntegrationDiscoverResult> DiscoverAsync(
        IntegrationConnectorContext context,
        CancellationToken cancellationToken = default)
    {
        var config = IntegrationReadOnlyDatabaseConnectorConfiguration.TryParse(context.Connector.ConfigurationJson);
        return Task.FromResult(new IntegrationDiscoverResult(
            config is not null,
            config is null ? [] : [config.AllowlistedSourceName],
            config is null ? "Database configuration is invalid." : null));
    }

    public Task<IntegrationFetchResult> FetchAsync(
        IntegrationConnectorContext context,
        string? checkpointJson,
        CancellationToken cancellationToken = default) =>
        databaseFetcher.FetchAsync(context.Connector, checkpointJson, cancellationToken)
            .ContinueWith(
                task => new IntegrationFetchResult(
                    task.Result.Success,
                    task.Result.Table?.Rows.Count ?? 0,
                    task.Result.CheckpointJson,
                    task.Result.FailureCategory,
                    task.Result.SafeMessage),
                cancellationToken);

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

public sealed class FolderWatchIntegrationConnectorImplementation : IIntegrationConnectorImplementation
{
    public string ConnectorKind => IntegrationConnectorKinds.FolderWatch;

    public Task<IntegrationConnectionTestResult> TestConnectionAsync(
        IntegrationConnectorContext context,
        CancellationToken cancellationToken = default)
    {
        var config = IntegrationFolderWatchConnectorConfiguration.TryParse(context.Connector.ConfigurationJson);
        if (config is null || string.IsNullOrWhiteSpace(config.RootPath))
        {
            return Task.FromResult(new IntegrationConnectionTestResult(
                false,
                IntegrationHealthStatuses.Misconfigured,
                "Folder watch configuration is invalid."));
        }

        var root = Path.GetFullPath(config.RootPath);
        return Task.FromResult(new IntegrationConnectionTestResult(
            Directory.Exists(root),
            Directory.Exists(root) ? IntegrationHealthStatuses.Healthy : IntegrationHealthStatuses.Offline,
            Directory.Exists(root) ? "Folder watch root verified." : "Folder watch root is unavailable."));
    }

    public Task<IntegrationDiscoverResult> DiscoverAsync(
        IntegrationConnectorContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new IntegrationDiscoverResult(true, ["files"], null));

    public Task<IntegrationFetchResult> FetchAsync(
        IntegrationConnectorContext context,
        string? checkpointJson,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new IntegrationFetchResult(true, 0, checkpointJson, null, null));

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

public sealed class WebhookIntegrationConnectorImplementation : IIntegrationConnectorImplementation
{
    public string ConnectorKind => IntegrationConnectorKinds.Webhook;

    public Task<IntegrationConnectionTestResult> TestConnectionAsync(
        IntegrationConnectorContext context,
        CancellationToken cancellationToken = default)
    {
        var config = IntegrationWebhookConnectorConfiguration.TryParse(context.Connector.ConfigurationJson);
        var ok = config is not null && !string.IsNullOrWhiteSpace(context.Connector.IngressKey);
        return Task.FromResult(new IntegrationConnectionTestResult(
            ok,
            ok ? IntegrationHealthStatuses.Healthy : IntegrationHealthStatuses.Misconfigured,
            ok ? "Webhook ingress registration verified." : "Webhook ingress key is missing."));
    }

    public Task<IntegrationDiscoverResult> DiscoverAsync(
        IntegrationConnectorContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new IntegrationDiscoverResult(true, ["events"], null));

    public Task<IntegrationFetchResult> FetchAsync(
        IntegrationConnectorContext context,
        string? checkpointJson,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new IntegrationFetchResult(true, 0, checkpointJson, null, "Webhook connector is event-driven."));

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

public sealed class ErpAdapterIntegrationConnectorImplementation : IIntegrationConnectorImplementation
{
    public string ConnectorKind => IntegrationConnectorKinds.ErpAdapter;

    public Task<IntegrationConnectionTestResult> TestConnectionAsync(
        IntegrationConnectorContext context,
        CancellationToken cancellationToken = default)
    {
        var config = IntegrationErpAdapterBridgeConfiguration.TryParse(context.Connector.ConfigurationJson);
        var ok = config is not null && config.ErpConnectionProfileId is not null;
        return Task.FromResult(new IntegrationConnectionTestResult(
            ok,
            ok ? IntegrationHealthStatuses.Healthy : IntegrationHealthStatuses.Misconfigured,
            ok ? "ERP adapter bridge configured." : "ERP adapter bridge is misconfigured."));
    }

    public Task<IntegrationDiscoverResult> DiscoverAsync(
        IntegrationConnectorContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new IntegrationDiscoverResult(
            true,
            IntegrationLogoAdapterMetadata.SupportedCapabilities.ToList(),
            null));

    public Task<IntegrationFetchResult> FetchAsync(
        IntegrationConnectorContext context,
        string? checkpointJson,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new IntegrationFetchResult(true, 0, checkpointJson, null, null));

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
