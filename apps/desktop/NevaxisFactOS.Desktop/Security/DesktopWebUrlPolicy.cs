using NevaxisFactOS.Desktop.Configuration;

namespace NevaxisFactOS.Desktop.Security;

public enum WebUrlValidationResult
{
    Valid,
    MissingInProduction,
    UnsafeScheme,
    InvalidUri,
    HttpsRequiredInProduction,
    LocalhostNotAllowedInProduction,
}

public sealed record WebUrlResolution(Uri? Url, WebUrlValidationResult Result, string? RawValue);

public static class DesktopWebUrlPolicy
{
    public const string EnvironmentVariableName = "NEVAXIS_FACTOS_DESKTOP_WEB_URL";
    public const string DevelopmentDefaultUrl = "http://127.0.0.1:3001";

    private static readonly HashSet<string> BlockedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "file",
        "javascript",
        "data",
        "vbscript",
    };

    public static WebUrlResolution Resolve(bool isDevelopment, string? configuredValue = null)
    {
        configuredValue ??= Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!isDevelopment && string.IsNullOrWhiteSpace(configuredValue))
        {
            configuredValue = DesktopBuildConfiguration.EmbeddedProductionWebUrl;
        }

        var effectiveValue = string.IsNullOrWhiteSpace(configuredValue)
            ? isDevelopment ? DevelopmentDefaultUrl : null
            : configuredValue.Trim();

        if (effectiveValue is null)
        {
            return new WebUrlResolution(null, WebUrlValidationResult.MissingInProduction, configuredValue);
        }

        if (!Uri.TryCreate(effectiveValue, UriKind.Absolute, out var uri))
        {
            return new WebUrlResolution(null, WebUrlValidationResult.InvalidUri, effectiveValue);
        }

        if (BlockedSchemes.Contains(uri.Scheme))
        {
            return new WebUrlResolution(null, WebUrlValidationResult.UnsafeScheme, effectiveValue);
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return new WebUrlResolution(null, WebUrlValidationResult.UnsafeScheme, effectiveValue);
        }

        if (!isDevelopment)
        {
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return new WebUrlResolution(null, WebUrlValidationResult.HttpsRequiredInProduction, effectiveValue);
            }

            if (IsLocalhost(uri))
            {
                return new WebUrlResolution(null, WebUrlValidationResult.LocalhostNotAllowedInProduction, effectiveValue);
            }
        }

        return new WebUrlResolution(NormalizeApplicationOrigin(uri), WebUrlValidationResult.Valid, effectiveValue);
    }

    public static Uri GetApplicationOrigin(Uri applicationUrl) =>
        NormalizeApplicationOrigin(applicationUrl);

    private static Uri NormalizeApplicationOrigin(Uri uri) =>
        new($"{uri.Scheme}://{uri.Authority}", UriKind.Absolute);

    private static bool IsLocalhost(Uri uri)
    {
        var host = uri.Host;
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
    }
}
