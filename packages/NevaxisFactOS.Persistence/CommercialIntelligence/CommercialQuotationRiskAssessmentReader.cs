using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.CommercialIntelligence;

public sealed record CommercialQuotationRiskAssessmentWindowSlice(
    int CandidateAssessmentCount,
    int ComparableAssessmentCount,
    int ExcludedAssessmentCount,
    int RiskBearingAssessmentCount,
    int InsufficientDataCount,
    int SourceConflictCount,
    int PriceRiskCount,
    int ExpiryRiskCount,
    int PaymentTermRiskCount,
    bool HasSourceConflict);

public static class CommercialQuotationRiskAssessmentReader
{
    public static async Task<IReadOnlyList<CommercialQuotationRiskAssessment>> ReadAssessmentsInWindowAsync(
        NevaxisFactOSDbContext dbContext,
        CommercialIntelligenceOperationContext context,
        Guid profileId,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        Guid? materialId,
        string? customerReference,
        CancellationToken cancellationToken)
    {
        var query = dbContext.CommercialQuotationRiskAssessments.AsNoTracking()
            .Where(a => a.TenantId == context.TenantId
                        && a.FacilityId == context.FacilityId
                        && a.ProfileId == profileId
                        && a.EvaluatedAtUtc >= windowStartUtc
                        && a.EvaluatedAtUtc <= windowEndUtc);

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

    public static CommercialQuotationRiskAssessmentWindowSlice SummarizeWindow(
        IReadOnlyList<CommercialQuotationRiskAssessment> assessments)
    {
        var candidate = assessments.Count;
        var insufficient = assessments.Count(a =>
            string.Equals(a.Outcome, CommercialQuotationRiskAssessmentOutcomes.InsufficientData, StringComparison.Ordinal));
        var sourceConflict = assessments.Count(a =>
            string.Equals(a.Outcome, CommercialQuotationRiskAssessmentOutcomes.SourceConflict, StringComparison.Ordinal)
            || string.Equals(a.EvidenceStatus, CommercialQuotationRiskEvidenceStatuses.SourceConflict, StringComparison.Ordinal));
        var excluded = insufficient + sourceConflict;
        var comparable = candidate - excluded;
        var riskBearing = assessments.Count(a =>
            string.Equals(a.Outcome, CommercialQuotationRiskAssessmentOutcomes.Attention, StringComparison.Ordinal));

        return new CommercialQuotationRiskAssessmentWindowSlice(
            candidate,
            comparable,
            excluded,
            riskBearing,
            insufficient,
            sourceConflict,
            CountPriceRisk(assessments),
            CountExpiryRisk(assessments),
            CountPaymentTermRisk(assessments),
            sourceConflict > 0);
    }

    public static CommercialQuotationRiskAssessmentWindowSlice SummarizeCustomer(
        IReadOnlyList<CommercialQuotationRiskAssessment> assessments,
        string customerReference)
    {
        var scoped = assessments
            .Where(a => string.Equals(a.CustomerReference, customerReference, StringComparison.Ordinal))
            .ToList();
        return SummarizeWindow(scoped);
    }

    private static int CountPriceRisk(IReadOnlyList<CommercialQuotationRiskAssessment> assessments) =>
        assessments.Count(a =>
            string.Equals(a.LeadingDriverCode, CommercialQuotationRiskDriverReasonCodes.QuotedPriceBelowReference, StringComparison.Ordinal)
            || string.Equals(a.LeadingDriverCode, CommercialQuotationRiskDriverReasonCodes.QuotedPriceAboveReference, StringComparison.Ordinal));

    private static int CountExpiryRisk(IReadOnlyList<CommercialQuotationRiskAssessment> assessments) =>
        assessments.Count(a =>
            string.Equals(a.LeadingDriverCode, CommercialQuotationRiskDriverReasonCodes.QuotationExpired, StringComparison.Ordinal)
            || string.Equals(a.LeadingDriverCode, CommercialQuotationRiskDriverReasonCodes.QuotationExpiringSoon, StringComparison.Ordinal));

    private static int CountPaymentTermRisk(IReadOnlyList<CommercialQuotationRiskAssessment> assessments) =>
        assessments.Count(a =>
            string.Equals(a.LeadingDriverCode, CommercialQuotationRiskDriverReasonCodes.PaymentTermExceeded, StringComparison.Ordinal));
}
