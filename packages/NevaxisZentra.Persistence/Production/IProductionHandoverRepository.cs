namespace NevaxisZentra.Persistence.Production;

/// <summary>
/// Repository for production shift handover records.
/// Handovers are immutable once created: INSERT only, no UPDATE/DELETE.
/// </summary>
public interface IProductionHandoverRepository
{
    Task<ProductionHandoverWriteResult> CreateHandoverAsync(
        ProductionOrderOperationContext context,
        CreateHandoverCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionOperationsListResult<HandoverReadModel>> ListHandoversAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        int limit,
        CancellationToken cancellationToken = default);
}
