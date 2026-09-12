namespace NevaxisFactOS.Persistence.Mold;

public static class MoldCodeNormalizer
{
    public static bool TryNormalize(string? code, out string displayCode, out string normalizedCode, out string? failureReason)
    {
        displayCode = string.Empty;
        normalizedCode = string.Empty;
        failureReason = null;

        if (string.IsNullOrWhiteSpace(code))
        {
            failureReason = "Mold code is required.";
            return false;
        }

        displayCode = code.Trim();
        normalizedCode = displayCode.ToUpperInvariant();
        return true;
    }
}
