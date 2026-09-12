namespace NevaxisFactOS.Persistence.Material;

public static class MaterialRequirementValidator
{
    public static IReadOnlyList<string> ValidateProfile(
        CreateMaterialRequirementProfileCommand command)
    {
        var errors = new List<string>();
        ValidateCommonProfileFields(
            command.GoodPartTargetQuantity,
            command.PartWeightGrams,
            command.RunnerWeightGramsPerShot,
            command.EffectiveCavityCount,
            command.SetupAllowanceGrams,
            command.ProcessLossAllowancePercent,
            command.PlannedVirginPercent,
            command.PlannedRegrindPercent,
            errors);
        return errors;
    }

    public static IReadOnlyList<string> ValidateProfileUpdate(
        UpdateMaterialRequirementProfileCommand command)
    {
        var errors = new List<string>();
        ValidateCommonProfileFields(
            command.GoodPartTargetQuantity,
            command.PartWeightGrams,
            command.RunnerWeightGramsPerShot,
            command.EffectiveCavityCount,
            command.SetupAllowanceGrams,
            command.ProcessLossAllowancePercent,
            command.PlannedVirginPercent,
            command.PlannedRegrindPercent,
            errors);
        return errors;
    }

    public static IReadOnlyList<string> ValidateObservation(
        CreateMaterialConsumptionObservationCommand command)
    {
        var errors = new List<string>();

        if (command.Quantity <= 0)
        {
            errors.Add("Quantity must be positive.");
        }

        if (command.VirginQuantity is < 0 || command.RegrindQuantity is < 0)
        {
            errors.Add("Virgin and regrind quantities must be non-negative.");
        }

        if (command.VirginQuantity.HasValue && command.RegrindQuantity.HasValue
            && command.VirginQuantity.Value + command.RegrindQuantity.Value > command.Quantity)
        {
            errors.Add("Virgin plus regrind quantities cannot exceed total quantity.");
        }

        if (string.IsNullOrWhiteSpace(command.UnitOfMeasure))
        {
            errors.Add("Unit of measure is required.");
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            errors.Add("Idempotency key is required.");
        }

        return errors;
    }

    private static void ValidateCommonProfileFields(
        decimal? goodPartTargetQuantity,
        decimal? partWeightGrams,
        decimal? runnerWeightGramsPerShot,
        int? effectiveCavityCount,
        decimal? setupAllowanceGrams,
        decimal? processLossAllowancePercent,
        decimal? plannedVirginPercent,
        decimal? plannedRegrindPercent,
        List<string> errors)
    {
        if (goodPartTargetQuantity is < 0)
        {
            errors.Add("Good part target quantity must be non-negative.");
        }

        if (partWeightGrams is < 0)
        {
            errors.Add("Part weight must be non-negative.");
        }

        if (runnerWeightGramsPerShot is < 0)
        {
            errors.Add("Runner weight must be non-negative.");
        }

        if (effectiveCavityCount is <= 0)
        {
            errors.Add("Effective cavity count must be positive when supplied.");
        }

        if (setupAllowanceGrams is < 0)
        {
            errors.Add("Setup allowance must be non-negative.");
        }

        if (processLossAllowancePercent is < 0 or > 100)
        {
            errors.Add("Process loss allowance must be between 0 and 100.");
        }

        if (plannedVirginPercent is < 0 or > 100 || plannedRegrindPercent is < 0 or > 100)
        {
            errors.Add("Virgin and regrind percentages must be between 0 and 100.");
        }

        if (plannedVirginPercent.HasValue && plannedRegrindPercent.HasValue
            && plannedVirginPercent.Value + plannedRegrindPercent.Value != 100m)
        {
            errors.Add("Planned virgin and regrind percentages must equal 100 when both are supplied.");
        }
    }
}
