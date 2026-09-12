namespace NevaxisFactOS.Persistence.Production;

/// <summary>
/// Write/read contract for production execution exceptions.
/// All writes are atomic with ProductionOrder version increment + audit + outbox.
/// No DELETE operations are permitted.
/// </summary>
public interface IProductionExceptionRepository
{
    Task<ProductionExceptionWriteResult> RaiseExceptionAsync(
        ProductionOrderOperationContext context,
        RaiseExceptionCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionExceptionWriteResult> AcknowledgeExceptionAsync(
        ProductionOrderOperationContext context,
        AcknowledgeExceptionCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionExceptionWriteResult> ResolveExceptionAsync(
        ProductionOrderOperationContext context,
        ResolveExceptionCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionOperationsListResult<ExceptionReadModel>> ListExceptionsAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        int limit,
        CancellationToken cancellationToken = default);

    // 5L: exception ownership routing
    Task<ProductionExceptionWriteResult> AssignExceptionAsync(
        ProductionOrderOperationContext context,
        AssignExceptionCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionExceptionWriteResult> UnassignExceptionAsync(
        ProductionOrderOperationContext context,
        UnassignExceptionCommand command,
        CancellationToken cancellationToken = default);
}
