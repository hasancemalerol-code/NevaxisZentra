using System.Net;
using System.Net.Sockets;

namespace NevaxisFactOS.Persistence.Integration.Rest;

public sealed record IntegrationRestEndpointValidationResult(
    bool Allowed,
    string? SafeMessage,
    Uri? NormalizedUri = null);

public static class IntegrationRestSsrfGuard
{
    private static readonly HashSet<string> BlockedHostnames = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "metadata.google.internal",
        "metadata",
    };

    public static IntegrationRestEndpointValidationResult ValidateRequestUri(
        Uri requestUri,
        bool allowLocalhostInDevelopment,
        bool isDevelopmentEnvironment)
    {
        if (!string.Equals(requestUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(requestUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return new IntegrationRestEndpointValidationResult(
                false,
                "Only HTTP and HTTPS schemes are allowlisted.");
        }

        if (string.IsNullOrWhiteSpace(requestUri.Host))
        {
            return new IntegrationRestEndpointValidationResult(false, "Host is required.");
        }

        if (BlockedHostnames.Contains(requestUri.Host))
        {
            if (!(allowLocalhostInDevelopment && isDevelopmentEnvironment))
            {
                return new IntegrationRestEndpointValidationResult(
                    false,
                    "Local or metadata destinations are blocked.");
            }
        }

        if (IPAddress.TryParse(requestUri.Host, out var parsedIp))
        {
            if (!IsPublicIpAllowed(parsedIp, allowLocalhostInDevelopment && isDevelopmentEnvironment))
            {
                return new IntegrationRestEndpointValidationResult(
                    false,
                    "Private, loopback, link-local, or metadata IP ranges are blocked.");
            }
        }
        else
        {
            try
            {
                var addresses = Dns.GetHostAddresses(requestUri.Host);
                if (addresses.Length == 0)
                {
                    return new IntegrationRestEndpointValidationResult(false, "Host could not be resolved.");
                }

                foreach (var address in addresses)
                {
                    if (!IsPublicIpAllowed(address, allowLocalhostInDevelopment && isDevelopmentEnvironment))
                    {
                        return new IntegrationRestEndpointValidationResult(
                            false,
                            "Resolved host maps to a blocked destination.");
                    }
                }
            }
            catch (SocketException)
            {
                return new IntegrationRestEndpointValidationResult(false, "Host resolution failed.");
            }
        }

        return new IntegrationRestEndpointValidationResult(true, null, requestUri);
    }

    private static bool IsPublicIpAllowed(IPAddress address, bool allowLocalhost)
    {
        if (IPAddress.IsLoopback(address))
        {
            return allowLocalhost;
        }

        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork && bytes.Length == 4)
        {
            if (bytes[0] == 10)
            {
                return false;
            }

            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return false;
            }

            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return false;
            }

            if (bytes[0] == 169 && bytes[1] == 254)
            {
                return false;
            }

            if (bytes[0] == 127)
            {
                return allowLocalhost;
            }
        }

        return true;
    }
}
