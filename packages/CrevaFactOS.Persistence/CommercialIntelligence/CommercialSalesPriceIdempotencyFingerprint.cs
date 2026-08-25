using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CrevaFactOS.Persistence.CommercialIntelligence;

internal static class CommercialSalesPriceIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string ComputeProfileFingerprint(
        CommercialIntelligenceOperationContext context,
        CreateCommercialSalesPriceProfileCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            profileCode = Normalize(command.ProfileCode),
            displayName = Normalize(command.DisplayName),
            comparisonDimension = Normalize(command.ComparisonDimension),
            command.ReferenceWindowDurationDays,
            command.MinimumComparableObservations,
            command.MaximumPositiveVarianceRatio,
            command.CustomerSpreadThreshold,
            allowedEvidenceStatuses = Normalize(command.AllowedEvidenceStatuses),
            allowedSourceEvidenceKinds = Normalize(command.AllowedSourceEvidenceKinds),
            requiredTaxInclusion = Normalize(command.RequiredTaxInclusion),
            requiredDiscountInclusion = Normalize(command.RequiredDiscountInclusion),
            requiredFreightInclusion = Normalize(command.RequiredFreightInclusion),
            requiredGrossNetBasis = Normalize(command.RequiredGrossNetBasis),
            sourceProvenance = Normalize(command.SourceProvenance),
        }, JsonOptions));

    public static string ComputeAssessmentFingerprint(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialSalesPriceAssessmentCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            command.ProfileId,
            command.MaterialId,
            customerReference = Normalize(command.CustomerReference),
            sourceEvidenceKind = Normalize(command.SourceEvidenceKind),
            comparisonDimension = Normalize(command.ComparisonDimension),
            command.CurrentUnitPrice,
            currencyCode = Normalize(command.CurrencyCode),
            normalizedUnit = Normalize(command.NormalizedUnit),
            windowStartUtc = command.WindowStartUtc,
            windowEndUtc = command.WindowEndUtc,
            priceBasisGrossNet = Normalize(command.PriceBasisGrossNet),
            priceBasisTaxInclusion = Normalize(command.PriceBasisTaxInclusion),
            priceBasisDiscountInclusion = Normalize(command.PriceBasisDiscountInclusion),
            priceBasisFreightInclusion = Normalize(command.PriceBasisFreightInclusion),
            sourceProvenance = Normalize(command.SourceProvenance),
        }, JsonOptions));

    public static string ComputeComparisonFingerprint(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialSalesPriceComparisonCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            command.ProfileId,
            command.ExpectedProfileVersion,
            comparisonType = Normalize(command.ComparisonType),
            command.MaterialId,
            customerReference = Normalize(command.CustomerReference),
            sourceEvidenceKind = Normalize(command.SourceEvidenceKind),
            currentWindowStartUtc = command.CurrentWindowStartUtc,
            currentWindowEndUtc = command.CurrentWindowEndUtc,
            referenceWindowStartUtc = command.ReferenceWindowStartUtc,
            referenceWindowEndUtc = command.ReferenceWindowEndUtc,
            currencyCode = Normalize(command.CurrencyCode),
            normalizedUnit = Normalize(command.NormalizedUnit),
            priceBasisGrossNet = Normalize(command.PriceBasisGrossNet),
            priceBasisTaxInclusion = Normalize(command.PriceBasisTaxInclusion),
            priceBasisDiscountInclusion = Normalize(command.PriceBasisDiscountInclusion),
            priceBasisFreightInclusion = Normalize(command.PriceBasisFreightInclusion),
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
