using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NevaxisFactOS.Persistence.IotPlc;

internal static class IotPlcIntelligenceIdempotencyFingerprint
{
    public static string ComputeProfileFingerprint(IotPlcOperationContext context, CreateIotPlcQualityProfileCommand command) =>
        ComputeHash(new { context.TenantId, context.FacilityId, command.ProfileCode, command.DisplayName, command.ExpectedObservationIntervalSeconds, command.Status });

    public static string ComputeProfileUpdateFingerprint(IotPlcOperationContext context, string profileCode, UpdateIotPlcQualityProfileCommand command) =>
        ComputeHash(new { context.TenantId, context.FacilityId, profileCode, command.DisplayName, command.ExpectedObservationIntervalSeconds, command.ExpectedVersion });

    public static string ComputeConnectivityEvaluateFingerprint(IotPlcOperationContext context, EvaluateIotPlcConnectivityCommand command) =>
        ComputeHash(new
        {
            context.TenantId,
            context.FacilityId,
            command.EndpointReference,
            command.EvaluationWindowStartUtc,
            command.EvaluationWindowEndUtc,
            command.ProfileReference,
            command.EvidenceMode,
            command.EvaluationTimeUtc,
        });

    public static string ComputeSignalEvaluateFingerprint(IotPlcOperationContext context, EvaluateIotPlcSignalQualityCommand command) =>
        ComputeHash(new
        {
            context.TenantId,
            context.FacilityId,
            command.SignalReference,
            command.EvaluationWindowStartUtc,
            command.EvaluationWindowEndUtc,
            command.ProfileReference,
            command.EvidenceMode,
            command.EvaluationTimeUtc,
        });

    private static string ComputeHash(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
