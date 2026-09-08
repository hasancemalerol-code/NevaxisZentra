using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CrevaFactOS.Desktop.Runtime;

public static class RuntimeIdentityReadinessProbe
{
    private const string SignInProbeBody =
        """{"organizationContext":"dev-pilot-tenant","email":"desktop-probe@invalid.local","password":"__desktop_probe__","deviceLabel":"desktop-probe"}""";

    public static async Task<bool> IsApiIdentityReachableAsync(
        Uri apiBaseUri,
        CancellationToken cancellationToken = default)
    {
        var signInUri = new Uri(apiBaseUri, "/api/identity/sign-in");
        return await IsDevelopmentPilotIdentitySignInHealthyAsync(signInUri, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task<bool> WaitForApiIdentityReachableAsync(
        Uri apiBaseUri,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await IsApiIdentityReachableAsync(apiBaseUri, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    public static async Task<bool> IsWebIdentityProxyReachableAsync(
        Uri webOriginUri,
        CancellationToken cancellationToken = default)
    {
        var signInUri = new Uri(webOriginUri, "/api/identity/sign-in");
        return await IsDevelopmentPilotIdentitySignInHealthyAsync(signInUri, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<bool> IsDevelopmentPilotIdentitySignInHealthyAsync(
        Uri signInUri,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8),
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, signInUri)
        {
            Content = new StringContent(SignInProbeBody, Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return false;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (!contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                if (!document.RootElement.TryGetProperty("title", out var titleElement) ||
                    titleElement.ValueKind != JsonValueKind.String)
                {
                    return false;
                }

                return string.Equals(
                    titleElement.GetString(),
                    "Invalid credentials.",
                    StringComparison.Ordinal);
            }
            catch (JsonException)
            {
                return false;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }
}
