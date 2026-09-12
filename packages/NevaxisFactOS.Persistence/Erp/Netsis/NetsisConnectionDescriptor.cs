namespace NevaxisFactOS.Persistence.Erp.Netsis;

/// <summary>
/// Vendor-local connection descriptor for Netsis adapter transport resolution.
/// Records discovered or configured integration context without canonical leakage.
/// </summary>
public sealed record NetsisConnectionDescriptor(
    string TransportKind,
    string? ProductFamily,
    string? ProductVersion,
    string? ExternalCompanyId,
    string? ExternalCompanyCode,
    int? Period,
    string? EndpointReference,
    string? CredentialReference,
    bool SimulateReadFailure = false);

public static class NetsisConnectionDescriptorFactory
{
    public static NetsisConnectionDescriptor FromAdapterContext(ErpAdapterConnectionContext connection)
    {
        var transportKind = ResolveTransportKind(connection);
        var simulateFailure = string.Equals(
            connection.CredentialReference,
            NetsisCredentialReferences.TestFixtureFailure,
            StringComparison.Ordinal);

        return new NetsisConnectionDescriptor(
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
                NetsisCredentialReferences.TestFixture,
                StringComparison.Ordinal)
            || string.Equals(
                connection.CredentialReference,
                NetsisCredentialReferences.TestFixtureFailure,
                StringComparison.Ordinal))
        {
            return NetsisTransportKinds.TestFixture;
        }

        if (string.Equals(connection.ConnectionMode, ErpConnectionModes.Api, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(connection.BaseEndpoint))
        {
            return NetsisTransportKinds.BridgeHttp;
        }

        return NetsisTransportKinds.Unconfigured;
    }
}
