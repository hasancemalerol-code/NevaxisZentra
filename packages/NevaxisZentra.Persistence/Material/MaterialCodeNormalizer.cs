namespace NevaxisZentra.Persistence.Material;

public static class MaterialCodeNormalizer
{
    public static bool TryNormalize(string? code, out string displayCode, out string normalizedCode, out string? failureReason)
    {
        displayCode = string.Empty;
        normalizedCode = string.Empty;
        failureReason = null;

        if (string.IsNullOrWhiteSpace(code))
        {
            failureReason = "Material code is required.";
            return false;
        }

        displayCode = code.Trim();
        normalizedCode = displayCode.ToUpperInvariant();
        return true;
    }
}
