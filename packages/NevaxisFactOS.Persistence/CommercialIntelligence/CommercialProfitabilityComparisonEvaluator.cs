namespace NevaxisFactOS.Persistence.CommercialIntelligence;

public sealed record CommercialProfitabilityComparisonDriverDraft(
    string ReasonCode,
    string Severity,
    decimal? ActualValue,
    decimal? ReferenceValue,
    decimal? ThresholdValue,
    string? Unit,
    decimal? NormalizedVariance,
    int AffectedAssessmentCount,
    string SupportingText,
    Guid? ComparisonResultId = null);

public sealed record CommercialProfitabilityComparisonResultDraft(
    string ResultDimension,
    string CanonicalResultReference,
    string? CustomerReference,
    Guid? MaterialId,
    string? ComponentKind,
    int CandidateAssessmentCount,
    int ComparableAssessmentCount,
    int ExcludedAssessmentCount,
    int CompleteCoverageCount,
    int NegativeContributionCount,
    int BelowThresholdCount,
    int? ReferenceComparableAssessmentCount,
    int? CurrentComparableAssessmentCount,
    int? ReferenceExcludedAssessmentCount,
    int? CurrentExcludedAssessmentCount,
    decimal? ReferenceMedianContributionMarginRatio,
    decimal? CurrentMedianContributionMarginRatio,
    decimal? ContributionMarginDelta,
    decimal? ReferenceMedianUnitContribution,
    decimal? CurrentMedianUnitContribution,
    decimal? ReferenceMedianUnitCost,
    decimal? CurrentMedianUnitCost,
    decimal? SignedUnitCostChange,
    decimal? SignedUnitCostChangeRatio,
    decimal? ReferenceMedianCostShare,
    decimal? CurrentMedianCostShare,
    decimal? CostShareDelta,
    string PositionState,
    int? ContributionRank,
    bool IsTied,
    string EvidenceStatus);

public sealed record CommercialProfitabilityComparisonEvaluationOutput(
    string ComparisonOutcome,
    string EvidenceStatus,
    string AggregationMethod,
    string? LeadingDriverCode,
    int CandidateAssessmentCount,
    int ComparableAssessmentCount,
    int ExcludedAssessmentCount,
    int CompleteCoverageCount,
    int PartialCoverageCount,
    int InsufficientDataCount,
    int SourceConflictCount,
    int NegativeContributionCount,
    int BelowThresholdCount,
    int? ReferenceCandidateAssessmentCount,
    int? ReferenceComparableAssessmentCount,
    int? ReferenceExcludedAssessmentCount,
    int? ReferenceCompleteCoverageCount,
    int? ReferencePartialCoverageCount,
    int? ReferenceInsufficientDataCount,
    int? ReferenceSourceConflictCount,
    int? ReferenceNegativeContributionCount,
    int? ReferenceBelowThresholdCount,
    int? CurrentCandidateAssessmentCount,
    int? CurrentComparableAssessmentCount,
    int? CurrentExcludedAssessmentCount,
    int? CurrentCompleteCoverageCount,
    int? CurrentPartialCoverageCount,
    int? CurrentInsufficientDataCount,
    int? CurrentSourceConflictCount,
    int? CurrentNegativeContributionCount,
    int? CurrentBelowThresholdCount,
    decimal? ReferenceMedianContributionMarginRatio,
    decimal? CurrentMedianContributionMarginRatio,
    decimal? ContributionMarginDelta,
    decimal? ReferenceMedianUnitContribution,
    decimal? CurrentMedianUnitContribution,
    IReadOnlyList<CommercialProfitabilityComparisonResultDraft> Results,
    IReadOnlyList<CommercialProfitabilityComparisonDriverDraft> Drivers);

public static class CommercialProfitabilityComparisonEvaluator
{
    private static readonly Dictionary<string, int> SeverityOrder = new(StringComparer.Ordinal)
    {
        ["critical"] = 0,
        ["high"] = 1,
        ["moderate"] = 2,
        ["info"] = 3,
    };

