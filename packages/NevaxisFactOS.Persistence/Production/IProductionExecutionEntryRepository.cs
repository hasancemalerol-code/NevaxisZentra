namespace NevaxisFactOS.Persistence.Production;

public interface IProductionExecutionEntryRepository
{
    Task<ProductionExecutionEntryWriteResult> RecordGoodAsync(
        ProductionOrderOperationContext context,
        RecordGoodProductionCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionExecutionEntryWriteResult> RecordScrapAsync(
        ProductionOrderOperationContext context,
        RecordScrapCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionExecutionEntryWriteResult> ReverseAsync(
        ProductionOrderOperationContext context,
        ReverseProductionEntryCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionExecutionEntryListResult> ListEntriesAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<ProductionQuantitySummary> GetSummaryAsync(
        Guid tenantId,
        Guid facilityId,
        Guid productionOrderId,
        CancellationToken cancellationToken = default);
}
