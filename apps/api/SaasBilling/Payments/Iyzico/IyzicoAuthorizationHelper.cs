using System.Security.Cryptography;
using System.Text;

namespace CrevaFactOS.Api.SaasBilling.Payments.Iyzico;

internal static class IyzicoAuthorizationHelper
{
    public static string BuildAuthorizationHeader(string apiKey, string secretKey, string uriPath, string requestBody)
    {
        var randomKey = Guid.NewGuid().ToString("N");
        var payload = randomKey + uriPath + requestBody;
        var signature = ComputeHmacSha256Hex(payload, secretKey);
        var authorizationContent = $"{apiKey}:{signature}:{randomKey}";
        return "IYZWSv2 " + Convert.ToBase64String(Encoding.UTF8.GetBytes(authorizationContent));
    }

    public static string ComputeWebhookSignatureV3(
        string secretKey,
        string iyziEventType,
        string? iyziPaymentId,
        string? token,
        string paymentConversationId,
        string status)
    {
        var key = string.IsNullOrWhiteSpace(token)
            ? secretKey + iyziEventType + (iyziPaymentId ?? string.Empty) + paymentConversationId + status
            : secretKey + iyziEventType + (iyziPaymentId ?? string.Empty) + token + paymentConversationId + status;
        return ComputeHmacSha256Hex(key, secretKey);
    }

    public static bool FixedTimeEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string ComputeHmacSha256Hex(string payload, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(keyBytes, payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