    private static readonly Dictionary<string, int> ReasonOrder = new(StringComparer.Ordinal)
    {
        [CommercialProfitabilityComparisonDriverReasonCodes.SourceConflict] = 0,
        [CommercialProfitabilityComparisonDriverReasonCodes.IncompatiblePriceBasis] = 1,
        [CommercialProfitabilityComparisonDriverReasonCodes.IncompatibleCurrency] = 2,
        [CommercialProfitabilityComparisonDriverReasonCodes.IncompatibleUnit] = 3,
        [CommercialProfitabilityComparisonDriverReasonCodes.InsufficientReferenceWindow] = 4,
        [CommercialProfitabilityComparisonDriverReasonCodes.InsufficientCurrentWindow] = 5,
        [CommercialProfitabilityComparisonDriverReasonCodes.InsufficientScopeEvidence] = 6,
        [CommercialProfitabilityComparisonDriverReasonCodes.IncompleteCostCoverage] = 7,
        [CommercialProfitabilityComparisonDriverReasonCodes.ContributionMarginDeterioration] = 8,
        [CommercialProfitabilityComparisonDriverReasonCodes.ContributionMarginImprovement] = 9,
        [CommercialProfitabilityComparisonDriverReasonCodes.NegativeContributionConcentration] = 10,
        [CommercialProfitabilityComparisonDriverReasonCodes.BelowThresholdConcentration] = 11,
        [CommercialProfitabilityComparisonDriverReasonCodes.DirectMaterialCostIncrease] = 12,
        [CommercialProfitabilityComparisonDriverReasonCodes.DirectLaborCostIncrease] = 13,
        [CommercialProfitabilityComparisonDriverReasonCodes.MachineCostIncrease] = 14,
        [CommercialProfitabilityComparisonDriverReasonCodes.EnergyCostIncrease] = 15,
        [CommercialProfitabilityComparisonDriverReasonCodes.ToolingCostIncrease] = 16,
        [CommercialProfitabilityComparisonDriverReasonCodes.QualityCostIncrease] = 17,
        [CommercialProfitabilityComparisonDriverReasonCodes.ScrapReworkCostIncrease] = 18,
        [CommercialProfitabilityComparisonDriverReasonCodes.PackagingCostIncrease] = 19,
        [CommercialProfitabilityComparisonDriverReasonCodes.LogisticsCostIncrease] = 20,
        [CommercialProfitabilityComparisonDriverReasonCodes.AllocatedOverheadCostIncrease] = 21,
        [CommercialProfitabilityComparisonDriverReasonCodes.OtherCostIncrease] = 22,
        [CommercialProfitabilityComparisonDriverReasonCodes.CostShareIncrease] = 23,
        [CommercialProfitabilityComparisonDriverReasonCodes.CustomerContributionConcentration] = 24,
        [CommercialProfitabilityComparisonDriverReasonCodes.MaterialContributionConcentration] = 25,
        [CommercialProfitabilityComparisonDriverReasonCodes.ContributionPositionTie] = 26,
        [CommercialProfitabilityComparisonDriverReasonCodes.WithinComparisonBand] = 27,
    };

    public static CommercialProfitabilityComparisonEvaluationOutput EvaluateTemporalContributionTrend(
        CommercialProfitabilityProfile profile,
        CommercialProfitabilityAssessmentWindowSlice referenceSlice,
        CommercialProfitabilityAssessmentWindowSlice currentSlice,
        IReadOnlyList<CommercialProfitabilityAssessment> referenceAssessments,
        IReadOnlyList<CommercialProfitabilityAssessment> currentAssessments) =>
        EvaluateTemporal(profile, referenceSlice, currentSlice, referenceAssessments, currentAssessments, includeCostComponents: false);

    public static CommercialProfitabilityComparisonEvaluationOutput EvaluateCostComponentMixChange(
        CommercialProfitabilityProfile profile,
        CommercialProfitabilityAssessmentWindowSlice referenceSlice,
        CommercialProfitabilityAssessmentWindowSlice currentSlice,
        IReadOnlyList<CommercialProfitabilityAssessment> referenceAssessments,
        IReadOnlyList<CommercialProfitabilityAssessment> currentAssessments) =>
        EvaluateCostMix(profile, referenceSlice, currentSlice, referenceAssessments, currentAssessments);

    public static CommercialProfitabilityComparisonEvaluationOutput EvaluateCustomerContributionPosition(
        CommercialProfitabilityProfile profile,
        IReadOnlyList<CommercialProfitabilityAssessment> assessments,
        IReadOnlyList<string> customerReferences) =>
        EvaluatePosition(profile, assessments, customerReferences, isCustomer: true);

    public static CommercialProfitabilityComparisonEvaluationOutput EvaluateMaterialContributionPosition(
        CommercialProfitabilityProfile profile,
        IReadOnlyList<CommercialProfitabilityAssessment> assessments,
        IReadOnlyList<Guid> materialIds) =>
        EvaluateMaterialPosition(profile, assessments, materialIds);

    public static IReadOnlyList<CommercialProfitabilityComparisonDriverDraft> OrderDrivers(
        IReadOnlyList<CommercialProfitabilityComparisonDriverDraft> drivers) =>
        drivers
            .OrderBy(d => SeverityOrder.GetValueOrDefault(d.Severity, 99))
            .ThenByDescending(d => Math.Abs(d.NormalizedVariance ?? 0m))
            .ThenByDescending(d => d.AffectedAssessmentCount)
            .ThenBy(d => ReasonOrder.GetValueOrDefault(d.ReasonCode, 99))
            .ToList();

    public static decimal? ComputeUnweightedMedian(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var ordered = values.OrderBy(v => v).ToList();
        var mid = ordered.Count / 2;
        return ordered.Count % 2 == 1
            ? ordered[mid]
            : (ordered[mid - 1] + ordered[mid]) / 2m;
    }

    public static decimal? ComputeSignedUnitCostChangeRatio(decimal? signedChange, decimal? referenceMedianUnitCost)
    {
        if (!signedChange.HasValue || !referenceMedianUnitCost.HasValue || referenceMedianUnitCost.Value <= 0m)
        {
            return null;
        }

        return signedChange.Value / referenceMedianUnitCost.Value;
    }

    public static decimal? ComputeComponentCostShare(decimal includedUnitCost, decimal totalIncludedUnitCost) =>
        totalIncludedUnitCost <= 0m ? null : includedUnitCost / totalIncludedUnitCost;

