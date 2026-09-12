using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NevaxisFactOS.Persistence.Mold;

internal static class MoldIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    internal static string ComputeCreateFingerprint(
        MoldOperationContext context,
        CreateMoldCommand command,
        string normalizedCode)
    {
        var payload = new
        {
            facilityId = context.FacilityId,
            tenantId = context.TenantId,
            codeNormalized = normalizedCode,
            name = NormalizeOptionalString(command.Name),
            description = NormalizeOptionalString(command.Description),
            serialNumber = NormalizeOptionalString(command.SerialNumber),
            manufacturer = NormalizeOptionalString(command.Manufacturer),
            manufactureYear = command.ManufactureYear,
            manufactureDate = FormatDate(command.ManufactureDate),
            commissioningDate = FormatDate(command.CommissioningDate),
            sourceType = MoldValueConversions.ToStorage(MoldValueConversions.SourceFromStorage(command.SourceType)),
            externalSourceReference = NormalizeOptionalString(command.ExternalSourceReference),
            ownershipType = MoldValueConversions.ToStorage(MoldValueConversions.OwnershipFromStorage(command.OwnershipType)),
            ownerReference = NormalizeOptionalString(command.OwnerReference),
            locationType = MoldValueConversions.ToStorage(MoldValueConversions.LocationFromStorage(command.LocationType)),
            locationLabel = NormalizeOptionalString(command.LocationLabel),
            moldType = NormalizeOptionalString(command.MoldType),
            cavityCount = command.CavityCount,
            nominalShotLife = command.NominalShotLife,
            currentShotCount = command.CurrentShotCount,
            primaryProductReference = NormalizeOptionalString(command.PrimaryProductReference),
            materialFamilyReference = NormalizeOptionalString(command.MaterialFamilyReference),
            runnerType = NormalizeOptionalString(command.RunnerType),
            hotRunnerPresent = command.HotRunnerPresent,
            moldWeightKg = FormatDecimal(command.MoldWeightKg),
            dimensionLengthMm = FormatDecimal(command.DimensionLengthMm),
            dimensionWidthMm = FormatDecimal(command.DimensionWidthMm),
            dimensionHeightMm = FormatDecimal(command.DimensionHeightMm),
            notes = NormalizeOptionalString(command.Notes),
        };

        return HashPayload(payload);
    }

    internal static bool TransitionPayloadMatches(
        MoldLifecycleEvent existing,
        TransitionMoldLifecycleCommand command)
    {
        var targetState = MoldValueConversions.LifecycleFromStorage(command.TargetState);
        return existing.NewState == targetState
            && StringsEqual(existing.Reason, command.Reason)
            && NullableStringsEqual(existing.SourceReference, command.SourceReference);
    }

    internal static bool CounterPayloadMatches(
        MoldCounterSnapshot existing,
        RecordMoldCounterSnapshotCommand command)
    {
        var sourceType = MoldValueConversions.CounterSourceFromStorage(command.SourceType);
        return existing.ShotCount == command.ShotCount
            && existing.SourceType == sourceType
            && NullableStringsEqual(existing.SourceReference, command.SourceReference)
            && NullableStringsEqual(existing.Note, command.Note);
    }

    private static string HashPayload(object payload) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, SerializerOptions))))
            .ToLowerInvariant();

    private static string? NormalizeOptionalString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? FormatDate(DateOnly? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? FormatDecimal(decimal? value) =>
        value?.ToString("G29", CultureInfo.InvariantCulture);

    private static bool StringsEqual(string stored, string incoming) =>
        string.Equals(stored.Trim(), incoming.Trim(), StringComparison.Ordinal);

    private static bool NullableStringsEqual(string? stored, string? incoming) =>
        string.Equals(
            NormalizeOptionalString(stored),
            NormalizeOptionalString(incoming),
            StringComparison.Ordinal);
}
