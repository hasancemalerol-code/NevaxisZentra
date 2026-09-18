using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NevaxisZentra.Persistence.Energy;

internal static class EnergyIntelligenceIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string ComputeBaselineFingerprint(EnergyOperationContext context, EvaluateEnergyBaselineCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            command.ComparisonDimension,
            command.MeasurementPointReference,
            command.ReportingWindowDurationSeconds,
            shiftReference = Normalize(command.ShiftReference),
            productionContextReference = Normalize(command.ProductionContextReference),
            command.RelativeTolerance,
        }, JsonOptions));

    public static string ComputeCreateTargetFingerprint(EnergyOperationContext context, CreateEnergyTargetCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            command.TargetReference,
            command.TargetScope,
            command.ScopeReference,
            command.TargetMetric,
            command.TargetValue,
            command.Unit,
            command.Tolerance,
            command.EffectiveFromUtc,
            command.EffectiveToUtc,
            sourceProvenance = Normalize(command.SourceProvenance),
        }, JsonOptions));

    public static string ComputeAssessmentFingerprint(EnergyOperationContext context, EvaluateEnergyIntelligenceAssessmentCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            command.AssessmentReference,
            command.ComparisonDimension,
            command.MeasurementPointReference,
            command.EvaluationWindowStartUtc,
            command.EvaluationWindowEndUtc,
            baselineReference = Normalize(command.BaselineReference),
            consumptionTargetReference = Normalize(command.ConsumptionTargetReference),
            demandTargetReference = Normalize(command.DemandTargetReference),
            idleTargetReference = Normalize(command.IdleTargetReference),
            intensityTargetReference = Normalize(command.IntensityTargetReference),
            productionContextReference = Normalize(command.ProductionContextReference),
        }, JsonOptions));

    private static string ComputeHex(string payload)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
