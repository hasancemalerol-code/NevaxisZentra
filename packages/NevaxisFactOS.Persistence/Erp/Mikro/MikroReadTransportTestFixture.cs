namespace NevaxisFactOS.Persistence.Erp.Mikro;

using System.Globalization;

/// <summary>
/// Deterministic development-only Mikro read transport fixture.
/// Clearly fake data — NOT real Mikro validation.
/// </summary>
public sealed class MikroReadTransportTestFixture : IMikroReadTransport
{
    public const string FixtureCompanyId = "MIKRO-TEST-COMPANY-01";
    public const string FixtureCompanyCode = "MIKRO-TEST-COMPANY-01";
    public const string FixtureProductFamily = "Mikro Test Fixture";
    public const string FixtureProductVersion = "test-1.0";

    public string TransportKind => MikroTransportKinds.TestFixture;

    private static readonly IReadOnlyList<MikroCustomerSourceRecord> Customers =
    [
        new(
            RecordId: "MIKRO-TEST-CUST-001",
            Code: "MIKRO-TEST-CUST-001",
            Name: "Mikro Test Cari A.Ş.",
            TaxNumber: "8888888881",
            Email: "mikro-test-cust-001@example.invalid",
            Phone: "+90-212-000-0101",
            Country: "TR",
            IsActive: true,
            SourceVersion: "test-v1",
            SourceUpdatedAtUtc: new DateTimeOffset(2026, 2, 15, 10, 0, 0, TimeSpan.Zero)),
        new(
            RecordId: "MIKRO-TEST-CUST-002",
            Code: "MIKRO-TEST-CUST-002",
            Name: "Mikro Test Cari B Ltd.",
            TaxNumber: "8888888882",
            Email: "mikro-test-cust-002@example.invalid",
            Phone: "+90-212-000-0102",
            Country: "TR",
            IsActive: true,
            SourceVersion: "test-v1",
            SourceUpdatedAtUtc: new DateTimeOffset(2026, 2, 16, 10, 0, 0, TimeSpan.Zero)),
    ];

    private static readonly IReadOnlyList<MikroProductSourceRecord> Products =
    [
        new(
            RecordId: "MIKRO-TEST-PRD-001",
            Code: "MIKRO-TEST-PRD-001",
            Name: "Mikro Test Malzeme 001",
            Unit: "AD",
            Barcode: "8698888000001",
            IsActive: true,
            SourceVersion: "test-v1",
            SourceUpdatedAtUtc: new DateTimeOffset(2026, 2, 15, 11, 0, 0, TimeSpan.Zero)),
        new(
            RecordId: "MIKRO-TEST-PRD-002",
            Code: "MIKRO-TEST-PRD-002",
            Name: "Mikro Test Malzeme 002",
            Unit: "KG",
            Barcode: "8698888000002",
            IsActive: true,
            SourceVersion: "test-v1",
            SourceUpdatedAtUtc: new DateTimeOffset(2026, 2, 16, 11, 0, 0, TimeSpan.Zero)),
    ];

    public Task<MikroTransportHealthResult> CheckHealthAsync(
        MikroConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureNotSimulatingFailure(descriptor);
        ValidateCompany(descriptor);

        return Task.FromResult(new MikroTransportHealthResult(
            IsReachable: true,
            IsContextValid: true,
            AreCredentialsValid: true,
            IsCompanyContextValid: true,
            ReadinessState: MikroReadinessStates.Reachable,
            LatencyMs: 1,
            ErrorCode: null,
            ErrorMessage: null));
    }

    public Task<MikroContextInfo> DiscoverContextAsync(
        MikroConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureNotSimulatingFailure(descriptor);
        ValidateCompany(descriptor);

        return Task.FromResult(new MikroContextInfo(
            ProductFamily: FixtureProductFamily,
            ProductVersion: FixtureProductVersion,
            CompanyId: FixtureCompanyId,
            CompanyCode: FixtureCompanyCode,
            CompanyName: "Mikro Test Şirketi",
            Period: 1,
            TransportKind: TransportKind,
            ReadOnlyMode: true,
            SupportedCapabilities:
            [
                ErpCapabilities.CustomersRead,
                ErpCapabilities.ProductsRead,
            ]));
    }

    public Task<MikroReadPageResult<MikroCustomerSourceRecord>> ReadCustomersAsync(
        MikroConnectionDescriptor descriptor,
        MikroReadPageRequest page,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureNotSimulatingFailure(descriptor);
        ValidateCompany(descriptor);
        return Task.FromResult(PageRecords(Customers, page));
    }

    public Task<MikroReadPageResult<MikroProductSourceRecord>> ReadProductsAsync(
        MikroConnectionDescriptor descriptor,
        MikroReadPageRequest page,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureNotSimulatingFailure(descriptor);
        ValidateCompany(descriptor);
        return Task.FromResult(PageRecords(Products, page));
    }

    internal static void EnsureNotSimulatingFailure(MikroConnectionDescriptor descriptor)
    {
        if (descriptor.SimulateReadFailure)
        {
            throw new MikroTransportException(
                MikroTransportErrorCodes.SourceReadFailed,
                "Mikro test transport failure mode is active.");
        }
    }

    internal static void ValidateCompany(MikroConnectionDescriptor descriptor)
    {
        if (!MatchesCompany(descriptor))
        {
            throw new MikroTransportException(
                MikroTransportErrorCodes.CompanyNotFound,
                "Configured Mikro company does not match test fixture company mapping.");
        }
    }

    internal static bool MatchesCompany(MikroConnectionDescriptor descriptor) =>
        string.Equals(descriptor.ExternalCompanyId, FixtureCompanyId, StringComparison.OrdinalIgnoreCase)
        || string.Equals(descriptor.ExternalCompanyCode, FixtureCompanyCode, StringComparison.OrdinalIgnoreCase);

    internal static MikroReadPageResult<T> PageRecords<T>(
        IReadOnlyList<T> records,
        MikroReadPageRequest page)
    {
        var pageSize = Math.Clamp(
            page.PageSize <= 0 ? MikroReadTransportDefaults.DefaultPageSize : page.PageSize,
            1,
            MikroReadTransportDefaults.MaxPageSize);

        var startIndex = 0;
        if (!string.IsNullOrWhiteSpace(page.ContinuationToken)
            && int.TryParse(page.ContinuationToken, out var parsedIndex))
        {
            startIndex = Math.Max(parsedIndex, 0);
        }

        var slice = records.Skip(startIndex).Take(pageSize).ToList();
        var nextIndex = startIndex + slice.Count;
        var continuation = nextIndex < records.Count ? nextIndex.ToString(CultureInfo.InvariantCulture) : null;
        return new MikroReadPageResult<T>(slice, continuation);
    }
}
