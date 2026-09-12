namespace NevaxisFactOS.Persistence.Erp.Logo;

using System.Globalization;

/// <summary>
/// Deterministic development-only Logo read transport fixture.
/// Clearly fake data — NOT real Logo validation.
/// </summary>
public sealed class LogoReadTransportTestFixture : ILogoReadTransport
{
    public const string FixtureCompanyId = "LOGO-TEST-COMPANY-01";
    public const string FixtureCompanyCode = "LOGO-TEST-COMPANY-01";
    public const string FixtureProductFamily = "Logo Test Fixture";
    public const string FixtureProductVersion = "test-1.0";

    public string TransportKind => LogoTransportKinds.TestFixture;

    private static readonly IReadOnlyList<LogoCustomerSourceRecord> Customers =
    [
        new(
            RecordId: "LOGO-TEST-CUST-001",
            Code: "LOGO-TEST-CUST-001",
            Name: "Logo Test Cari A.Ş.",
            TaxNumber: "9999999991",
            Email: "logo-test-cust-001@example.invalid",
            Phone: "+90-212-000-0001",
            Country: "TR",
            IsActive: true,
            SourceVersion: "test-v1",
            SourceUpdatedAtUtc: new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero)),
        new(
            RecordId: "LOGO-TEST-CUST-002",
            Code: "LOGO-TEST-CUST-002",
            Name: "Logo Test Cari B Ltd.",
            TaxNumber: "9999999992",
            Email: "logo-test-cust-002@example.invalid",
            Phone: "+90-212-000-0002",
            Country: "TR",
            IsActive: true,
            SourceVersion: "test-v1",
            SourceUpdatedAtUtc: new DateTimeOffset(2026, 1, 16, 10, 0, 0, TimeSpan.Zero)),
    ];

    private static readonly IReadOnlyList<LogoProductSourceRecord> Products =
    [
        new(
            RecordId: "LOGO-TEST-PRD-001",
            Code: "LOGO-TEST-PRD-001",
            Name: "Logo Test Malzeme 001",
            Unit: "AD",
            Barcode: "8699999000001",
            IsActive: true,
            SourceVersion: "test-v1",
            SourceUpdatedAtUtc: new DateTimeOffset(2026, 1, 15, 11, 0, 0, TimeSpan.Zero)),
        new(
            RecordId: "LOGO-TEST-PRD-002",
            Code: "LOGO-TEST-PRD-002",
            Name: "Logo Test Malzeme 002",
            Unit: "KG",
            Barcode: "8699999000002",
            IsActive: true,
            SourceVersion: "test-v1",
            SourceUpdatedAtUtc: new DateTimeOffset(2026, 1, 16, 11, 0, 0, TimeSpan.Zero)),
    ];

    public Task<LogoTransportHealthResult> CheckHealthAsync(
        LogoConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateCompany(descriptor);

        return Task.FromResult(new LogoTransportHealthResult(
            IsReachable: true,
            IsContextValid: true,
            AreCredentialsValid: true,
            IsCompanyContextValid: true,
            ReadinessState: LogoReadinessStates.Reachable,
            LatencyMs: 1,
            ErrorCode: null,
            ErrorMessage: null));
    }

    public Task<LogoContextInfo> DiscoverContextAsync(
        LogoConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateCompany(descriptor);

        return Task.FromResult(new LogoContextInfo(
            ProductFamily: FixtureProductFamily,
            ProductVersion: FixtureProductVersion,
            CompanyId: FixtureCompanyId,
            CompanyCode: FixtureCompanyCode,
            CompanyName: "Logo Test Şirketi",
            Period: 1,
            TransportKind: TransportKind,
            ReadOnlyMode: true,
            SupportedCapabilities:
            [
                ErpCapabilities.CustomersRead,
                ErpCapabilities.ProductsRead,
            ]));
    }

    public Task<LogoReadPageResult<LogoCustomerSourceRecord>> ReadCustomersAsync(
        LogoConnectionDescriptor descriptor,
        LogoReadPageRequest page,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateCompany(descriptor);
        return Task.FromResult(PageRecords(Customers, page));
    }

    public Task<LogoReadPageResult<LogoProductSourceRecord>> ReadProductsAsync(
        LogoConnectionDescriptor descriptor,
        LogoReadPageRequest page,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateCompany(descriptor);
        return Task.FromResult(PageRecords(Products, page));
    }

    internal static void ValidateCompany(LogoConnectionDescriptor descriptor)
    {
        if (!MatchesCompany(descriptor))
        {
            throw new LogoTransportException(
                LogoTransportErrorCodes.CompanyNotFound,
                "Configured Logo company does not match test fixture company mapping.");
        }
    }

    internal static bool MatchesCompany(LogoConnectionDescriptor descriptor) =>
        string.Equals(descriptor.ExternalCompanyId, FixtureCompanyId, StringComparison.OrdinalIgnoreCase)
        || string.Equals(descriptor.ExternalCompanyCode, FixtureCompanyCode, StringComparison.OrdinalIgnoreCase);

    internal static LogoReadPageResult<T> PageRecords<T>(
        IReadOnlyList<T> records,
        LogoReadPageRequest page)
    {
        var pageSize = Math.Clamp(
            page.PageSize <= 0 ? LogoReadTransportDefaults.DefaultPageSize : page.PageSize,
            1,
            LogoReadTransportDefaults.MaxPageSize);

        var startIndex = 0;
        if (!string.IsNullOrWhiteSpace(page.ContinuationToken)
            && int.TryParse(page.ContinuationToken, out var parsedIndex))
        {
            startIndex = Math.Max(parsedIndex, 0);
        }

        var slice = records.Skip(startIndex).Take(pageSize).ToList();
        var nextIndex = startIndex + slice.Count;
        var continuation = nextIndex < records.Count ? nextIndex.ToString(CultureInfo.InvariantCulture) : null;
        return new LogoReadPageResult<T>(slice, continuation);
    }
}
