using Microsoft.Extensions.Configuration;

namespace CrevaFactOS.Persistence.Erp.Mikro;

public sealed record MikroTransportHealthResult(
    bool IsReachable,
    bool IsContextValid,
    bool AreCredentialsValid,
    bool IsCompanyContextValid,
    string ReadinessState,
    int? LatencyMs,
    string? ErrorCode,
    string? ErrorMessage);

public interface IMikroReadTransport
{
    string TransportKind { get; }

    Task<MikroTransportHealthResult> CheckHealthAsync(
        MikroConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default);

    Task<MikroContextInfo> DiscoverContextAsync(
        MikroConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default);

    Task<MikroReadPageResult<MikroCustomerSourceRecord>> ReadCustomersAsync(
        MikroConnectionDescriptor descriptor,
        MikroReadPageRequest page,
        CancellationToken cancellationToken = default);

    Task<MikroReadPageResult<MikroProductSourceRecord>> ReadProductsAsync(
        MikroConnectionDescriptor descriptor,
        MikroReadPageRequest page,
        CancellationToken cancellationToken = default);
}

public interface IMikroCredentialResolver
{
    string? ResolveBearerToken(string? credentialReference);
}

public sealed class ConfigurationMikroCredentialResolver(IConfiguration configuration) : IMikroCredentialResolver
{
    public string? ResolveBearerToken(string? credentialReference)
    {
        if (string.IsNullOrWhiteSpace(credentialReference))
        {
            return null;
        }

        return configuration[$"MikroIntegration:Credentials:{credentialReference}"];
    }
}

public sealed class MikroReadTransportOptions
{
    public int DefaultPageSize { get; init; } = MikroReadTransportDefaults.DefaultPageSize;

    public int MaxPageSize { get; init; } = MikroReadTransportDefaults.MaxPageSize;

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);
}

public sealed class MikroTransportResolver(
    MikroReadTransportTestFixture testFixture,
    MikroBridgeReadTransport bridgeTransport,
    MikroUnconfiguredReadTransport unconfiguredTransport)
{
    public IMikroReadTransport Resolve(MikroConnectionDescriptor descriptor) =>
        descriptor.TransportKind switch
        {
            MikroTransportKinds.TestFixture => testFixture,
            MikroTransportKinds.BridgeHttp => bridgeTransport,
            _ => unconfiguredTransport,
        };
}

public sealed class MikroUnconfiguredReadTransport : IMikroReadTransport
{
    public string TransportKind => MikroTransportKinds.Unconfigured;

    public Task<MikroTransportHealthResult> CheckHealthAsync(
        MikroConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var readiness = string.IsNullOrWhiteSpace(descriptor.EndpointReference)
            && string.IsNullOrWhiteSpace(descriptor.CredentialReference)
            ? MikroReadinessStates.CredentialsRequired
            : MikroReadinessStates.TransportUnavailable;

        return Task.FromResult(new MikroTransportHealthResult(
            IsReachable: false,
            IsContextValid: false,
            AreCredentialsValid: false,
            IsCompanyContextValid: false,
            ReadinessState: readiness,
            LatencyMs: null,
            ErrorCode: MikroTransportErrorCodes.TransportUnconfigured,
            ErrorMessage: "Mikro transport is not configured. Provide bridge endpoint and credential reference or approved test fixture."));
    }

    public Task<MikroContextInfo> DiscoverContextAsync(
        MikroConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default) =>
        throw new MikroTransportException(
            MikroTransportErrorCodes.TransportUnconfigured,
            "Mikro transport is not configured.");

    public Task<MikroReadPageResult<MikroCustomerSourceRecord>> ReadCustomersAsync(
        MikroConnectionDescriptor descriptor,
        MikroReadPageRequest page,
        CancellationToken cancellationToken = default) =>
        throw new MikroTransportException(
            MikroTransportErrorCodes.TransportUnconfigured,
            "Mikro transport is not configured.");

    public Task<MikroReadPageResult<MikroProductSourceRecord>> ReadProductsAsync(
        MikroConnectionDescriptor descriptor,
        MikroReadPageRequest page,
        CancellationToken cancellationToken = default) =>
        throw new MikroTransportException(
            MikroTransportErrorCodes.TransportUnconfigured,
            "Mikro transport is not configured.");
}