    private static CommercialProfitabilityComparisonEvaluationOutput EvaluateTemporal(
        CommercialProfitabilityProfile profile,
        CommercialProfitabilityAssessmentWindowSlice referenceSlice,
        CommercialProfitabilityAssessmentWindowSlice currentSlice,
        IReadOnlyList<CommercialProfitabilityAssessment> referenceAssessments,
        IReadOnlyList<CommercialProfitabilityAssessment> currentAssessments,
        bool includeCostComponents)
    {
        var drivers = new List<CommercialProfitabilityComparisonDriverDraft>();
        var minimum = profile.MinimumComparableAssessmentCountPerWindow;
        var adverseBand = profile.MaximumAdverseContributionMarginDelta;
        var favorableBand = profile.MinimumFavorableContributionMarginDelta;

        if (referenceSlice.HasSourceConflict || currentSlice.HasSourceConflict)
        {
            drivers.Add(Draft(CommercialProfitabilityComparisonDriverReasonCodes.SourceConflict, "critical", null, null, null, "assessment", null,
                referenceSlice.SourceConflictCount + currentSlice.SourceConflictCount,
                "Comparable contribution assessments could not be normalized without source conflict."));
            return BuildTemporalOutput(CommercialProfitabilityComparisonOutcomes.SourceConflict,
                CommercialProfitabilityEvidenceStatuses.SourceConflict, drivers, referenceSlice, currentSlice, null, null, null, null, null, []);
        }

        if (referenceSlice.ComparableAssessmentCount < minimum)
        {
            drivers.Add(Draft(CommercialProfitabilityComparisonDriverReasonCodes.InsufficientReferenceWindow, "info",
                referenceSlice.ComparableAssessmentCount, minimum, null, "assessment", null, referenceSlice.ComparableAssessmentCount,
                $"Reference window comparable assessment count {referenceSlice.ComparableAssessmentCount} is below profile minimum {minimum}."));
        }

        if (currentSlice.ComparableAssessmentCount < minimum)
        {
            drivers.Add(Draft(CommercialProfitabilityComparisonDriverReasonCodes.InsufficientCurrentWindow, "info",
                currentSlice.ComparableAssessmentCount, minimum, null, "assessment", null, currentSlice.ComparableAssessmentCount,
                $"Current window comparable assessment count {currentSlice.ComparableAssessmentCount} is below profile minimum {minimum}."));
        }

        if (referenceSlice.ComparableAssessmentCount < minimum || currentSlice.ComparableAssessmentCount < minimum)
        {
            return BuildTemporalOutput(CommercialProfitabilityComparisonOutcomes.InsufficientData,
                CommercialProfitabilityEvidenceStatuses.InsufficientData, drivers, referenceSlice, currentSlice, null, null, null, null, null, []);
        }

        var referenceMedian = ComputeMarginMedian(referenceAssessments);
        var currentMedian = ComputeMarginMedian(currentAssessments);
        var delta = referenceMedian.HasValue && currentMedian.HasValue ? currentMedian - referenceMedian : null;
        decimal? referenceUnitContribution = null;
        decimal? currentUnitContribution = null;
        if (CommercialProfitabilityAssessmentReader.AreUnitContributionsCompatible(
                CommercialProfitabilityAssessmentReader.FilterMedianComparable(referenceAssessments)))
        {
            referenceUnitContribution = ComputeUnitContributionMedian(referenceAssessments);
        }

        if (CommercialProfitabilityAssessmentReader.AreUnitContributionsCompatible(
                CommercialProfitabilityAssessmentReader.FilterMedianComparable(currentAssessments)))
        {
            currentUnitContribution = ComputeUnitContributionMedian(currentAssessments);
        }

        AppendConcentrationDrivers(drivers, currentSlice);
        string outcome;
        if (delta.HasValue && delta.Value < -adverseBand)
        {
            outcome = CommercialProfitabilityComparisonOutcomes.ContributionDeterioration;
            drivers.Add(Draft(CommercialProfitabilityComparisonDriverReasonCodes.ContributionMarginDeterioration, "high",
                currentMedian, referenceMedian, adverseBand, "contribution-margin-ratio", delta,
                currentSlice.ComparableAssessmentCount,
                $"Current median contribution margin {currentMedian:0.######} is below reference {referenceMedian:0.######} by more than adverse band {adverseBand:0.######}."));
        }
        else if (delta.HasValue && delta.Value > favorableBand)
        {
            outcome = CommercialProfitabilityComparisonOutcomes.ContributionImprovement;
            drivers.Add(Draft(CommercialProfitabilityComparisonDriverReasonCodes.ContributionMarginImprovement, "moderate",
                currentMedian, referenceMedian, favorableBand, "contribution-margin-ratio", delta,
                currentSlice.ComparableAssessmentCount,
                $"Current median contribution margin {currentMedian:0.######} exceeds reference {referenceMedian:0.######} by more than favorable band {favorableBand:0.######}."));
        }
        else
        {
            outcome = CommercialProfitabilityComparisonOutcomes.WithinComparisonBand;
            drivers.Add(Draft(CommercialProfitabilityComparisonDriverReasonCodes.WithinComparisonBand, "info",
                currentMedian, referenceMedian, adverseBand, "contribution-margin-ratio", delta ?? 0m,
                currentSlice.ComparableAssessmentCount,
                $"Contribution margin delta {delta:0.######} remains within comparison band."));
        }

        var results = new List<CommercialProfitabilityComparisonResultDraft>
        {
            new(
                CommercialProfitabilityComparisonResultDimensions.Summary,
                "summary",
                null,
                null,
                null,
                referenceSlice.CandidateAssessmentCount + currentSlice.CandidateAssessmentCount,
                referenceSlice.ComparableAssessmentCount + currentSlice.ComparableAssessmentCount,
                referenceSlice.ExcludedAssessmentCount + currentSlice.ExcludedAssessmentCount,
                referenceSlice.CompleteCoverageCount + currentSlice.CompleteCoverageCount,
                referenceSlice.NegativeContributionCount + currentSlice.NegativeContributionCount,
                referenceSlice.BelowThresholdCount + currentSlice.BelowThresholdCount,
                referenceSlice.ComparableAssessmentCount,
                currentSlice.ComparableAssessmentCount,
                referenceSlice.ExcludedAssessmentCount,
                currentSlice.ExcludedAssessmentCount,
                referenceMedian,
                currentMedian,
                delta,
                referenceUnitContribution,
                currentUnitContribution,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                CommercialProfitabilityContributionPositionStates.WithinObservedContributionBand,
                1,
                false,
                CommercialProfitabilityEvidenceStatuses.Simulated),
        };

        return BuildTemporalOutput(outcome, CommercialProfitabilityEvidenceStatuses.Simulated, drivers, referenceSlice, currentSlice,
            referenceMedian, currentMedian, delta, referenceUnitContribution, currentUnitContribution, results);
    }

