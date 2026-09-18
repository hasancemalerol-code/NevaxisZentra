using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NevaxisZentra.Persistence.CommercialIntelligence;

internal static class CommercialProfitabilityIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string ComputeProfileFingerprint(
        CommercialIntelligenceOperationContext context,
        CreateCommercialProfitabilityProfileCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            profileCode = Normalize(command.ProfileCode),
            name = Normalize(command.Name),
            command.MinimumContributionMarginRatio,
            command.MaximumEvidenceAgeDays,
            requiredCostComponentKinds = Normalize(command.RequiredCostComponentKinds),
            requiredCurrencyCode = Normalize(command.RequiredCurrencyCode),
            requiredUnitOfMeasure = Normalize(command.RequiredUnitOfMeasure),
            requiredGrossNetBasis = Normalize(command.RequiredGrossNetBasis),
            requiredTaxInclusion = Normalize(command.RequiredTaxInclusion),
            requiredDiscountInclusion = Normalize(command.RequiredDiscountInclusion),
            requiredFreightInclusion = Normalize(command.RequiredFreightInclusion),
            allowedEvidenceStatuses = Normalize(command.AllowedEvidenceStatuses),
            sourceProvenance = Normalize(command.SourceProvenance),
        }, JsonOptions));

    public static string ComputeAssessmentFingerprint(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialProfitabilityAssessmentCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            command.ProfileId,
            command.ExpectedProfileVersion,
            command.SalesEvidenceObservationId,
            sourceProvenance = Normalize(command.SourceProvenance),
        }, JsonOptions));

    public static string ComputeComparisonFingerprint(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialProfitabilityComparisonCommand command) =>
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
