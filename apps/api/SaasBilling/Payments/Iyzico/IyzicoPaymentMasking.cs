namespace NevaxisZentra.Api.SaasBilling.Payments.Iyzico;

internal static class IyzicoPaymentMasking
{
    public static string MaskToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return "tok_***";
        }

        var trimmed = token.Trim();
        if (trimmed.Length <= 4)
        {
            return "tok_***";
        }

        return $"tok_***{trimmed[^4..]}";
    }

    public static string MaskPaymentReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return "pay_***";
        }

        var trimmed = reference.Trim();
        if (trimmed.Length <= 4)
        {
            return "pay_***";
        }

        return $"pay_***{trimmed[^4..]}";
    }
}
