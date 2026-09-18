namespace NevaxisZentra.Persistence.ProductionPlanning;

public interface IPlanningLogisticsSourceReader
{
    Task<PlanningLogisticsSourceProjection> ReadForPlanAsync(
        Guid tenantId,
        Guid facilityId,
        ProductionPlan plan,
        CancellationToken cancellationToken = default);
}
