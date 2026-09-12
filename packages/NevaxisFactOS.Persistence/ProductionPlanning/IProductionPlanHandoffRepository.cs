namespace NevaxisFactOS.Persistence.ProductionPlanning;

public interface IProductionPlanHandoffRepository
{
    Task<ProductionPlanHandoffResult> PrepareHandoffAsync(
        ProductionPlanHandoffContext context,
        PrepareProductionPlanHandoffCommand command,
        CancellationToken cancellationToken = default);
}
