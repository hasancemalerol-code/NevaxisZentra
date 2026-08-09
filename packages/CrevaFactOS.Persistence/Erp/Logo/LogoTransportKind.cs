namespace CrevaFactOS.Persistence.Erp.Logo;

/// <summary>
/// Vendor-local Logo integration transport classification.
/// Does not alter canonical ERP provider identity (<c>logo</c>).
/// </summary>
public static class LogoTransportKinds
{
    public const string Unconfigured = "unconfigured";
    public const string TestFixture = "test-fixture";
    public const string BridgeHttp = "bridge-http";
}

public static class LogoCredentialReferences
{
    /// <summary>
    /// Development-only test transport selector. Must never be used for production Logo connections.
    /// </summary>
    public const string TestFixture = "logo-test-fixture";
}
