using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CrevaFactOS.Desktop.Runtime;

public static class RuntimeIdentityReadinessProbe
{
    private const string SignInProbeBody =
        """{"organizationContext":"__desktop_probe__","email":"probe@invalid.local","password":"__probe__","deviceLabel":"desktop-probe"}""";

    public static async Task<bool> IsApiIdentityReachableAsync(
        Uri apiBaseUri,
        CancellationToken cancellationToken = default)
    {
        var signInUri = new Uri(apiBaseUri, "/api/identity/sign-in");
        return await IsIdentitySignInEndpointReachableAsync(signInUri, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task<bool> IsWebIdentityProxyReachableAsync(
        Uri webOriginUri,
        CancellationToken cancellationToken = default)
    {
        var signInUri = new Uri(webOriginUri, "/api/identity/sign-in");
        return await IsIdentitySignInEndpointReachableAsync(signInUri, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<bool> IsIdentitySignInEndpointReachableAsync(
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
                return document.RootElement.ValueKind == JsonValueKind.Object;
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
