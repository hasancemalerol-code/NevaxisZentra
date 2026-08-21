using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CrevaFactOS.Persistence.IotPlc;

internal static class IotPlcIdempotencyFingerprint
{
    public static string ComputeObservationFingerprint(
        IotPlcOperationContext context,
        IngestIotPlcObservationCommand command) =>
        ComputeHash(new
        {
            context.TenantId,
            context.FacilityId,
            command.SignalReference,
            command.ObservedAtUtc,
            command.BooleanValue,
            command.IntegerValue,
            command.DecimalValue,
            command.TextValue,
            command.TimestampValue,
            command.QualityState,
            command.EvidenceState,
            command.SourceSequenceReference,
            command.SourceProvenance,
        });

    public static string ComputeHealthFingerprint(
        IotPlcOperationContext context,
        IngestIotPlcConnectionHealthCommand command) =>
        ComputeHash(new
        {
            context.TenantId,
            context.FacilityId,
            command.EndpointReference,
            command.ObservedAtUtc,
            command.HealthState,
            command.Source,
            command.EvidenceState,
            command.ReasonCode,
            command.LatencyMs,
            command.LastSuccessfulObservationAtUtc,
        });

    private static string ComputeHash(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
