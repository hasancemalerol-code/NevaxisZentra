using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NevaxisFactOS.Persistence.Material;

internal static class MaterialIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    internal static string ComputeCreateFingerprint(
        MaterialOperationContext context,
        CreateMaterialCommand command,
        string normalizedCode)
    {
        var payload = new
        {
            facilityId = context.FacilityId,
            tenantId = context.TenantId,
            codeNormalized = normalizedCode,
            name = NormalizeOptionalString(command.Name),
            description = NormalizeOptionalString(command.Description),
            materialCategory = MaterialValueConversions.ToStorage(
                MaterialValueConversions.CategoryFromStorage(command.MaterialCategory)),
            polymerFamily = MaterialValueConversions.ToStorage(
                MaterialValueConversions.PolymerFromStorage(command.PolymerFamily)),
            manufacturer = NormalizeOptionalString(command.Manufacturer),
            tradeNameOrGrade = NormalizeOptionalString(command.TradeNameOrGrade),
            colorName = NormalizeOptionalString(command.ColorName),
            colorCode = NormalizeOptionalString(command.ColorCode),
            physicalForm = MaterialValueConversions.ToStorage(
                MaterialValueConversions.PhysicalFormFromStorage(command.PhysicalForm)),
            unitOfMeasure = NormalizeOptionalString(command.UnitOfMeasure),
            isActive = command.IsActive,
            sourceType = MaterialValueConversions.ToStorage(
                MaterialValueConversions.SourceFromStorage(command.SourceType)),
            sourceSystem = NormalizeOptionalString(command.SourceSystem),
            sourceReference = NormalizeOptionalString(command.SourceReference),
            externalMaterialCode = NormalizeOptionalString(command.ExternalMaterialCode),
            dataRecordedAtUtc = FormatDateTime(command.DataRecordedAtUtc),
        };

        return HashPayload(payload);
    }

    internal static bool TransitionPayloadMatches(
        MaterialLifecycleEvent existing,
        TransitionMaterialLifecycleCommand command)
    {
        var targetStatus = MaterialValueConversions.LifecycleFromStorage(command.TargetStatus);
        return existing.NewStatus == targetStatus
            && StringsEqual(existing.Reason, command.Reason)
            && NullableStringsEqual(existing.SourceReference, command.SourceReference);
    }

    private static string HashPayload(object payload) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, SerializerOptions))))
            .ToLowerInvariant();

    private static string? NormalizeOptionalString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? FormatDateTime(DateTimeOffset? value) =>
        value?.ToString("O", CultureInfo.InvariantCulture);

    private static bool StringsEqual(string stored, string incoming) =>
        string.Equals(stored.Trim(), incoming.Trim(), StringComparison.Ordinal);

    private static bool NullableStringsEqual(string? stored, string? incoming) =>
        string.Equals(
            NormalizeOptionalString(stored),
            NormalizeOptionalString(incoming),
            StringComparison.Ordinal);
}
