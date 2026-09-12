namespace NevaxisFactOS.Persistence.ProductionPlanning;

public sealed class UnavailablePlanningLogisticsSourceReader : IPlanningLogisticsSourceReader
{
    public Task<PlanningLogisticsSourceProjection> ReadForPlanAsync(
        Guid tenantId,
        Guid facilityId,
        ProductionPlan plan,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PlanningLogisticsSourceProjection(
            Linked: false,
            UnavailableReason: "source-unavailable",
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
            SourceUpdatedAtUtc: DateTimeOffset.MinValue));
}
