using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NevaxisFactOS.Persistence.AiTasks;

internal static class AiTaskIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string ComputeSourceLinkedFingerprint(
        AiTaskOperationContext context,
        CreateSourceLinkedAiTaskCommand command) =>
        ComputeHex(JsonSerializer.Serialize(new
        {
            context.TenantId,
            context.FacilityId,
            sourceModule = Normalize(command.SourceModule),
            sourceEntityId = Normalize(command.SourceEntityId),
            title = Normalize(command.Title),
            summary = Normalize(command.Summary),
            category = Normalize(command.Category),
            priority = Normalize(command.Priority),
            riskLevel = Normalize(command.RiskLevel),
            detectedIssue = Normalize(command.DetectedIssue),
            reasoning = Normalize(command.Reasoning),
            recommendation = command.Recommendation,
            confidence = command.Confidence,
            impact = command.Impact,
            sources = command.Sources,
            siteId = Normalize(command.SiteId),
            productionLineId = Normalize(command.ProductionLineId),
            machineId = Normalize(command.MachineId),
            moldId = Normalize(command.MoldId),
            productionOrderId = Normalize(command.ProductionOrderId),
            salesOrderId = Normalize(command.SalesOrderId),
            materialId = Normalize(command.MaterialId),
            supplierId = Normalize(command.SupplierId),
            customerId = Normalize(command.CustomerId),
            dueAtUtc = command.DueAtUtc,
        }, JsonOptions));

    private static string ComputeHex(string payload)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
