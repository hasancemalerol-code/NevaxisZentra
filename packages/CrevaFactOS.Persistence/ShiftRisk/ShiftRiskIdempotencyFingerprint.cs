using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CrevaFactOS.Persistence.ShiftRisk;

internal static class ShiftRiskIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    internal static string ComputeEvaluationInputFingerprint(ShiftRiskEvaluationContext context) =>
        HashPayload(new
        {
            productionShiftId = context.ProductionShiftId,
            shiftDate = context.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            plannedStartUtc = FormatDateTime(context.PlannedStartUtc),
            plannedEndUtc = FormatDateTime(context.PlannedEndUtc),
            productionOrderReference = NormalizeOptionalString(context.ProductionOrderReference),
            machineAssetId = context.MachineAssetId,
            machineFamilyScope = NormalizeOptionalString(context.MachineFamilyScope),
            requiredCompetencyCode = NormalizeOptionalString(context.RequiredCompetencyCode),
            ruleSetVersion = ShiftRiskConstants.RuleSetVersion,
        });

    internal static string ComputeEvaluateCommandFingerprint(
        ShiftRiskOperationContext context,
        EvaluateShiftRiskCommand command) =>
        HashPayload(new
        {
            facilityId = context.FacilityId,
            tenantId = context.TenantId,
            productionShiftId = command.ProductionShiftId,
            shiftDate = command.ShiftDate.Trim(),
            productionOrderReference = NormalizeOptionalString(command.ProductionOrderReference),
            machineAssetId = command.MachineAssetId,
            machineFamilyScope = NormalizeOptionalString(command.MachineFamilyScope),
            requiredCompetencyCode = NormalizeOptionalString(command.RequiredCompetencyCode),
        });

    internal static string ComputeCreateRequirementFingerprint(
        ShiftRiskOperationContext context,
        CreateShiftCoverageRequirementCommand command) =>
        HashPayload(new
        {
            facilityId = context.FacilityId,
            tenantId = context.TenantId,
            productionShiftId = command.ProductionShiftId,
            shiftDate = command.ShiftDate.Trim(),
            productionOrderReference = NormalizeOptionalString(command.ProductionOrderReference),
            machineAssetId = command.MachineAssetId,
            machineFamilyScope = NormalizeOptionalString(command.MachineFamilyScope),
            requiredCompetencyCode = command.RequiredCompetencyCode.Trim(),
            minimumApprovedOperatorCount = command.MinimumApprovedOperatorCount,
            isCriticalRole = command.IsCriticalRole,
            sourceReference = command.SourceReference.Trim(),
            effectiveFrom = NormalizeOptionalString(command.EffectiveFrom),
            effectiveUntil = NormalizeOptionalString(command.EffectiveUntil),
            isActive = command.IsActive,
        });

    private static string? NormalizeOptionalString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatDateTime(DateTimeOffset value) =>
        value.ToString("O", CultureInfo.InvariantCulture);

    private static string HashPayload(object payload)
    {
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
