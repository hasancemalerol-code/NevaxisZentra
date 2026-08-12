using System.Globalization;

namespace CrevaFactOS.Persistence.Material;

public static class MaterialRequirementCalculator
{
    private const int GramDecimalPlaces = 4;
    private static readonly MidpointRounding Rounding = MidpointRounding.AwayFromZero;

    public static MaterialRequirementCalculationResult CalculateRequirement(
        MaterialRequirementProfileDetailReadModel profile)
    {
        var missingInputs = new List<string>();
        var evidence = BuildProfileEvidence(profile);

        if (!profile.GoodPartTargetQuantity.HasValue)
        {
            missingInputs.Add("goodPartTargetQuantity");
        }

        if (!profile.PartWeightGrams.HasValue)
        {
            missingInputs.Add("partWeightGrams");
        }

        if (missingInputs.Count > 0)
        {
            return new MaterialRequirementCalculationResult(
                "insufficient-data",
                null, null, null, null, null, null, null, null,
                missingInputs,
                evidence,
                MaterialRequirementConstants.RuleSetVersion,
                MaterialRequirementConstants.TheoreticalRequirementDisclaimer);
        }

        var targetGoodParts = profile.GoodPartTargetQuantity!.Value;
        var partWeight = profile.PartWeightGrams!.Value;
        var netPartMaterialGrams = RoundGrams(targetGoodParts * partWeight);

        int? requiredShots = null;
        decimal? runnerMaterialGrams = null;

        if (profile.EffectiveCavityCount is > 0 && profile.RunnerWeightGramsPerShot.HasValue)
        {
            requiredShots = (int)Math.Ceiling(targetGoodParts / profile.EffectiveCavityCount.Value);
            runnerMaterialGrams = RoundGrams(requiredShots.Value * profile.RunnerWeightGramsPerShot.Value);
        }
        else if (profile.RunnerWeightGramsPerShot.HasValue && profile.EffectiveCavityCount is null or <= 0)
        {
            missingInputs.Add("effectiveCavityCount");
        }

        var setupAllowance = profile.SetupAllowanceGrams ?? 0m;
        var baseRequirement = netPartMaterialGrams + (runnerMaterialGrams ?? 0m) + setupAllowance;

        decimal? processLossGrams = null;
        if (profile.ProcessLossAllowancePercent.HasValue)
        {
            processLossGrams = RoundGrams(baseRequirement * profile.ProcessLossAllowancePercent.Value / 100m);
        }

        var theoretical = RoundGrams(baseRequirement + (processLossGrams ?? 0m));

        decimal? virginRequirement = null;
        decimal? regrindRequirement = null;

        if (profile.PlannedVirginPercent.HasValue && profile.PlannedRegrindPercent.HasValue)
        {
            virginRequirement = RoundGrams(theoretical * profile.PlannedVirginPercent.Value / 100m);
            regrindRequirement = RoundGrams(theoretical * profile.PlannedRegrindPercent.Value / 100m);
        }

        var status = missingInputs.Count > 0 ? "insufficient-data" : "calculated";

        return new MaterialRequirementCalculationResult(
            status,
            theoretical,
            virginRequirement,
            regrindRequirement,
            netPartMaterialGrams,
            requiredShots,
            runnerMaterialGrams,
            processLossGrams,
            setupAllowance,
            missingInputs,
            evidence,
            MaterialRequirementConstants.RuleSetVersion,
            MaterialRequirementConstants.TheoreticalRequirementDisclaimer);
    }

