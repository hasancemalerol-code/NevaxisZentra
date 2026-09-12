using NevaxisFactOS.Persistence.Material;

namespace NevaxisFactOS.Persistence.Integration.Projections;

public sealed record IntegrationProjectionWriteResult(
    bool Success,
    bool Created,
    bool Updated,
    bool Unchanged,
    bool Duplicate,
    Guid? ProjectionId,
    string? ErrorCode,
    string? SafeMessage);

public interface IIntegrationMaterialProjectionWriter
{
    Task<IntegrationProjectionWriteResult> UpsertAsync(
        MaterialOperationContext context,
        IReadOnlyDictionary<string, string?> mappedValues,
        string sourceSystem,
        string sourceReference,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class IntegrationMaterialProjectionWriter(
    IMaterialRepository materialRepository) : IIntegrationMaterialProjectionWriter
{
    public async Task<IntegrationProjectionWriteResult> UpsertAsync(
        MaterialOperationContext context,
        IReadOnlyDictionary<string, string?> mappedValues,
        string sourceSystem,
        string sourceReference,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (!mappedValues.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
        {
            return Fail(MaterialFailureCodes.ValidationFailed, "Material code is required.");
        }

        if (!mappedValues.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name))
        {
            return Fail(MaterialFailureCodes.ValidationFailed, "Material name is required.");
        }

        var command = new CreateMaterialCommand(
            code,
            name,
            GetOptional(mappedValues, "description"),
            GetOptional(mappedValues, "materialcategory") ?? "other",
            GetOptional(mappedValues, "polymerfamily") ?? "unknown",
            GetOptional(mappedValues, "manufacturer"),
            GetOptional(mappedValues, "tradenameorgrade"),
            GetOptional(mappedValues, "colorname"),
            GetOptional(mappedValues, "colorcode"),
            GetOptional(mappedValues, "physicalform") ?? "granule",
            GetOptional(mappedValues, "unitofmeasure") ?? "kg",
            ParseBool(GetOptional(mappedValues, "isactive"), true),
            GetOptional(mappedValues, "sourcetype") ?? "imported",
            sourceSystem,
            sourceReference,
            GetOptional(mappedValues, "externalmaterialcode") ?? code,
            DateTimeOffset.UtcNow,
            idempotencyKey);

        var result = await materialRepository.CreateAsync(context, command, cancellationToken);
        if (!result.Succeeded)
        {
            if (result.FailureCode == MaterialFailureCodes.DuplicateCode)
            {
                return new IntegrationProjectionWriteResult(
                    true,
                    false,
                    false,
                    true,
                    true,
                    null,
                    null,
                    null);
            }

            return Fail(result.FailureCode ?? MaterialFailureCodes.ValidationFailed, result.FailureReason ?? "Material write failed.");
        }

        return new IntegrationProjectionWriteResult(
            true,
            !result.IdempotentReplay,
            result.IdempotentReplay,
            result.IdempotentReplay,
            result.IdempotentReplay,
            result.EntityId,
            null,
            null);
    }

    private static string? GetOptional(IReadOnlyDictionary<string, string?> values, string key) =>
        values.TryGetValue(key, out var value) ? value : null;

    private static bool ParseBool(string? value, bool defaultValue) =>
        value switch
        {
            null or "" => defaultValue,
            "1" or "yes" or "true" => true,
            "0" or "no" or "false" => false,
            _ => bool.TryParse(value, out var parsed) ? parsed : defaultValue,
        };

    private static IntegrationProjectionWriteResult Fail(string errorCode, string message) =>
        new(false, false, false, false, false, null, errorCode, message);
}
