namespace NevaxisFactOS.Persistence.CommercialIntelligence;

public sealed record CommercialQuotationRiskComparisonDriverDraft(
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

public sealed record CommercialQuotationRiskComparisonResultDraft(
    string CustomerReference,
    int CandidateAssessmentCount,
    int ComparableAssessmentCount,
    int ExcludedAssessmentCount,
    int RiskBearingAssessmentCount,
    decimal? RiskIncidenceFraction,
    int PriceRiskCount,
    int ExpiryRiskCount,
    int PaymentTermRiskCount,
    string PositionState,
    int? RiskRank,
    bool IsTied,
    string EvidenceStatus);

public sealed record CommercialQuotationRiskComparisonEvaluationOutput(
    string ComparisonOutcome,
    string EvidenceStatus,
    string? LeadingDriverCode,
    int CandidateAssessmentCount,
    int ComparableAssessmentCount,
    int ExcludedAssessmentCount,
    int RiskBearingAssessmentCount,
    int InsufficientDataCount,
    int SourceConflictCount,
    int PriceRiskCount,
    int ExpiryRiskCount,
    int PaymentTermRiskCount,
    int? ReferenceCandidateAssessmentCount,
    int? ReferenceComparableAssessmentCount,
    int? ReferenceExcludedAssessmentCount,
    int? ReferenceRiskBearingAssessmentCount,
    int? CurrentCandidateAssessmentCount,
    int? CurrentComparableAssessmentCount,
    int? CurrentExcludedAssessmentCount,
    int? CurrentRiskBearingAssessmentCount,
    decimal? ReferenceRiskIncidence,
    decimal? CurrentRiskIncidence,
    decimal? RiskIncidenceDelta,
    IReadOnlyList<CommercialQuotationRiskComparisonResultDraft> CustomerResults,
    IReadOnlyList<CommercialQuotationRiskComparisonDriverDraft> Drivers);

public static class CommercialQuotationRiskComparisonEvaluator
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
        [CommercialQuotationRiskComparisonDriverReasonCodes.SourceConflict] = 0,
        [CommercialQuotationRiskComparisonDriverReasonCodes.InsufficientReferenceWindow] = 1,
        [CommercialQuotationRiskComparisonDriverReasonCodes.InsufficientCurrentWindow] = 2,
        [CommercialQuotationRiskComparisonDriverReasonCodes.InsufficientCustomerEvidence] = 3,
        [CommercialQuotationRiskComparisonDriverReasonCodes.RiskIncidenceIncrease] = 4,
        [CommercialQuotationRiskComparisonDriverReasonCodes.RiskIncidenceDecrease] = 5,
        [CommercialQuotationRiskComparisonDriverReasonCodes.PriceRiskConcentration] = 6,
        [CommercialQuotationRiskComparisonDriverReasonCodes.ExpiryRiskConcentration] = 7,
        [CommercialQuotationRiskComparisonDriverReasonCodes.PaymentTermRiskConcentration] = 8,
        [CommercialQuotationRiskComparisonDriverReasonCodes.CustomerRiskConcentration] = 9,
        [CommercialQuotationRiskComparisonDriverReasonCodes.CustomerRiskTie] = 10,
        [CommercialQuotationRiskComparisonDriverReasonCodes.WithinRiskBand] = 11,
    };

    public static CommercialQuotationRiskComparisonEvaluationOutput EvaluateTemporalRiskTrend(
        CommercialQuotationRiskProfile profile,
        CommercialQuotationRiskAssessmentWindowSlice referenceSlice,
        CommercialQuotationRiskAssessmentWindowSlice currentSlice)
    {
        var drivers = new List<CommercialQuotationRiskComparisonDriverDraft>();
        var minimum = profile.MinimumComparableObservations;
        var band = profile.MaximumRiskIncidenceDelta;

        if (referenceSlice.HasSourceConflict || currentSlice.HasSourceConflict)
        {
            drivers.Add(Draft(
                CommercialQuotationRiskComparisonDriverReasonCodes.SourceConflict,
                "critical",
                null,
                null,
                null,
                "assessment",
                null,
                referenceSlice.SourceConflictCount + currentSlice.SourceConflictCount,
                "Comparable quotation-risk assessments could not be normalized without source conflict."));
            return BuildTemporalOutput(
                CommercialQuotationRiskComparisonOutcomes.SourceConflict,
                CommercialQuotationRiskEvidenceStatuses.SourceConflict,
                drivers,
                referenceSlice,
                currentSlice,
                null,
                null,
                null);
        }

        if (referenceSlice.ComparableAssessmentCount < minimum)
        {
            drivers.Add(Draft(
                CommercialQuotationRiskComparisonDriverReasonCodes.InsufficientReferenceWindow,
                "info",
                referenceSlice.ComparableAssessmentCount,
                minimum,
                null,
                "assessment",
                null,
                referenceSlice.ComparableAssessmentCount,
                $"Reference window comparable assessment count {referenceSlice.ComparableAssessmentCount} is below profile minimum {minimum}."));
        }

        if (currentSlice.ComparableAssessmentCount < minimum)
        {
            drivers.Add(Draft(
                CommercialQuotationRiskComparisonDriverReasonCodes.InsufficientCurrentWindow,
                "info",
                currentSlice.ComparableAssessmentCount,
                minimum,
                null,
                "assessment",
                null,
                currentSlice.ComparableAssessmentCount,
                $"Current window comparable assessment count {currentSlice.ComparableAssessmentCount} is below profile minimum {minimum}."));
        }

        if (referenceSlice.ComparableAssessmentCount < minimum || currentSlice.ComparableAssessmentCount < minimum)
        {
            return BuildTemporalOutput(
                CommercialQuotationRiskComparisonOutcomes.InsufficientData,
                CommercialQuotationRiskEvidenceStatuses.InsufficientData,
                drivers,
                referenceSlice,
                currentSlice,
                null,
                null,
                null);
        }

        var referenceIncidence = ComputeIncidence(
            referenceSlice.RiskBearingAssessmentCount,
            referenceSlice.ComparableAssessmentCount);
        var currentIncidence = ComputeIncidence(
            currentSlice.RiskBearingAssessmentCount,
            currentSlice.ComparableAssessmentCount);
        var delta = currentIncidence - referenceIncidence;

        AppendConcentrationDrivers(drivers, currentSlice, "current");

        string outcome;
        if (delta > band)
        {
            outcome = CommercialQuotationRiskComparisonOutcomes.RiskIncrease;
            drivers.Add(Draft(
                CommercialQuotationRiskComparisonDriverReasonCodes.RiskIncidenceIncrease,
                "high",
                currentIncidence,
                referenceIncidence,
                band,
                "incidence-fraction",
                delta,
                currentSlice.RiskBearingAssessmentCount,
                $"Current risk incidence {currentIncidence:0.######} exceeds reference {referenceIncidence:0.######} by more than band {band:0.######}."));
        }
        else if (delta < -band)
        {
            outcome = CommercialQuotationRiskComparisonOutcomes.RiskDecrease;
            drivers.Add(Draft(
                CommercialQuotationRiskComparisonDriverReasonCodes.RiskIncidenceDecrease,
                "moderate",
                currentIncidence,
                referenceIncidence,
                band,
                "incidence-fraction",
                delta,
                currentSlice.RiskBearingAssessmentCount,
                $"Current risk incidence {currentIncidence:0.######} is below reference {referenceIncidence:0.######} by more than band {band:0.######}."));
        }
        else
        {
            outcome = CommercialQuotationRiskComparisonOutcomes.WithinRiskBand;
            drivers.Add(Draft(
                CommercialQuotationRiskComparisonDriverReasonCodes.WithinRiskBand,
                "info",
                currentIncidence,
                referenceIncidence,
                band,
                "incidence-fraction",
                delta,
                currentSlice.ComparableAssessmentCount,
                $"Risk incidence delta {delta:0.######} remains within profile band {band:0.######}."));
        }

        return BuildTemporalOutput(
            outcome,
            CommercialQuotationRiskEvidenceStatuses.Simulated,
            drivers,
            referenceSlice,
            currentSlice,
            referenceIncidence,
            currentIncidence,
            delta);
    }

    public static CommercialQuotationRiskComparisonEvaluationOutput EvaluateCustomerRiskPosition(
        CommercialQuotationRiskProfile profile,
        IReadOnlyList<CommercialQuotationRiskAssessment> assessments,
        IReadOnlyList<string> customerReferences)
    {
        var drivers = new List<CommercialQuotationRiskComparisonDriverDraft>();
        var minimum = profile.MinimumComparableObservations;
        var results = new List<CommercialQuotationRiskComparisonResultDraft>();

        foreach (var customerReference in customerReferences.OrderBy(c => c, StringComparer.Ordinal))
        {
            var slice = CommercialQuotationRiskAssessmentReader.SummarizeCustomer(assessments, customerReference);
            if (slice.HasSourceConflict)
            {
                results.Add(new CommercialQuotationRiskComparisonResultDraft(
                    customerReference,
                    slice.CandidateAssessmentCount,
                    slice.ComparableAssessmentCount,
                    slice.ExcludedAssessmentCount,
                    slice.RiskBearingAssessmentCount,
                    null,
                    slice.PriceRiskCount,
                    slice.ExpiryRiskCount,
                    slice.PaymentTermRiskCount,
                    CommercialQuotationRiskCustomerPositionStates.SourceConflict,
                    null,
                    false,
                    CommercialQuotationRiskEvidenceStatuses.SourceConflict));
                continue;
            }

            if (slice.ComparableAssessmentCount < minimum)
            {
                results.Add(new CommercialQuotationRiskComparisonResultDraft(
                    customerReference,
                    slice.CandidateAssessmentCount,
                    slice.ComparableAssessmentCount,
                    slice.ExcludedAssessmentCount,
                    slice.RiskBearingAssessmentCount,
                    null,
                    slice.PriceRiskCount,
                    slice.ExpiryRiskCount,
                    slice.PaymentTermRiskCount,
                    CommercialQuotationRiskCustomerPositionStates.InsufficientData,
                    null,
                    false,
                    CommercialQuotationRiskEvidenceStatuses.InsufficientData));
                continue;
            }

            var incidence = ComputeIncidence(slice.RiskBearingAssessmentCount, slice.ComparableAssessmentCount);
            results.Add(new CommercialQuotationRiskComparisonResultDraft(
                customerReference,
                slice.CandidateAssessmentCount,
                slice.ComparableAssessmentCount,
                slice.ExcludedAssessmentCount,
                slice.RiskBearingAssessmentCount,
                incidence,
                slice.PriceRiskCount,
                slice.ExpiryRiskCount,
                slice.PaymentTermRiskCount,
                CommercialQuotationRiskCustomerPositionStates.WithinRiskBand,
                null,
                false,
                CommercialQuotationRiskEvidenceStatuses.Simulated));
        }

        if (results.Any(r => r.PositionState == CommercialQuotationRiskCustomerPositionStates.SourceConflict))
        {
            drivers.Add(Draft(
                CommercialQuotationRiskComparisonDriverReasonCodes.SourceConflict,
                "critical",
                null,
                null,
                null,
                "assessment",
                null,
                results.Sum(r => r.ExcludedAssessmentCount),
                "Customer quotation-risk comparison encountered source-conflicting assessments."));
            return BuildCustomerOutput(
                CommercialQuotationRiskComparisonOutcomes.SourceConflict,
                CommercialQuotationRiskEvidenceStatuses.SourceConflict,
                drivers,
                results);
        }

        var eligible = results
            .Where(r => r.RiskIncidenceFraction.HasValue)
            .OrderByDescending(r => r.RiskIncidenceFraction!.Value)
            .ThenByDescending(r => r.RiskBearingAssessmentCount)
            .ThenByDescending(r => r.ComparableAssessmentCount)
            .ThenBy(r => r.CustomerReference, StringComparer.Ordinal)
            .ToList();

        if (eligible.Count == 0)
        {
            drivers.Add(Draft(
                CommercialQuotationRiskComparisonDriverReasonCodes.InsufficientCustomerEvidence,
                "info",
                results.Sum(r => r.ComparableAssessmentCount),
                minimum,
                null,
                "assessment",
                null,
                results.Sum(r => r.CandidateAssessmentCount),
                "No customer had sufficient comparable quotation-risk assessments."));
            return BuildCustomerOutput(
                CommercialQuotationRiskComparisonOutcomes.InsufficientData,
                CommercialQuotationRiskEvidenceStatuses.InsufficientData,
                drivers,
                results);
        }

        var lowestIncidence = eligible[^1].RiskIncidenceFraction!.Value;
        var highestIncidence = eligible[0].RiskIncidenceFraction!.Value;
        var tiedGroups = eligible
            .GroupBy(r => (r.RiskIncidenceFraction, r.RiskBearingAssessmentCount, r.ComparableAssessmentCount))
            .ToList();

        for (var index = 0; index < eligible.Count; index++)
        {
            var item = eligible[index];
            var rank = index + 1;
            var isTied = tiedGroups.Any(g =>
                g.Key.RiskIncidenceFraction == item.RiskIncidenceFraction
                && g.Key.RiskBearingAssessmentCount == item.RiskBearingAssessmentCount
                && g.Key.ComparableAssessmentCount == item.ComparableAssessmentCount
                && g.Count() > 1);
            string position;
            if (isTied)
            {
                position = CommercialQuotationRiskCustomerPositionStates.TiedRiskPosition;
            }
            else if (item.RiskIncidenceFraction == lowestIncidence && item.RiskIncidenceFraction == highestIncidence)
            {
                position = CommercialQuotationRiskCustomerPositionStates.WithinRiskBand;
            }
            else if (item.RiskIncidenceFraction == lowestIncidence)
            {
                position = CommercialQuotationRiskCustomerPositionStates.LowestObservedRiskPosition;
            }
            else if (item.RiskIncidenceFraction == highestIncidence)
            {
                position = CommercialQuotationRiskCustomerPositionStates.HigherObservedRiskPosition;
            }
            else
            {
                position = CommercialQuotationRiskCustomerPositionStates.WithinRiskBand;
            }

            results[results.FindIndex(r => r.CustomerReference == item.CustomerReference)] =
                item with { PositionState = position, RiskRank = rank, IsTied = isTied };
        }

        var aggregate = CommercialQuotationRiskAssessmentReader.SummarizeWindow(assessments);
        if (results.Any(r => r.IsTied))
        {
            drivers.Add(Draft(
                CommercialQuotationRiskComparisonDriverReasonCodes.CustomerRiskTie,
                "high",
                highestIncidence,
                lowestIncidence,
                null,
                "incidence-fraction",
                highestIncidence - lowestIncidence,
                results.Count(r => r.IsTied),
                "One or more customers share identical observable risk-position values."));
        }
        else if (highestIncidence > lowestIncidence)
        {
            drivers.Add(Draft(
                CommercialQuotationRiskComparisonDriverReasonCodes.CustomerRiskConcentration,
                "moderate",
                highestIncidence,
                lowestIncidence,
                null,
                "incidence-fraction",
                highestIncidence - lowestIncidence,
                eligible.Count(r => r.PositionState == CommercialQuotationRiskCustomerPositionStates.HigherObservedRiskPosition),
                "Customer quotation-risk incidence spread is observable across canonical customer references."));
        }
        else
        {
            drivers.Add(Draft(
                CommercialQuotationRiskComparisonDriverReasonCodes.WithinRiskBand,
                "info",
                highestIncidence,
                lowestIncidence,
                profile.MaximumRiskIncidenceDelta,
                "incidence-fraction",
                0m,
                eligible.Count,
                "Customer quotation-risk positions remain within the observed spread."));
        }

        AppendConcentrationDrivers(drivers, aggregate, "customer");

        return BuildCustomerOutput(
            CommercialQuotationRiskComparisonOutcomes.WithinRiskBand,
            CommercialQuotationRiskEvidenceStatuses.Simulated,
            drivers,
            results);
    }

    public static IReadOnlyList<CommercialQuotationRiskComparisonDriverDraft> OrderDrivers(
        IReadOnlyList<CommercialQuotationRiskComparisonDriverDraft> drivers) =>
        drivers
            .OrderBy(d => SeverityOrder.GetValueOrDefault(d.Severity, 99))
            .ThenByDescending(d => Math.Abs(d.NormalizedVariance ?? 0m))
            .ThenByDescending(d => d.AffectedAssessmentCount)
            .ThenBy(d => ReasonOrder.GetValueOrDefault(d.ReasonCode, 99))
            .ToList();

    private static void AppendConcentrationDrivers(
        List<CommercialQuotationRiskComparisonDriverDraft> drivers,
        CommercialQuotationRiskAssessmentWindowSlice slice,
        string scopeLabel)
    {
        if (slice.PriceRiskCount > 0)
        {
            drivers.Add(Draft(
                CommercialQuotationRiskComparisonDriverReasonCodes.PriceRiskConcentration,
                "moderate",
                slice.PriceRiskCount,
                slice.ComparableAssessmentCount,
                null,
                "assessment",
                slice.ComparableAssessmentCount == 0 ? null : (decimal)slice.PriceRiskCount / slice.ComparableAssessmentCount,
                slice.PriceRiskCount,
                $"{scopeLabel} window contains {slice.PriceRiskCount} price-risk assessments."));
        }

        if (slice.ExpiryRiskCount > 0)
        {
            drivers.Add(Draft(
                CommercialQuotationRiskComparisonDriverReasonCodes.ExpiryRiskConcentration,
                "moderate",
                slice.ExpiryRiskCount,
                slice.ComparableAssessmentCount,
                null,
                "assessment",
                slice.ComparableAssessmentCount == 0 ? null : (decimal)slice.ExpiryRiskCount / slice.ComparableAssessmentCount,
                slice.ExpiryRiskCount,
                $"{scopeLabel} window contains {slice.ExpiryRiskCount} expiry-risk assessments."));
        }

        if (slice.PaymentTermRiskCount > 0)
        {
            drivers.Add(Draft(
                CommercialQuotationRiskComparisonDriverReasonCodes.PaymentTermRiskConcentration,
                "moderate",
                slice.PaymentTermRiskCount,
                slice.ComparableAssessmentCount,
                null,
                "assessment",
                slice.ComparableAssessmentCount == 0 ? null : (decimal)slice.PaymentTermRiskCount / slice.ComparableAssessmentCount,
                slice.PaymentTermRiskCount,
                $"{scopeLabel} window contains {slice.PaymentTermRiskCount} payment-term-risk assessments."));
        }
    }

    private static decimal ComputeIncidence(int riskBearingCount, int comparableCount) =>
        comparableCount == 0 ? 0m : (decimal)riskBearingCount / comparableCount;

    private static CommercialQuotationRiskComparisonEvaluationOutput BuildTemporalOutput(
        string outcome,
        string evidenceStatus,
        List<CommercialQuotationRiskComparisonDriverDraft> drivers,
        CommercialQuotationRiskAssessmentWindowSlice referenceSlice,
        CommercialQuotationRiskAssessmentWindowSlice currentSlice,
        decimal? referenceIncidence,
        decimal? currentIncidence,
        decimal? delta)
    {
        var ordered = OrderDrivers(drivers);
        var combinedCandidate = referenceSlice.CandidateAssessmentCount + currentSlice.CandidateAssessmentCount;
        var combinedComparable = referenceSlice.ComparableAssessmentCount + currentSlice.ComparableAssessmentCount;
        var combinedExcluded = referenceSlice.ExcludedAssessmentCount + currentSlice.ExcludedAssessmentCount;
        return new CommercialQuotationRiskComparisonEvaluationOutput(
            outcome,
            evidenceStatus,
            ordered.Count > 0 ? ordered[0].ReasonCode : null,
            combinedCandidate,
            combinedComparable,
            combinedExcluded,
            referenceSlice.RiskBearingAssessmentCount + currentSlice.RiskBearingAssessmentCount,
            referenceSlice.InsufficientDataCount + currentSlice.InsufficientDataCount,
            referenceSlice.SourceConflictCount + currentSlice.SourceConflictCount,
            referenceSlice.PriceRiskCount + currentSlice.PriceRiskCount,
            referenceSlice.ExpiryRiskCount + currentSlice.ExpiryRiskCount,
            referenceSlice.PaymentTermRiskCount + currentSlice.PaymentTermRiskCount,
            referenceSlice.CandidateAssessmentCount,
            referenceSlice.ComparableAssessmentCount,
            referenceSlice.ExcludedAssessmentCount,
            referenceSlice.RiskBearingAssessmentCount,
            currentSlice.CandidateAssessmentCount,
            currentSlice.ComparableAssessmentCount,
            currentSlice.ExcludedAssessmentCount,
            currentSlice.RiskBearingAssessmentCount,
            referenceIncidence,
            currentIncidence,
            delta,
            [],
            ordered);
    }

    private static CommercialQuotationRiskComparisonEvaluationOutput BuildCustomerOutput(
        string outcome,
        string evidenceStatus,
        List<CommercialQuotationRiskComparisonDriverDraft> drivers,
        List<CommercialQuotationRiskComparisonResultDraft> results)
    {
        var ordered = OrderDrivers(drivers);
        return new CommercialQuotationRiskComparisonEvaluationOutput(
            outcome,
            evidenceStatus,
            ordered.Count > 0 ? ordered[0].ReasonCode : null,
            results.Sum(r => r.CandidateAssessmentCount),
            results.Sum(r => r.ComparableAssessmentCount),
            results.Sum(r => r.ExcludedAssessmentCount),
            results.Sum(r => r.RiskBearingAssessmentCount),
            results.Count(r => r.PositionState == CommercialQuotationRiskCustomerPositionStates.InsufficientData),
            results.Count(r => r.PositionState == CommercialQuotationRiskCustomerPositionStates.SourceConflict),
            results.Sum(r => r.PriceRiskCount),
            results.Sum(r => r.ExpiryRiskCount),
            results.Sum(r => r.PaymentTermRiskCount),
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

    private static CommercialQuotationRiskComparisonDriverDraft Draft(
        string reasonCode,
        string severity,
        decimal? actualValue,
        decimal? referenceValue,
        decimal? thresholdValue,
        string? unit,
        decimal? normalizedVariance,
        int affectedAssessmentCount,
        string supportingText) =>
        new(
            reasonCode,
            severity,
            actualValue,
            referenceValue,
            thresholdValue,
            unit,
            normalizedVariance,
            affectedAssessmentCount,
            supportingText);
}
