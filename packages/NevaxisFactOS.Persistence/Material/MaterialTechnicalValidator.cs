namespace NevaxisFactOS.Persistence.Material;

public static class MaterialTechnicalValidator
{
    public static string? ValidateTechnicalSpecification(
        decimal? densityGPerCm3,
        decimal? meltFlowRate,
        decimal? meltFlowTestTemperatureC,
        decimal? meltFlowTestLoadKg,
        decimal? moistureLimitPercent,
        bool? dryingRequired,
        decimal? dryingTemperatureMinC,
        decimal? dryingTemperatureMaxC,
        decimal? dryingTimeMinHours,
        decimal? dryingTimeMaxHours,
        decimal? meltTemperatureMinC,
        decimal? meltTemperatureMaxC,
        decimal? moldTemperatureMinC,
        decimal? moldTemperatureMaxC,
        decimal? storageTemperatureMinC,
        decimal? storageTemperatureMaxC,
        decimal? maximumRegrindPercent)
    {
        if (densityGPerCm3 is <= 0)
        {
            return "Density must be positive when supplied.";
        }

        if (meltFlowRate is <= 0)
        {
            return "Melt flow rate must be positive when supplied.";
        }

        if (meltFlowTestTemperatureC is <= 0)
        {
            return "Melt flow test temperature must be positive when supplied.";
        }

        if (meltFlowTestLoadKg is <= 0)
        {
            return "Melt flow test load must be positive when supplied.";
        }

        if (moistureLimitPercent is < 0 or > 100)
        {
            return "Moisture limit must be between 0 and 100 percent.";
        }

        if (maximumRegrindPercent is < 0 or > 100)
        {
            return "Maximum regrind percent must be between 0 and 100.";
        }

        var dryingRangeFailure = ValidateMinMax(
            dryingTemperatureMinC,
            dryingTemperatureMaxC,
            "Drying temperature minimum cannot exceed maximum.");
        if (dryingRangeFailure is not null)
        {
            return dryingRangeFailure;
        }

        var dryingTimeFailure = ValidateMinMax(
            dryingTimeMinHours,
            dryingTimeMaxHours,
            "Drying time minimum cannot exceed maximum.");
        if (dryingTimeFailure is not null)
        {
            return dryingTimeFailure;
        }

        var meltTempFailure = ValidateMinMax(
            meltTemperatureMinC,
            meltTemperatureMaxC,
            "Melt temperature minimum cannot exceed maximum.");
        if (meltTempFailure is not null)
        {
            return meltTempFailure;
        }

        var moldTempFailure = ValidateMinMax(
            moldTemperatureMinC,
            moldTemperatureMaxC,
            "Mold temperature minimum cannot exceed maximum.");
        if (moldTempFailure is not null)
        {
            return moldTempFailure;
        }

        var storageTempFailure = ValidateMinMax(
            storageTemperatureMinC,
            storageTemperatureMaxC,
            "Storage temperature minimum cannot exceed maximum.");
        if (storageTempFailure is not null)
        {
            return storageTempFailure;
        }

        if (dryingRequired == false)
        {
            if (dryingTemperatureMinC.HasValue || dryingTemperatureMaxC.HasValue
                || dryingTimeMinHours.HasValue || dryingTimeMaxHours.HasValue)
            {
                return "Drying parameters must not be supplied when drying is not required.";
            }
        }

        return null;
    }

    public static bool HasTechnicalData(MaterialTechnicalSpecification? spec)
    {
        if (spec is null)
        {
            return false;
        }

        return spec.DensityGPerCm3.HasValue
            || spec.MeltFlowRate.HasValue
            || spec.MoistureLimitPercent.HasValue
            || spec.DryingRequired.HasValue
            || spec.MeltTemperatureMinC.HasValue
            || spec.MeltTemperatureMaxC.HasValue
            || spec.MoldTemperatureMinC.HasValue
            || spec.MoldTemperatureMaxC.HasValue
            || spec.StorageTemperatureMinC.HasValue
            || spec.StorageTemperatureMaxC.HasValue
            || spec.MaximumRegrindPercent.HasValue
            || !string.IsNullOrWhiteSpace(spec.TechnicalNotes)
            || !string.IsNullOrWhiteSpace(spec.SafetyDataSheetReference)
            || !string.IsNullOrWhiteSpace(spec.TechnicalDataSheetReference);
    }

    private static string? ValidateMinMax(decimal? min, decimal? max, string message)
    {
        if (min.HasValue && max.HasValue && min.Value > max.Value)
        {
            return message;
        }

        return null;
    }
}
