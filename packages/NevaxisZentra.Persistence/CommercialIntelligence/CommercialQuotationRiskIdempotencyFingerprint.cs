using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NevaxisZentra.Persistence.CommercialIntelligence;

internal static class CommercialQuotationRiskIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string ComputeProfileFingerprint(
        CommercialIntelligenceOperationContext context,
        CreateCommercialQuotationRiskProfileCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            profileCode = Normalize(command.ProfileCode),
            name = Normalize(command.Name),
            command.ReferenceWindowDurationDays,
            command.MinimumComparableObservations,
            command.MaximumNegativePriceVarianceRatio,
            command.MaximumPositivePriceVarianceRatio,
            command.MaximumPaymentTermDays,
            command.ExpiryWarningDays,
            command.MaximumEvidenceAgeDays,
            allowedEvidenceStatuses = Normalize(command.AllowedEvidenceStatuses),
            allowedSalesEvidenceKinds = Normalize(command.AllowedSalesEvidenceKinds),
            requiredTaxInclusion = Normalize(command.RequiredTaxInclusion),
            requiredDiscountInclusion = Normalize(command.RequiredDiscountInclusion),
            requiredFreightInclusion = Normalize(command.RequiredFreightInclusion),
            requiredGrossNetBasis = Normalize(command.RequiredGrossNetBasis),
            sourceProvenance = Normalize(command.SourceProvenance),
        }, JsonOptions));

    public static string ComputeAssessmentFingerprint(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialQuotationRiskAssessmentCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            command.ProfileId,
            command.ExpectedProfileVersion,
            command.EvidenceObservationId,
            referenceWindowStartUtc = command.ReferenceWindowStartUtc,
            referenceWindowEndUtc = command.ReferenceWindowEndUtc,
            sourceProvenance = Normalize(command.SourceProvenance),
        }, JsonOptions));

    public static string ComputeComparisonFingerprint(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialQuotationRiskComparisonCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            command.ProfileId,
            command.ExpectedProfileVersion,
            comparisonType = Normalize(command.ComparisonType),
            command.MaterialId,
            customerReference = Normalize(command.CustomerReference),
            currentWindowStartUtc = command.CurrentWindowStartUtc,
            currentWindowEndUtc = command.CurrentWindowEndUtc,
            referenceWindowStartUtc = command.ReferenceWindowStartUtc,
            referenceWindowEndUtc = command.ReferenceWindowEndUtc,
            sourceProvenance = Normalize(command.SourceProvenance),
        }, JsonOptions));

    private static string ComputeHex(string payload)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
