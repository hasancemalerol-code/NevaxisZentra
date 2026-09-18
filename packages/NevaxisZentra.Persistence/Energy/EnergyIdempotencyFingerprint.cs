using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NevaxisZentra.Persistence.Energy;

internal static class EnergyIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string ComputeReadingFingerprint(EnergyOperationContext context, IngestEnergyReadingCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            command.MeasurementPointReference,
            command.OccurredAtUtc,
            command.CumulativeEnergyKwh,
            command.IntervalEnergyKwh,
            command.ActivePowerKw,
            command.MaximumDemandKw,
            sourceType = Normalize(command.SourceType),
            sourceReference = Normalize(command.SourceReference),
            evidenceState = Normalize(command.EvidenceState),
        }, JsonOptions));

    public static string ComputeEvaluateFingerprint(EnergyOperationContext context, EvaluateEnergyConsumptionCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            command.MeasurementPointReference,
            command.WindowStartUtc,
            command.WindowEndUtc,
            command.TariffSnapshotId,
            allocations = command.Allocations?.Select(a => new
            {
                a.TargetType,
                targetReference = Normalize(a.TargetReference),
                a.AllocatedKwh,
                a.AllocationMethod,
            }).OrderBy(a => a.TargetType).ThenBy(a => a.targetReference),
        }, JsonOptions));

    private static string ComputeHex(string payload)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
