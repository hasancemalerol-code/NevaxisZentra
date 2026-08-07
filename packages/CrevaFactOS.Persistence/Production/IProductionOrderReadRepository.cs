namespace CrevaFactOS.Persistence.Production;

public interface IProductionOrderReadRepository
{
    Task<ProductionOrderListResult> ListAsync(
        ProductionOrderOperationContext context,
        string? statusFilter,
        string? searchFilter,
        int? limit,
        CancellationToken cancellationToken = default);

    Task<ProductionOrderDetailResult> FindByIdAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default);
}
