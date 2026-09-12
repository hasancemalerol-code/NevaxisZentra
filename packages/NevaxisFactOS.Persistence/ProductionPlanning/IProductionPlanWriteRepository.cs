namespace NevaxisFactOS.Persistence.ProductionPlanning;

public interface IProductionPlanWriteRepository
{
    Task<ProductionPlanWriteResult> CreateDraftAsync(
        ProductionPlanWriteContext context,
        CreateProductionPlanDraftCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionPlanWriteResult> UpdateDraftAsync(
        ProductionPlanWriteContext context,
        UpdateProductionPlanDraftCommand command,
        CancellationToken cancellationToken = default);
}