    public static MaterialRegrindPolicyAssessmentResult AssessRegrindPolicy(
        MaterialRequirementProfileDetailReadModel profile,
        decimal? maximumRegrindPercent,
        DateTimeOffset? specificationRecordedAtUtc,
        string? specificationSource)
    {
        var drivers = new List<MaterialAssessmentDriverReadModel>();
        var evidence = new List<MaterialAssessmentEvidenceReadModel>
        {
            new("plannedRegrindPercent", FormatPercent(profile.PlannedRegrindPercent), profile.SourceReference, profile.DataRecordedAtUtc, !profile.PlannedRegrindPercent.HasValue),
            new("maximumRegrindPercent", FormatPercent(maximumRegrindPercent), specificationSource, specificationRecordedAtUtc, !maximumRegrindPercent.HasValue),
        };

        if (!profile.PlannedRegrindPercent.HasValue)
        {
            return new MaterialRegrindPolicyAssessmentResult(
                "insufficient-data",
                "material.regrind.planned-missing",
                "Planlanan regrind oranı belirtilmemiş",
                null, maximumRegrindPercent, "unknown", drivers, evidence);
        }

        var planned = profile.PlannedRegrindPercent.Value;

        if (!maximumRegrindPercent.HasValue)
        {
            if (planned > 0)
            {
                return new MaterialRegrindPolicyAssessmentResult(
                    "insufficient-data",
                    "material.regrind.limit-unknown",
                    "Teknik regrind limiti bilinmiyor",
                    planned, null, "unknown", drivers, evidence);
            }

            return new MaterialRegrindPolicyAssessmentResult(
                "compliant",
                "material.regrind.zero-planned",
                "Regrind kullanımı planlanmamış",
                planned, null, "pass", drivers, evidence);
        }

        var limit = maximumRegrindPercent.Value;

        if (limit == 0 && planned > 0)
        {
            drivers.Add(new MaterialAssessmentDriverReadModel(
                "regrind-prohibited", "Regrind kullanımı yasak", FormatPercent(planned), "fail"));
            return new MaterialRegrindPolicyAssessmentResult(
                "prohibited",
                "material.regrind.prohibited",
                "Malzeme teknik limiti regrind kullanımına izin vermiyor",
                planned, limit, "fail", drivers, evidence);
        }

        if (planned > limit)
        {
            drivers.Add(new MaterialAssessmentDriverReadModel(
                "regrind-exceeds-limit", "Planlanan regrind limiti aşıyor", FormatPercent(planned), "fail"));
            return new MaterialRegrindPolicyAssessmentResult(
                "exceeds-limit",
                "material.regrind.limit-exceeded",
                "Planlanan regrind oranı teknik limiti aşıyor",
                planned, limit, "fail", drivers, evidence);
        }

        drivers.Add(new MaterialAssessmentDriverReadModel(
            "regrind-compliant", "Regrind politikası uyumlu", FormatPercent(planned), "pass"));
        return new MaterialRegrindPolicyAssessmentResult(
            "compliant",
            "material.regrind.compliant",
            "Planlanan regrind oranı teknik limit dahilinde",
            planned, limit, "pass", drivers, evidence);
    }

