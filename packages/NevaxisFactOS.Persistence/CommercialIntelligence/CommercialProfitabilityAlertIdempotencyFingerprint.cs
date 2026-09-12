using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NevaxisFactOS.Persistence.CommercialIntelligence;

public static class CommercialProfitabilityAlertIdempotencyFingerprint
{
    public static string ComputeMaterializationFingerprint(
        CommercialIntelligenceOperationContext context,
        Guid comparisonRunId,
        string alertCategory,
        string sourceSnapshotJson) =>
        Compute(new
        {
            tenantId = context.TenantId,
            facilityId = context.FacilityId,
            comparisonRunId,
            alertCategory = alertCategory.Trim(),
            sourceSnapshotJson,
        });

    public static string ComputeTransitionFingerprint(
        CommercialIntelligenceOperationContext context,
        Guid alertId,
        string targetStatus,
        uint expectedVersion,
        string? note) =>
        Compute(new
        {
            tenantId = context.TenantId,
            facilityId = context.FacilityId,
            alertId,
            targetStatus = targetStatus.Trim(),
            expectedVersion,
            note = note?.Trim(),
        });

    private static string Compute(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
