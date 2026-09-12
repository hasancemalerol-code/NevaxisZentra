using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NevaxisFactOS.Persistence.OperationalRisk;

internal static class OperationalRiskIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string ComputeCaseFingerprint(OperationalRiskOperationContext context, CreateOperationalRiskCaseCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            category = Normalize(command.Category),
            scopeType = Normalize(command.ScopeType),
            scopeReference = Normalize(command.ScopeReference),
            title = Normalize(command.Title),
            description = Normalize(command.Description),
            evidenceState = Normalize(command.EvidenceState),
            command.LikelihoodLevel,
            command.ImpactLevel,
            sourceModule = Normalize(command.SourceModule),
            sourceReference = Normalize(command.SourceReference),
            sourceProvenance = Normalize(command.SourceProvenance),
            firstDetectedAtUtc = command.FirstDetectedAtUtc,
            lastObservedAtUtc = command.LastObservedAtUtc,
        }, JsonOptions));

    public static string ComputeEvidenceFingerprint(OperationalRiskOperationContext context, AppendOperationalRiskEvidenceCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            command.CaseId,
            evidenceTypeCode = Normalize(command.EvidenceTypeCode),
            evidenceState = Normalize(command.EvidenceState),
            observedAtUtc = command.ObservedAtUtc,
            sourceModule = Normalize(command.SourceModule),
            sourceReference = Normalize(command.SourceReference),
            summary = Normalize(command.Summary),
            command.NumericValue,
            unit = Normalize(command.Unit),
            comparisonReference = Normalize(command.ComparisonReference),
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
