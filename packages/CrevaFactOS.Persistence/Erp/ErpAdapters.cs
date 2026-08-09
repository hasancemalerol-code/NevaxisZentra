namespace CrevaFactOS.Persistence.Erp;

/// <summary>
/// In-process development ERP Simulator. Deterministic stable IDs.
/// Future second-PC handoff: HTTP/JSON over the same IErpAdapter contract
/// (CheckHealth / ReadCustomers / ReadProducts / ReadOrders) without changing domain consumers.
/// </summary>
public sealed class SimulatorErpAdapter : IErpAdapter
{
    public const string DisplayName = "ERP Simulator";

    public string ProviderKey => ErpProviderKeys.Simulator;

    public IReadOnlySet<string> Capabilities { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ErpCapabilities.CustomersRead,
        ErpCapabilities.ProductsRead,
        ErpCapabilities.OrdersRead,
    };

    public Task<ErpAdapterHealthResult> CheckHealthAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Deterministic healthy unless explicitly marked unavailable via company code sentinel.
        var unavailable = string.Equals(
            connection.ExternalCompanyCode,
            "SIM-UNAVAILABLE",
            StringComparison.OrdinalIgnoreCase);

        if (unavailable)
        {
            return Task.FromResult(new ErpAdapterHealthResult(
                IsHealthy: false,
                Status: ErpConnectionStatuses.Unavailable,
                LatencyMs: 1,
                ErrorCode: "erp.simulator.unavailable",
                ErrorMessage: "Simulator intentionally unavailable.",
                CheckedAtUtc: DateTimeOffset.UtcNow));
        }

        return Task.FromResult(new ErpAdapterHealthResult(
            IsHealthy: true,
            Status: ErpConnectionStatuses.Healthy,
            LatencyMs: 1,
            ErrorCode: null,
            ErrorMessage: null,
            CheckedAtUtc: DateTimeOffset.UtcNow));
    }

    public Task<IReadOnlyList<ErpExternalCustomerRecord>> ReadCustomersAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureCapability(ErpCapabilities.CustomersRead);

        IReadOnlyList<ErpExternalCustomerRecord> customers =
        [
            new(
                ExternalId: "ERP-CUST-001",
                ExternalCode: "ERP-CUST-001",
                Name: "ABC Plastik Sanayi A.Ş.",
                TaxNumber: "1234567890",
                TaxOffice: "Kadıköy",
                Email: "info@abcplastik.example",
                Phone: "+90-216-000-0001",
                CountryCode: "TR",
                IsActive: true,
                SourceVersion: null,
                SourceUpdatedAtUtc: null),
        ];

        return Task.FromResult(customers);
    }

    public Task<IReadOnlyList<ErpExternalProductRecord>> ReadProductsAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureCapability(ErpCapabilities.ProductsRead);

        IReadOnlyList<ErpExternalProductRecord> products =
        [
            new(
                ExternalId: "ERP-PRD-001",
                ExternalCode: "ERP-PRD-001",
                Name: "PP CAP 32",
                Unit: "AD",
                Barcode: "8690000000001",
                IsActive: true,
                SourceVersion: null,
                SourceUpdatedAtUtc: null),
            new(
                ExternalId: "ERP-PRD-002",
                ExternalCode: "ERP-PRD-002",
                Name: "HDPE CONTAINER",
                Unit: "AD",
                Barcode: "8690000000002",
                IsActive: true,
                SourceVersion: null,
                SourceUpdatedAtUtc: null),
        ];

        return Task.FromResult(products);
    }

    public Task<IReadOnlyList<ErpExternalOrderRecord>> ReadOrdersAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureCapability(ErpCapabilities.OrdersRead);

        IReadOnlyList<ErpExternalOrderRecord> orders =
        [
            new(
                ExternalId: "ERP-ORD-001",
                ExternalCode: "ERP-ORD-001",
                OrderNumber: "ERP-ORD-001",
                ExternalCustomerId: "ERP-CUST-001",
                ExternalCustomerCode: "ERP-CUST-001",
                OrderDateUtc: new DateTimeOffset(2026, 8, 1, 8, 0, 0, TimeSpan.Zero),
                RequestedDeliveryDateUtc: new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero),
                CurrencyCode: "TRY",
                Status: ErpOrderStatuses.Open,
                Reference: "SIM-REF-001",
                Notes: "Development ERP Simulator order",
                SourceVersion: "1",
                SourceUpdatedAtUtc: new DateTimeOffset(2026, 8, 1, 8, 0, 0, TimeSpan.Zero),
                Lines:
                [
                    new(
                        ExternalLineId: "ERP-ORD-001-L1",
                        LineNumber: 1,
                        ExternalProductId: "ERP-PRD-001",
                        ExternalProductCode: "ERP-PRD-001",
                        Description: "PP CAP 32",
                        Quantity: 10000m,
                        Unit: "AD",
                        RequestedDeliveryDateUtc: new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero),
                        SourceVersion: "1"),
                    new(
                        ExternalLineId: "ERP-ORD-001-L2",
                        LineNumber: 2,
                        ExternalProductId: "ERP-PRD-002",
                        ExternalProductCode: "ERP-PRD-002",
                        Description: "HDPE CONTAINER",
                        Quantity: 5000m,
                        Unit: "AD",
                        RequestedDeliveryDateUtc: new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero),
                        SourceVersion: "1"),
                ]),
        ];

        return Task.FromResult(orders);
    }

    private void EnsureCapability(string capability)
    {
        if (!Capabilities.Contains(capability))
        {
            throw new InvalidOperationException($"Simulator does not support capability '{capability}'.");
        }
    }
}

