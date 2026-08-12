using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CrevaFactOS.Persistence.ShiftWorkforce;

internal static class ShiftWorkforceIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    internal static string ComputeCreateCompetencyFingerprint(
        ShiftWorkforceOperationContext context,
        Guid personId,
        CreateShiftWorkforceCompetencyCommand command,
        Guid competencyDefinitionId) =>
        HashPayload(new
        {
            facilityId = context.FacilityId,
            tenantId = context.TenantId,
            workforcePersonId = personId,
            competencyDefinitionId,
            machineFamilyScope = NormalizeOptionalString(command.MachineFamilyScope),
            processScope = NormalizeOptionalString(command.ProcessScope),
            materialScope = NormalizeOptionalString(command.MaterialScope),
            equipmentScope = NormalizeOptionalString(command.EquipmentScope),
            competencyLevel = command.CompetencyLevel.Trim(),
            verificationStatus = command.VerificationStatus.Trim(),
            validFromUtc = FormatDateTime(command.ValidFromUtc),
            validUntilUtc = FormatDateTime(command.ValidUntilUtc),
            operationalNote = NormalizeOptionalString(command.OperationalNote),
        });

    private static string? NormalizeOptionalString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? FormatDateTime(DateTimeOffset? value) =>
        value?.ToString("O", CultureInfo.InvariantCulture);

    private static string HashPayload(object payload)
    {
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
