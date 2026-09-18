using System.Security.Cryptography;
using System.Text;

namespace NevaxisZentra.Persistence.SaasIdentity;

public static class SaasIdentityTokenUtilities
{
    private const int DefaultTokenByteLength = 32;

    public static string GenerateOpaqueToken(int byteLength = DefaultTokenByteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Base64UrlEncode(bytes);
    }

    public static string HashToken(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static string GenerateReference(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        return $"{prefix}_{GenerateOpaqueToken(16)}";
    }

    public static bool FixedTimeEquals(string leftHash, string rightHash) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(leftHash),
            Encoding.UTF8.GetBytes(rightHash));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
