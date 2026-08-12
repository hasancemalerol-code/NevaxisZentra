using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CrevaFactOS.Persistence.Material;

internal static class MaterialRequirementIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    internal static string ComputeProfileCreateFingerprint(
        MaterialOperationContext context,
        CreateMaterialRequirementProfileCommand command) =>
        HashPayload(new
        {
            tenantId = context.TenantId,
            facilityId = context.FacilityId,
            materialId = command.MaterialId,
            requirementType = MaterialRequirementValueConversions.ToStorage(
                MaterialRequirementValueConversions.RequirementTypeFromStorage(command.RequirementType)),
            productionOrderReference = NormalizeOptionalString(command.ProductionOrderReference),
            productReference = NormalizeOptionalString(command.ProductReference),
            moldReference = NormalizeOptionalString(command.MoldReference),
            plannedProductionQuantity = command.PlannedProductionQuantity,
            goodPartTargetQuantity = command.GoodPartTargetQuantity,
            partWeightGrams = command.PartWeightGrams,
            runnerWeightGramsPerShot = command.RunnerWeightGramsPerShot,
            effectiveCavityCount = command.EffectiveCavityCount,
            setupAllowanceGrams = command.SetupAllowanceGrams,
            processLossAllowancePercent = command.ProcessLossAllowancePercent,
            plannedVirginPercent = command.PlannedVirginPercent,
            plannedRegrindPercent = command.PlannedRegrindPercent,
            sourceType = MaterialValueConversions.ToStorage(
                MaterialValueConversions.SourceFromStorage(command.SourceType)),
            sourceReference = NormalizeOptionalString(command.SourceReference),
            dataRecordedAtUtc = FormatDateTime(command.DataRecordedAtUtc),
        });

    internal static string ComputeObservationFingerprint(
        MaterialOperationContext context,
        CreateMaterialConsumptionObservationCommand command) =>
        HashPayload(new
        {
            tenantId = context.TenantId,
            facilityId = context.FacilityId,
            materialId = command.MaterialId,
            materialLotId = command.MaterialLotId,
            requirementProfileId = command.RequirementProfileId,
            observationType = MaterialRequirementValueConversions.ToStorage(
                MaterialRequirementValueConversions.ObservationTypeFromStorage(command.ObservationType)),
            quantity = command.Quantity,
            unitOfMeasure = command.UnitOfMeasure.Trim(),
            virginQuantity = command.VirginQuantity,
            regrindQuantity = command.RegrindQuantity,
            goodPartQuantity = command.GoodPartQuantity,
            scrapQuantity = command.ScrapQuantity,
            productionOrderReference = NormalizeOptionalString(command.ProductionOrderReference),
            productReference = NormalizeOptionalString(command.ProductReference),
            moldReference = NormalizeOptionalString(command.MoldReference),
            observedAtUtc = FormatDateTime(command.ObservedAtUtc),
            sourceType = MaterialValueConversions.ToStorage(
                MaterialValueConversions.SourceFromStorage(command.SourceType)),
            sourceReference = NormalizeOptionalString(command.SourceReference),
            notes = NormalizeOptionalString(command.Notes),
        });

    internal static string ComputeAssessmentInputFingerprint(
        MaterialRequirementAssessmentType assessmentType,
        Guid materialId,
        Guid? requirementProfileId,
        string ruleSetVersion,
        string normalizedInputJson) =>
        HashPayload(new
        {
            assessmentType = MaterialRequirementValueConversions.ToStorage(assessmentType),
            materialId,
            requirementProfileId,
            ruleSetVersion,
            input = normalizedInputJson,
        });

    internal static bool ObservationPayloadMatches(
        MaterialConsumptionObservation existing,
        CreateMaterialConsumptionObservationCommand command,
        string fingerprint) =>
        existing.PayloadFingerprint == fingerprint
        && existing.Quantity == command.Quantity
        && existing.ObservationType == MaterialRequirementValueConversions.ObservationTypeFromStorage(command.ObservationType);

    private static string HashPayload(object payload) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, SerializerOptions))))
            .ToLowerInvariant();

    private static string? NormalizeOptionalString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatDateTime(DateTimeOffset value) =>
        value.ToString("O", CultureInfo.InvariantCulture);

    private static string? FormatDateTime(DateTimeOffset? value) =>
        value?.ToString("O", CultureInfo.InvariantCulture);
}
