using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace NevaxisZentra.Persistence.Erp.Netsis;

/// <summary>
/// Read-only HTTP transport for NevaxisZentra Netsis Bridge on the Netsis integration PC.
/// </summary>
public sealed class NetsisBridgeReadTransport(
    HttpClient httpClient,
    INetsisCredentialResolver credentialResolver,
    NetsisReadTransportOptions options) : INetsisReadTransport
{
    public string TransportKind => NetsisTransportKinds.BridgeHttp;

    public async Task<NetsisTransportHealthResult> CheckHealthAsync(
        NetsisConnectionDescriptor descriptor,
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
                    NetsisTransportErrorCodes.AuthenticationFailed,
                    "Netsis bridge authentication failed.",
                    NetsisReadinessStates.CredentialsRequired,
                    stopwatch);
            }

            if (!response.IsSuccessStatusCode)
            {
                return FailedHealth(
                    NetsisTransportErrorCodes.TransportUnreachable,
                    "Netsis bridge is unreachable.",
                    NetsisReadinessStates.TransportUnavailable,
                    stopwatch);
            }

            var payload = await response.Content.ReadFromJsonAsync<NetsisBridgeHealthResponse>(cancellationToken);
            var companyValid = ValidateCompanyMapping(descriptor, payload?.CompanyId, payload?.CompanyCode);

            return new NetsisTransportHealthResult(
                IsReachable: true,
                IsContextValid: payload?.ContextValid ?? false,
                AreCredentialsValid: true,
                IsCompanyContextValid: companyValid,
                ReadinessState: payload?.ReadinessState ?? NetsisReadinessStates.Reachable,
                LatencyMs: (int)stopwatch.ElapsedMilliseconds,
                ErrorCode: companyValid ? null : NetsisTransportErrorCodes.CompanyNotFound,
                ErrorMessage: companyValid ? null : "Configured company does not match bridge context.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            return FailedHealth(
                NetsisTransportErrorCodes.Timeout,
                "Netsis bridge request timed out.",
                NetsisReadinessStates.TransportUnavailable,
                stopwatch);
        }
        catch (HttpRequestException)
        {
            return FailedHealth(
                NetsisTransportErrorCodes.TransportUnreachable,
                "Netsis bridge is unreachable.",
                NetsisReadinessStates.TransportUnavailable,
                stopwatch);
        }
    }

    public async Task<NetsisContextInfo> DiscoverContextAsync(
        NetsisConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, descriptor, "context", null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<NetsisBridgeContextResponse>(cancellationToken)
            ?? throw new NetsisTransportException(
                NetsisTransportErrorCodes.ContextInvalid,
                "Netsis bridge returned invalid context.");

        if (!ValidateCompanyMapping(descriptor, payload.CompanyId, payload.CompanyCode))
        {
            throw new NetsisTransportException(
                NetsisTransportErrorCodes.CompanyNotFound,
                "Configured company does not match bridge context.");
        }

        return new NetsisContextInfo(
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

    public async Task<NetsisReadPageResult<NetsisCustomerSourceRecord>> ReadCustomersAsync(
        NetsisConnectionDescriptor descriptor,
        NetsisReadPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = BuildPageQuery(page);
        using var response = await SendAsync(HttpMethod.Get, descriptor, "customers", query, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<NetsisBridgeCustomerPageResponse>(cancellationToken)
            ?? throw new NetsisTransportException(
                NetsisTransportErrorCodes.SourceReadFailed,
                "Netsis bridge returned invalid customer page.");

        return new NetsisReadPageResult<NetsisCustomerSourceRecord>(
            payload.Items?.Select(MapCustomer).ToList() ?? [],
            payload.ContinuationToken);
    }

    public async Task<NetsisReadPageResult<NetsisProductSourceRecord>> ReadProductsAsync(
        NetsisConnectionDescriptor descriptor,
        NetsisReadPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = BuildPageQuery(page);
        using var response = await SendAsync(HttpMethod.Get, descriptor, "products", query, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<NetsisBridgeProductPageResponse>(cancellationToken)
            ?? throw new NetsisTransportException(
                NetsisTransportErrorCodes.SourceReadFailed,
                "Netsis bridge returned invalid product page.");

        return new NetsisReadPageResult<NetsisProductSourceRecord>(
            payload.Items?.Select(MapProduct).ToList() ?? [],
            payload.ContinuationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        NetsisConnectionDescriptor descriptor,
        string relativePath,
        string? query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(descriptor.EndpointReference))
        {
            throw new NetsisTransportException(
                NetsisTransportErrorCodes.TransportUnconfigured,
                "Netsis bridge endpoint is not configured.");
        }

        var token = credentialResolver.ResolveBearerToken(descriptor.CredentialReference);
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new NetsisTransportException(
                NetsisTransportErrorCodes.AuthenticationFailed,
                "Netsis bridge credential reference could not be resolved.");
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

    private static string BuildPageQuery(NetsisReadPageRequest page)
    {
        var pageSize = Math.Clamp(
            page.PageSize <= 0 ? NetsisReadTransportDefaults.DefaultPageSize : page.PageSize,
            1,
            NetsisReadTransportDefaults.MaxPageSize);
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
            throw new NetsisTransportException(
                NetsisTransportErrorCodes.AuthenticationFailed,
                "Netsis bridge authentication failed.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new NetsisTransportException(
                NetsisTransportErrorCodes.PermissionDenied,
                "Netsis bridge denied read access.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new NetsisTransportException(
                NetsisTransportErrorCodes.SourceReadFailed,
                "Netsis bridge read request failed.");
        }

        _ = cancellationToken;
    }

    private static NetsisTransportHealthResult FailedHealth(
        string errorCode,
        string message,
        string readiness,
        Stopwatch stopwatch) =>
        new(
            IsReachable: false,
            IsContextValid: false,
            AreCredentialsValid: errorCode != NetsisTransportErrorCodes.AuthenticationFailed,
            IsCompanyContextValid: false,
            ReadinessState: readiness,
            LatencyMs: (int)stopwatch.ElapsedMilliseconds,
            ErrorCode: errorCode,
            ErrorMessage: message);

    private static bool ValidateCompanyMapping(
        NetsisConnectionDescriptor descriptor,
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

    private static NetsisCustomerSourceRecord MapCustomer(NetsisBridgeCustomerItem item) =>
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

    private static NetsisProductSourceRecord MapProduct(NetsisBridgeProductItem item) =>
        new(
            item.RecordId ?? item.Code ?? string.Empty,
            item.Code,
            item.Name ?? string.Empty,
            item.Unit,
            item.Barcode,
            item.IsActive ?? true,
            item.SourceVersion,
            item.SourceUpdatedAtUtc);

    private sealed record NetsisBridgeHealthResponse(
        bool ContextValid,
        string? ReadinessState,
        string? CompanyId,
        string? CompanyCode);

    private sealed record NetsisBridgeContextResponse(
        string? ProductFamily,
        string? ProductVersion,
        string? CompanyId,
        string? CompanyCode,
        string? CompanyName,
        int? Period,
        string? TransportKind,
        bool ReadOnlyMode,
        IReadOnlyList<string>? SupportedCapabilities);

    private sealed record NetsisBridgeCustomerPageResponse(
        IReadOnlyList<NetsisBridgeCustomerItem>? Items,
        string? ContinuationToken);

    private sealed record NetsisBridgeProductPageResponse(
        IReadOnlyList<NetsisBridgeProductItem>? Items,
        string? ContinuationToken);

    private sealed record NetsisBridgeCustomerItem(
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

    private sealed record NetsisBridgeProductItem(
        string? RecordId,
        string? Code,
        string? Name,
        string? Unit,
        string? Barcode,
        bool? IsActive,
        string? SourceVersion,
        DateTimeOffset? SourceUpdatedAtUtc);
}
