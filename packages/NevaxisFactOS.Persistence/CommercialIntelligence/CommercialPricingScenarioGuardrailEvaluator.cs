namespace NevaxisFactOS.Persistence.CommercialIntelligence;

public sealed record CommercialPricingScenarioGuardrailEvaluation(
    string GuardrailResult,
    string AssessmentOutcome,
    string EvidenceStatus,
    string CostCoverageStatus,
    string? LeadingDriverCode,
    string? InsufficientDataReason,
    decimal BaselineUnitPrice,
    decimal ProposedUnitPrice,
    decimal TotalIncludedUnitCost,
    decimal ExpectedUnitContribution,
    decimal? ExpectedContributionMarginRatio,
    decimal? BaselineContributionMarginRatio,
    decimal? ContributionMarginDelta,
    decimal ProfileMinimumContributionMarginRatio,
    IReadOnlyList<CommercialProfitabilityDriverDraft> Drivers,
    IReadOnlyList<CommercialProfitabilityCostComponentCandidate> ComponentSnapshots);

public static class CommercialPricingScenarioGuardrailEvaluator
{
    public static CommercialPricingScenarioGuardrailEvaluation Evaluate(
        CommercialProfitabilityProfile profile,
        CommercialProfitabilityEvidenceObservation salesEvidence,
        decimal proposedUnitPrice,
        CommercialProfitabilityCostEvidenceReadResult costEvidence,
        IReadOnlyList<string> requiredKinds,
        DateTimeOffset evaluatedAtUtc)
    {
        var baselinePrice = salesEvidence.ComparableNetSalesUnitPrice ?? 0m;
        var proposedEvidence = CloneSalesEvidenceWithPrice(salesEvidence, proposedUnitPrice);
        var proposedEvaluation = CommercialProfitabilityEvaluator.Evaluate(
            profile,
            proposedEvidence,
            costEvidence,
            requiredKinds,
            evaluatedAtUtc);

        decimal? baselineMargin = null;
        decimal? marginDelta = null;
        if (baselinePrice > 0m)
        {
            var baselineContribution = CommercialProfitabilityEvaluator.ComputeUnitContribution(
                baselinePrice,
                proposedEvaluation.TotalIncludedUnitCost);
            baselineMargin = CommercialProfitabilityEvaluator.ComputeContributionMarginRatio(
                baselineContribution,
                baselinePrice);
            if (baselineMargin.HasValue && proposedEvaluation.ContributionMarginRatio.HasValue)
            {
                marginDelta = proposedEvaluation.ContributionMarginRatio.Value - baselineMargin.Value;
            }
        }

        var guardrailResult = MapOutcomeToGuardrail(proposedEvaluation.Outcome);
        var insufficientReason = guardrailResult == CommercialPricingScenarioGuardrailResults.InsufficientData
            ? ResolveInsufficientDataReason(proposedEvaluation)
            : null;

        return new CommercialPricingScenarioGuardrailEvaluation(
            guardrailResult,
            proposedEvaluation.Outcome,
            proposedEvaluation.EvidenceStatus,
            proposedEvaluation.CostCoverageStatus,
            proposedEvaluation.LeadingDriverCode,
            insufficientReason,
            baselinePrice,
            proposedEvaluation.ComparableNetSalesUnitPrice,
            proposedEvaluation.TotalIncludedUnitCost,
            proposedEvaluation.UnitContribution,
            proposedEvaluation.ContributionMarginRatio,
            baselineMargin,
            marginDelta,
            profile.MinimumContributionMarginRatio,
            proposedEvaluation.Drivers,
            proposedEvaluation.ComponentSnapshots);
    }

    private static CommercialProfitabilityEvidenceObservation CloneSalesEvidenceWithPrice(
        CommercialProfitabilityEvidenceObservation source,
        decimal proposedUnitPrice) =>
        new()
        {
            Id = source.Id,
            TenantId = source.TenantId,
            FacilityId = source.FacilityId,
            ObservationKind = source.ObservationKind,
            EvidenceSetReference = source.EvidenceSetReference,
            CustomerReference = source.CustomerReference,
            MaterialId = source.MaterialId,
            OrderReference = source.OrderReference,
            QuotationReference = source.QuotationReference,
            ComparableNetSalesUnitPrice = proposedUnitPrice,
            CurrencyCode = source.CurrencyCode,
            UnitOfMeasure = source.UnitOfMeasure,
            GrossNetBasis = source.GrossNetBasis,
            TaxBasis = source.TaxBasis,
            DiscountBasis = source.DiscountBasis,
            FreightBasis = source.FreightBasis,
            SourceKind = source.SourceKind,
            EvidenceStatus = source.EvidenceStatus,
            EvidenceTimestampUtc = source.EvidenceTimestampUtc,
            IsCancelledOrReturned = source.IsCancelledOrReturned,
            SourceReference = source.SourceReference,
            SourceProvenance = source.SourceProvenance,
        };

    private static string MapOutcomeToGuardrail(string assessmentOutcome) =>
        assessmentOutcome switch
        {
            CommercialProfitabilityAssessmentOutcomes.WithinThreshold =>
                CommercialPricingScenarioGuardrailResults.Passes,
            CommercialProfitabilityAssessmentOutcomes.BelowThreshold =>
                CommercialPricingScenarioGuardrailResults.NeedsReview,
            CommercialProfitabilityAssessmentOutcomes.NegativeContribution =>
                CommercialPricingScenarioGuardrailResults.Blocked,
            CommercialProfitabilityAssessmentOutcomes.InsufficientData =>
                CommercialPricingScenarioGuardrailResults.InsufficientData,
            CommercialProfitabilityAssessmentOutcomes.SourceConflict =>
                CommercialPricingScenarioGuardrailResults.Blocked,
            _ => CommercialPricingScenarioGuardrailResults.InsufficientData,
        };

    private static string? ResolveInsufficientDataReason(CommercialProfitabilityEvaluationOutput evaluation)
    {
        var leadingDriver = evaluation.Drivers.FirstOrDefault(d =>
            string.Equals(d.ReasonCode, evaluation.LeadingDriverCode, StringComparison.Ordinal));

        if (leadingDriver is not null && !string.IsNullOrWhiteSpace(leadingDriver.SupportingText))
        {
            return leadingDriver.SupportingText;
        }

        return evaluation.CostCoverageStatus switch
        {
            CommercialProfitabilityCostCoverageStatuses.InsufficientCostEvidence =>
                "Yeterli maliyet kanıtı bulunamadı.",
            CommercialProfitabilityCostCoverageStatuses.PartialCostCoverage =>
                "Gerekli maliyet bileşenleri eksik.",
            _ => "Değerlendirme için yeterli kanıt yok.",
        };
    }
}
