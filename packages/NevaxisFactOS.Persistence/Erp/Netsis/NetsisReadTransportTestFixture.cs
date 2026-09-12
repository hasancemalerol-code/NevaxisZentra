namespace NevaxisFactOS.Persistence.Erp.Netsis;

using System.Globalization;

/// <summary>
/// Deterministic development-only Netsis read transport fixture.
/// Clearly fake data — NOT real Netsis validation.
/// </summary>
public sealed class NetsisReadTransportTestFixture : INetsisReadTransport
{
    public const string FixtureCompanyId = "NETSIS-TEST-COMPANY-01";
    public const string FixtureCompanyCode = "NETSIS-TEST-COMPANY-01";
    public const string FixtureProductFamily = "Netsis Test Fixture";
    public const string FixtureProductVersion = "test-1.0";

    public string TransportKind => NetsisTransportKinds.TestFixture;

    private static readonly IReadOnlyList<NetsisCustomerSourceRecord> Customers =
    [
        new(
            RecordId: "NETSIS-TEST-CUST-001",
            Code: "NETSIS-TEST-CUST-001",
            Name: "Netsis Test Cari A.Ş.",
            TaxNumber: "7777777771",
            Email: "netsis-test-cust-001@example.invalid",
            Phone: "+90-212-000-0201",
            Country: "TR",
            IsActive: true,
            SourceVersion: "test-v1",
            SourceUpdatedAtUtc: new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero)),
        new(
            RecordId: "NETSIS-TEST-CUST-002",
            Code: "NETSIS-TEST-CUST-002",
            Name: "Netsis Test Cari B Ltd.",
            TaxNumber: "7777777772",
            Email: "netsis-test-cust-002@example.invalid",
            Phone: "+90-212-000-0202",
            Country: "TR",
            IsActive: true,
            SourceVersion: "test-v1",
            SourceUpdatedAtUtc: new DateTimeOffset(2026, 3, 16, 10, 0, 0, TimeSpan.Zero)),
    ];

    private static readonly IReadOnlyList<NetsisProductSourceRecord> Products =
    [
        new(
            RecordId: "NETSIS-TEST-PRD-001",
            Code: "NETSIS-TEST-PRD-001",
            Name: "Netsis Test Malzeme 001",
            Unit: "AD",
            Barcode: "8697777000001",
            IsActive: true,
            SourceVersion: "test-v1",
            SourceUpdatedAtUtc: new DateTimeOffset(2026, 3, 15, 11, 0, 0, TimeSpan.Zero)),
        new(
            RecordId: "NETSIS-TEST-PRD-002",
            Code: "NETSIS-TEST-PRD-002",
            Name: "Netsis Test Malzeme 002",
            Unit: "KG",
            Barcode: "8697777000002",
            IsActive: true,
            SourceVersion: "test-v1",
            SourceUpdatedAtUtc: new DateTimeOffset(2026, 3, 16, 11, 0, 0, TimeSpan.Zero)),
    ];

    public Task<NetsisTransportHealthResult> CheckHealthAsync(
        NetsisConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureNotSimulatingFailure(descriptor);
        ValidateCompany(descriptor);

        return Task.FromResult(new NetsisTransportHealthResult(
            IsReachable: true,
            IsContextValid: true,
            AreCredentialsValid: true,
            IsCompanyContextValid: true,
            ReadinessState: NetsisReadinessStates.Reachable,
            LatencyMs: 1,
            ErrorCode: null,
            ErrorMessage: null));
    }

    public Task<NetsisContextInfo> DiscoverContextAsync(
        NetsisConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureNotSimulatingFailure(descriptor);
        ValidateCompany(descriptor);

        return Task.FromResult(new NetsisContextInfo(
            ProductFamily: FixtureProductFamily,
            ProductVersion: FixtureProductVersion,
            CompanyId: FixtureCompanyId,
            CompanyCode: FixtureCompanyCode,
            CompanyName: "Netsis Test Şirketi",
            Period: 1,
            TransportKind: TransportKind,
            ReadOnlyMode: true,
            SupportedCapabilities:
            [
                ErpCapabilities.CustomersRead,
                ErpCapabilities.ProductsRead,
            ]));
    }

    public Task<NetsisReadPageResult<NetsisCustomerSourceRecord>> ReadCustomersAsync(
        NetsisConnectionDescriptor descriptor,
        NetsisReadPageRequest page,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureNotSimulatingFailure(descriptor);
        ValidateCompany(descriptor);
        return Task.FromResult(PageRecords(Customers, page));
    }

    public Task<NetsisReadPageResult<NetsisProductSourceRecord>> ReadProductsAsync(
        NetsisConnectionDescriptor descriptor,
        NetsisReadPageRequest page,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureNotSimulatingFailure(descriptor);
        ValidateCompany(descriptor);
        return Task.FromResult(PageRecords(Products, page));
    }

    internal static void EnsureNotSimulatingFailure(NetsisConnectionDescriptor descriptor)
    {
        if (descriptor.SimulateReadFailure)
        {
            throw new NetsisTransportException(
                NetsisTransportErrorCodes.SourceReadFailed,
                "Netsis test transport failure mode is active.");
        }
    }

    internal static void ValidateCompany(NetsisConnectionDescriptor descriptor)
    {
        if (!MatchesCompany(descriptor))
        {
            throw new NetsisTransportException(
                NetsisTransportErrorCodes.CompanyNotFound,
                "Configured Netsis company does not match test fixture company mapping.");
        }
    }

    internal static bool MatchesCompany(NetsisConnectionDescriptor descriptor) =>
        string.Equals(descriptor.ExternalCompanyId, FixtureCompanyId, StringComparison.OrdinalIgnoreCase)
        || string.Equals(descriptor.ExternalCompanyCode, FixtureCompanyCode, StringComparison.OrdinalIgnoreCase);

    internal static NetsisReadPageResult<T> PageRecords<T>(
        IReadOnlyList<T> records,
        NetsisReadPageRequest page)
    {
        var pageSize = Math.Clamp(
            page.PageSize <= 0 ? NetsisReadTransportDefaults.DefaultPageSize : page.PageSize,
            1,
            NetsisReadTransportDefaults.MaxPageSize);

        var startIndex = 0;
        if (!string.IsNullOrWhiteSpace(page.ContinuationToken)
            && int.TryParse(page.ContinuationToken, out var parsedIndex))
        {
            startIndex = Math.Max(parsedIndex, 0);
        }

        var slice = records.Skip(startIndex).Take(pageSize).ToList();
        var nextIndex = startIndex + slice.Count;
        var continuation = nextIndex < records.Count ? nextIndex.ToString(CultureInfo.InvariantCulture) : null;
        return new NetsisReadPageResult<T>(slice, continuation);
    }
}