    private static CommercialProfitabilityComparisonEvaluationOutput EvaluateCostMix(
        CommercialProfitabilityProfile profile,
        CommercialProfitabilityAssessmentWindowSlice referenceSlice,
        CommercialProfitabilityAssessmentWindowSlice currentSlice,
        IReadOnlyList<CommercialProfitabilityAssessment> referenceAssessments,
        IReadOnlyList<CommercialProfitabilityAssessment> currentAssessments)
    {
        var temporal = EvaluateTemporal(profile, referenceSlice, currentSlice, referenceAssessments, currentAssessments, includeCostComponents: true);
        if (temporal.ComparisonOutcome is CommercialProfitabilityComparisonOutcomes.SourceConflict
            or CommercialProfitabilityComparisonOutcomes.InsufficientData)
        {
            return temporal;
        }

        var drivers = temporal.Drivers.ToList();
        var componentKinds = CommercialProfitabilityCostComponentKinds.All
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();
        var results = new List<CommercialProfitabilityComparisonResultDraft>();
        var sort = 1;
        foreach (var kind in componentKinds)
        {
            var referenceCosts = ExtractIncludedUnitCosts(referenceAssessments, kind);
            var currentCosts = ExtractIncludedUnitCosts(currentAssessments, kind);
            var referenceShares = ExtractCostShares(referenceAssessments, kind);
            var currentShares = ExtractCostShares(currentAssessments, kind);
            if (referenceCosts.Count == 0 && currentCosts.Count == 0)
            {
                continue;
            }

            var referenceMedianCost = ComputeUnweightedMedian(referenceCosts);
            var currentMedianCost = ComputeUnweightedMedian(currentCosts);
            var signedChange = referenceMedianCost.HasValue && currentMedianCost.HasValue
                ? currentMedianCost - referenceMedianCost
                : null;
            var signedRatio = ComputeSignedUnitCostChangeRatio(signedChange, referenceMedianCost);
            var referenceShare = ComputeUnweightedMedian(referenceShares);
            var currentShare = ComputeUnweightedMedian(currentShares);
            var shareDelta = referenceShare.HasValue && currentShare.HasValue ? currentShare - referenceShare : null;
            results.Add(new CommercialProfitabilityComparisonResultDraft(
                CommercialProfitabilityComparisonResultDimensions.CostComponent,
                kind,
                null,
                null,
                kind,
                referenceCosts.Count + currentCosts.Count,
                referenceCosts.Count + currentCosts.Count,
                0,
                referenceCosts.Count + currentCosts.Count,
                0,
                0,
                referenceCosts.Count,
                currentCosts.Count,
                0,
                0,
                null,
                null,
                null,
                null,
                null,
                referenceMedianCost,
                currentMedianCost,
                signedChange,
                signedRatio,
                referenceShare,
                currentShare,
                shareDelta,
                CommercialProfitabilityContributionPositionStates.WithinObservedContributionBand,
                sort,
                false,
                CommercialProfitabilityEvidenceStatuses.Simulated));

            if (signedRatio.HasValue && signedRatio.Value > 0m)
            {
                drivers.Add(Draft(MapCostIncreaseDriver(kind), "moderate", currentMedianCost, referenceMedianCost, null,
                    "unit-cost", signedRatio, currentCosts.Count,
                    $"Observed unit-cost increase for {kind}."));
            }

            if (shareDelta.HasValue && shareDelta.Value > profile.MaximumCostShareIncrease)
            {
                drivers.Add(Draft(CommercialProfitabilityComparisonDriverReasonCodes.CostShareIncrease, "moderate",
                    currentShare, referenceShare, profile.MaximumCostShareIncrease, "cost-share", shareDelta,
                    currentShares.Count,
                    $"Observed cost-share increase for {kind} exceeds profile maximum {profile.MaximumCostShareIncrease:0.######}."));
            }

            sort++;
        }

        var ordered = OrderDrivers(drivers);
        return temporal with
        {
            LeadingDriverCode = ordered.Count > 0 ? ordered[0].ReasonCode : temporal.LeadingDriverCode,
            Results = results,
            Drivers = ordered,
        };
    }

