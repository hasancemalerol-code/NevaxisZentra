using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NevaxisZentra.Persistence.OperationalRisk;

internal static class OperationalRiskDetectionIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string ComputeProfileFingerprint(
        OperationalRiskOperationContext context,
        CreateOperationalRiskDetectionProfileCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            profileCode = Normalize(command.ProfileCode),
            displayName = Normalize(command.DisplayName),
            sourceProvenance = Normalize(command.SourceProvenance),
            rules = command.Rules.Select(r => new
            {
                r.RuleCode,
                r.SourceModule,
                r.SourceOutcome,
                r.RiskCategory,
                r.DefaultLikelihood,
                r.DefaultImpact,
                r.ThresholdVarianceBand,
                r.DriverReasonCode,
                r.PriorityOrder,
                r.IsActive,
            }).OrderBy(r => r.RuleCode),
        }, JsonOptions));

    public static string ComputeRunFingerprint(
        OperationalRiskOperationContext context,
        ExecuteOperationalRiskDetectionRunCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            command.ProfileId,
            windowStartUtc = command.WindowStartUtc,
            windowEndUtc = command.WindowEndUtc,
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
