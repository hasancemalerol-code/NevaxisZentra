namespace CrevaFactOS.Persistence.Production;

public sealed class UnavailableProductionPlanningLogisticsSignalReader : IProductionPlanningLogisticsSignalReader
{
    private static readonly ProductionLogisticsRiskSignal UnavailableSignal =
        new("none", 0, false, "planning-logistics-unavailable", null, Available: false);

    public Task<ProductionLogisticsRiskSignal> ReadAggregateForFacilityAsync(
        Guid tenantId,
        Guid facilityId,
        IReadOnlyCollection<Guid> linkedPlanIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableSignal);

    public Task<IReadOnlyDictionary<Guid, ProductionLogisticsRiskSignal>> ReadForPlansAsync(
        Guid tenantId,
        Guid facilityId,
        IReadOnlyCollection<Guid> planIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, ProductionLogisticsRiskSignal>>(new Dictionary<Guid, ProductionLogisticsRiskSignal>());
}
