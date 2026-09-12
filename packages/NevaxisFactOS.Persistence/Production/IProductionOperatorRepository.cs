namespace NevaxisFactOS.Persistence.Production;

/// <summary>
/// Repository for operator assignment/release against production orders.
/// All writes are atomic: assignment + ProductionOrder summary + audit + outbox in one transaction.
/// Primary operator replacement is transactional (old release + new assign + version bump).
/// </summary>
public interface IProductionOperatorRepository
{
    Task<ProductionAssignmentWriteResult> AssignOperatorAsync(
        ProductionOrderOperationContext context,
        AssignOperatorCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionAssignmentWriteResult> ReleaseOperatorAsync(
        ProductionOrderOperationContext context,
        ReleaseOperatorCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionOperationsListResult<OperatorAssignmentReadModel>> ListOperatorAssignmentsAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<OperatorExecutionSummaryResult> GetOperatorExecutionSummaryAsync(
        ProductionOrderOperationContext context,
        string subjectId,
        CancellationToken cancellationToken = default);
}