    public static MaterialVarianceAssessmentResult AssessVariance(
        MaterialRequirementCalculationResult requirement,
        decimal observedConsumptionGrams,
        decimal tolerancePercent = MaterialRequirementConstants.DefaultVarianceTolerancePercent)
    {
        var drivers = new List<MaterialAssessmentDriverReadModel>();
        var evidence = new List<MaterialAssessmentEvidenceReadModel>();

        if (requirement.Status == "insufficient-data" || !requirement.TheoreticalRequirementGrams.HasValue)
        {
            return new MaterialVarianceAssessmentResult(
                "insufficient-data", null, observedConsumptionGrams, null, null,
                tolerancePercent, drivers, evidence);
        }

        var theoretical = requirement.TheoreticalRequirementGrams.Value;
        var variance = RoundGrams(observedConsumptionGrams - theoretical);
        decimal? variancePercent = null;

        if (theoretical > 0)
        {
            variancePercent = RoundPercent(Math.Abs(variance) / theoretical * 100m);
        }

        string status;
        if (variancePercent.HasValue && variancePercent.Value <= tolerancePercent)
        {
            status = "within-tolerance";
            drivers.Add(new MaterialAssessmentDriverReadModel("variance-within", "Sapma tolerans dahilinde", FormatPercent(variancePercent), "pass"));
        }
        else if (variance > 0)
        {
            status = "above-plan";
            drivers.Add(new MaterialAssessmentDriverReadModel("variance-above", "Gözlenen tüketim plan üstünde", FormatPercent(variancePercent), "warn"));
        }
        else
        {
            status = "below-plan";
            drivers.Add(new MaterialAssessmentDriverReadModel("variance-below", "Gözlenen tüketim plan altında", FormatPercent(variancePercent), "warn"));
        }

        evidence.Add(new MaterialAssessmentEvidenceReadModel("theoreticalRequirementGrams", theoretical.ToString("F4", CultureInfo.InvariantCulture), "requirement-calculation", null, false));
        evidence.Add(new MaterialAssessmentEvidenceReadModel("observedConsumptionGrams", observedConsumptionGrams.ToString("F4", CultureInfo.InvariantCulture), "consumption-observations", null, false));

        return new MaterialVarianceAssessmentResult(
            status, theoretical, observedConsumptionGrams, variance, variancePercent,
            tolerancePercent, drivers, evidence);
    }

    public static decimal ConvertQuantityToGrams(decimal quantity, string unitOfMeasure)
    {
        var normalized = unitOfMeasure.Trim().ToLowerInvariant();
        return normalized switch
        {
            "g" or "gram" or "grams" => quantity,
            "kg" or "kilogram" or "kilograms" => quantity * 1000m,
            _ => quantity * 1000m,
        };
    }

    public static decimal RoundGrams(decimal value) =>
        Math.Round(value, GramDecimalPlaces, Rounding);

    private static decimal RoundPercent(decimal value) =>
        Math.Round(value, 2, Rounding);

    private static string? FormatPercent(decimal? value) =>
        value?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

    private static IReadOnlyList<MaterialAssessmentEvidenceReadModel> BuildProfileEvidence(
        MaterialRequirementProfileDetailReadModel profile) =>
    [
        new("goodPartTargetQuantity", profile.GoodPartTargetQuantity?.ToString("F4", CultureInfo.InvariantCulture), profile.SourceReference, profile.DataRecordedAtUtc, !profile.GoodPartTargetQuantity.HasValue),
        new("partWeightGrams", profile.PartWeightGrams?.ToString("F4", CultureInfo.InvariantCulture), profile.SourceReference, profile.DataRecordedAtUtc, !profile.PartWeightGrams.HasValue),
        new("runnerWeightGramsPerShot", profile.RunnerWeightGramsPerShot?.ToString("F4", CultureInfo.InvariantCulture), profile.SourceReference, profile.DataRecordedAtUtc, !profile.RunnerWeightGramsPerShot.HasValue),
        new("effectiveCavityCount", profile.EffectiveCavityCount?.ToString(CultureInfo.InvariantCulture), profile.SourceReference, profile.DataRecordedAtUtc, !profile.EffectiveCavityCount.HasValue),
        new("setupAllowanceGrams", profile.SetupAllowanceGrams?.ToString("F4", CultureInfo.InvariantCulture), profile.SourceReference, profile.DataRecordedAtUtc, !profile.SetupAllowanceGrams.HasValue),
        new("processLossAllowancePercent", FormatPercent(profile.ProcessLossAllowancePercent), profile.SourceReference, profile.DataRecordedAtUtc, !profile.ProcessLossAllowancePercent.HasValue),
        new("productionOrderReference", profile.ProductionOrderReference, profile.SourceReference, profile.DataRecordedAtUtc, string.IsNullOrWhiteSpace(profile.ProductionOrderReference)),
        new("moldReference", profile.MoldReference, profile.SourceReference, profile.DataRecordedAtUtc, string.IsNullOrWhiteSpace(profile.MoldReference)),
    ];
}
