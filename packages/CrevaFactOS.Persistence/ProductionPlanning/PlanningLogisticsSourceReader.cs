using System.Text.Json;
using CrevaFactOS.Persistence.Export;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.ProductionPlanning;

public sealed class PlanningLogisticsSourceReader(CrevaFactOSDbContext dbContext)
    : IPlanningLogisticsSourceReader
{
    public async Task<PlanningLogisticsSourceProjection> ReadForPlanAsync(
        Guid tenantId,
        Guid facilityId,
        ProductionPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var orderReferences = ExtractOrderReferences(plan);
        if (orderReferences.Count == 0)
        {
            return NotLinked("not-linked");
        }

        var exportCase = await dbContext.ExportCases
            .AsNoTracking()
            .Where(c =>
                c.TenantId == tenantId
                && c.FacilityId == facilityId
                && (orderReferences.Contains(c.SourceOrderReference)
                    || orderReferences.Contains(c.SourceOrderId.ToString())))
            .OrderByDescending(c => c.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (exportCase is null)
        {
            return NotLinked("not-linked");
        }

        var shipment = await dbContext.ExportShipments
            .AsNoTracking()
            .Where(s =>
                s.TenantId == tenantId
                && s.FacilityId == facilityId
                && s.ExportCaseId == exportCase.Id
                && s.Status != ExportShipmentStatus.Cancelled)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (shipment is null)
        {
            return BuildProjection(
                linked: false,
                unavailableReason: "shipment-context-unavailable",
                exportCase,
                shipment: null);
        }

        return BuildProjection(
            linked: true,
            unavailableReason: null,
            exportCase,
            shipment);
    }

    private static PlanningLogisticsSourceProjection BuildProjection(
        bool linked,
        string? unavailableReason,
        ExportCase exportCase,
        ExportShipment? shipment)
    {
        var packingReady = PlanningLogisticsExportReadinessNormalizer.IsPackingReady(
            exportCase.PackingStatus);
        var shipmentReady = PlanningLogisticsExportReadinessNormalizer.IsShipmentReady(
            shipment?.Status);
        var exportHandoffReady = PlanningLogisticsExportReadinessNormalizer.IsExportHandoffReady(
            exportCase.ReadinessStatus,
            exportCase.PackingStatus,
            shipment?.Status);
        var exportHandoffBlocked = PlanningLogisticsExportReadinessNormalizer.IsExportHandoffBlocked(
            exportCase.ReadinessStatus,
            exportCase.BlockingIssueCount);
        var normalizedBlockingReason =
            PlanningLogisticsExportReadinessNormalizer.ResolveNormalizedBlockingReason(
                exportHandoffBlocked,
                packingReady,
                shipmentReady);

        return new PlanningLogisticsSourceProjection(
            Linked: linked,
            UnavailableReason: unavailableReason,
            ExportCaseId: exportCase.Id,
            ExportCaseNumber: exportCase.CaseNumber,
            ShipmentId: shipment?.Id,
            ShipmentReference: shipment?.ShipmentReference,
            SourceOrderReference: exportCase.SourceOrderReference,
            PlannedPickupAtUtc: shipment?.PlannedPickupAtUtc,
            PlannedDepartureAtUtc: shipment?.PlannedDepartureAtUtc,
            ExportReadinessStatus: PlanningLogisticsExportReadinessNormalizer.ToStorage(
                exportCase.ReadinessStatus),
            ExportHandoffReady: exportHandoffReady,
            ExportHandoffBlocked: exportHandoffBlocked,
            PackingReady: packingReady,
            ShipmentReady: shipmentReady,
            NormalizedBlockingReason: normalizedBlockingReason,
            SourceVersion: shipment is null
                ? exportCase.Version
                : Math.Max(exportCase.Version, shipment.Version),
            SourceUpdatedAtUtc: shipment is null
                ? exportCase.UpdatedAtUtc
                : shipment.UpdatedAtUtc > exportCase.UpdatedAtUtc
                    ? shipment.UpdatedAtUtc
                    : exportCase.UpdatedAtUtc);
    }

    private static PlanningLogisticsSourceProjection NotLinked(string reason) =>
        new(
            Linked: false,
            UnavailableReason: reason,
            ExportCaseId: null,
            ExportCaseNumber: null,
            ShipmentId: null,
            ShipmentReference: null,
            SourceOrderReference: null,
            PlannedPickupAtUtc: null,
            PlannedDepartureAtUtc: null,
            ExportReadinessStatus: "unknown",
            ExportHandoffReady: false,
            ExportHandoffBlocked: false,
            PackingReady: false,
            ShipmentReady: false,
            NormalizedBlockingReason: null,
            SourceVersion: 0,
            SourceUpdatedAtUtc: DateTimeOffset.MinValue);

    internal static HashSet<string> ExtractOrderReferences(ProductionPlan plan)
    {
        var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        TryAddDemandReferences(plan.DemandsJson, references);
        TryAddSourceReferences(plan.SourceReferencesJson, references);

        return references;
    }

    private static void TryAddDemandReferences(string demandsJson, HashSet<string> references)
    {
        if (string.IsNullOrWhiteSpace(demandsJson))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(demandsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var demand in document.RootElement.EnumerateArray())
            {
                if (demand.TryGetProperty("orderReference", out var orderReference)
                    && orderReference.ValueKind == JsonValueKind.String)
                {
                    var value = orderReference.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        references.Add(value.Trim());
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Ignore malformed pilot JSON.
        }
    }

    private static void TryAddSourceReferences(string sourceReferencesJson, HashSet<string> references)
    {
        if (string.IsNullOrWhiteSpace(sourceReferencesJson))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(sourceReferencesJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var source in document.RootElement.EnumerateArray())
            {
                if (source.TryGetProperty("sourceId", out var sourceId)
                    && sourceId.ValueKind == JsonValueKind.String)
                {
                    var value = sourceId.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        references.Add(value.Trim());
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Ignore malformed pilot JSON.
        }
    }
}
