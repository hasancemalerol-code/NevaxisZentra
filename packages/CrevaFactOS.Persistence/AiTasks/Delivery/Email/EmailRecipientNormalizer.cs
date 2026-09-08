using System.Text.RegularExpressions;

namespace CrevaFactOS.Persistence.AiTasks.Delivery.Email;

public sealed record EmailRecipientNormalizationResult(
    bool Success,
    string? NormalizedAddress,
    string? MaskedDisplay,
    string? ErrorCode);

public static partial class EmailRecipientNormalizer
{
    private const int MaxEmailLength = 320;

    private static readonly Regex EmailPattern = EmailAddressRegex();

    public static EmailRecipientNormalizationResult Normalize(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return new EmailRecipientNormalizationResult(false, null, null, "recipient-empty");
        }

        var trimmed = rawInput.Trim();
        if (trimmed.Length > MaxEmailLength)
        {
            return new EmailRecipientNormalizationResult(false, null, null, "recipient-too-long");
        }

        if (ContainsHeaderInjection(trimmed))
        {
            return new EmailRecipientNormalizationResult(false, null, null, "recipient-header-injection");
        }

        if (!EmailPattern.IsMatch(trimmed))
        {
            return new EmailRecipientNormalizationResult(false, null, null, "recipient-invalid-syntax");
        }

        var atIndex = trimmed.LastIndexOf('@');
        if (atIndex <= 0 || atIndex == trimmed.Length - 1)
        {
            return new EmailRecipientNormalizationResult(false, null, null, "recipient-invalid-syntax");
        }

        var localPart = trimmed[..atIndex];
        var domain = trimmed[(atIndex + 1)..].ToLowerInvariant();
        var normalized = $"{localPart}@{domain}";

        return new EmailRecipientNormalizationResult(true, normalized, MaskEmail(normalized), null);
    }

    public static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "***";
        }

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return "***";
        }

        var local = email[..atIndex];
        var domain = email[(atIndex + 1)..];
        var maskedLocal = local.Length <= 1
            ? "*"
            : $"{local[0]}***{local[^1]}";
        return $"{maskedLocal}@{domain}";
    }

    public static bool ContainsHeaderInjection(string value) =>
        value.Contains('\r') || value.Contains('\n') || value.Contains('\0');

    public static string SanitizeHeaderValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("\0", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex EmailAddressRegex();
}
