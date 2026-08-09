namespace CrevaFactOS.Persistence.Erp;

/// <summary>
/// Vendor-neutral ERP writeback boundary. Vendor DTO transformation stays inside adapters.
/// Real Logo/Mikro/Netsis execution is out of scope for ERP-4L.
/// </summary>
public interface IErpWritebackAdapter
{
    string ProviderKey { get; }

    IReadOnlySet<string> WritebackCapabilities { get; }

    Task<ErpWritebackValidationResult> ValidateWritebackAsync(
        ErpAdapterConnectionContext connection,
        ErpWritebackExecutionContext executionContext,
        CancellationToken cancellationToken = default);

    Task<ErpWritebackExecutionResult> ExecuteWritebackAsync(
        ErpAdapterConnectionContext connection,
        ErpWritebackExecutionContext executionContext,
        CancellationToken cancellationToken = default);
}

public sealed record ErpWritebackExecutionContext(
    Guid WritebackRequestId,
    string CommandType,
    string PayloadJson,
    string PayloadHash,
    string IdempotencyKey);

public sealed record ErpWritebackValidationResult(
    bool IsValid,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record ErpWritebackExecutionResult(
    bool Succeeded,
    string? ExternalExecutionReference,
    string? ErrorCode,
    string? ErrorMessage);

public interface IErpWritebackAdapterResolver
{
    IErpWritebackAdapter? Resolve(string providerKey);
}

public sealed class ErpWritebackAdapterResolver(IEnumerable<IErpWritebackAdapter> adapters) : IErpWritebackAdapterResolver
{
    private readonly Dictionary<string, IErpWritebackAdapter> _adapters =
        adapters.ToDictionary(a => a.ProviderKey, StringComparer.OrdinalIgnoreCase);

    public IErpWritebackAdapter? Resolve(string providerKey) =>
        _adapters.TryGetValue(providerKey, out var adapter) ? adapter : null;
}