    private static CommercialProfitabilityComparisonEvaluationOutput EvaluatePosition(
        CommercialProfitabilityProfile profile,
        IReadOnlyList<CommercialProfitabilityAssessment> assessments,
        IReadOnlyList<string> customerReferences,
        bool isCustomer)
    {
        var drivers = new List<CommercialProfitabilityComparisonDriverDraft>();
        var minimum = profile.MinimumComparableAssessmentCountPerWindow;
        var results = new List<CommercialProfitabilityComparisonResultDraft>();

        foreach (var customerReference in customerReferences.OrderBy(c => c, StringComparer.Ordinal))
        {
            var scoped = assessments.Where(a => string.Equals(a.CustomerReference, customerReference, StringComparison.Ordinal)).ToList();
            var slice = CommercialProfitabilityAssessmentReader.SummarizeWindow(scoped);
            if (slice.HasSourceConflict)
            {
                results.Add(BuildPositionResult(customerReference, null, slice, null, null,
                    CommercialProfitabilityContributionPositionStates.SourceConflict,
                    CommercialProfitabilityEvidenceStatuses.SourceConflict));
                continue;
            }

            if (slice.ComparableAssessmentCount < minimum)
            {
                results.Add(BuildPositionResult(customerReference, null, slice, null, null,
                    CommercialProfitabilityContributionPositionStates.InsufficientData,
                    CommercialProfitabilityEvidenceStatuses.InsufficientData));
                continue;
            }

            var medianMargin = ComputeMarginMedian(scoped);
            decimal? medianUnitContribution = CommercialProfitabilityAssessmentReader.AreUnitContributionsCompatible(
                CommercialProfitabilityAssessmentReader.FilterMedianComparable(scoped))
                ? ComputeUnitContributionMedian(scoped)
                : null;
            results.Add(BuildPositionResult(customerReference, null, slice, medianMargin, medianUnitContribution,
                CommercialProfitabilityContributionPositionStates.WithinObservedContributionBand,
                CommercialProfitabilityEvidenceStatuses.Simulated));
        }

        return FinalizePositionOutput(profile, drivers, results, isCustomer);
    }

    private static CommercialProfitabilityComparisonEvaluationOutput EvaluateMaterialPosition(
        CommercialProfitabilityProfile profile,
        IReadOnlyList<CommercialProfitabilityAssessment> assessments,
        IReadOnlyList<Guid> materialIds)
    {
        var drivers = new List<CommercialProfitabilityComparisonDriverDraft>();
        var minimum = profile.MinimumComparableAssessmentCountPerWindow;
        var results = new List<CommercialProfitabilityComparisonResultDraft>();

        foreach (var materialId in materialIds.OrderBy(id => id))
        {
            var scoped = assessments.Where(a => a.MaterialId == materialId).ToList();
            var slice = CommercialProfitabilityAssessmentReader.SummarizeWindow(scoped);
            if (slice.HasSourceConflict)
            {
                results.Add(BuildPositionResult(null, materialId, slice, null, null,
                    CommercialProfitabilityContributionPositionStates.SourceConflict,
                    CommercialProfitabilityEvidenceStatuses.SourceConflict));
                continue;
            }

            if (slice.ComparableAssessmentCount < minimum)
            {
                results.Add(BuildPositionResult(null, materialId, slice, null, null,
                    CommercialProfitabilityContributionPositionStates.InsufficientData,
                    CommercialProfitabilityEvidenceStatuses.InsufficientData));
                continue;
            }

            var medianMargin = ComputeMarginMedian(scoped);
            decimal? medianUnitContribution = CommercialProfitabilityAssessmentReader.AreUnitContributionsCompatible(
                CommercialProfitabilityAssessmentReader.FilterMedianComparable(scoped))
                ? ComputeUnitContributionMedian(scoped)
                : null;
            results.Add(BuildPositionResult(null, materialId, slice, medianMargin, medianUnitContribution,
                CommercialProfitabilityContributionPositionStates.WithinObservedContributionBand,
                CommercialProfitabilityEvidenceStatuses.Simulated));
        }

        return FinalizePositionOutput(profile, drivers, results, isCustomer: false);
    }

