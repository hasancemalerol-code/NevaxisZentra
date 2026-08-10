using System.Diagnostics;
using System.Globalization;

namespace CrevaFactOS.Persistence.Erp.Netsis;

/// <summary>
/// Real Netsis ERP adapter — read-only customers/products via Netsis transport layer.
/// Vendor-specific DTOs and transport details remain inside the Netsis namespace.
/// </summary>
public sealed class NetsisErpAdapter(
    NetsisTransportResolver transportResolver,
    NetsisReadTransportOptions options) : IErpAdapter
{
    public const string DisplayName = "Netsis";

    public string ProviderKey => ErpProviderKeys.Netsis;

    public IReadOnlySet<string> Capabilities { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ErpCapabilities.CustomersRead,
        ErpCapabilities.ProductsRead,
    };

    public async Task<ErpAdapterHealthResult> CheckHealthAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var descriptor = NetsisConnectionDescriptorFactory.FromAdapterContext(connection);
        var transport = transportResolver.Resolve(descriptor);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(options.RequestTimeout);

            var health = await transport.CheckHealthAsync(descriptor, timeoutCts.Token);
            stopwatch.Stop();

            var isHealthy = health.IsReachable
                && health.IsContextValid
                && health.AreCredentialsValid
                && health.IsCompanyContextValid;

            var metadata = await TryBuildMetadataAsync(transport, descriptor, health, timeoutCts.Token);

            return new ErpAdapterHealthResult(
                IsHealthy: isHealthy,
                Status: isHealthy ? ErpConnectionStatuses.Healthy : MapUnhealthyStatus(health.ReadinessState),
                LatencyMs: health.LatencyMs ?? (int)stopwatch.ElapsedMilliseconds,
                ErrorCode: isHealthy ? null : health.ErrorCode,
                ErrorMessage: isHealthy ? null : health.ErrorMessage,
                CheckedAtUtc: DateTimeOffset.UtcNow,
                IntegrationMetadata: metadata);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return new ErpAdapterHealthResult(
                false,
                ErpConnectionStatuses.Unavailable,
                (int)stopwatch.ElapsedMilliseconds,
                NetsisTransportErrorCodes.Timeout,
                "Netsis transport request timed out.",
                DateTimeOffset.UtcNow,
                null);
        }
        catch (NetsisTransportException ex)
        {
            return new ErpAdapterHealthResult(
                false,
                ErpConnectionStatuses.Unavailable,
                (int)stopwatch.ElapsedMilliseconds,
                ex.ErrorCode,
                ex.Message,
                DateTimeOffset.UtcNow,
                null);
        }
    }

    public async Task<IReadOnlyList<ErpExternalCustomerRecord>> ReadCustomersAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureCapability(ErpCapabilities.CustomersRead);

        var descriptor = NetsisConnectionDescriptorFactory.FromAdapterContext(connection);
        var transport = transportResolver.Resolve(descriptor);
        return await ReadAllPagesAsync(
            (page, ct) => transport.ReadCustomersAsync(descriptor, page, ct),
            NetsisCustomerMapper.ToCanonical,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ErpExternalProductRecord>> ReadProductsAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureCapability(ErpCapabilities.ProductsRead);

        var descriptor = NetsisConnectionDescriptorFactory.FromAdapterContext(connection);
        var transport = transportResolver.Resolve(descriptor);
        return await ReadAllPagesAsync(
            (page, ct) => transport.ReadProductsAsync(descriptor, page, ct),
            NetsisProductMapper.ToCanonical,
            cancellationToken);
    }

    public Task<IReadOnlyList<ErpExternalOrderRecord>> ReadOrdersAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default) =>
        Unsupported<IReadOnlyList<ErpExternalOrderRecord>>("orders.read", cancellationToken);

    public Task<IReadOnlyList<ErpExternalWarehouseRecord>> ReadWarehousesAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default) =>
        Unsupported<IReadOnlyList<ErpExternalWarehouseRecord>>("warehouses.read", cancellationToken);

    public Task<IReadOnlyList<ErpExternalInventoryBalanceRecord>> ReadInventoryAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default) =>
        Unsupported<IReadOnlyList<ErpExternalInventoryBalanceRecord>>("inventory.read", cancellationToken);

    public Task<IReadOnlyList<ErpExternalPurchaseOrderRecord>> ReadPurchaseOrdersAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default) =>
        Unsupported<IReadOnlyList<ErpExternalPurchaseOrderRecord>>("purchasing.read", cancellationToken);

    public Task<IReadOnlyList<ErpExternalProductionOrderRecord>> ReadProductionOrdersAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default) =>
        Unsupported<IReadOnlyList<ErpExternalProductionOrderRecord>>("production.read", cancellationToken);

    public Task<IReadOnlyList<ErpExternalDocumentRecord>> ReadDocumentsAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default) =>
        Unsupported<IReadOnlyList<ErpExternalDocumentRecord>>("documents.read", cancellationToken);

    private static Task<T> Unsupported<T>(string capability, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromException<T>(
            new NotSupportedException($"netsis provider does not support '{capability}' in ERP-NETSIS-1."));
    }

    private static void EnsureCapability(string capability)
    {
        _ = capability;
    }

    private async Task<IReadOnlyList<TCanonical>> ReadAllPagesAsync<TSource, TCanonical>(
        Func<NetsisReadPageRequest, CancellationToken, Task<NetsisReadPageResult<TSource>>> readPage,
        Func<TSource, TCanonical> map,
        CancellationToken cancellationToken)
    {
        var results = new List<TCanonical>();
        string? cursor = null;

        do
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(options.RequestTimeout);

            var page = await readPage(
                new NetsisReadPageRequest(options.DefaultPageSize, cursor),
                timeoutCts.Token);

            foreach (var item in page.Items)
            {
                if (string.IsNullOrWhiteSpace(GetRecordId(item)))
                {
                    throw new NetsisTransportException(
                        NetsisTransportErrorCodes.SourceReadFailed,
                        "Netsis source record is missing a stable identifier.");
                }

                results.Add(map(item));
            }

            cursor = page.ContinuationToken;
        }
        while (!string.IsNullOrWhiteSpace(cursor));

        return results;
    }

    private static string? GetRecordId<T>(T item) =>
        item switch
        {
            NetsisCustomerSourceRecord customer => customer.RecordId,
            NetsisProductSourceRecord product => product.RecordId,
            _ => null,
        };

    private static async Task<IReadOnlyDictionary<string, string>?> TryBuildMetadataAsync(
        INetsisReadTransport transport,
        NetsisConnectionDescriptor descriptor,
        NetsisTransportHealthResult health,
        CancellationToken cancellationToken)
    {
        if (!health.IsReachable)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["netsis.readiness"] = health.ReadinessState,
                ["netsis.transportKind"] = transport.TransportKind,
                ["netsis.adapterReady"] = "true",
                ["netsis.liveValidated"] = "false",
            };
        }

        try
        {
            var context = await transport.DiscoverContextAsync(descriptor, cancellationToken);
            var liveValidated = descriptor.TransportKind != NetsisTransportKinds.TestFixture
                && health.IsCompanyContextValid;

            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["netsis.readiness"] = health.ReadinessState,
                ["netsis.transportKind"] = context.TransportKind,
                ["netsis.productFamily"] = context.ProductFamily ?? "",
                ["netsis.productVersion"] = context.ProductVersion ?? "",
                ["netsis.companyId"] = context.CompanyId ?? "",
                ["netsis.companyCode"] = context.CompanyCode ?? "",
                ["netsis.companyName"] = context.CompanyName ?? "",
                ["netsis.period"] = context.Period?.ToString(CultureInfo.InvariantCulture) ?? "",
                ["netsis.adapterReady"] = "true",
                ["netsis.liveValidated"] = liveValidated ? "true" : "false",
            };
        }
        catch (NetsisTransportException)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["netsis.readiness"] = health.ReadinessState,
                ["netsis.transportKind"] = transport.TransportKind,
                ["netsis.adapterReady"] = "true",
                ["netsis.liveValidated"] = "false",
            };
        }
    }

    private static string MapUnhealthyStatus(string readiness) =>
        readiness switch
        {
            NetsisReadinessStates.CredentialsRequired => ErpConnectionStatuses.Configured,
            NetsisReadinessStates.Configured => ErpConnectionStatuses.Configured,
            _ => ErpConnectionStatuses.Unavailable,
        };
}
