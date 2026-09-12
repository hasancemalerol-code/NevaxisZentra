using System.Globalization;
using System.Text.Json;

namespace NevaxisFactOS.Persistence.Mold;

internal static class MoldCompatibilityEvaluator
{
    internal sealed record EvaluationInput(
        MoldCompatibilityRequirementReadModel? Requirement,
        MachineCompatibilityCapabilityReadModel? MachineProfile,
        MoldRegistryEntry? MoldRegistry);

    internal sealed record EvaluationOutput(
        string Status,
        IReadOnlyList<MoldCompatibilityRuleResultReadModel> Reasons,
        IReadOnlyDictionary<string, string> Evidence,
        decimal? TheoreticalPartsPerHour,
        IReadOnlyList<string> MissingFields,
        bool RequirementStale,
        bool MachineProfileStale);

    internal static EvaluationOutput Evaluate(EvaluationInput input)
    {
        var reasons = new List<MoldCompatibilityRuleResultReadModel>();
        var missingFields = new List<string>();
        var evidence = new Dictionary<string, string>(StringComparer.Ordinal);

        if (input.Requirement is null)
        {
            missingFields.Add("mold-compatibility-requirement");
            return BuildOutput(
                MoldCompatibilityStatuses.InsufficientData,
                reasons,
                evidence,
                null,
                missingFields,
                false,
                false);
        }

        if (input.MachineProfile is null)
        {
            missingFields.Add("machine-compatibility-profile");
            return BuildOutput(
                MoldCompatibilityStatuses.InsufficientData,
                reasons,
                evidence,
                null,
                missingFields,
                IsStale(input.Requirement.SourceType, input.Requirement.DataRecordedAtUtc),
                false);
        }

        var requirementStale = IsStale(input.Requirement.SourceType, input.Requirement.DataRecordedAtUtc);
        var machineStale = IsStale(input.MachineProfile.SourceType, input.MachineProfile.DataRecordedAtUtc);

        evidence["requirementSourceType"] = input.Requirement.SourceType;
        evidence["requirementSourceReference"] = input.Requirement.SourceReference ?? string.Empty;
        evidence["requirementDataRecordedAtUtc"] = FormatTimestamp(input.Requirement.DataRecordedAtUtc);
        evidence["machineSourceType"] = input.MachineProfile.SourceType;
        evidence["machineSourceReference"] = input.MachineProfile.SourceReference ?? string.Empty;
        evidence["machineDataRecordedAtUtc"] = FormatTimestamp(input.MachineProfile.DataRecordedAtUtc);
        evidence["ruleSetVersion"] = MoldCompatibilityConstants.RuleSetVersion;

        EvaluateClampForce(input.Requirement, input.MachineProfile, reasons);
        EvaluateMoldWidth(input.Requirement, input.MachineProfile, reasons);
        EvaluateMoldHeight(input.Requirement, input.MachineProfile, reasons);
        EvaluateMoldDepth(input.Requirement, input.MachineProfile, reasons);
        EvaluateTieBar(input.Requirement, input.MachineProfile, reasons);
        EvaluateShotCapacity(input.Requirement, input.MachineProfile, reasons);
        EvaluateInjectionUnit(input.Requirement, input.MachineProfile, reasons);
        EvaluateMaterialGroup(input.Requirement, input.MachineProfile, reasons);

        if (requirementStale || machineStale)
        {
            reasons.Add(new MoldCompatibilityRuleResultReadModel(
                "data-stale-warning",
                "Veri güncelliği uyarısı",
                MoldCompatibilityRuleStatuses.Warn,
                null,
                null,
                requirementStale ? input.Requirement.SourceType : input.MachineProfile.SourceType,
                requirementStale ? input.Requirement.DataRecordedAtUtc : input.MachineProfile.DataRecordedAtUtc,
                false));
        }

        CollectMissingFields(input.Requirement, input.MachineProfile, missingFields);

        var theoretical = ComputeTheoreticalCapacity(input.Requirement, input.MoldRegistry);
        if (theoretical.HasValue)
        {
            evidence["theoreticalCapacityLabel"] = "Teorik Kapasite";
            evidence["theoreticalCapacityDisclaimer"] =
                "Gerçek üretim kapasitesi veya OEE değildir.";
            evidence["theoreticalPartsPerHour"] = theoretical.Value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        var status = ResolveOverallStatus(reasons, missingFields);
        return BuildOutput(status, reasons, evidence, theoretical, missingFields, requirementStale, machineStale);
    }

    private static EvaluationOutput BuildOutput(
        string status,
        IReadOnlyList<MoldCompatibilityRuleResultReadModel> reasons,
        IReadOnlyDictionary<string, string> evidence,
        decimal? theoretical,
        IReadOnlyList<string> missingFields,
        bool requirementStale,
        bool machineProfileStale) =>
        new(status, reasons, evidence, theoretical, missingFields, requirementStale, machineProfileStale);

    private static string ResolveOverallStatus(
        List<MoldCompatibilityRuleResultReadModel> reasons,
        List<string> missingFields)
    {
        if (reasons.Any(r => r.Status == MoldCompatibilityRuleStatuses.Fail))
        {
            return MoldCompatibilityStatuses.Incompatible;
        }

        if (missingFields.Count > 0
            || reasons.Any(r => r.Status == MoldCompatibilityRuleStatuses.Unknown && r.IsCritical))
        {
            return MoldCompatibilityStatuses.InsufficientData;
        }

        if (reasons.Any(r => r.Status == MoldCompatibilityRuleStatuses.Warn))
        {
            return MoldCompatibilityStatuses.Conditional;
        }

        if (reasons.Count == 0
            || reasons.All(r => r.Status == MoldCompatibilityRuleStatuses.Pass))
        {
            return MoldCompatibilityStatuses.Compatible;
        }

        return MoldCompatibilityStatuses.InsufficientData;
    }

    private static void EvaluateClampForce(
        MoldCompatibilityRequirementReadModel requirement,
        MachineCompatibilityCapabilityReadModel machine,
        List<MoldCompatibilityRuleResultReadModel> reasons)
    {
        if (requirement.RequiredClampForceTon is null || machine.ClampForceTon is null)
        {
            reasons.Add(UnknownRule(
                "clamp-force",
                "Klemp kuvveti",
                FormatTon(requirement.RequiredClampForceTon),
                FormatTon(machine.ClampForceTon),
                machine.SourceType,
                machine.DataRecordedAtUtc,
                isCritical: true));
            return;
        }

        if (requirement.RequiredClampForceTon.Value > machine.ClampForceTon.Value)
        {
            reasons.Add(new MoldCompatibilityRuleResultReadModel(
                "clamp-force-insufficient",
                "Klemp kuvveti yetersiz",
                MoldCompatibilityRuleStatuses.Fail,
                FormatTon(requirement.RequiredClampForceTon),
                FormatTon(machine.ClampForceTon),
                machine.SourceType,
                machine.DataRecordedAtUtc,
                true));
            return;
        }

        reasons.Add(PassRule(
            "clamp-force",
            "Klemp kuvveti",
            FormatTon(requirement.RequiredClampForceTon),
            FormatTon(machine.ClampForceTon),
            machine.SourceType,
            machine.DataRecordedAtUtc));
    }

    private static void EvaluateMoldWidth(
        MoldCompatibilityRequirementReadModel requirement,
        MachineCompatibilityCapabilityReadModel machine,
        List<MoldCompatibilityRuleResultReadModel> reasons) =>
        EvaluateDimension(
            requirement.MoldWidthMm,
            machine.MaximumMoldWidthMm,
            "mold-width",
            "Kalıp genişliği",
            "mold-width-exceeds",
            "Kalıp genişliği makine limitini aşıyor",
            machine,
            reasons,
            isCritical: true);

    private static void EvaluateMoldHeight(
        MoldCompatibilityRequirementReadModel requirement,
        MachineCompatibilityCapabilityReadModel machine,
        List<MoldCompatibilityRuleResultReadModel> reasons) =>
        EvaluateDimension(
            requirement.MoldHeightMm,
            machine.MaximumMoldHeightMm,
            "mold-height",
            "Kalıp yüksekliği",
            "mold-height-exceeds",
            "Kalıp yüksekliği makine limitini aşıyor",
            machine,
            reasons,
            isCritical: true);

    private static void EvaluateDimension(
        decimal? required,
        decimal? availableMax,
        string passCode,
        string labelTr,
        string failCode,
        string failLabelTr,
        MachineCompatibilityCapabilityReadModel machine,
        List<MoldCompatibilityRuleResultReadModel> reasons,
        bool isCritical)
    {
        if (required is null || availableMax is null)
        {
            reasons.Add(UnknownRule(
                passCode,
                labelTr,
                FormatMm(required),
                FormatMm(availableMax),
                machine.SourceType,
                machine.DataRecordedAtUtc,
                isCritical));
            return;
        }

        if (required.Value > availableMax.Value)
        {
            reasons.Add(new MoldCompatibilityRuleResultReadModel(
                failCode,
                failLabelTr,
                MoldCompatibilityRuleStatuses.Fail,
                FormatMm(required),
                FormatMm(availableMax),
                machine.SourceType,
                machine.DataRecordedAtUtc,
                isCritical));
            return;
        }

        reasons.Add(PassRule(
            passCode,
            labelTr,
            FormatMm(required),
            FormatMm(availableMax),
            machine.SourceType,
            machine.DataRecordedAtUtc));
    }

    private static void EvaluateMoldDepth(
        MoldCompatibilityRequirementReadModel requirement,
        MachineCompatibilityCapabilityReadModel machine,
        List<MoldCompatibilityRuleResultReadModel> reasons)
    {
        if (requirement.MoldDepthMm is null)
        {
            reasons.Add(UnknownRule(
                "mold-depth",
                "Kalıp derinliği",
                null,
                null,
                machine.SourceType,
                machine.DataRecordedAtUtc,
                isCritical: false));
            return;
        }

        if (machine.MinimumMoldDepthMm is not null && requirement.MoldDepthMm.Value < machine.MinimumMoldDepthMm.Value)
        {
            reasons.Add(new MoldCompatibilityRuleResultReadModel(
                "mold-depth-too-shallow",
                "Kalıp derinliği minimum limitin altında",
                MoldCompatibilityRuleStatuses.Fail,
                FormatMm(requirement.MoldDepthMm),
                FormatMm(machine.MinimumMoldDepthMm),
                machine.SourceType,
                machine.DataRecordedAtUtc,
                true));
            return;
        }

        if (machine.MaximumMoldDepthMm is not null && requirement.MoldDepthMm.Value > machine.MaximumMoldDepthMm.Value)
        {
            reasons.Add(new MoldCompatibilityRuleResultReadModel(
                "mold-depth-too-deep",
                "Kalıp derinliği maksimum limiti aşıyor",
                MoldCompatibilityRuleStatuses.Fail,
                FormatMm(requirement.MoldDepthMm),
                FormatMm(machine.MaximumMoldDepthMm),
                machine.SourceType,
                machine.DataRecordedAtUtc,
                true));
            return;
        }

        if (machine.MinimumMoldDepthMm is null && machine.MaximumMoldDepthMm is null)
        {
            reasons.Add(UnknownRule(
                "mold-depth",
                "Kalıp derinliği",
                FormatMm(requirement.MoldDepthMm),
                null,
                machine.SourceType,
                machine.DataRecordedAtUtc,
                isCritical: false));
            return;
        }

        reasons.Add(PassRule(
            "mold-depth",
            "Kalıp derinliği",
            FormatMm(requirement.MoldDepthMm),
            $"{FormatMm(machine.MinimumMoldDepthMm)} – {FormatMm(machine.MaximumMoldDepthMm)}",
            machine.SourceType,
            machine.DataRecordedAtUtc));
    }

    private static void EvaluateTieBar(
        MoldCompatibilityRequirementReadModel requirement,
        MachineCompatibilityCapabilityReadModel machine,
        List<MoldCompatibilityRuleResultReadModel> reasons)
    {
        EvaluateTieBarAxis(
            requirement.RequiredTieBarWidthMm,
            machine.TieBarWidthMm,
            "tie-bar-width",
            "Kundura genişliği",
            machine,
            reasons);
        EvaluateTieBarAxis(
            requirement.RequiredTieBarHeightMm,
            machine.TieBarHeightMm,
            "tie-bar-height",
            "Kundura yüksekliği",
            machine,
            reasons);
    }

    private static void EvaluateTieBarAxis(
        decimal? required,
        decimal? available,
        string code,
        string labelTr,
        MachineCompatibilityCapabilityReadModel machine,
        List<MoldCompatibilityRuleResultReadModel> reasons)
    {
        if (required is null || available is null)
        {
            reasons.Add(UnknownRule(
                code,
                labelTr,
                FormatMm(required),
                FormatMm(available),
                machine.SourceType,
                machine.DataRecordedAtUtc,
                isCritical: false));
            return;
        }

        if (required.Value > available.Value)
        {
            reasons.Add(new MoldCompatibilityRuleResultReadModel(
                $"{code}-insufficient",
                $"{labelTr} yetersiz",
                MoldCompatibilityRuleStatuses.Fail,
                FormatMm(required),
                FormatMm(available),
                machine.SourceType,
                machine.DataRecordedAtUtc,
                true));
            return;
        }

        var marginRatio = (available.Value - required.Value) / available.Value;
        if (marginRatio <= 0.05m)
        {
            reasons.Add(new MoldCompatibilityRuleResultReadModel(
                $"{code}-margin-warning",
                $"{labelTr} tolerans sınırında",
                MoldCompatibilityRuleStatuses.Warn,
                FormatMm(required),
                FormatMm(available),
                machine.SourceType,
                machine.DataRecordedAtUtc,
                false));
            return;
        }

        reasons.Add(PassRule(
            code,
            labelTr,
            FormatMm(required),
            FormatMm(available),
            machine.SourceType,
            machine.DataRecordedAtUtc));
    }

    private static void EvaluateShotCapacity(
        MoldCompatibilityRequirementReadModel requirement,
        MachineCompatibilityCapabilityReadModel machine,
        List<MoldCompatibilityRuleResultReadModel> reasons)
    {
        if (requirement.MinimumShotCapacityGrams is null || machine.MaximumShotCapacityGrams is null)
        {
            reasons.Add(UnknownRule(
                "shot-capacity",
                "Enjeksiyon hacmi",
                FormatGrams(requirement.MinimumShotCapacityGrams),
                FormatGrams(machine.MaximumShotCapacityGrams),
                machine.SourceType,
                machine.DataRecordedAtUtc,
                isCritical: false));
            return;
        }

        if (requirement.MinimumShotCapacityGrams.Value > machine.MaximumShotCapacityGrams.Value)
        {
            reasons.Add(new MoldCompatibilityRuleResultReadModel(
                "shot-capacity-insufficient",
                "Enjeksiyon hacmi yetersiz",
                MoldCompatibilityRuleStatuses.Fail,
                FormatGrams(requirement.MinimumShotCapacityGrams),
                FormatGrams(machine.MaximumShotCapacityGrams),
                machine.SourceType,
                machine.DataRecordedAtUtc,
                true));
            return;
        }

        reasons.Add(PassRule(
            "shot-capacity",
            "Enjeksiyon hacmi",
            FormatGrams(requirement.MinimumShotCapacityGrams),
            FormatGrams(machine.MaximumShotCapacityGrams),
            machine.SourceType,
            machine.DataRecordedAtUtc));
    }

    private static void EvaluateInjectionUnit(
        MoldCompatibilityRequirementReadModel requirement,
        MachineCompatibilityCapabilityReadModel machine,
        List<MoldCompatibilityRuleResultReadModel> reasons)
    {
        if (string.IsNullOrWhiteSpace(requirement.RequiredInjectionUnit)
            || string.IsNullOrWhiteSpace(machine.InjectionUnit))
        {
            reasons.Add(UnknownRule(
                "injection-unit",
                "Enjeksiyon ünitesi",
                requirement.RequiredInjectionUnit,
                machine.InjectionUnit,
                machine.SourceType,
                machine.DataRecordedAtUtc,
                isCritical: false));
            return;
        }

        if (!string.Equals(
                requirement.RequiredInjectionUnit!.Trim(),
                machine.InjectionUnit!.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add(new MoldCompatibilityRuleResultReadModel(
                "injection-unit-mismatch",
                "Enjeksiyon ünitesi uyumsuz",
                MoldCompatibilityRuleStatuses.Fail,
                requirement.RequiredInjectionUnit.Trim(),
                machine.InjectionUnit.Trim(),
                machine.SourceType,
                machine.DataRecordedAtUtc,
                true));
            return;
        }

        reasons.Add(PassRule(
            "injection-unit",
            "Enjeksiyon ünitesi",
            requirement.RequiredInjectionUnit.Trim(),
            machine.InjectionUnit.Trim(),
            machine.SourceType,
            machine.DataRecordedAtUtc));
    }

    private static void EvaluateMaterialGroup(
        MoldCompatibilityRequirementReadModel requirement,
        MachineCompatibilityCapabilityReadModel machine,
        List<MoldCompatibilityRuleResultReadModel> reasons)
    {
        if (string.IsNullOrWhiteSpace(requirement.SupportedMaterialGroup))
        {
            reasons.Add(UnknownRule(
                "material-group",
                "Malzeme grubu",
                null,
                null,
                machine.SourceType,
                machine.DataRecordedAtUtc,
                isCritical: false));
            return;
        }

        if (machine.SupportedMaterialGroups.Count == 0)
        {
            reasons.Add(UnknownRule(
                "material-group",
                "Malzeme grubu",
                requirement.SupportedMaterialGroup.Trim(),
                null,
                machine.SourceType,
                machine.DataRecordedAtUtc,
                isCritical: false));
            return;
        }

        var required = NormalizeToken(requirement.SupportedMaterialGroup);
        var supported = machine.SupportedMaterialGroups
            .Select(NormalizeToken)
            .Where(v => v.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!supported.Contains(required))
        {
            reasons.Add(new MoldCompatibilityRuleResultReadModel(
                "material-group-unsupported",
                "Malzeme grubu desteklenmiyor",
                MoldCompatibilityRuleStatuses.Fail,
                requirement.SupportedMaterialGroup.Trim(),
                string.Join(", ", machine.SupportedMaterialGroups),
                machine.SourceType,
                machine.DataRecordedAtUtc,
                true));
            return;
        }

        reasons.Add(PassRule(
            "material-group",
            "Malzeme grubu",
            requirement.SupportedMaterialGroup.Trim(),
            string.Join(", ", machine.SupportedMaterialGroups),
            machine.SourceType,
            machine.DataRecordedAtUtc));
    }

    internal static decimal? ComputeTheoreticalCapacity(
        MoldCompatibilityRequirementReadModel? requirement,
        MoldRegistryEntry? moldRegistry)
    {
        var cavityCount = requirement?.EffectiveCavityCount ?? moldRegistry?.CavityCount;
        var cycleSeconds = requirement?.NominalCycleTimeSeconds;

        if (cavityCount is null or <= 0 || cycleSeconds is null or <= 0)
        {
            return null;
        }

        return Math.Round(cavityCount.Value * 3600m / cycleSeconds.Value, 3, MidpointRounding.AwayFromZero);
    }

    private static void CollectMissingFields(
        MoldCompatibilityRequirementReadModel requirement,
        MachineCompatibilityCapabilityReadModel machine,
        List<string> missingFields)
    {
        if (requirement.RequiredClampForceTon is null)
        {
            missingFields.Add("requiredClampForceTon");
        }

        if (requirement.MoldWidthMm is null && requirement.MoldHeightMm is null)
        {
            missingFields.Add("moldDimensions");
        }

        if (machine.ClampForceTon is null
            && machine.MaximumMoldWidthMm is null
            && machine.MaximumMoldHeightMm is null)
        {
            missingFields.Add("machineTechnicalProfile");
        }
    }

    internal static bool IsStale(string sourceType, DateTimeOffset? recordedAtUtc)
    {
        if (string.Equals(sourceType, MoldSourceTypes.Manual, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (recordedAtUtc is null)
        {
            return true;
        }

        return recordedAtUtc.Value < DateTimeOffset.UtcNow.AddDays(-MoldCompatibilityConstants.ManualProfileStaleAfterDays);
    }

    internal static string SerializeReasons(IReadOnlyList<MoldCompatibilityRuleResultReadModel> reasons) =>
        JsonSerializer.Serialize(reasons, MoldCompatibilityJson.Options);

    internal static string SerializeEvidence(IReadOnlyDictionary<string, string> evidence) =>
        JsonSerializer.Serialize(evidence, MoldCompatibilityJson.Options);

    internal static IReadOnlyList<MoldCompatibilityRuleResultReadModel> DeserializeReasons(string json) =>
        JsonSerializer.Deserialize<List<MoldCompatibilityRuleResultReadModel>>(json, MoldCompatibilityJson.Options)
        ?? [];

    internal static IReadOnlyDictionary<string, string> DeserializeEvidence(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(json, MoldCompatibilityJson.Options)
        ?? new Dictionary<string, string>(StringComparer.Ordinal);

    private static MoldCompatibilityRuleResultReadModel PassRule(
        string code,
        string labelTr,
        string? required,
        string? available,
        string source,
        DateTimeOffset? timestamp) =>
        new(code, labelTr, MoldCompatibilityRuleStatuses.Pass, required, available, source, timestamp, false);

    private static MoldCompatibilityRuleResultReadModel UnknownRule(
        string code,
        string labelTr,
        string? required,
        string? available,
        string source,
        DateTimeOffset? timestamp,
        bool isCritical) =>
        new(code, labelTr, MoldCompatibilityRuleStatuses.Unknown, required, available, source, timestamp, isCritical);

    private static string FormatTon(decimal? value) =>
        value is null ? "—" : $"{value.Value.ToString("0.###", CultureInfo.InvariantCulture)} ton";

    private static string FormatMm(decimal? value) =>
        value is null ? "—" : $"{value.Value.ToString("0.###", CultureInfo.InvariantCulture)} mm";

    private static string FormatGrams(decimal? value) =>
        value is null ? "—" : $"{value.Value.ToString("0.###", CultureInfo.InvariantCulture)} g";

    private static string FormatTimestamp(DateTimeOffset? value) =>
        value?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string NormalizeToken(string value) => value.Trim().ToLowerInvariant();
}

internal static class MoldCompatibilityJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
