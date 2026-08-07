namespace CrevaFactOS.Persistence.ProductionPlanning;

public interface IProductionPlanTransferRepository
{
    Task<ProductionPlanTransferRequestResult> CreateTransferRequestAsync(
        ProductionPlanTransferContext context,
        CreateTransferRequestCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionPlanTransferExecuteResult> ExecuteTransferAsync(
        ProductionPlanTransferContext context,
        ExecuteTransferCommand command,
        IEnumerable<IProductionPlanTransferAdapter> adapters,
        CancellationToken cancellationToken = default);
}
