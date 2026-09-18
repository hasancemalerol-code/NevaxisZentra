using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace NevaxisZentra.Persistence.Erp.Mikro;

/// <summary>
/// Read-only HTTP transport for NevaxisZentra Mikro Bridge on the Mikro integration PC.
/// </summary>
public sealed class MikroBridgeReadTransport(
    HttpClient httpClient,
    IMikroCredentialResolver credentialResolver,
    MikroReadTransportOptions options) : IMikroReadTransport
{
    public string TransportKind => MikroTransportKinds.BridgeHttp;

    public async Task<MikroTransportHealthResult> CheckHealthAsync(
        MikroConnectionDescriptor descriptor,
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
                    MikroTransportErrorCodes.AuthenticationFailed,
                    "Mikro bridge authentication failed.",
                    MikroReadinessStates.CredentialsRequired,
                    stopwatch);
            }

            if (!response.IsSuccessStatusCode)
            {
                return FailedHealth(
                    MikroTransportErrorCodes.TransportUnreachable,
                    "Mikro bridge is unreachable.",
                    MikroReadinessStates.TransportUnavailable,
                    stopwatch);
            }

            var payload = await response.Content.ReadFromJsonAsync<MikroBridgeHealthResponse>(cancellationToken);
            var companyValid = ValidateCompanyMapping(descriptor, payload?.CompanyId, payload?.CompanyCode);

            return new MikroTransportHealthResult(
                IsReachable: true,
                IsContextValid: payload?.ContextValid ?? false,
                AreCredentialsValid: true,
                IsCompanyContextValid: companyValid,
                ReadinessState: payload?.ReadinessState ?? MikroReadinessStates.Reachable,
                LatencyMs: (int)stopwatch.ElapsedMilliseconds,
                ErrorCode: companyValid ? null : MikroTransportErrorCodes.CompanyNotFound,
                ErrorMessage: companyValid ? null : "Configured company does not match bridge context.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            return FailedHealth(
                MikroTransportErrorCodes.Timeout,
                "Mikro bridge request timed out.",
                MikroReadinessStates.TransportUnavailable,
                stopwatch);
        }
        catch (HttpRequestException)
        {
            return FailedHealth(
                MikroTransportErrorCodes.TransportUnreachable,
                "Mikro bridge is unreachable.",
                MikroReadinessStates.TransportUnavailable,
                stopwatch);
        }
    }

    public async Task<MikroContextInfo> DiscoverContextAsync(
        MikroConnectionDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, descriptor, "context", null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<MikroBridgeContextResponse>(cancellationToken)
            ?? throw new MikroTransportException(
                MikroTransportErrorCodes.ContextInvalid,
                "Mikro bridge returned invalid context.");

        if (!ValidateCompanyMapping(descriptor, payload.CompanyId, payload.CompanyCode))
        {
            throw new MikroTransportException(
                MikroTransportErrorCodes.CompanyNotFound,
                "Configured company does not match bridge context.");
        }

        return new MikroContextInfo(
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

    public async Task<MikroReadPageResult<MikroCustomerSourceRecord>> ReadCustomersAsync(
        MikroConnectionDescriptor descriptor,
        MikroReadPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = BuildPageQuery(page);
        using var response = await SendAsync(HttpMethod.Get, descriptor, "customers", query, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<MikroBridgeCustomerPageResponse>(cancellationToken)
            ?? throw new MikroTransportException(
                MikroTransportErrorCodes.SourceReadFailed,
                "Mikro bridge returned invalid customer page.");

        return new MikroReadPageResult<MikroCustomerSourceRecord>(
            payload.Items?.Select(MapCustomer).ToList() ?? [],
            payload.ContinuationToken);
    }

    public async Task<MikroReadPageResult<MikroProductSourceRecord>> ReadProductsAsync(
        MikroConnectionDescriptor descriptor,
        MikroReadPageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = BuildPageQuery(page);
        using var response = await SendAsync(HttpMethod.Get, descriptor, "products", query, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<MikroBridgeProductPageResponse>(cancellationToken)
            ?? throw new MikroTransportException(
                MikroTransportErrorCodes.SourceReadFailed,
                "Mikro bridge returned invalid product page.");

        return new MikroReadPageResult<MikroProductSourceRecord>(
            payload.Items?.Select(MapProduct).ToList() ?? [],
            payload.ContinuationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        MikroConnectionDescriptor descriptor,
        string relativePath,
        string? query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(descriptor.EndpointReference))
        {
            throw new MikroTransportException(
                MikroTransportErrorCodes.TransportUnconfigured,
                "Mikro bridge endpoint is not configured.");
        }

        var token = credentialResolver.ResolveBearerToken(descriptor.CredentialReference);
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new MikroTransportException(
                MikroTransportErrorCodes.AuthenticationFailed,
                "Mikro bridge credential reference could not be resolved.");
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

    private static string BuildPageQuery(MikroReadPageRequest page)
    {
        var pageSize = Math.Clamp(
            page.PageSize <= 0 ? MikroReadTransportDefaults.DefaultPageSize : page.PageSize,
            1,
            MikroReadTransportDefaults.MaxPageSize);
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
            throw new MikroTransportException(
                MikroTransportErrorCodes.AuthenticationFailed,
                "Mikro bridge authentication failed.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new MikroTransportException(
                MikroTransportErrorCodes.PermissionDenied,
                "Mikro bridge denied read access.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new MikroTransportException(
                MikroTransportErrorCodes.SourceReadFailed,
                "Mikro bridge read request failed.");
        }

        _ = cancellationToken;
    }

    private static MikroTransportHealthResult FailedHealth(
        string errorCode,
        string message,
        string readiness,
        Stopwatch stopwatch) =>
        new(
            IsReachable: false,
            IsContextValid: false,
            AreCredentialsValid: errorCode != MikroTransportErrorCodes.AuthenticationFailed,
            IsCompanyContextValid: false,
            ReadinessState: readiness,
            LatencyMs: (int)stopwatch.ElapsedMilliseconds,
            ErrorCode: errorCode,
            ErrorMessage: message);

    private static bool ValidateCompanyMapping(
        MikroConnectionDescriptor descriptor,
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

    private static MikroCustomerSourceRecord MapCustomer(MikroBridgeCustomerItem item) =>
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

    private static MikroProductSourceRecord MapProduct(MikroBridgeProductItem item) =>
        new(
            item.RecordId ?? item.Code ?? string.Empty,
            item.Code,
            item.Name ?? string.Empty,
            item.Unit,
            item.Barcode,
            item.IsActive ?? true,
            item.SourceVersion,
            item.SourceUpdatedAtUtc);

    private sealed record MikroBridgeHealthResponse(
        bool ContextValid,
        string? ReadinessState,
        string? CompanyId,
        string? CompanyCode);

    private sealed record MikroBridgeContextResponse(
        string? ProductFamily,
        string? ProductVersion,
        string? CompanyId,
        string? CompanyCode,
        string? CompanyName,
        int? Period,
        string? TransportKind,
        bool ReadOnlyMode,
        IReadOnlyList<string>? SupportedCapabilities);

    private sealed record MikroBridgeCustomerPageResponse(
        IReadOnlyList<MikroBridgeCustomerItem>? Items,
        string? ContinuationToken);

    private sealed record MikroBridgeProductPageResponse(
        IReadOnlyList<MikroBridgeProductItem>? Items,
        string? ContinuationToken);

    private sealed record MikroBridgeCustomerItem(
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

    private sealed record MikroBridgeProductItem(
        string? RecordId,
        string? Code,
        string? Name,
        string? Unit,
        string? Barcode,
        bool? IsActive,
        string? SourceVersion,
        DateTimeOffset? SourceUpdatedAtUtc);
}
