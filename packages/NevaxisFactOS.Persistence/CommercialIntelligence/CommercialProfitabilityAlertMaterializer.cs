using System.Text.Json;

namespace NevaxisFactOS.Persistence.CommercialIntelligence;

public static class CommercialProfitabilityAlertCategoryResolver
{
    private static readonly HashSet<string> CostMixDeteriorationDrivers = new(StringComparer.Ordinal)
    {
        CommercialProfitabilityComparisonDriverReasonCodes.DirectMaterialCostIncrease,
        CommercialProfitabilityComparisonDriverReasonCodes.DirectLaborCostIncrease,
        CommercialProfitabilityComparisonDriverReasonCodes.MachineCostIncrease,
        CommercialProfitabilityComparisonDriverReasonCodes.EnergyCostIncrease,
        CommercialProfitabilityComparisonDriverReasonCodes.ToolingCostIncrease,
        CommercialProfitabilityComparisonDriverReasonCodes.QualityCostIncrease,
        CommercialProfitabilityComparisonDriverReasonCodes.ScrapReworkCostIncrease,
        CommercialProfitabilityComparisonDriverReasonCodes.PackagingCostIncrease,
        CommercialProfitabilityComparisonDriverReasonCodes.LogisticsCostIncrease,
        CommercialProfitabilityComparisonDriverReasonCodes.AllocatedOverheadCostIncrease,
        CommercialProfitabilityComparisonDriverReasonCodes.OtherCostIncrease,
        CommercialProfitabilityComparisonDriverReasonCodes.CostShareIncrease,
    };

    public static bool TryResolveCategory(
        CommercialProfitabilityComparisonRun run,
        IReadOnlyList<CommercialProfitabilityComparisonResult> results,
        out string category)
    {
        category = string.Empty;

        if (string.Equals(run.ComparisonOutcome, CommercialProfitabilityComparisonOutcomes.InsufficientData, StringComparison.Ordinal)
            || string.Equals(run.ComparisonOutcome, CommercialProfitabilityComparisonOutcomes.SourceConflict, StringComparison.Ordinal))
        {
            category = CommercialProfitabilityAlertCategories.DataReview;
            return true;
        }

        var hasDeteriorationOutcome = string.Equals(
            run.ComparisonOutcome,
            CommercialProfitabilityComparisonOutcomes.ContributionDeterioration,
            StringComparison.Ordinal);

        var hasLowerPosition = results.Any(result =>
            string.Equals(
                result.PositionState,
                CommercialProfitabilityContributionPositionStates.LowerObservedContributionPosition,
                StringComparison.Ordinal));

        var hasCostMixDriverSignal = string.Equals(
            run.ComparisonType,
            CommercialProfitabilityComparisonTypes.CostComponentMixChange,
            StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(run.LeadingDriverCode)
            && CostMixDeteriorationDrivers.Contains(run.LeadingDriverCode);

        if (!hasDeteriorationOutcome && !hasLowerPosition && !hasCostMixDriverSignal)
        {
            return false;
        }

        category = run.ComparisonType switch
        {
            CommercialProfitabilityComparisonTypes.TemporalContributionTrend =>
                CommercialProfitabilityAlertCategories.ProductContributionDeterioration,
            CommercialProfitabilityComparisonTypes.CustomerContributionPosition =>
                CommercialProfitabilityAlertCategories.CustomerContributionDeterioration,
            CommercialProfitabilityComparisonTypes.MaterialContributionPosition =>
                CommercialProfitabilityAlertCategories.MaterialContributionDeterioration,
            CommercialProfitabilityComparisonTypes.CostComponentMixChange =>
                CommercialProfitabilityAlertCategories.CostMixDeterioration,
            _ => string.Empty,
        };

        return !string.IsNullOrWhiteSpace(category);
    }
}

public static class CommercialProfitabilityAlertSeverityMapper
{
    public static string MapSeverity(
        CommercialProfitabilityComparisonRun run,
        CommercialProfitabilityProfile profile,
        IReadOnlyList<CommercialProfitabilityComparisonDriver> drivers)
    {
        if (string.Equals(run.ComparisonOutcome, CommercialProfitabilityComparisonOutcomes.SourceConflict, StringComparison.Ordinal))
        {
            return CommercialProfitabilityAlertSeverities.Critical;
        }

        if (string.Equals(run.ComparisonOutcome, CommercialProfitabilityComparisonOutcomes.InsufficientData, StringComparison.Ordinal))
        {
            return run.PartialCoverageCount > 0 || run.CompleteCoverageCount > 0
                ? CommercialProfitabilityAlertSeverities.Medium
                : CommercialProfitabilityAlertSeverities.Low;
        }

        var adverseThreshold = profile.MaximumAdverseContributionMarginDelta;
        var costShareThreshold = profile.MaximumCostShareIncrease;
        var marginDelta = Math.Abs(run.ContributionMarginDelta ?? 0m);

        if (run.NegativeContributionCount > 0
            || drivers.Any(driver => string.Equals(
                driver.ReasonCode,
                CommercialProfitabilityComparisonDriverReasonCodes.NegativeContributionConcentration,
                StringComparison.Ordinal)))
        {
            return CommercialProfitabilityAlertSeverities.Critical;
        }

        if (marginDelta >= adverseThreshold * 2m)
        {
            return CommercialProfitabilityAlertSeverities.Critical;
        }

        if (marginDelta > adverseThreshold)
        {
            return CommercialProfitabilityAlertSeverities.High;
        }

        var costShareDriver = drivers.FirstOrDefault(driver =>
            string.Equals(driver.ReasonCode, CommercialProfitabilityComparisonDriverReasonCodes.CostShareIncrease, StringComparison.Ordinal)
            && driver.NormalizedVariance.HasValue
            && Math.Abs(driver.NormalizedVariance.Value) > costShareThreshold);

        if (costShareDriver is not null)
        {
            return CommercialProfitabilityAlertSeverities.High;
        }

        if (string.Equals(run.ComparisonOutcome, CommercialProfitabilityComparisonOutcomes.ContributionDeterioration, StringComparison.Ordinal))
        {
            return marginDelta >= adverseThreshold
                ? CommercialProfitabilityAlertSeverities.High
                : CommercialProfitabilityAlertSeverities.Medium;
        }

        return CommercialProfitabilityAlertSeverities.Medium;
    }
}

public static class CommercialProfitabilityAlertPresentationBuilder
{
    public static string BuildTitle(string category, CommercialProfitabilityComparisonRun run) =>
        category switch
        {
            CommercialProfitabilityAlertCategories.ProductContributionDeterioration =>
                "Ürün katkı marjı kötüleşmesi tespit edildi",
            CommercialProfitabilityAlertCategories.CustomerContributionDeterioration =>
                "Müşteri katkı pozisyonu kötüleşmesi tespit edildi",
            CommercialProfitabilityAlertCategories.MaterialContributionDeterioration =>
                "Malzeme katkı pozisyonu kötüleşmesi tespit edildi",
            CommercialProfitabilityAlertCategories.CostMixDeterioration =>
                "Maliyet bileşeni/mix kötüleşmesi tespit edildi",
            CommercialProfitabilityAlertCategories.DataReview =>
                "Kârlılık karşılaştırması için veri incelemesi gerekli",
            _ => "Kârlılık uyarısı",
        };

