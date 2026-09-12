namespace NevaxisFactOS.Persistence.Mold;

public static class MoldTechnicalValidator
{
    public const long MaxShotCount = 999_999_999_999;

    public static string? ValidateTechnicalProfile(
        int? cavityCount,
        long? nominalShotLife,
        long? currentShotCount,
        decimal? moldWeightKg,
        decimal? dimensionLengthMm,
        decimal? dimensionWidthMm,
        decimal? dimensionHeightMm,
        int? manufactureYear,
        DateOnly? manufactureDate,
        DateOnly? commissioningDate)
    {
        if (cavityCount is <= 0)
        {
            return "Cavity count must be positive when provided.";
        }

        if (nominalShotLife is < 0)
        {
            return "Nominal shot life cannot be negative.";
        }

        if (currentShotCount is < 0)
        {
            return "Current shot count cannot be negative.";
        }

        if (currentShotCount is > MaxShotCount)
        {
            return "Current shot count exceeds supported limits.";
        }

        foreach (var dimension in new[] { moldWeightKg, dimensionLengthMm, dimensionWidthMm, dimensionHeightMm })
        {
            if (dimension is < 0)
            {
                return "Weight and dimensions cannot be negative.";
            }
        }

        var currentYear = DateTime.UtcNow.Year + 1;
        if (manufactureYear.HasValue && (manufactureYear.Value < 1900 || manufactureYear.Value > currentYear))
        {
            return "Manufacture year is not plausible.";
        }

        if (manufactureDate.HasValue && (manufactureDate.Value.Year < 1900 || manufactureDate.Value.Year > currentYear))
        {
            return "Manufacture date is not plausible.";
        }

        if (commissioningDate.HasValue && (commissioningDate.Value.Year < 1900 || commissioningDate.Value.Year > currentYear + 1))
        {
            return "Commissioning date is not plausible.";
        }

        return null;
    }
}
