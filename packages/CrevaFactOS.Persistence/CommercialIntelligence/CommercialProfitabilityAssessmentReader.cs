using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.CommercialIntelligence;

public sealed record CommercialProfitabilityAssessmentWindowSlice(
    int CandidateAssessmentCount,
    int ComparableAssessmentCount,
    int ExcludedAssessmentCount,
    int CompleteCoverageCount,
    int PartialCoverageCount,
    int InsufficientDataCount,
    int SourceConflictCount,
    int NegativeContributionCount,
    int BelowThresholdCount,
    bool HasSourceConflict);

public static class CommercialProfitabilityAssessmentReader
{
    public static async Task<IReadOnlyList<CommercialProfitabilityAssessment>> ReadAssessmentsInWindowAsync(
        CrevaFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        Guid profileId,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        Guid? materialId,
        string? customerReference,
        bool includeCostComponents,
        CancellationToken cancellationToken)
    {
        IQueryable<CommercialProfitabilityAssessment> query = dbContext.CommercialProfitabilityAssessments.AsNoTracking()
            .Where(a => a.TenantId == context.TenantId
                        && a.FacilityId == context.FacilityId
                        && a.ProfileId == profileId
                        && a.EvaluatedAtUtc >= windowStartUtc
                        && a.EvaluatedAtUtc <= windowEndUtc);

        if (includeCostComponents)
        {
            query = query.Include(a => a.CostComponents);
        }

        if (materialId.HasValue)
        {
            query = query.Where(a => a.MaterialId == materialId.Value);
        }

        if (!string.IsNullOrWhiteSpace(customerReference))
        {
            query = query.Where(a => a.CustomerReference == customerReference.Trim());
        }

        return await query.ToListAsync(cancellationToken);
    }

    public static CommercialProfitabilityAssessmentWindowSlice SummarizeWindow(
        IReadOnlyList<CommercialProfitabilityAssessment> assessments)
    {
        var candidate = assessments.Count;
        var insufficient = assessments.Count(a =>
            string.Equals(a.Outcome, CommercialProfitabilityAssessmentOutcomes.InsufficientData, StringComparison.Ordinal));
        var sourceConflict = assessments.Count(a =>
            string.Equals(a.Outcome, CommercialProfitabilityAssessmentOutcomes.SourceConflict, StringComparison.Ordinal)
            || string.Equals(a.EvidenceStatus, CommercialProfitabilityEvidenceStatuses.SourceConflict, StringComparison.Ordinal));
        var excluded = insufficient + sourceConflict;
        var comparable = candidate - excluded;
        var completeCoverage = assessments.Count(a =>
            string.Equals(a.CostCoverageStatus, CommercialProfitabilityCostCoverageStatuses.CompleteRequiredCostCoverage, StringComparison.Ordinal));
        var partialCoverage = assessments.Count(a =>
            string.Equals(a.CostCoverageStatus, CommercialProfitabilityCostCoverageStatuses.PartialCostCoverage, StringComparison.Ordinal));
        var negativeContribution = assessments.Count(a =>
            string.Equals(a.Outcome, CommercialProfitabilityAssessmentOutcomes.NegativeContribution, StringComparison.Ordinal));
        var belowThreshold = assessments.Count(a =>
            string.Equals(a.Outcome, CommercialProfitabilityAssessmentOutcomes.BelowThreshold, StringComparison.Ordinal));

        return new CommercialProfitabilityAssessmentWindowSlice(
            candidate,
            comparable,
            excluded,
            completeCoverage,
            partialCoverage,
            insufficient,
            sourceConflict,
            negativeContribution,
            belowThreshold,
            sourceConflict > 0);
    }

    public static IReadOnlyList<CommercialProfitabilityAssessment> FilterMedianComparable(
        IReadOnlyList<CommercialProfitabilityAssessment> assessments) =>
        assessments
            .Where(a =>
                string.Equals(a.CostCoverageStatus, CommercialProfitabilityCostCoverageStatuses.CompleteRequiredCostCoverage, StringComparison.Ordinal)
                && a.ContributionMarginRatio.HasValue
                && a.ContributionMarginRatio.Value < 1m)
            .ToList();

    public static bool AreUnitContributionsCompatible(IReadOnlyList<CommercialProfitabilityAssessment> assessments)
    {
        if (assessments.Count == 0)
        {
            return false;
        }

        var currency = assessments[0].CurrencyCode;
        var unit = assessments[0].UnitOfMeasure;
        var grossNet = assessments[0].GrossNetBasis;
        return assessments.All(a =>
            string.Equals(a.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.UnitOfMeasure, unit, StringComparison.Ordinal)
            && string.Equals(a.GrossNetBasis, grossNet, StringComparison.Ordinal));
    }
}
