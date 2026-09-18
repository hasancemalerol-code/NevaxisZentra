namespace NevaxisZentra.Persistence.Erp.Mikro;

/// <summary>
/// Mikro-side customer/cari source record. Confined to Mikro integration layer.
/// </summary>
public sealed record MikroCustomerSourceRecord(
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
/// Mikro-side product/material source record. Confined to Mikro integration layer.
/// </summary>
public sealed record MikroProductSourceRecord(
    string RecordId,
    string? Code,
    string Name,
    string? Unit,
    string? Barcode,
    bool IsActive,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc);

public static class MikroCustomerMapper
{
    public static ErpExternalCustomerRecord ToCanonical(MikroCustomerSourceRecord source) =>
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

public static class MikroProductMapper
{
    public static ErpExternalProductRecord ToCanonical(MikroProductSourceRecord source) =>
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
