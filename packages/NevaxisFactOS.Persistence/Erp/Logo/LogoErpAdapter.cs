using System.Diagnostics;
using System.Globalization;

namespace NevaxisFactOS.Persistence.Erp.Logo;

/// <summary>
/// Real Logo ERP adapter — read-only customers/products via Logo transport layer.
/// Vendor-specific DTOs and transport details remain inside the Logo namespace.
/// </summary>
public sealed class LogoErpAdapter(
    LogoTransportResolver transportResolver,
    LogoReadTransportOptions options) : IErpAdapter
{
    public const string DisplayName = "Logo";

    public string ProviderKey => ErpProviderKeys.Logo;

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
        var descriptor = LogoConnectionDescriptorFactory.FromAdapterContext(connection);
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
                LogoTransportErrorCodes.Timeout,
                "Logo transport request timed out.",
                DateTimeOffset.UtcNow,
                null);
        }
        catch (LogoTransportException ex)
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

        var descriptor = LogoConnectionDescriptorFactory.FromAdapterContext(connection);
        var transport = transportResolver.Resolve(descriptor);
        return await ReadAllPagesAsync(
            (page, ct) => transport.ReadCustomersAsync(descriptor, page, ct),
            LogoCustomerMapper.ToCanonical,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ErpExternalProductRecord>> ReadProductsAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureCapability(ErpCapabilities.ProductsRead);

        var descriptor = LogoConnectionDescriptorFactory.FromAdapterContext(connection);
        var transport = transportResolver.Resolve(descriptor);
        return await ReadAllPagesAsync(
            (page, ct) => transport.ReadProductsAsync(descriptor, page, ct),
            LogoProductMapper.ToCanonical,
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
            new NotSupportedException($"logo provider does not support '{capability}' in ERP-LOGO-1."));
    }

    private static void EnsureCapability(string capability)
    {
        _ = capability;
    }

    private async Task<IReadOnlyList<TCanonical>> ReadAllPagesAsync<TSource, TCanonical>(
        Func<LogoReadPageRequest, CancellationToken, Task<LogoReadPageResult<TSource>>> readPage,
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
                new LogoReadPageRequest(options.DefaultPageSize, cursor),
                timeoutCts.Token);

            foreach (var item in page.Items)
            {
                if (string.IsNullOrWhiteSpace(GetRecordId(item)))
                {
                    throw new LogoTransportException(
                        LogoTransportErrorCodes.SourceReadFailed,
                        "Logo source record is missing a stable identifier.");
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
            LogoCustomerSourceRecord customer => customer.RecordId,
            LogoProductSourceRecord product => product.RecordId,
            _ => null,
        };

    private static async Task<IReadOnlyDictionary<string, string>?> TryBuildMetadataAsync(
        ILogoReadTransport transport,
        LogoConnectionDescriptor descriptor,
        LogoTransportHealthResult health,
        CancellationToken cancellationToken)
    {
        if (!health.IsReachable)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["logo.readiness"] = health.ReadinessState,
                ["logo.transportKind"] = transport.TransportKind,
                ["logo.adapterReady"] = "true",
                ["logo.liveValidated"] = "false",
            };
        }

        try
        {
            var context = await transport.DiscoverContextAsync(descriptor, cancellationToken);
            var liveValidated = descriptor.TransportKind != LogoTransportKinds.TestFixture
                && health.IsCompanyContextValid;

            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["logo.readiness"] = health.ReadinessState,
                ["logo.transportKind"] = context.TransportKind,
                ["logo.productFamily"] = context.ProductFamily ?? "",
                ["logo.productVersion"] = context.ProductVersion ?? "",
                ["logo.companyId"] = context.CompanyId ?? "",
                ["logo.companyCode"] = context.CompanyCode ?? "",
                ["logo.companyName"] = context.CompanyName ?? "",
                ["logo.period"] = context.Period?.ToString(CultureInfo.InvariantCulture) ?? "",
                ["logo.adapterReady"] = "true",
                ["logo.liveValidated"] = liveValidated ? "true" : "false",
            };
        }
        catch (LogoTransportException)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["logo.readiness"] = health.ReadinessState,
                ["logo.transportKind"] = transport.TransportKind,
                ["logo.adapterReady"] = "true",
                ["logo.liveValidated"] = "false",
            };
        }
    }

    private static string MapUnhealthyStatus(string readiness) =>
        readiness switch
        {
            LogoReadinessStates.CredentialsRequired => ErpConnectionStatuses.Configured,
            LogoReadinessStates.Configured => ErpConnectionStatuses.Configured,
            _ => ErpConnectionStatuses.Unavailable,
        };
}
