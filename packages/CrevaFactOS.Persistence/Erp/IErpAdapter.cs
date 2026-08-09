namespace CrevaFactOS.Persistence.Erp;

/// <summary>
/// Vendor-neutral ERP adapter boundary.
/// Vendor-specific DTOs/fields/rules must not leak outside adapter implementations.
/// ERP-1L is read-only: no writeback methods.
/// </summary>
public interface IErpAdapter
{
    string ProviderKey { get; }

    IReadOnlySet<string> Capabilities { get; }

    Task<ErpAdapterHealthResult> CheckHealthAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ErpExternalCustomerRecord>> ReadCustomersAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ErpExternalProductRecord>> ReadProductsAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ErpExternalOrderRecord>> ReadOrdersAsync(
        ErpAdapterConnectionContext connection,
        CancellationToken cancellationToken = default);
}

public sealed record ErpAdapterConnectionContext(
    Guid ConnectionProfileId,
    string Provider,
    string ConnectionMode,
    string? ExternalCompanyId,
    string? ExternalCompanyCode,
    string? BaseEndpoint,
    string? CredentialReference);

public sealed record ErpAdapterHealthResult(
    bool IsHealthy,
    string Status,
    int? LatencyMs,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset CheckedAtUtc);

/// <summary>
/// Canonical external customer shape produced by adapters after vendor normalization.
/// </summary>
public sealed record ErpExternalCustomerRecord(
    string ExternalId,
    string? ExternalCode,
    string Name,
    string? TaxNumber,
    string? TaxOffice,
    string? Email,
    string? Phone,
    string? CountryCode,
    bool IsActive,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc);

/// <summary>
/// Canonical external product shape produced by adapters after vendor normalization.
/// </summary>
public sealed record ErpExternalProductRecord(
    string ExternalId,
    string? ExternalCode,
    string Name,
    string? Unit,
    string? Barcode,
    bool IsActive,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc);

/// <summary>
/// Vendor-normalized order shape. Vendor-specific DTOs must not leak here.
/// </summary>
public sealed record ErpExternalOrderRecord(
    string ExternalId,
    string? ExternalCode,
    string OrderNumber,
    string ExternalCustomerId,
    string? ExternalCustomerCode,
    DateTimeOffset OrderDateUtc,
    DateTimeOffset? RequestedDeliveryDateUtc,
    string? CurrencyCode,
    string Status,
    string? Reference,
    string? Notes,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc,
    IReadOnlyList<ErpExternalOrderLineRecord> Lines);

public sealed record ErpExternalOrderLineRecord(
    string ExternalLineId,
    int LineNumber,
    string ExternalProductId,
    string? ExternalProductCode,
    string? Description,
    decimal Quantity,
    string Unit,
    DateTimeOffset? RequestedDeliveryDateUtc,
    string? SourceVersion);

public interface IErpAdapterResolver
{
    IErpAdapter Resolve(string providerKey);
}
