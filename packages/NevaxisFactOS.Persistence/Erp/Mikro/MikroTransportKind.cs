namespace NevaxisFactOS.Persistence.Erp.Mikro;

/// <summary>
/// Vendor-local Mikro integration transport classification.
/// Does not alter canonical ERP provider identity (<c>mikro</c>).
/// </summary>
public static class MikroTransportKinds
{
    public const string Unconfigured = "unconfigured";
    public const string TestFixture = "test-fixture";
    public const string BridgeHttp = "bridge-http";
}

public static class MikroCredentialReferences
{
    /// <summary>
    /// Development-only test transport selector. Must never be used for production Mikro connections.
    /// </summary>
    public const string TestFixture = "mikro-test-fixture";

    /// <summary>
    /// Development-only deterministic read failure selector for failure-preservation tests.
    /// </summary>
    public const string TestFixtureFailure = "mikro-test-fixture-failure";
}
