namespace NevaxisZentra.Persistence.Production;

/// <summary>
/// Repository for shift assignment operations against production orders.
/// All writes are atomic: assignment + ProductionOrder summary + audit + outbox in one transaction.
/// </summary>
public interface IProductionShiftRepository
{
    /// <summary>
    /// Assigns a shift to a production order.
    /// Fails if an active shift assignment already exists — use ReassignShiftAsync instead.
    /// </summary>
    Task<ProductionAssignmentWriteResult> AssignShiftAsync(
        ProductionOrderOperationContext context,
        AssignShiftCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases the current active shift assignment and creates a new one atomically.
    /// Requires an explicit replacement reason.
    /// </summary>
    Task<ProductionAssignmentWriteResult> ReassignShiftAsync(
        ProductionOrderOperationContext context,
        ReassignShiftCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionOperationsListResult<ShiftAssignmentReadModel>> ListShiftAssignmentsAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<ShiftExecutionSummaryResult> GetShiftExecutionSummaryAsync(
        ProductionOrderOperationContext context,
        Guid productionShiftId,
        DateOnly shiftDate,
        CancellationToken cancellationToken = default);
}
