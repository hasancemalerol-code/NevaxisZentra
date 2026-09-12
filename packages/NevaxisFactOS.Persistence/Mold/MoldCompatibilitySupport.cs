using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NevaxisFactOS.Persistence.Mold;

internal static class MoldCompatibilityFingerprint
{
    internal static string ComputeEvaluationFingerprint(
        MoldCompatibilityRequirementReadModel requirement,
        MachineCompatibilityCapabilityReadModel machineProfile) =>
        ComputeHash(new
        {
            ruleSetVersion = MoldCompatibilityConstants.RuleSetVersion,
            moldId = requirement.MoldId,
            requirementVersion = requirement.Version,
            machineAssetId = machineProfile.MachineAssetId,
            machineProfileVersion = machineProfile.Version,
            requiredClampForceTon = FormatDecimal(requirement.RequiredClampForceTon),
            moldWidthMm = FormatDecimal(requirement.MoldWidthMm),
            moldHeightMm = FormatDecimal(requirement.MoldHeightMm),
            moldDepthMm = FormatDecimal(requirement.MoldDepthMm),
            requiredTieBarWidthMm = FormatDecimal(requirement.RequiredTieBarWidthMm),
            requiredTieBarHeightMm = FormatDecimal(requirement.RequiredTieBarHeightMm),
            minimumShotCapacityGrams = FormatDecimal(requirement.MinimumShotCapacityGrams),
            requiredInjectionUnit = NormalizeOptional(requirement.RequiredInjectionUnit),
            supportedMaterialGroup = NormalizeOptional(requirement.SupportedMaterialGroup),
            nominalCycleTimeSeconds = FormatDecimal(requirement.NominalCycleTimeSeconds),
            effectiveCavityCount = requirement.EffectiveCavityCount,
            machineClampForceTon = FormatDecimal(machineProfile.ClampForceTon),
            machineMaxWidthMm = FormatDecimal(machineProfile.MaximumMoldWidthMm),
            machineMaxHeightMm = FormatDecimal(machineProfile.MaximumMoldHeightMm),
            machineMinDepthMm = FormatDecimal(machineProfile.MinimumMoldDepthMm),
            machineMaxDepthMm = FormatDecimal(machineProfile.MaximumMoldDepthMm),
            machineTieBarWidthMm = FormatDecimal(machineProfile.TieBarWidthMm),
            machineTieBarHeightMm = FormatDecimal(machineProfile.TieBarHeightMm),
            machineMaxShotCapacityGrams = FormatDecimal(machineProfile.MaximumShotCapacityGrams),
            machineInjectionUnit = NormalizeOptional(machineProfile.InjectionUnit),
            machineMaterialGroups = machineProfile.SupportedMaterialGroups
                .Select(NormalizeOptional)
                .Where(v => v is not null)
                .OrderBy(v => v, StringComparer.Ordinal)
                .ToArray(),
        });

    private static string ComputeHash(object payload)
    {
        var json = JsonSerializer.Serialize(payload, MoldCompatibilityJson.Options);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? FormatDecimal(decimal? value) =>
        value?.ToString("0.###", CultureInfo.InvariantCulture);
}

internal static class MoldCompatibilityValidator
{
    internal static string? ValidateRequirementCommand(UpsertMoldCompatibilityRequirementCommand command)
    {
        if (command.MoldId == Guid.Empty)
        {
            return "Mold identifier is required.";
        }

        if (!IsKnownSourceType(command.SourceType))
        {
            return "Source type is invalid.";
        }

        if (command.RequiredClampForceTon is < 0)
        {
            return "Required clamp force cannot be negative.";
        }

        if (command.MoldWidthMm is < 0 || command.MoldHeightMm is < 0 || command.MoldDepthMm is < 0)
        {
            return "Mold dimensions cannot be negative.";
        }

        if (command.RequiredTieBarWidthMm is < 0 || command.RequiredTieBarHeightMm is < 0)
        {
            return "Tie-bar dimensions cannot be negative.";
        }

        if (command.MinimumShotCapacityGrams is < 0)
        {
            return "Minimum shot capacity cannot be negative.";
        }

        if (command.NominalCycleTimeSeconds is <= 0)
        {
            return "Nominal cycle time must be greater than zero when provided.";
        }

        if (command.EffectiveCavityCount is <= 0)
        {
            return "Effective cavity count must be greater than zero when provided.";
        }

        return null;
    }

    internal static string? ValidateMachineProfileCommand(UpsertMachineCompatibilityCapabilityCommand command)
    {
        if (command.MachineAssetId == Guid.Empty)
        {
            return "Machine asset identifier is required.";
        }

        if (string.IsNullOrWhiteSpace(command.MachineCode))
        {
            return "Machine code is required.";
        }

        if (string.IsNullOrWhiteSpace(command.MachineDisplayName))
        {
            return "Machine display name is required.";
        }

        if (!IsKnownSourceType(command.SourceType))
        {
            return "Source type is invalid.";
        }

        if (command.ClampForceTon is < 0
            || command.MaximumMoldWidthMm is < 0
            || command.MaximumMoldHeightMm is < 0
            || command.MinimumMoldDepthMm is < 0
            || command.MaximumMoldDepthMm is < 0
            || command.TieBarWidthMm is < 0
            || command.TieBarHeightMm is < 0
            || command.MaximumShotCapacityGrams is < 0)
        {
            return "Machine capability values cannot be negative.";
        }

        if (command.MinimumMoldDepthMm is not null
            && command.MaximumMoldDepthMm is not null
            && command.MinimumMoldDepthMm.Value > command.MaximumMoldDepthMm.Value)
        {
            return "Minimum mold depth cannot exceed maximum mold depth.";
        }

        return null;
    }

    private static bool IsKnownSourceType(string sourceType) =>
        string.Equals(sourceType, MoldSourceTypes.Manual, StringComparison.OrdinalIgnoreCase)
        || string.Equals(sourceType, MoldSourceTypes.Imported, StringComparison.OrdinalIgnoreCase)
        || string.Equals(sourceType, MoldSourceTypes.Erp, StringComparison.OrdinalIgnoreCase)
        || string.Equals(sourceType, MoldSourceTypes.Unknown, StringComparison.OrdinalIgnoreCase);
}

internal static class MoldCompatibilityMaterialGroups
{
    internal static string Serialize(IReadOnlyList<string>? groups)
    {
        if (groups is null || groups.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            ",",
            groups
                .Select(g => g.Trim())
                .Where(g => g.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    internal static IReadOnlyList<string> Deserialize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
