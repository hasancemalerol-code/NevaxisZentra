namespace NevaxisFactOS.Persistence.Erp.Mikro;

/// <summary>
/// Vendor-local connection descriptor for Mikro adapter transport resolution.
/// Records discovered or configured integration context without canonical leakage.
/// </summary>
public sealed record MikroConnectionDescriptor(
    string TransportKind,
    string? ProductFamily,
    string? ProductVersion,
    string? ExternalCompanyId,
    string? ExternalCompanyCode,
    int? Period,
    string? EndpointReference,
    string? CredentialReference,
    bool SimulateReadFailure = false);

public static class MikroConnectionDescriptorFactory
{
    public static MikroConnectionDescriptor FromAdapterContext(ErpAdapterConnectionContext connection)
    {
        var transportKind = ResolveTransportKind(connection);
        var simulateFailure = string.Equals(
            connection.CredentialReference,
            MikroCredentialReferences.TestFixtureFailure,
            StringComparison.Ordinal);

        return new MikroConnectionDescriptor(
            TransportKind: transportKind,
            ProductFamily: null,
            ProductVersion: null,
            ExternalCompanyId: connection.ExternalCompanyId,
            ExternalCompanyCode: connection.ExternalCompanyCode,
            Period: null,
            EndpointReference: connection.BaseEndpoint,
            CredentialReference: connection.CredentialReference,
            SimulateReadFailure: simulateFailure);
    }

    internal static string ResolveTransportKind(ErpAdapterConnectionContext connection)
    {
        if (string.Equals(
                connection.CredentialReference,
                MikroCredentialReferences.TestFixture,
                StringComparison.Ordinal)
            || string.Equals(
                connection.CredentialReference,
                MikroCredentialReferences.TestFixtureFailure,
                StringComparison.Ordinal))
        {
            return MikroTransportKinds.TestFixture;
        }

        if (string.Equals(connection.ConnectionMode, ErpConnectionModes.Api, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(connection.BaseEndpoint))
        {
            return MikroTransportKinds.BridgeHttp;
        }

        return MikroTransportKinds.Unconfigured;
    }
}
