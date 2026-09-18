using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NevaxisZentra.Persistence.Mold;

internal static class MoldHistoryIdempotencyFingerprint
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    internal static string ComputeMaintenanceCreateFingerprint(
        MoldOperationContext context,
        CreateMoldMaintenanceCommand command) =>
        ComputeHash(new
        {
            kind = "maintenance-create",
            facilityId = context.FacilityId,
            tenantId = context.TenantId,
            moldId = command.MoldId,
            maintenanceType = MoldHistoryValueConversions.ToStorage(
                MoldHistoryValueConversions.MaintenanceTypeFromStorage(command.MaintenanceType)),
            status = MoldHistoryValueConversions.ToStorage(
                MoldHistoryValueConversions.MaintenanceStatusFromStorage(command.Status)),
            openedAtUtc = command.OpenedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            summary = NormalizeOptionalString(command.Summary),
            findings = NormalizeOptionalString(command.Findings),
            actionsTaken = NormalizeOptionalString(command.ActionsTaken),
            performedByDisplayName = NormalizeOptionalString(command.PerformedByDisplayName),
            externalMaintenanceRequestReference = NormalizeOptionalString(command.ExternalMaintenanceRequestReference),
            externalWorkOrderReference = NormalizeOptionalString(command.ExternalWorkOrderReference),
            sourceType = MoldValueConversions.ToStorage(MoldValueConversions.SourceFromStorage(command.SourceType)),
            sourceReference = NormalizeOptionalString(command.SourceReference),
        });

    internal static string ComputeRevisionAppendFingerprint(
        MoldOperationContext context,
        AppendMoldRevisionCommand command,
        string revisionNumberNormalized) =>
        ComputeHash(new
        {
            kind = "revision-append",
            facilityId = context.FacilityId,
            tenantId = context.TenantId,
            moldId = command.MoldId,
            revisionNumberNormalized,
            revisionType = MoldHistoryValueConversions.ToStorage(
                MoldHistoryValueConversions.RevisionTypeFromStorage(command.RevisionType)),
            revisionDate = command.RevisionDate.ToString("O", CultureInfo.InvariantCulture),
            summary = NormalizeOptionalString(command.Summary),
            reason = NormalizeOptionalString(command.Reason),
            technicalChange = NormalizeOptionalString(command.TechnicalChange),
            previousCavityCount = command.PreviousCavityCount,
            newCavityCount = command.NewCavityCount,
            performedByDisplayName = NormalizeOptionalString(command.PerformedByDisplayName),
            externalReference = NormalizeOptionalString(command.ExternalReference),
            sourceType = MoldValueConversions.ToStorage(MoldValueConversions.SourceFromStorage(command.SourceType)),
            sourceReference = NormalizeOptionalString(command.SourceReference),
        });

    internal static string ComputeTrialAppendFingerprint(
        MoldOperationContext context,
        AppendMoldTrialCommand command,
        string trialNumberNormalized) =>
        ComputeHash(new
        {
            kind = "trial-append",
            facilityId = context.FacilityId,
            tenantId = context.TenantId,
            moldId = command.MoldId,
            trialNumberNormalized,
            trialDate = command.TrialDate.ToString("O", CultureInfo.InvariantCulture),
            result = MoldHistoryValueConversions.ToStorage(
                MoldHistoryValueConversions.TrialResultFromStorage(command.Result)),
            machineReference = NormalizeOptionalString(command.MachineReference),
            productionOrderReference = NormalizeOptionalString(command.ProductionOrderReference),
            productReference = NormalizeOptionalString(command.ProductReference),
            materialReference = NormalizeOptionalString(command.MaterialReference),
            cycleTimeSeconds = FormatDecimal(command.CycleTimeSeconds),
            shotCount = command.ShotCount,
            goodPartCount = command.GoodPartCount,
            scrapPartCount = command.ScrapPartCount,
            findings = NormalizeOptionalString(command.Findings),
            decision = NormalizeOptionalString(command.Decision),
            performedByDisplayName = NormalizeOptionalString(command.PerformedByDisplayName),
            sourceType = MoldValueConversions.ToStorage(MoldValueConversions.SourceFromStorage(command.SourceType)),
            sourceReference = NormalizeOptionalString(command.SourceReference),
        });

    private static string ComputeHash(object payload)
    {
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? NormalizeOptionalString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string? FormatDecimal(decimal? value) =>
        value?.ToString("0.###", CultureInfo.InvariantCulture);
}
