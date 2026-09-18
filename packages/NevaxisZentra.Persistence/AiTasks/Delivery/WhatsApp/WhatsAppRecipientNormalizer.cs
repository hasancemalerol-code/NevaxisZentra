using System.Text.RegularExpressions;

namespace NevaxisZentra.Persistence.AiTasks.Delivery.WhatsApp;

public sealed record WhatsAppRecipientNormalizationResult(
    bool Success,
    string? E164,
    string? MaskedDisplay,
    string? ErrorCode);

public static partial class WhatsAppRecipientNormalizer
{
    private static readonly Regex DigitOnly = DigitOnlyRegex();

    public static WhatsAppRecipientNormalizationResult Normalize(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return new WhatsAppRecipientNormalizationResult(false, null, null, "recipient-empty");
        }

        var trimmed = rawInput.Trim();
        if (!trimmed.StartsWith('+'))
        {
            return new WhatsAppRecipientNormalizationResult(false, null, null, "recipient-missing-country-code");
        }

        var digits = DigitOnly.Replace(trimmed, string.Empty);
        if (digits.Length < 8 || digits.Length > 15)
        {
            return new WhatsAppRecipientNormalizationResult(false, null, null, "recipient-invalid-length");
        }

        if (!digits.All(char.IsDigit))
        {
            return new WhatsAppRecipientNormalizationResult(false, null, null, "recipient-invalid-characters");
        }

        var e164 = "+" + digits;
        return new WhatsAppRecipientNormalizationResult(true, e164, MaskE164(e164), null);
    }

    public static string MaskE164(string e164)
    {
        if (string.IsNullOrWhiteSpace(e164) || e164.Length < 6)
        {
            return "***";
        }

        var visibleTail = e164[^2..];
        var prefix = e164[..Math.Min(4, e164.Length - 2)];
        return $"{prefix} *** *** ** {visibleTail}";
    }

    [GeneratedRegex(@"\D", RegexOptions.CultureInvariant)]
    private static partial Regex DigitOnlyRegex();
}
