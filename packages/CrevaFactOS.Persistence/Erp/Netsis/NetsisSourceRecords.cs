namespace CrevaFactOS.Persistence.Erp.Netsis;

/// <summary>
/// Netsis-side customer/cari source record. Confined to Netsis integration layer.
/// </summary>
public sealed record NetsisCustomerSourceRecord(
    string RecordId,
    string? Code,
    string Name,
    string? TaxNumber,
    string? Email,
    string? Phone,
    string? Country,
    bool IsActive,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc);

/// <summary>
/// Netsis-side product/material source record. Confined to Netsis integration layer.
/// </summary>
public sealed record NetsisProductSourceRecord(
    string RecordId,
    string? Code,
    string Name,
    string? Unit,
    string? Barcode,
    bool IsActive,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc);

public static class NetsisCustomerMapper
{
    public static ErpExternalCustomerRecord ToCanonical(NetsisCustomerSourceRecord source) =>
        new(
            ExternalId: source.RecordId,
            ExternalCode: source.Code,
            Name: source.Name,
            TaxNumber: source.TaxNumber,
            TaxOffice: null,
            Email: source.Email,
            Phone: source.Phone,
            CountryCode: source.Country,
            IsActive: source.IsActive,
            SourceVersion: source.SourceVersion,
            SourceUpdatedAtUtc: source.SourceUpdatedAtUtc);
}

public static class NetsisProductMapper
{
    public static ErpExternalProductRecord ToCanonical(NetsisProductSourceRecord source) =>
        new(
            ExternalId: source.RecordId,
            ExternalCode: source.Code,
            Name: source.Name,
            Unit: source.Unit,
            Barcode: source.Barcode,
            IsActive: source.IsActive,
            SourceVersion: source.SourceVersion,
            SourceUpdatedAtUtc: source.SourceUpdatedAtUtc);
}
