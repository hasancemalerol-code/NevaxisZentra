using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NevaxisFactOS.Persistence.ProcessOee;

public static class ProcessOeeIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string ComputeWindowFingerprint(ProcessOeeOperationContext context, CreateProcessOeeWindowCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            command.MachineAssetId,
            productionOrderReference = Normalize(command.ProductionOrderReference),
            moldReference = Normalize(command.MoldReference),
            command.WindowStartUtc,
            command.WindowEndUtc,
            command.PlannedProductionSeconds,
            command.IdealCycleTimeSeconds,
            command.EffectiveCavityCount,
            sourceType = ProcessOeeValueConversions.NormalizeSourceType(command.SourceType),
        }, JsonOptions));

    public static bool WindowPayloadMatches(
        ProcessOeeEvaluationWindow existing,
        CreateProcessOeeWindowCommand command,
        string fingerprint) =>
        string.Equals(existing.CreateIdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal);

    public static string ComputeAssessmentFingerprint(ProcessOeeCalculationResult calculation) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            calculation.InputFingerprint,
            calculation.Status,
            calculation.PlannedProductionSeconds,
            calculation.OperatingSeconds,
            calculation.ObservedCycleCount,
            calculation.TotalPieceCount,
            calculation.GoodPieceCount,
            calculation.ScrapPieceCount,
        }, JsonOptions));

    public static string ComputeEvaluationInputFingerprint(ProcessOeeEvidenceBundle evidence, ProcessOeeEvaluationWindow window) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            window.Id,
            window.MachineAssetId,
            window.WindowStartUtc,
            window.WindowEndUtc,
            window.PlannedProductionSeconds,
            window.IdealCycleTimeSeconds,
            window.EffectiveCavityCount,
            evidence.OperatingSeconds,
            evidence.DowntimeSeconds,
            evidence.ObservedCycleCount,
            evidence.TotalPieceCount,
            evidence.GoodPieceCount,
            evidence.ScrapPieceCount,
        }, JsonOptions));

    private static string ComputeHex(string payload)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
