namespace CrevaFactOS.Persistence.ProcessOee;

public static class ProcessOeeValidator
{
    public static IReadOnlyList<string> ValidateCreateWindow(CreateProcessOeeWindowCommand command)
    {
        var errors = new List<string>();
        if (command.MachineAssetId == Guid.Empty)
        {
            errors.Add("MachineAssetId is required.");
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            errors.Add("IdempotencyKey is required.");
        }

        ValidateWindowBounds(command.WindowStartUtc, command.WindowEndUtc, command.PlannedProductionSeconds, errors);
        ValidatePositiveOptional(command.PlannedProductionSeconds, "PlannedProductionSeconds", errors);
        ValidatePositiveOptionalDecimal(command.IdealCycleTimeSeconds, "IdealCycleTimeSeconds", errors);
        ValidatePositiveOptionalInt(command.EffectiveCavityCount, "EffectiveCavityCount", errors);
        return errors;
    }

    public static IReadOnlyList<string> ValidateUpdateWindow(UpdateProcessOeeWindowCommand command)
    {
        var errors = new List<string>();
        if (command.WindowId == Guid.Empty)
        {
            errors.Add("WindowId is required.");
        }

        ValidateWindowBounds(command.WindowStartUtc, command.WindowEndUtc, command.PlannedProductionSeconds, errors);
        ValidatePositiveOptional(command.PlannedProductionSeconds, "PlannedProductionSeconds", errors);
        ValidatePositiveOptionalDecimal(command.IdealCycleTimeSeconds, "IdealCycleTimeSeconds", errors);
        ValidatePositiveOptionalInt(command.EffectiveCavityCount, "EffectiveCavityCount", errors);
        return errors;
    }

    public static IReadOnlyList<string> ValidateEvaluateBounded(EvaluateProcessOeeBoundedCommand command)
    {
        var errors = new List<string>();
        if (command.MachineAssetId == Guid.Empty)
        {
            errors.Add("MachineAssetId is required.");
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            errors.Add("IdempotencyKey is required.");
        }

        ValidateWindowBounds(command.WindowStartUtc, command.WindowEndUtc, command.PlannedProductionSeconds, errors);
        return errors;
    }

    private static void ValidateWindowBounds(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        int? plannedProductionSeconds,
        List<string> errors)
    {
        if (endUtc <= startUtc)
        {
            errors.Add("WindowEndUtc must be after WindowStartUtc.");
        }

        var durationSeconds = ProcessOeeOperatingTimeCalculator.ComputeWindowDurationSeconds(startUtc, endUtc);
        if (durationSeconds <= 0)
        {
            errors.Add("Evaluation window must have a positive duration.");
        }

        if (durationSeconds > ProcessOeeConstants.MaxWindowDurationSeconds)
        {
            errors.Add($"Evaluation window exceeds maximum duration of {ProcessOeeConstants.MaxWindowDurationSeconds} seconds.");
        }

        if (plannedProductionSeconds.HasValue && plannedProductionSeconds.Value > durationSeconds)
        {
            errors.Add("PlannedProductionSeconds cannot exceed the evaluation window duration.");
        }
    }

    private static void ValidatePositiveOptional(int? value, string field, List<string> errors)
    {
        if (value.HasValue && value.Value <= 0)
        {
            errors.Add($"{field} must be positive when provided.");
        }
    }

    private static void ValidatePositiveOptionalInt(int? value, string field, List<string> errors)
    {
        if (value.HasValue && value.Value <= 0)
        {
            errors.Add($"{field} must be positive when provided.");
        }
    }

    private static void ValidatePositiveOptionalDecimal(decimal? value, string field, List<string> errors)
    {
        if (value.HasValue && value.Value <= 0)
        {
            errors.Add($"{field} must be positive when provided.");
        }
    }
}
