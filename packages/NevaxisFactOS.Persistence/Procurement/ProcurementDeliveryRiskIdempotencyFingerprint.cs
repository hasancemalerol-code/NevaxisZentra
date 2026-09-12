using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NevaxisFactOS.Persistence.Procurement;

public static class ProcurementDeliveryRiskIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string ComputeAssessmentInputFingerprint(
        Guid purchaseOrderLineId,
        string ruleSetVersion,
        string serializedAssessmentInput) =>
        Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes($"{purchaseOrderLineId:N}:{ruleSetVersion}:{serializedAssessmentInput}"))).ToLowerInvariant();

    public static string SerializeAssessmentInput(ProcurementDeliveryRiskAssessmentResult result) =>
        JsonSerializer.Serialize(result, JsonOptions);
}
