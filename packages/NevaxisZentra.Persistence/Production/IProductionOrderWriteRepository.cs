namespace NevaxisZentra.Persistence.Production;

public interface IProductionOrderWriteRepository
{
    Task<ProductionOrderWriteResult> ConsumeHandoffAsync(
        ProductionOrderOperationContext context,
        ConsumeHandoffCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionOrderWriteResult> StartAsync(
        ProductionOrderOperationContext context,
        StartProductionOrderCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionOrderWriteResult> PauseAsync(
        ProductionOrderOperationContext context,
        PauseProductionOrderCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionOrderWriteResult> ResumeAsync(
        ProductionOrderOperationContext context,
        ResumeProductionOrderCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionOrderWriteResult> CompleteAsync(
        ProductionOrderOperationContext context,
        CompleteProductionOrderCommand command,
        CancellationToken cancellationToken = default);
}
