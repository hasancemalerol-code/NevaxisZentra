using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace NevaxisZentra.Persistence.Erp.Logo;

/// <summary>
/// Read-only HTTP transport for NevaxisZentra Logo Bridge on the Logo integration PC.
/// </summary>
public sealed class LogoBridgeReadTransport(
    HttpClient httpClient,
    ILogoCredentialResolver credentialResolver,
    LogoReadTransportOptions options) : ILogoReadTransport
{
    public string TransportKind => LogoTransportKinds.BridgeHttp;

    public async Task<LogoTransportHealthResult> CheckHealthAsync(
        LogoConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await SendAsync(HttpMethod.Get, descriptor, "health", null, cancellationToken);
            stopwatch.Stop();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return FailedHealth(
                    LogoTransportErrorCodes.AuthenticationFailed,
                    "Logo bridge authentication failed.",
                    LogoReadinessStates.CredentialsRequired,
                    stopwatch);
            }

            if (!response.IsSuccessStatusCode)
            {
                return FailedHealth(
                    LogoTransportErrorCodes.TransportUnreachable,
                    "Logo bridge is unreachable.",
                    LogoReadinessStates.TransportUnavailable,
                    stopwatch);
            }

            var payload = await response.Content.ReadFromJsonAsync<LogoBridgeHealthResponse>(cancellationToken);
            var companyValid = ValidateCompanyMapping(descriptor, payload?.CompanyId, payload?.CompanyCode);

            return new LogoTransportHealthResult(
                IsReachable: true,
                IsContextValid: payload?.ContextValid ?? false,
                AreCredentialsValid: true,
                IsCompanyContextValid: companyValid,
                ReadinessState: payload?.ReadinessState ?? LogoReadinessStates.Reachable,
                LatencyMs: (int)stopwatch.ElapsedMilliseconds,
                ErrorCode: companyValid ? null : LogoTransportErrorCodes.CompanyNotFound,
                ErrorMessage: companyValid ? null : "Configured company does not match bridge context.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            return FailedHealth(
                LogoTransportErrorCodes.Timeout,
                "Logo bridge request timed out.",
                LogoReadinessStates.TransportUnavailable,
                stopwatch);
        }
        catch (HttpRequestException)
        {
            return FailedHealth(
                LogoTransportErrorCodes.TransportUnreachable,
                "Logo bridge is unreachable.",
                LogoReadinessStates.TransportUnavailable,
                stopwatch);
        }
    }

    public async Task<LogoContextInfo> DiscoverContextAsync(
        LogoConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, descriptor, "context", null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<LogoBridgeContextResponse>(cancellationToken)
            ?? throw new LogoTransportException(
                LogoTransportErrorCodes.ContextInvalid,
                "Logo bridge returned invalid context.");

        if (!ValidateCompanyMapping(descriptor, payload.CompanyId, payload.CompanyCode))
        {
            throw new LogoTransportException(
                LogoTransportErrorCodes.CompanyNotFound,
                "Configured company does not match bridge context.");
        }

        return new LogoContextInfo(
            payload.ProductFamily,
            payload.ProductVersion,
            payload.CompanyId,
            payload.CompanyCode,
            payload.CompanyName,
            payload.Period,
            payload.TransportKind ?? TransportKind,
            payload.ReadOnlyMode,
            payload.SupportedCapabilities ?? []);
    }

    public async Task<LogoReadPageResult<LogoCustomerSourceRecord>> ReadCustomersAsync(
        LogoConnectionDescriptor descriptor,
        LogoReadPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = BuildPageQuery(page);
        using var response = await SendAsync(HttpMethod.Get, descriptor, "customers", query, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<LogoBridgeCustomerPageResponse>(cancellationToken)
            ?? throw new LogoTransportException(
                LogoTransportErrorCodes.SourceReadFailed,
                "Logo bridge returned invalid customer page.");

        return new LogoReadPageResult<LogoCustomerSourceRecord>(
            payload.Items?.Select(MapCustomer).ToList() ?? [],
            payload.ContinuationToken);
    }

    public async Task<LogoReadPageResult<LogoProductSourceRecord>> ReadProductsAsync(
        LogoConnectionDescriptor descriptor,
        LogoReadPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = BuildPageQuery(page);
        using var response = await SendAsync(HttpMethod.Get, descriptor, "products", query, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<LogoBridgeProductPageResponse>(cancellationToken)
            ?? throw new LogoTransportException(
                LogoTransportErrorCodes.SourceReadFailed,
                "Logo bridge returned invalid product page.");

        return new LogoReadPageResult<LogoProductSourceRecord>(
            payload.Items?.Select(MapProduct).ToList() ?? [],
            payload.ContinuationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        LogoConnectionDescriptor descriptor,
        string relativePath,
        string? query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(descriptor.EndpointReference))
        {
            throw new LogoTransportException(
                LogoTransportErrorCodes.TransportUnconfigured,
                "Logo bridge endpoint is not configured.");
        }

        var token = credentialResolver.ResolveBearerToken(descriptor.CredentialReference);
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new LogoTransportException(
                LogoTransportErrorCodes.AuthenticationFailed,
                "Logo bridge credential reference could not be resolved.");
        }

        using var request = new HttpRequestMessage(method, BuildUri(descriptor.EndpointReference, relativePath, query));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(options.RequestTimeout);
        return await httpClient.SendAsync(request, timeoutCts.Token);
    }

    private static string BuildUri(string baseEndpoint, string relativePath, string? query)
    {
        var trimmed = baseEndpoint.TrimEnd('/');
        var path = $"{trimmed}/{relativePath}";
        return string.IsNullOrWhiteSpace(query) ? path : $"{path}?{query}";
    }

    private static string BuildPageQuery(LogoReadPageRequest page)
    {
        var pageSize = Math.Clamp(
            page.PageSize <= 0 ? LogoReadTransportDefaults.DefaultPageSize : page.PageSize,
            1,
            LogoReadTransportDefaults.MaxPageSize);
        var parts = new List<string> { $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(page.ContinuationToken))
        {
            parts.Add($"cursor={Uri.EscapeDataString(page.ContinuationToken)}");
        }

        return string.Join('&', parts);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new LogoTransportException(
                LogoTransportErrorCodes.AuthenticationFailed,
                "Logo bridge authentication failed.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new LogoTransportException(
                LogoTransportErrorCodes.PermissionDenied,
                "Logo bridge denied read access.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new LogoTransportException(
                LogoTransportErrorCodes.SourceReadFailed,
                "Logo bridge read request failed.");
        }

        _ = cancellationToken;
    }

    private static LogoTransportHealthResult FailedHealth(
        string errorCode,
        string message,
        string readiness,
        Stopwatch stopwatch) =>
        new(
            IsReachable: false,
            IsContextValid: false,
            AreCredentialsValid: errorCode != LogoTransportErrorCodes.AuthenticationFailed,
            IsCompanyContextValid: false,
            ReadinessState: readiness,
            LatencyMs: (int)stopwatch.ElapsedMilliseconds,
            ErrorCode: errorCode,
            ErrorMessage: message);

    private static bool ValidateCompanyMapping(
        LogoConnectionDescriptor descriptor,
        string? bridgeCompanyId,
        string? bridgeCompanyCode)
    {
        if (string.IsNullOrWhiteSpace(descriptor.ExternalCompanyId)
            && string.IsNullOrWhiteSpace(descriptor.ExternalCompanyCode))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(descriptor.ExternalCompanyId)
            && string.Equals(descriptor.ExternalCompanyId, bridgeCompanyId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(descriptor.ExternalCompanyCode)
            && string.Equals(descriptor.ExternalCompanyCode, bridgeCompanyCode, StringComparison.OrdinalIgnoreCase);
    }

    private static LogoCustomerSourceRecord MapCustomer(LogoBridgeCustomerItem item) =>
        new(
            item.RecordId ?? item.Code ?? string.Empty,
            item.Code,
            item.Name ?? string.Empty,
            item.TaxNumber,
            item.Email,
            item.Phone,
            item.Country,
            item.IsActive ?? true,
            item.SourceVersion,
            item.SourceUpdatedAtUtc);

    private static LogoProductSourceRecord MapProduct(LogoBridgeProductItem item) =>
        new(
            item.RecordId ?? item.Code ?? string.Empty,
            item.Code,
            item.Name ?? string.Empty,
            item.Unit,
            item.Barcode,
            item.IsActive ?? true,
            item.SourceVersion,
            item.SourceUpdatedAtUtc);

    private sealed record LogoBridgeHealthResponse(
        bool ContextValid,
        string? ReadinessState,
        string? CompanyId,
        string? CompanyCode);

    private sealed record LogoBridgeContextResponse(
        string? ProductFamily,
        string? ProductVersion,
        string? CompanyId,
        string? CompanyCode,
        string? CompanyName,
        int? Period,
        string? TransportKind,
        bool ReadOnlyMode,
        IReadOnlyList<string>? SupportedCapabilities);

    private sealed record LogoBridgeCustomerPageResponse(
        IReadOnlyList<LogoBridgeCustomerItem>? Items,
        string? ContinuationToken);

    private sealed record LogoBridgeProductPageResponse(
        IReadOnlyList<LogoBridgeProductItem>? Items,
        string? ContinuationToken);

    private sealed record LogoBridgeCustomerItem(
        string? RecordId,
        string? Code,
        string? Name,
        string? TaxNumber,
        string? Email,
        string? Phone,
        string? Country,
        bool? IsActive,
        string? SourceVersion,
        DateTimeOffset? SourceUpdatedAtUtc);

    private sealed record LogoBridgeProductItem(
        string? RecordId,
        string? Code,
        string? Name,
        string? Unit,
        string? Barcode,
        bool? IsActive,
        string? SourceVersion,
        DateTimeOffset? SourceUpdatedAtUtc);
}
