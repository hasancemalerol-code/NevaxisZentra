using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace CrevaFactOS.Persistence.AiTasks.Delivery;

public sealed record AiTaskSecureAccessTokenPayload(
    int Version,
    Guid TenantId,
    Guid GrantId,
    string Nonce,
    DateTimeOffset ExpiresAtUtc);

public sealed record AiTaskSecureAccessTokenIssueResult(
    string RawToken,
    string TokenHash,
    DateTimeOffset ExpiresAtUtc);

public sealed class AiTaskSecureAccessTokenService(IDataProtectionProvider dataProtectionProvider)
{
    private const string ProtectorPurpose = "CrevaFactOS.AiTask.SecureAccessToken.v1";
    private const int TokenVersion = 1;

    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);

    public AiTaskSecureAccessTokenIssueResult IssueToken(
        Guid tenantId,
        Guid grantId,
        DateTimeOffset expiresAtUtc)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var payload = new AiTaskSecureAccessTokenPayload(TokenVersion, tenantId, grantId, nonce, expiresAtUtc);
        var payloadJson = JsonSerializer.Serialize(payload);
        var protectedPayload = protector.Protect(payloadJson);
        var rawToken = $"{protectedPayload}.{nonce}";
        return new AiTaskSecureAccessTokenIssueResult(rawToken, ComputeHash(rawToken), expiresAtUtc);
    }

    public bool TryResolve(
        string rawToken,
        out AiTaskSecureAccessTokenPayload? payload,
        out string? tokenHash)
    {
        payload = null;
        tokenHash = null;

        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return false;
        }

        var separatorIndex = rawToken.LastIndexOf('.');
        if (separatorIndex <= 0 || separatorIndex >= rawToken.Length - 1)
        {
            return false;
        }

        var protectedPart = rawToken[..separatorIndex];
        var nonce = rawToken[(separatorIndex + 1)..];

        try
        {
            var payloadJson = protector.Unprotect(protectedPart);
            var resolved = JsonSerializer.Deserialize<AiTaskSecureAccessTokenPayload>(payloadJson);
            if (resolved is null || resolved.Version != TokenVersion || resolved.Nonce != nonce)
            {
                return false;
            }

            payload = resolved;
            tokenHash = ComputeHash(rawToken);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public static string ComputeHash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool ConstantTimeEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var result = 0;
        for (var index = 0; index < left.Length; index++)
        {
            result |= left[index] ^ right[index];
        }

        return result == 0;
    }
}
