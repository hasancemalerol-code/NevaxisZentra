namespace NevaxisZentra.Persistence.Erp.Netsis;

/// <summary>
/// Vendor-local Netsis integration transport classification.
/// Does not alter canonical ERP provider identity (<c>netsis</c>).
/// </summary>
public static class NetsisTransportKinds
{
    public const string Unconfigured = "unconfigured";
    public const string TestFixture = "test-fixture";
    public const string BridgeHttp = "bridge-http";
}

public static class NetsisCredentialReferences
{
    /// <summary>
    /// Development-only test transport selector. Must never be used for production Netsis connections.
    /// </summary>
    public const string TestFixture = "netsis-test-fixture";

    /// <summary>
    /// Development-only deterministic read failure selector for failure-preservation tests.
    /// </summary>
    public const string TestFixtureFailure = "netsis-test-fixture-failure";
}
