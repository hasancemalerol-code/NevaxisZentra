namespace CrevaFactOS.Persistence.Erp;

/// <summary>
/// In-process development ERP Simulator. Deterministic stable IDs.
/// Future second-PC handoff: HTTP/JSON over the same IErpAdapter contract
/// (CheckHealth / ReadCustomers / ReadProducts) without changing domain consumers.
/// </summary>
public sealed class SimulatorErpAdapter : IErpAdapter
{
    public const string DisplayName = "ERP Simulator";

    public string ProviderKey => ErpProviderKeys.Simulator;

    public IReadOnlySet<string> Capabilities { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ErpCapabilities.CustomersRead,
        ErpCapabilities.ProductsRead,
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
            ErrorMessage: $"{ProviderKey} adapter is not implemented in ERP-1L.",
            CheckedAtUtc: DateTimeOffset.UtcNow));
    }

    public Task<IReadOnlyList<ErpExternalCustomerRecord>> ReadCustomersAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default) =>
        Task.FromException<IReadOnlyList<ErpExternalCustomerRecord>>(
            new NotSupportedException($"{ProviderKey} customers.read is not implemented in ERP-1L."));

    public Task<IReadOnlyList<ErpExternalProductRecord>> ReadProductsAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default) =>
        Task.FromException<IReadOnlyList<ErpExternalProductRecord>>(
            new NotSupportedException($"{ProviderKey} products.read is not implemented in ERP-1L."));
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
