namespace CrevaFactOS.Persistence.Production;

public interface IProductionPlanningLogisticsSignalReader
{
    Task<ProductionLogisticsRiskSignal> ReadAggregateForFacilityAsync(
        Guid tenantId,
        Guid facilityId,
        IReadOnlyCollection<Guid> linkedPlanIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, ProductionLogisticsRiskSignal>> ReadForPlansAsync(
        Guid tenantId,
        Guid facilityId,
        IReadOnlyCollection<Guid> planIds,
        CancellationToken cancellationToken = default);
}
