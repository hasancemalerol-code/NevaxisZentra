using Microsoft.Extensions.Configuration;

namespace CrevaFactOS.Persistence.Erp.Netsis;

public sealed record NetsisTransportHealthResult(
    bool IsReachable,
    bool IsContextValid,
    bool AreCredentialsValid,
    bool IsCompanyContextValid,
    string ReadinessState,
    int? LatencyMs,
    string? ErrorCode,
    string? ErrorMessage);

public interface INetsisReadTransport
{
    string TransportKind { get; }

    Task<NetsisTransportHealthResult> CheckHealthAsync(
        NetsisConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default);

    Task<NetsisContextInfo> DiscoverContextAsync(
        NetsisConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default);

    Task<NetsisReadPageResult<NetsisCustomerSourceRecord>> ReadCustomersAsync(
        NetsisConnectionDescriptor descriptor,
        NetsisReadPageRequest page,
        CancellationToken cancellationToken = default);

    Task<NetsisReadPageResult<NetsisProductSourceRecord>> ReadProductsAsync(
        NetsisConnectionDescriptor descriptor,
        NetsisReadPageRequest page,
        CancellationToken cancellationToken = default);
}

public interface INetsisCredentialResolver
{
    string? ResolveBearerToken(string? credentialReference);
}

public sealed class ConfigurationNetsisCredentialResolver(IConfiguration configuration) : INetsisCredentialResolver
{
    public string? ResolveBearerToken(string? credentialReference)
    {
        if (string.IsNullOrWhiteSpace(credentialReference))
        {
            return null;
        }

        return configuration[$"NetsisIntegration:Credentials:{credentialReference}"];
    }
}

public sealed class NetsisReadTransportOptions
{
    public int DefaultPageSize { get; init; } = NetsisReadTransportDefaults.DefaultPageSize;

    public int MaxPageSize { get; init; } = NetsisReadTransportDefaults.MaxPageSize;

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);
}

public sealed class NetsisTransportResolver(
    NetsisReadTransportTestFixture testFixture,
    NetsisBridgeReadTransport bridgeTransport,
    NetsisUnconfiguredReadTransport unconfiguredTransport)
{
    public INetsisReadTransport Resolve(NetsisConnectionDescriptor descriptor) =>
        descriptor.TransportKind switch
        {
            NetsisTransportKinds.TestFixture => testFixture,
            NetsisTransportKinds.BridgeHttp => bridgeTransport,
            _ => unconfiguredTransport,
        };
}

public sealed class NetsisUnconfiguredReadTransport : INetsisReadTransport
{
    public string TransportKind => NetsisTransportKinds.Unconfigured;

    public Task<NetsisTransportHealthResult> CheckHealthAsync(
        NetsisConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var readiness = string.IsNullOrWhiteSpace(descriptor.EndpointReference)
            && string.IsNullOrWhiteSpace(descriptor.CredentialReference)
            ? NetsisReadinessStates.CredentialsRequired
            : NetsisReadinessStates.TransportUnavailable;

        return Task.FromResult(new NetsisTransportHealthResult(
            IsReachable: false,
            IsContextValid: false,
            AreCredentialsValid: false,
            IsCompanyContextValid: false,
            ReadinessState: readiness,
            LatencyMs: null,
            ErrorCode: NetsisTransportErrorCodes.TransportUnconfigured,
            ErrorMessage: "Netsis transport is not configured. Provide bridge endpoint and credential reference or approved test fixture."));
    }

    public Task<NetsisContextInfo> DiscoverContextAsync(
        NetsisConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default) =>
        throw new NetsisTransportException(
            NetsisTransportErrorCodes.TransportUnconfigured,
            "Netsis transport is not configured.");

    public Task<NetsisReadPageResult<NetsisCustomerSourceRecord>> ReadCustomersAsync(
        NetsisConnectionDescriptor descriptor,
        NetsisReadPageRequest page,
        CancellationToken cancellationToken = default) =>
        throw new NetsisTransportException(
            NetsisTransportErrorCodes.TransportUnconfigured,
            "Netsis transport is not configured.");

    public Task<NetsisReadPageResult<NetsisProductSourceRecord>> ReadProductsAsync(
        NetsisConnectionDescriptor descriptor,
        NetsisReadPageRequest page,
        CancellationToken cancellationToken = default) =>
        throw new NetsisTransportException(
            NetsisTransportErrorCodes.TransportUnconfigured,
            "Netsis transport is not configured.");
}
