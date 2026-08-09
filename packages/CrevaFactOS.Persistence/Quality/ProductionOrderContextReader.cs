using Microsoft.EntityFrameworkCore;
using CrevaFactOS.Persistence.Production;

namespace CrevaFactOS.Persistence.Quality;

public interface IProductionOrderContextReader
{
    Task<ProductionOrderQualityContext?> ReadAsync(
        QualityOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default);
}

public sealed class ProductionOrderContextReader(CrevaFactOSDbContext dbContext) : IProductionOrderContextReader
{
    public async Task<ProductionOrderQualityContext?> ReadAsync(
        QualityOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.ProductionOrders.AsNoTracking()
            .SingleOrDefaultAsync(
                o => o.TenantId == context.TenantId
                    && o.FacilityId == context.FacilityId
                    && o.Id == productionOrderId,
                cancellationToken);

        if (order is null)
        {
            return null;
        }

        return new ProductionOrderQualityContext(
            order.Id,
            order.ReferenceCode,
            order.ProductReference,
            ProductionOrderValueConversions.ToStorage(order.Status),
            order.PlannedQuantity,
            order.GoodQuantity,
            order.ScrapQuantity,
            true);
    }
}

public sealed class UnavailableProductionOrderContextReader : IProductionOrderContextReader
{
    public Task<ProductionOrderQualityContext?> ReadAsync(
        QualityOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ProductionOrderQualityContext?>(null);
}
