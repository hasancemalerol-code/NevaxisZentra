using CrevaFactOS.Persistence.Erp;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Development;

internal static class DevelopmentPilotErpPurchaseOrderSeed
{
    internal static async Task EnsureErpPurchaseOrderPilotRecordsAsync(
        CrevaFactOSDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.CanonicalErpPurchaseOrders.AsNoTracking()
                .AnyAsync(p => p.Id == DevelopmentPilotIds.ErpPilotPurchaseOrderOverdueId, cancellationToken))
        {
            return;
        }

        var sourceUpdated = seedTimestampUtc.AddHours(-2);
        var overdueCommitment = seedTimestampUtc.AddDays(-14);
        var deliveryRiskCommitment = seedTimestampUtc.AddDays(14);
        var onTrackCommitment = seedTimestampUtc.AddDays(30);

        dbContext.CanonicalErpPurchaseOrders.AddRange(
            BuildPurchaseOrder(
                DevelopmentPilotIds.ErpPilotPurchaseOrderOverdueId,
                tenantId,
                facilityId,
                "PILOT-PO-OVERDUE",
                "SUP-OVERDUE",
                "Gecikmiş Tedarikçi A.Ş.",
                overdueCommitment,
                ErpPurchaseOrderStatuses.Open,
                sourceUpdated,
                seedTimestampUtc),
            BuildPurchaseOrder(
                DevelopmentPilotIds.ErpPilotPurchaseOrderDeliveryRiskId,
                tenantId,
                facilityId,
                "PILOT-PO-PARTIAL",
                "SUP-PARTIAL",
                "Kısmi Teslim Tedarikçi Ltd.",
                deliveryRiskCommitment,
                ErpPurchaseOrderStatuses.PartiallyReceived,
                sourceUpdated,
                seedTimestampUtc),
            BuildPurchaseOrder(
                DevelopmentPilotIds.ErpPilotPurchaseOrderInsufficientId,
                tenantId,
                facilityId,
                "PILOT-PO-UNKNOWN",
                "SUP-UNKNOWN",
                "Eksik Veri Tedarikçi",
                null,
                ErpPurchaseOrderStatuses.Open,
                sourceUpdated,
                seedTimestampUtc),
            BuildPurchaseOrder(
                DevelopmentPilotIds.ErpPilotPurchaseOrderOnTrackId,
                tenantId,
                facilityId,
                "PILOT-PO-ONTRACK",
                "SUP-ONTRACK",
                "Zamanında Tedarikçi San.",
                onTrackCommitment,
                ErpPurchaseOrderStatuses.Open,
                sourceUpdated,
                seedTimestampUtc));

        dbContext.CanonicalErpPurchaseOrderLines.AddRange(
            BuildLine(
                DevelopmentPilotIds.ErpPilotPurchaseOrderOverdueLineId,
                DevelopmentPilotIds.ErpPilotPurchaseOrderOverdueId,
                tenantId,
                facilityId,
                1,
                overdueCommitment,
                ordered: 1000m,
                received: 0m,
                open: 1000m),
            BuildLine(
                DevelopmentPilotIds.ErpPilotPurchaseOrderDeliveryRiskLineId,
                DevelopmentPilotIds.ErpPilotPurchaseOrderDeliveryRiskId,
                tenantId,
                facilityId,
                1,
                deliveryRiskCommitment,
                ordered: 2000m,
                received: 800m,
                open: 1200m),
            BuildLine(
                DevelopmentPilotIds.ErpPilotPurchaseOrderInsufficientLineId,
                DevelopmentPilotIds.ErpPilotPurchaseOrderInsufficientId,
                tenantId,
                facilityId,
                1,
                commitment: null,
                ordered: 500m,
                received: null,
                open: null),
            BuildLine(
                DevelopmentPilotIds.ErpPilotPurchaseOrderOnTrackLineId,
                DevelopmentPilotIds.ErpPilotPurchaseOrderOnTrackId,
                tenantId,
                facilityId,
                1,
                onTrackCommitment,
                ordered: 750m,
                received: 0m,
                open: 750m));
    }

    private static CanonicalErpPurchaseOrder BuildPurchaseOrder(
        Guid id,
        Guid tenantId,
        Guid facilityId,
        string number,
        string supplierCode,
        string supplierName,
        DateTimeOffset? expectedDelivery,
        string status,
        DateTimeOffset sourceUpdated,
        DateTimeOffset seedTimestampUtc) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            ConnectionProfileId = DevelopmentPilotIds.ErpPilotSimulatorConnectionId,
            Provider = ErpProviderKeys.Simulator,
            ExternalId = number,
            ExternalCode = number,
            PurchaseOrderNumber = number,
            ExternalSupplierId = supplierCode,
            ExternalSupplierCode = supplierCode,
            SupplierName = supplierName,
            OrderDateUtc = seedTimestampUtc.AddDays(-30),
            ExpectedDeliveryDateUtc = expectedDelivery,
            Status = status,
            CurrencyCode = "TRY",
            SourceVersion = "pilot-v1",
            SourceUpdatedAtUtc = sourceUpdated,
            ImportedAtUtc = seedTimestampUtc,
            UpdatedAtUtc = seedTimestampUtc,
        };

    private static CanonicalErpPurchaseOrderLine BuildLine(
        Guid id,
        Guid purchaseOrderId,
        Guid tenantId,
        Guid facilityId,
        int lineNumber,
        DateTimeOffset? commitment,
        decimal ordered,
        decimal? received,
        decimal? open) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            ConnectionProfileId = DevelopmentPilotIds.ErpPilotSimulatorConnectionId,
            PurchaseOrderId = purchaseOrderId,
            ExternalLineId = $"{purchaseOrderId:N}-L{lineNumber}",
            LineNumber = lineNumber,
            ExternalProductId = "PILOT-RAW-001",
            ExternalProductCode = "PILOT-RAW-001",
            Description = "Pilot hammadde satırı",
            OrderedQuantity = ordered,
            ReceivedQuantity = received,
            OpenQuantity = open,
            Unit = "KG",
            ExpectedDeliveryDateUtc = commitment,
            SourceVersion = "pilot-v1",
        };
}