    private static CommercialProfitabilityComparisonEvaluationOutput FinalizePositionOutput(
        CommercialProfitabilityProfile profile,
        List<CommercialProfitabilityComparisonDriverDraft> drivers,
        List<CommercialProfitabilityComparisonResultDraft> results,
        bool isCustomer)
    {
        if (results.Any(r => r.PositionState == CommercialProfitabilityContributionPositionStates.SourceConflict))
        {
            drivers.Add(Draft(CommercialProfitabilityComparisonDriverReasonCodes.SourceConflict, "critical", null, null, null, "assessment", null,
                results.Sum(r => r.ExcludedAssessmentCount),
                "Contribution position comparison encountered source-conflicting assessments."));
            return BuildPositionRunOutput(CommercialProfitabilityComparisonOutcomes.SourceConflict,
                CommercialProfitabilityEvidenceStatuses.SourceConflict, drivers, results);
        }

        var eligible = results
            .Where(r => r.CurrentMedianContributionMarginRatio.HasValue)
            .OrderByDescending(r => r.CurrentMedianContributionMarginRatio!.Value)
            .ThenByDescending(r => r.CurrentMedianUnitContribution ?? decimal.MinValue)
            .ThenByDescending(r => r.ComparableAssessmentCount)
            .ThenBy(r => r.CanonicalResultReference, StringComparer.Ordinal)
            .ToList();

        if (eligible.Count == 0)
        {
            drivers.Add(Draft(CommercialProfitabilityComparisonDriverReasonCodes.InsufficientScopeEvidence, "info",
                results.Sum(r => r.ComparableAssessmentCount), profile.MinimumComparableAssessmentCountPerWindow, null, "assessment", null,
                results.Sum(r => r.CandidateAssessmentCount),
                "No scope had sufficient comparable contribution assessments."));
            return BuildPositionRunOutput(CommercialProfitabilityComparisonOutcomes.InsufficientData,
                CommercialProfitabilityEvidenceStatuses.InsufficientData, drivers, results);
        }

        var highest = eligible[0].CurrentMedianContributionMarginRatio!.Value;
        var lowest = eligible[^1].CurrentMedianContributionMarginRatio!.Value;
        for (var index = 0; index < eligible.Count; index++)
        {
            var item = eligible[index];
            var rank = index + 1;
            var ties = eligible.Count(e =>
                e.CurrentMedianContributionMarginRatio == item.CurrentMedianContributionMarginRatio
                && (e.CurrentMedianUnitContribution ?? decimal.MinValue) == (item.CurrentMedianUnitContribution ?? decimal.MinValue)
                && e.ComparableAssessmentCount == item.ComparableAssessmentCount);
            var isTied = ties > 1;
            string position;
            if (isTied)
            {
                position = CommercialProfitabilityContributionPositionStates.TiedContributionPosition;
            }
            else if (item.CurrentMedianContributionMarginRatio == highest && item.CurrentMedianContributionMarginRatio == lowest)
            {
                position = CommercialProfitabilityContributionPositionStates.WithinObservedContributionBand;
            }
            else if (item.CurrentMedianContributionMarginRatio == highest)
            {
                position = CommercialProfitabilityContributionPositionStates.HigherObservedContributionPosition;
            }
            else if (item.CurrentMedianContributionMarginRatio == lowest)
            {
                position = CommercialProfitabilityContributionPositionStates.LowerObservedContributionPosition;
            }
            else
            {
                position = CommercialProfitabilityContributionPositionStates.WithinObservedContributionBand;
            }

            var resultIndex = results.FindIndex(r => r.CanonicalResultReference == item.CanonicalResultReference);
            results[resultIndex] = item with { PositionState = position, ContributionRank = rank, IsTied = isTied };
        }

        if (results.Any(r => r.IsTied))
        {
            drivers.Add(Draft(CommercialProfitabilityComparisonDriverReasonCodes.ContributionPositionTie, "high",
                highest, lowest, null, "contribution-margin-ratio", highest - lowest, results.Count(r => r.IsTied),
                "One or more scope references share identical observable contribution-position values."));
        }
        else if (highest > lowest)
        {
            var code = isCustomer
                ? CommercialProfitabilityComparisonDriverReasonCodes.CustomerContributionConcentration
                : CommercialProfitabilityComparisonDriverReasonCodes.MaterialContributionConcentration;
            drivers.Add(Draft(code, "moderate", highest, lowest, null, "contribution-margin-ratio", highest - lowest,
                eligible.Count(r => r.PositionState == CommercialProfitabilityContributionPositionStates.HigherObservedContributionPosition),
                "Observable contribution-position spread across canonical scope references."));
        }
        else
        {
            drivers.Add(Draft(CommercialProfitabilityComparisonDriverReasonCodes.WithinComparisonBand, "info",
                highest, lowest, profile.MaximumAdverseContributionMarginDelta, "contribution-margin-ratio", 0m,
                eligible.Count, "Contribution positions remain within the observed spread."));
        }

        return BuildPositionRunOutput(CommercialProfitabilityComparisonOutcomes.WithinComparisonBand,
            CommercialProfitabilityEvidenceStatuses.Simulated, drivers, results);
    }

    private static CommercialProfitabilityComparisonResultDraft BuildPositionResult(
        string? customerReference,
        Guid? materialId,
        CommercialProfitabilityAssessmentWindowSlice slice,
        decimal? medianMargin,
        decimal? medianUnitContribution,
        string positionState,
        string evidenceStatus) =>
        new(
            customerReference is not null
                ? CommercialProfitabilityComparisonResultDimensions.Customer
                : CommercialProfitabilityComparisonResultDimensions.Material,
            customerReference ?? materialId!.Value.ToString(),
            customerReference,
            materialId,
            null,
            slice.CandidateAssessmentCount,
            slice.ComparableAssessmentCount,
            slice.ExcludedAssessmentCount,
            slice.CompleteCoverageCount,
            slice.NegativeContributionCount,
            slice.BelowThresholdCount,
            null,
            slice.ComparableAssessmentCount,
            null,
            slice.ExcludedAssessmentCount,
            null,
            medianMargin,
            null,
            null,
            medianUnitContribution,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            positionState,
            null,
            false,
            evidenceStatus);

