namespace CrevaFactOS.Persistence.ProductionPlanning;

public static class PlanningLogisticsExportReadinessNormalizer
{
    public static bool IsPackingReady(Export.ExportPackingStatus packingStatus) =>
        packingStatus is Export.ExportPackingStatus.Complete
            or Export.ExportPackingStatus.Verified;

    public static bool IsShipmentReady(Export.ExportShipmentStatus? shipmentStatus) =>
        shipmentStatus is Export.ExportShipmentStatus.Ready
            or Export.ExportShipmentStatus.ClearedForDispatch
            or Export.ExportShipmentStatus.Dispatched
            or Export.ExportShipmentStatus.Closed
            or Export.ExportShipmentStatus.HandedToBroker;

    public static bool IsExportHandoffReady(
        Export.ExportReadinessStatus readinessStatus,
        Export.ExportPackingStatus packingStatus,
        Export.ExportShipmentStatus? shipmentStatus) =>
        readinessStatus == Export.ExportReadinessStatus.Ready
        && IsPackingReady(packingStatus)
        && (shipmentStatus is null || IsShipmentReady(shipmentStatus));

    public static bool IsExportHandoffBlocked(
        Export.ExportReadinessStatus readinessStatus,
        int blockingIssueCount) =>
        readinessStatus == Export.ExportReadinessStatus.NotReady
        || blockingIssueCount > 0;

    public static string ToStorage(Export.ExportReadinessStatus readinessStatus) =>
        readinessStatus switch
        {
            Export.ExportReadinessStatus.Ready => "ready",
            Export.ExportReadinessStatus.Attention => "attention",
            Export.ExportReadinessStatus.NotReady => "not-ready",
            _ => "unknown",
        };

    public static string? ResolveNormalizedBlockingReason(
        bool exportHandoffBlocked,
        bool packingReady,
        bool shipmentReady) =>
        exportHandoffBlocked
            ? "export-readiness-blocked"
            : !packingReady
                ? "packing-not-ready"
                : !shipmentReady
                    ? "shipment-not-ready"
                    : null;
}