    public static string BuildEvidenceSummary(
        CommercialProfitabilityComparisonRun run,
        IReadOnlyList<CommercialProfitabilityComparisonDriver> drivers)
    {
        var leadingDriver = drivers
            .OrderBy(driver => driver.SortOrder)
            .FirstOrDefault()?.ReasonCode ?? run.LeadingDriverCode;

        var marginPart = run.ContributionMarginDelta.HasValue
            ? $"Katkı marjı değişimi: {run.ContributionMarginDelta.Value:0.####}. "
            : string.Empty;

        var driverPart = leadingDriver is not null
            ? $"Önde gelen sürücü: {leadingDriver}. "
            : string.Empty;

        return $"{marginPart}{driverPart}Karşılaştırma sonucu: {run.ComparisonOutcome}. Kanıt durumu: {run.EvidenceStatus}.";
    }

    public static string BuildSourceSnapshotJson(
        CommercialProfitabilityComparisonRun run,
        IReadOnlyList<CommercialProfitabilityComparisonDriver> drivers)
    {
        var payload = new
        {
            comparisonReference = run.ComparisonReference,
            comparisonType = run.ComparisonType,
            comparisonOutcome = run.ComparisonOutcome,
            evidenceStatus = run.EvidenceStatus,
            leadingDriverCode = run.LeadingDriverCode,
            contributionMarginDelta = run.ContributionMarginDelta,
            referenceMedianContributionMarginRatio = run.ReferenceMedianContributionMarginRatio,
            currentMedianContributionMarginRatio = run.CurrentMedianContributionMarginRatio,
            drivers = drivers
                .OrderBy(driver => driver.SortOrder)
                .Select(driver => new
                {
                    driver.ReasonCode,
                    driver.Severity,
                    driver.AffectedAssessmentCount,
                    driver.NormalizedVariance,
                })
                .ToList(),
        };

        return JsonSerializer.Serialize(payload);
    }

    public static string BuildMaterializationIdempotencyKey(Guid comparisonRunId, string alertCategory) =>
        $"profitability-alert:{comparisonRunId:D}:{alertCategory}";

    public static string BuildTaskDraftIdempotencyKey(Guid alertId) =>
        $"profitability-alert-task:{alertId:D}:{CommercialProfitabilityAlertConstants.TaskPurposeHumanReviewDraft}";
}
