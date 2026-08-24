using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CrevaFactOS.Persistence.CommercialIntelligence;

internal static class CommercialIntelligenceIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string ComputeProfileFingerprint(
        CommercialIntelligenceOperationContext context,
        CreateCommercialPurchasePriceProfileCommand command) =>
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
            command.SupplierSpreadThreshold,
            allowedEvidenceStatuses = Normalize(command.AllowedEvidenceStatuses),
            requiredTaxInclusion = Normalize(command.RequiredTaxInclusion),
            requiredDiscountInclusion = Normalize(command.RequiredDiscountInclusion),
            requiredFreightInclusion = Normalize(command.RequiredFreightInclusion),
            sourceProvenance = Normalize(command.SourceProvenance),
        }, JsonOptions));

    public static string ComputeAssessmentFingerprint(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialPurchasePriceAssessmentCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            command.ProfileId,
            command.MaterialId,
            supplierReference = Normalize(command.SupplierReference),
            comparisonDimension = Normalize(command.ComparisonDimension),
            command.CurrentUnitPrice,
            currencyCode = Normalize(command.CurrencyCode),
            normalizedUnit = Normalize(command.NormalizedUnit),
            windowStartUtc = command.WindowStartUtc,
            windowEndUtc = command.WindowEndUtc,
            priceBasisTaxInclusion = Normalize(command.PriceBasisTaxInclusion),
            priceBasisDiscountInclusion = Normalize(command.PriceBasisDiscountInclusion),
            priceBasisFreightInclusion = Normalize(command.PriceBasisFreightInclusion),
            sourceProvenance = Normalize(command.SourceProvenance),
        }, JsonOptions));

    public static string ComputeComparisonFingerprint(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialPurchasePriceComparisonCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            command.ProfileId,
            command.ExpectedProfileVersion,
            comparisonType = Normalize(command.ComparisonType),
            command.MaterialId,
            currentWindowStartUtc = command.CurrentWindowStartUtc,
            currentWindowEndUtc = command.CurrentWindowEndUtc,
            referenceWindowStartUtc = command.ReferenceWindowStartUtc,
            referenceWindowEndUtc = command.ReferenceWindowEndUtc,
            currencyCode = Normalize(command.CurrencyCode),
            normalizedUnit = Normalize(command.NormalizedUnit),
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
