namespace CrevaFactOS.Persistence.ShiftWorkforce;

public static class ShiftWorkforceEmployeeCodeNormalizer
{
    public static bool TryNormalize(string? employeeCode, out string displayCode, out string normalizedCode, out string? failureReason)
    {
        displayCode = string.Empty;
        normalizedCode = string.Empty;
        failureReason = null;

        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            failureReason = "Employee code is required.";
            return false;
        }

        displayCode = employeeCode.Trim();
        normalizedCode = displayCode.ToUpperInvariant();
        return true;
    }
}