/// <summary>
/// Placeholder Logo adapter — contract readiness only. No real Logo SDK/connection.
/// </summary>
public sealed class LogoErpAdapterPlaceholder : UnsupportedErpAdapterBase
{
    public override string ProviderKey => ErpProviderKeys.Logo;
}

/// <summary>
/// Placeholder Mikro adapter — contract readiness only. No real Mikro SDK/connection.
/// </summary>
public sealed class MikroErpAdapterPlaceholder : UnsupportedErpAdapterBase
{
    public override string ProviderKey => ErpProviderKeys.Mikro;
}

/// <summary>
/// Placeholder Netsis adapter — contract readiness only. No real Netsis SDK/connection.
/// </summary>
public sealed class NetsisErpAdapterPlaceholder : UnsupportedErpAdapterBase
{
    public override string ProviderKey => ErpProviderKeys.Netsis;
}

public abstract class UnsupportedErpAdapterBase : IErpAdapter
{
    public abstract string ProviderKey { get; }

    public IReadOnlySet<string> Capabilities { get; } = new HashSet<string>(StringComparer.Ordinal);

    public Task<ErpAdapterHealthResult> CheckHealthAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ErpAdapterHealthResult(
            IsHealthy: false,
            Status: ErpConnectionStatuses.Unavailable,
            LatencyMs: null,
            ErrorCode: "erp.adapter.not-implemented",
            ErrorMessage: $"{ProviderKey} adapter is not implemented in ERP-2L.",
            CheckedAtUtc: DateTimeOffset.UtcNow));
    }

    public Task<IReadOnlyList<ErpExternalCustomerRecord>> ReadCustomersAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default) =>
        Task.FromException<IReadOnlyList<ErpExternalCustomerRecord>>(
            new NotSupportedException($"{ProviderKey} customers.read is not implemented in ERP-2L."));

    public Task<IReadOnlyList<ErpExternalProductRecord>> ReadProductsAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default) =>
        Task.FromException<IReadOnlyList<ErpExternalProductRecord>>(
            new NotSupportedException($"{ProviderKey} products.read is not implemented in ERP-2L."));

    public Task<IReadOnlyList<ErpExternalOrderRecord>> ReadOrdersAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default) =>
        Task.FromException<IReadOnlyList<ErpExternalOrderRecord>>(
            new NotSupportedException($"{ProviderKey} orders.read is not implemented in ERP-2L."));
}

public sealed class ErpAdapterResolver : IErpAdapterResolver
{
    private readonly Dictionary<string, IErpAdapter> _adapters;

    public ErpAdapterResolver(IEnumerable<IErpAdapter> adapters)
    {
        _adapters = adapters.ToDictionary(a => a.ProviderKey, StringComparer.OrdinalIgnoreCase);
    }

    public IErpAdapter Resolve(string providerKey)
    {
        if (_adapters.TryGetValue(providerKey, out var adapter))
        {
            return adapter;
        }

        throw new InvalidOperationException($"Unknown ERP provider '{providerKey}'.");
    }
}
