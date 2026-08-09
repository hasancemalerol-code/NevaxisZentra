namespace CrevaFactOS.Persistence.Erp.Logo;

/// <summary>
/// Logo-side customer/cari source record. Confined to Logo integration layer.
/// </summary>
public sealed record LogoCustomerSourceRecord(
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
/// Logo-side product/material-card source record. Confined to Logo integration layer.
/// </summary>
public sealed record LogoProductSourceRecord(
    string RecordId,
    string? Code,
    string Name,
    string? Unit,
    string? Barcode,
    bool IsActive,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc);

public static class LogoCustomerMapper
{
    public static ErpExternalCustomerRecord ToCanonical(LogoCustomerSourceRecord source) =>
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

public static class LogoProductMapper
{
    public static ErpExternalProductRecord ToCanonical(LogoProductSourceRecord source) =>
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
