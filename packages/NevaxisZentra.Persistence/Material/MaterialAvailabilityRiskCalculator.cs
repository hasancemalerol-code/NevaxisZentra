using System.Globalization;
using NevaxisZentra.Persistence.Erp;

namespace NevaxisZentra.Persistence.Material;

public static class MaterialAvailabilityRiskCalculator
{
    public static MaterialAvailabilityRiskAssessmentResult AssessAvailabilityRisk(
        MaterialRegistryEntry material,
        MaterialRequirementProfileDetailReadModel? profile,
        MaterialRequirementCalculationResult? requirement,
        IReadOnlyList<CanonicalErpInventoryBalance> erpBalances,
        DateTimeOffset now,
        bool hasErpReadAccess)
    {
        var drivers = new List<MaterialAssessmentDriverReadModel>();
        var evidence = new List<MaterialAssessmentEvidenceReadModel>();

        if (profile is null)
        {
            drivers.Add(new MaterialAssessmentDriverReadModel(
                MaterialAvailabilityRiskDriverCodes.RequirementMissing,
                "Malzeme ihtiyaç profili belirtilmedi",
                null,
                "unknown"));
            return BuildResult(
                MaterialAvailabilityRiskStatuses.InsufficientData,
                null, null, null, null, null, null, null, null, null, null, drivers, evidence);
        }

        evidence.Add(new MaterialAssessmentEvidenceReadModel(
            "productionOrderReference",
            profile.ProductionOrderReference,
            profile.SourceReference,
            profile.DataRecordedAtUtc,
            string.IsNullOrWhiteSpace(profile.ProductionOrderReference)));

        if (requirement is null || requirement.Status == "insufficient-data" || !requirement.TheoreticalRequirementGrams.HasValue)
        {
            foreach (var missing in requirement?.MissingInputs ?? ["requirementProfile"])
            {
                drivers.Add(new MaterialAssessmentDriverReadModel(
                    MaterialAvailabilityRiskDriverCodes.RequirementIncomplete,
                    $"Gerekli ihtiyaç girdisi eksik: {missing}",
                    missing,
                    "unknown"));
            }

            return BuildResult(
                MaterialAvailabilityRiskStatuses.InsufficientData,
                null, null, null, null, null, null, null, null, null, null, drivers, evidence);
        }

        var requiredGrams = requirement.TheoreticalRequirementGrams.Value;
        evidence.Add(new MaterialAssessmentEvidenceReadModel(
            "theoreticalRequirementGrams",
            requiredGrams.ToString("F4", CultureInfo.InvariantCulture),
            "material-requirement-calculation",
            profile.DataRecordedAtUtc,
            false));

        if (!hasErpReadAccess)
        {
            drivers.Add(new MaterialAssessmentDriverReadModel(
                MaterialAvailabilityRiskDriverCodes.ErpReadDenied,
                "ERP stok okuma yetkisi yok",
                null,
                "unknown"));
            return BuildResult(
                MaterialAvailabilityRiskStatuses.InsufficientData,
                requiredGrams, "g", null, null, null, null, null, null, null, null, drivers, evidence);
        }

        if (string.IsNullOrWhiteSpace(material.ExternalMaterialCode))
        {
            drivers.Add(new MaterialAssessmentDriverReadModel(
                MaterialAvailabilityRiskDriverCodes.MaterialErpLinkMissing,
                "Malzeme ERP ürün referansı tanımlı değil",
                material.Code,
                "unknown"));
            return BuildResult(
                MaterialAvailabilityRiskStatuses.InsufficientData,
                requiredGrams, "g", null, null, null, null, null, null, null, null, drivers, evidence);
        }

        var linkCode = material.ExternalMaterialCode.Trim();
        var matchingBalances = erpBalances
            .Where(b =>
                string.Equals(b.ExternalProductId, linkCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(b.ExternalProductCode, linkCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matchingBalances.Count == 0)
        {
            drivers.Add(new MaterialAssessmentDriverReadModel(
                MaterialAvailabilityRiskDriverCodes.ErpBalanceMissing,
                "ERP stok bakiyesi bulunamadı",
                linkCode,
                "unknown"));
            evidence.Add(new MaterialAssessmentEvidenceReadModel(
                "erpProductReference", linkCode, "material-master", material.DataRecordedAtUtc, true));
            return BuildResult(
                MaterialAvailabilityRiskStatuses.InsufficientData,
                requiredGrams, "g", null, null, null, null, null, null, null, null, drivers, evidence);
        }

        var units = matchingBalances
            .Select(b => NormalizeUnit(b.Unit))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (units.Count > 1)
        {
            drivers.Add(new MaterialAssessmentDriverReadModel(
                MaterialAvailabilityRiskDriverCodes.UnitConflict,
                "ERP stok kayıtları birim uyumsuzluğu içeriyor",
                string.Join(", ", units),
                "fail"));
            return BuildResult(
                MaterialAvailabilityRiskStatuses.SourceConflict,
                requiredGrams, "g", null, null, null, null, null, null, null, null, drivers, evidence);
        }

        var erpUnit = units[0];
        if (!matchingBalances.All(b => b.QuantityAvailable.HasValue))
        {
            drivers.Add(new MaterialAssessmentDriverReadModel(
                MaterialAvailabilityRiskDriverCodes.ErpQuantityUnknown,
                "ERP kullanılabilir miktar bilinmiyor",
                null,
                "unknown"));
            return BuildResult(
                MaterialAvailabilityRiskStatuses.InsufficientData,
                requiredGrams, "g", null, null, erpUnit, null, null, null, null, null, drivers, evidence);
        }

        var latestSourceUpdated = matchingBalances
            .Select(b => b.SourceUpdatedAtUtc)
            .Where(d => d.HasValue)
            .OrderByDescending(d => d)
            .FirstOrDefault();

        var freshness = ErpSyncRepository.ResolveFreshness(latestSourceUpdated, now);
        evidence.Add(new MaterialAssessmentEvidenceReadModel(
            "erpSourceFreshness", freshness, "erp-inventory-balances", latestSourceUpdated, false));

        if (freshness == ErpSourceFreshness.Stale)
        {
            drivers.Add(new MaterialAssessmentDriverReadModel(
                MaterialAvailabilityRiskDriverCodes.ErpSourceStale,
                "ERP stok verisi bayat",
                freshness,
                "unknown"));
            return BuildResult(
                MaterialAvailabilityRiskStatuses.InsufficientData,
                requiredGrams, "g", null, null, erpUnit, null, null, latestSourceUpdated, freshness, null, drivers, evidence);
        }

        var availableQuantity = matchingBalances.Sum(b => b.QuantityAvailable!.Value);
        decimal? reservedQuantity = matchingBalances.Any(b => b.QuantityReserved.HasValue)
            ? matchingBalances.Where(b => b.QuantityReserved.HasValue).Sum(b => b.QuantityReserved!.Value)
            : null;

        if (!reservedQuantity.HasValue)
        {
            drivers.Add(new MaterialAssessmentDriverReadModel(
                MaterialAvailabilityRiskDriverCodes.ErpQuantityUnknown,
                "ERP rezervasyon miktarı bilinmiyor",
                null,
                "unknown"));
            return BuildResult(
                MaterialAvailabilityRiskStatuses.InsufficientData,
                requiredGrams, "g", availableQuantity, null, erpUnit, null, null, latestSourceUpdated, freshness, null, drivers, evidence);
        }

        var requiredInErpUnit = ConvertGramsToUnit(requiredGrams, erpUnit);
        var availableAfterReservation = availableQuantity - reservedQuantity.Value;
        decimal? shortageQuantity = null;
        string status;

        evidence.Add(new MaterialAssessmentEvidenceReadModel(
            "erpAvailableQuantity",
            availableQuantity.ToString("F4", CultureInfo.InvariantCulture),
            "erp-inventory-balances",
            latestSourceUpdated,
            false));
        evidence.Add(new MaterialAssessmentEvidenceReadModel(
            "erpReservedQuantity",
            reservedQuantity.Value.ToString("F4", CultureInfo.InvariantCulture),
            "erp-inventory-balances",
            latestSourceUpdated,
            false));
        evidence.Add(new MaterialAssessmentEvidenceReadModel(
            "requiredQuantity",
            requiredInErpUnit.ToString("F4", CultureInfo.InvariantCulture),
            "material-requirement-calculation",
            profile.DataRecordedAtUtc,
            false));

        if (availableAfterReservation < requiredInErpUnit)
        {
            status = MaterialAvailabilityRiskStatuses.Shortage;
            shortageQuantity = requiredInErpUnit - availableAfterReservation;
            drivers.Add(new MaterialAssessmentDriverReadModel(
                MaterialAvailabilityRiskDriverCodes.ShortageDetected,
                "Kullanılabilir stok ihtiyacı karşılamıyor",
                shortageQuantity.Value.ToString("F4", CultureInfo.InvariantCulture),
                "fail"));
        }
        else
        {
            status = MaterialAvailabilityRiskStatuses.Sufficient;
            drivers.Add(new MaterialAssessmentDriverReadModel(
                MaterialAvailabilityRiskDriverCodes.SufficientCoverage,
                "Kullanılabilir stok ihtiyacı karşılıyor",
                availableAfterReservation.ToString("F4", CultureInfo.InvariantCulture),
                "pass"));
        }

        return BuildResult(
            status,
            requiredGrams,
            "g",
            availableQuantity,
            reservedQuantity,
            erpUnit,
            availableAfterReservation,
            shortageQuantity,
            latestSourceUpdated,
            freshness,
            null,
            drivers,
            evidence);
    }

    public static decimal ConvertGramsToUnit(decimal grams, string unitOfMeasure)
    {
        var normalized = NormalizeUnit(unitOfMeasure);
        return normalized switch
        {
            "g" or "gram" or "grams" => MaterialRequirementCalculator.RoundGrams(grams),
            _ => MaterialRequirementCalculator.RoundGrams(grams / 1000m),
        };
    }

    private static string NormalizeUnit(string unit) =>
        unit.Trim().ToLowerInvariant();

    private static MaterialAvailabilityRiskAssessmentResult BuildResult(
        string status,
        decimal? requiredGrams,
        string? requiredUnit,
        decimal? erpAvailable,
        decimal? erpReserved,
        string? erpUnit,
        decimal? availableAfterReservation,
        decimal? shortageQuantity,
        DateTimeOffset? erpSourceUpdatedAtUtc,
        string? sourceFreshness,
        decimal? observedLotQuantityGrams,
        IReadOnlyList<MaterialAssessmentDriverReadModel> drivers,
        IReadOnlyList<MaterialAssessmentEvidenceReadModel> evidence) =>
        new(
            status,
            requiredGrams,
            requiredUnit,
            erpAvailable,
            erpReserved,
            erpUnit,
            availableAfterReservation,
            shortageQuantity,
            erpSourceUpdatedAtUtc,
            sourceFreshness,
            observedLotQuantityGrams,
            drivers,
            evidence);
}