    private static decimal? ComputeMarginMedian(IReadOnlyList<CommercialProfitabilityAssessment> assessments)
    {
        var values = CommercialProfitabilityAssessmentReader.FilterMedianComparable(assessments)
            .Select(a => a.ContributionMarginRatio!.Value)
            .ToList();
        return ComputeUnweightedMedian(values);
    }

    private static decimal? ComputeUnitContributionMedian(IReadOnlyList<CommercialProfitabilityAssessment> assessments)
    {
        var values = CommercialProfitabilityAssessmentReader.FilterMedianComparable(assessments)
            .Select(a => a.UnitContribution)
            .ToList();
        return ComputeUnweightedMedian(values);
    }

    private static List<decimal> ExtractIncludedUnitCosts(IReadOnlyList<CommercialProfitabilityAssessment> assessments, string kind) =>
        assessments
            .SelectMany(a => a.CostComponents)
            .Where(c => string.Equals(c.InclusionStatus, CommercialProfitabilityComponentInclusionStatuses.Included, StringComparison.Ordinal)
                        && string.Equals(c.ComponentKind, kind, StringComparison.Ordinal))
            .Select(c => c.UnitCost)
            .ToList();

    private static List<decimal> ExtractCostShares(IReadOnlyList<CommercialProfitabilityAssessment> assessments, string kind)
    {
        var shares = new List<decimal>();
        foreach (var assessment in assessments)
        {
            var included = assessment.CostComponents
                .Where(c => string.Equals(c.InclusionStatus, CommercialProfitabilityComponentInclusionStatuses.Included, StringComparison.Ordinal))
                .ToList();
            if (included.Count == 0)
            {
                continue;
            }

            var total = included.Sum(c => c.UnitCost);
            var component = included.SingleOrDefault(c => string.Equals(c.ComponentKind, kind, StringComparison.Ordinal));
            if (component is null)
            {
                continue;
            }

            var share = ComputeComponentCostShare(component.UnitCost, total);
            if (share.HasValue)
            {
                shares.Add(share.Value);
            }
        }

        return shares;
    }

    private static string MapCostIncreaseDriver(string kind) => kind switch
    {
        CommercialProfitabilityCostComponentKinds.DirectMaterial => CommercialProfitabilityComparisonDriverReasonCodes.DirectMaterialCostIncrease,
        CommercialProfitabilityCostComponentKinds.DirectLabor => CommercialProfitabilityComparisonDriverReasonCodes.DirectLaborCostIncrease,
        CommercialProfitabilityCostComponentKinds.Machine => CommercialProfitabilityComparisonDriverReasonCodes.MachineCostIncrease,
        CommercialProfitabilityCostComponentKinds.Energy => CommercialProfitabilityComparisonDriverReasonCodes.EnergyCostIncrease,
        CommercialProfitabilityCostComponentKinds.Tooling => CommercialProfitabilityComparisonDriverReasonCodes.ToolingCostIncrease,
        CommercialProfitabilityCostComponentKinds.Quality => CommercialProfitabilityComparisonDriverReasonCodes.QualityCostIncrease,
        CommercialProfitabilityCostComponentKinds.ScrapRework => CommercialProfitabilityComparisonDriverReasonCodes.ScrapReworkCostIncrease,
        CommercialProfitabilityCostComponentKinds.Packaging => CommercialProfitabilityComparisonDriverReasonCodes.PackagingCostIncrease,
        CommercialProfitabilityCostComponentKinds.Logistics => CommercialProfitabilityComparisonDriverReasonCodes.LogisticsCostIncrease,
        CommercialProfitabilityCostComponentKinds.AllocatedOverhead => CommercialProfitabilityComparisonDriverReasonCodes.AllocatedOverheadCostIncrease,
        _ => CommercialProfitabilityComparisonDriverReasonCodes.OtherCostIncrease,
    };

    private static void AppendConcentrationDrivers(
        List<CommercialProfitabilityComparisonDriverDraft> drivers,
        CommercialProfitabilityAssessmentWindowSlice slice)
    {
        if (slice.NegativeContributionCount > 0)
        {
            drivers.Add(Draft(CommercialProfitabilityComparisonDriverReasonCodes.NegativeContributionConcentration, "high",
                slice.NegativeContributionCount, slice.ComparableAssessmentCount, null, "assessment",
                slice.ComparableAssessmentCount == 0 ? null : (decimal)slice.NegativeContributionCount / slice.ComparableAssessmentCount,
                slice.NegativeContributionCount,
                $"Current window contains {slice.NegativeContributionCount} negative-contribution assessments."));
        }

        if (slice.BelowThresholdCount > 0)
        {
            drivers.Add(Draft(CommercialProfitabilityComparisonDriverReasonCodes.BelowThresholdConcentration, "moderate",
                slice.BelowThresholdCount, slice.ComparableAssessmentCount, null, "assessment",
                slice.ComparableAssessmentCount == 0 ? null : (decimal)slice.BelowThresholdCount / slice.ComparableAssessmentCount,
                slice.BelowThresholdCount,
                $"Current window contains {slice.BelowThresholdCount} below-threshold assessments."));
        }

        if (slice.PartialCoverageCount > 0)
        {
            drivers.Add(Draft(CommercialProfitabilityComparisonDriverReasonCodes.IncompleteCostCoverage, "moderate",
                slice.PartialCoverageCount, slice.ComparableAssessmentCount, null, "assessment", null,
                slice.PartialCoverageCount,
                $"Current window contains {slice.PartialCoverageCount} partial-cost-coverage assessments."));
        }
    }

