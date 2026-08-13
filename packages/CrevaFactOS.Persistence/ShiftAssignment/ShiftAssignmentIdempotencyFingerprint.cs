using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CrevaFactOS.Persistence.ShiftWorkforce;

namespace CrevaFactOS.Persistence.ShiftAssignment;

internal static class ShiftAssignmentIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    internal static string ComputeAssessmentInputFingerprint(ShiftAssignmentEvaluationContext context) =>
        HashPayload(new
        {
            productionOrderReference = context.ProductionOrderReference.Trim(),
            machineAssetId = context.MachineAssetId,
            productionShiftId = context.ProductionShiftId,
            shiftDate = context.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            plannedStartUtc = FormatDateTime(context.PlannedStartUtc),
            plannedEndUtc = FormatDateTime(context.PlannedEndUtc),
            requiredCompetencyCode = context.RequiredCompetencyCode.Trim(),
            machineFamilyScope = NormalizeOptionalString(context.MachineFamilyScope),
            processScope = NormalizeOptionalString(context.ProcessScope),
            ruleSetVersion = ShiftAssignmentConstants.RuleSetVersion,
        });

    internal static string ComputeCreateAssignmentFingerprint(
        ShiftAssignmentOperationContext context,
        CreateShiftOperatorAssignmentCommand command) =>
        HashPayload(new
        {
            facilityId = context.FacilityId,
            tenantId = context.TenantId,
            productionOrderReference = command.ProductionOrderReference.Trim(),
            machineAssetId = command.MachineAssetId,
            productionShiftId = command.ProductionShiftId,
            shiftDate = command.ShiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            workforcePersonId = command.WorkforcePersonId,
            plannedStartUtc = FormatDateTime(command.PlannedStartUtc),
            plannedEndUtc = FormatDateTime(command.PlannedEndUtc),
            eligibilityAssessmentId = command.EligibilityAssessmentId,
            eligibilityResultId = command.EligibilityResultId,
            requiredCompetencyCode = command.RequiredCompetencyCode.Trim(),
            machineFamilyScope = NormalizeOptionalString(command.MachineFamilyScope),
            processScope = NormalizeOptionalString(command.ProcessScope),
        });

    internal static string ComputeApproveAssignmentFingerprint(
        ShiftAssignmentOperationContext context,
        Guid assignmentId,
        uint expectedVersion) =>
        HashPayload(new
        {
            facilityId = context.FacilityId,
            tenantId = context.TenantId,
            assignmentId,
            expectedVersion,
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
