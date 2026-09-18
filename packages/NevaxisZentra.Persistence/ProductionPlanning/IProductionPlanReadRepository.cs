namespace NevaxisZentra.Persistence.ProductionPlanning;

public interface IProductionPlanReadRepository
{
    Task<ProductionPlanListReadResult> ListAsync(
        ProductionPlanListReadInput input,
        CancellationToken cancellationToken = default);

    Task<ProductionPlanDetailReadResult> FindByIdAsync(
        ProductionPlanDetailReadInput input,
        CancellationToken cancellationToken = default);
}