    private static CommercialProfitabilityComparisonEvaluationOutput BuildTemporalOutput(
        string outcome,
        string evidenceStatus,
        List<CommercialProfitabilityComparisonDriverDraft> drivers,
        CommercialProfitabilityAssessmentWindowSlice referenceSlice,
        CommercialProfitabilityAssessmentWindowSlice currentSlice,
        decimal? referenceMedian,
        decimal? currentMedian,
        decimal? delta,
        decimal? referenceUnitContribution,
        decimal? currentUnitContribution,
        IReadOnlyList<CommercialProfitabilityComparisonResultDraft> results)
    {
        var ordered = OrderDrivers(drivers);
        return new CommercialProfitabilityComparisonEvaluationOutput(
            outcome,
            evidenceStatus,
            CommercialProfitabilityComparisonAggregationMethods.UnweightedMedian,
            ordered.Count > 0 ? ordered[0].ReasonCode : null,
            referenceSlice.CandidateAssessmentCount + currentSlice.CandidateAssessmentCount,
            referenceSlice.ComparableAssessmentCount + currentSlice.ComparableAssessmentCount,
            referenceSlice.ExcludedAssessmentCount + currentSlice.ExcludedAssessmentCount,
            referenceSlice.CompleteCoverageCount + currentSlice.CompleteCoverageCount,
            referenceSlice.PartialCoverageCount + currentSlice.PartialCoverageCount,
            referenceSlice.InsufficientDataCount + currentSlice.InsufficientDataCount,
            referenceSlice.SourceConflictCount + currentSlice.SourceConflictCount,
            referenceSlice.NegativeContributionCount + currentSlice.NegativeContributionCount,
            referenceSlice.BelowThresholdCount + currentSlice.BelowThresholdCount,
            referenceSlice.CandidateAssessmentCount,
            referenceSlice.ComparableAssessmentCount,
            referenceSlice.ExcludedAssessmentCount,
            referenceSlice.CompleteCoverageCount,
            referenceSlice.PartialCoverageCount,
            referenceSlice.InsufficientDataCount,
            referenceSlice.SourceConflictCount,
            referenceSlice.NegativeContributionCount,
            referenceSlice.BelowThresholdCount,
            currentSlice.CandidateAssessmentCount,
            currentSlice.ComparableAssessmentCount,
            currentSlice.ExcludedAssessmentCount,
            currentSlice.CompleteCoverageCount,
            currentSlice.PartialCoverageCount,
            currentSlice.InsufficientDataCount,
            currentSlice.SourceConflictCount,
            currentSlice.NegativeContributionCount,
            currentSlice.BelowThresholdCount,
            referenceMedian,
            currentMedian,
            delta,
            referenceUnitContribution,
            currentUnitContribution,
            results,
            ordered);
    }

    private static CommercialProfitabilityComparisonEvaluationOutput BuildPositionRunOutput(
        string outcome,
        string evidenceStatus,
        List<CommercialProfitabilityComparisonDriverDraft> drivers,
        List<CommercialProfitabilityComparisonResultDraft> results)
    {
        var ordered = OrderDrivers(drivers);
        return new CommercialProfitabilityComparisonEvaluationOutput(
            outcome,
            evidenceStatus,
            CommercialProfitabilityComparisonAggregationMethods.UnweightedMedian,
            ordered.Count > 0 ? ordered[0].ReasonCode : null,
            results.Sum(r => r.CandidateAssessmentCount),
            results.Sum(r => r.ComparableAssessmentCount),
            results.Sum(r => r.ExcludedAssessmentCount),
            results.Sum(r => r.CompleteCoverageCount),
            0,
            results.Count(r => r.PositionState == CommercialProfitabilityContributionPositionStates.InsufficientData),
            results.Count(r => r.PositionState == CommercialProfitabilityContributionPositionStates.SourceConflict),
            results.Sum(r => r.NegativeContributionCount),
            results.Sum(r => r.BelowThresholdCount),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            results,
            ordered);
    }

    private static CommercialProfitabilityComparisonDriverDraft Draft(
        string reasonCode,
        string severity,
        decimal? actualValue,
        decimal? referenceValue,
        decimal? thresholdValue,
        string? unit,
        decimal? normalizedVariance,
        int affectedAssessmentCount,
        string supportingText) =>
        new(reasonCode, severity, actualValue, referenceValue, thresholdValue, unit, normalizedVariance, affectedAssessmentCount, supportingText);
}
