using Microsoft.Extensions.Configuration;

namespace NevaxisFactOS.Persistence.Erp.Logo;

public sealed record LogoTransportHealthResult(
    bool IsReachable,
    bool IsContextValid,
    bool AreCredentialsValid,
    bool IsCompanyContextValid,
    string ReadinessState,
    int? LatencyMs,
    string? ErrorCode,
    string? ErrorMessage);

public interface ILogoReadTransport
{
    string TransportKind { get; }

    Task<LogoTransportHealthResult> CheckHealthAsync(
        LogoConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default);

    Task<LogoContextInfo> DiscoverContextAsync(
        LogoConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default);

    Task<LogoReadPageResult<LogoCustomerSourceRecord>> ReadCustomersAsync(
        LogoConnectionDescriptor descriptor,
        LogoReadPageRequest page,
        CancellationToken cancellationToken = default);

    Task<LogoReadPageResult<LogoProductSourceRecord>> ReadProductsAsync(
        LogoConnectionDescriptor descriptor,
        LogoReadPageRequest page,
        CancellationToken cancellationToken = default);
}

public interface ILogoCredentialResolver
{
    string? ResolveBearerToken(string? credentialReference);
}

public sealed class ConfigurationLogoCredentialResolver(IConfiguration configuration) : ILogoCredentialResolver
{
    public string? ResolveBearerToken(string? credentialReference)
    {
        if (string.IsNullOrWhiteSpace(credentialReference))
        {
            return null;
        }

        return configuration[$"LogoIntegration:Credentials:{credentialReference}"];
    }
}

public sealed class LogoReadTransportOptions
{
    public int DefaultPageSize { get; init; } = LogoReadTransportDefaults.DefaultPageSize;

    public int MaxPageSize { get; init; } = LogoReadTransportDefaults.MaxPageSize;

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);
}

public sealed class LogoTransportResolver(
    LogoReadTransportTestFixture testFixture,
    LogoBridgeReadTransport bridgeTransport,
    LogoUnconfiguredReadTransport unconfiguredTransport)
{
    public ILogoReadTransport Resolve(LogoConnectionDescriptor descriptor) =>
        descriptor.TransportKind switch
        {
            LogoTransportKinds.TestFixture => testFixture,
            LogoTransportKinds.BridgeHttp => bridgeTransport,
            _ => unconfiguredTransport,
        };
}

public sealed class LogoUnconfiguredReadTransport : ILogoReadTransport
{
    public string TransportKind => LogoTransportKinds.Unconfigured;

    public Task<LogoTransportHealthResult> CheckHealthAsync(
        LogoConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var readiness = string.IsNullOrWhiteSpace(descriptor.EndpointReference)
            && string.IsNullOrWhiteSpace(descriptor.CredentialReference)
            ? LogoReadinessStates.CredentialsRequired
            : LogoReadinessStates.TransportUnavailable;

        return Task.FromResult(new LogoTransportHealthResult(
            IsReachable: false,
            IsContextValid: false,
            AreCredentialsValid: false,
            IsCompanyContextValid: false,
            ReadinessState: readiness,
            LatencyMs: null,
            ErrorCode: LogoTransportErrorCodes.TransportUnconfigured,
            ErrorMessage: "Logo transport is not configured. Provide bridge endpoint and credential reference or approved test fixture."));
    }

    public Task<LogoContextInfo> DiscoverContextAsync(
        LogoConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default) =>
        throw new LogoTransportException(
            LogoTransportErrorCodes.TransportUnconfigured,
            "Logo transport is not configured.");

    public Task<LogoReadPageResult<LogoCustomerSourceRecord>> ReadCustomersAsync(
        LogoConnectionDescriptor descriptor,
        LogoReadPageRequest page,
        CancellationToken cancellationToken = default) =>
        throw new LogoTransportException(
            LogoTransportErrorCodes.TransportUnconfigured,
            "Logo transport is not configured.");

    public Task<LogoReadPageResult<LogoProductSourceRecord>> ReadProductsAsync(
        LogoConnectionDescriptor descriptor,
        LogoReadPageRequest page,
        CancellationToken cancellationToken = default) =>
        throw new LogoTransportException(
            LogoTransportErrorCodes.TransportUnconfigured,
            "Logo transport is not configured.");
}
