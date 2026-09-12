namespace NevaxisFactOS.Persistence.Erp.Logo;

/// <summary>
/// Vendor-local connection descriptor for Logo adapter transport resolution.
/// Records discovered or configured integration context without canonical leakage.
/// </summary>
public sealed record LogoConnectionDescriptor(
    string TransportKind,
    string? ProductFamily,
    string? ProductVersion,
    string? ExternalCompanyId,
    string? ExternalCompanyCode,
    int? Period,
    string? EndpointReference,
    string? CredentialReference);

public static class LogoConnectionDescriptorFactory
{
    public static LogoConnectionDescriptor FromAdapterContext(ErpAdapterConnectionContext connection)
    {
        var transportKind = ResolveTransportKind(connection);
        return new LogoConnectionDescriptor(
            TransportKind: transportKind,
            ProductFamily: null,
            ProductVersion: null,
            ExternalCompanyId: connection.ExternalCompanyId,
            ExternalCompanyCode: connection.ExternalCompanyCode,
            Period: null,
            EndpointReference: connection.BaseEndpoint,
            CredentialReference: connection.CredentialReference);
    }

    internal static string ResolveTransportKind(ErpAdapterConnectionContext connection)
    {
        if (string.Equals(
                connection.CredentialReference,
                LogoCredentialReferences.TestFixture,
                StringComparison.Ordinal))
        {
            return LogoTransportKinds.TestFixture;
        }

        if (string.Equals(connection.ConnectionMode, ErpConnectionModes.Api, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(connection.BaseEndpoint))
        {
            return LogoTransportKinds.BridgeHttp;
        }

        return LogoTransportKinds.Unconfigured;
    }
}
