namespace NevaxisZentra.Persistence.Production;

/// <summary>
/// Write/read contract for downtime, setup, and manual cycle operational events.
/// All writes are atomic with ProductionOrder version increment + audit + outbox.
/// </summary>
public interface IProductionOperationsRepository
{
    // ── Downtime ──────────────────────────────────────────────────────────────

    Task<ProductionOperationsWriteResult> StartDowntimeAsync(
        ProductionOrderOperationContext context,
        StartDowntimeCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionOperationsWriteResult> EndDowntimeAsync(
        ProductionOrderOperationContext context,
        EndDowntimeCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionOperationsListResult<DowntimeReadModel>> ListDowntimesAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        int limit,
        CancellationToken cancellationToken = default);

    // ── Setup / Changeover ────────────────────────────────────────────────────

    Task<ProductionOperationsWriteResult> StartSetupSessionAsync(
        ProductionOrderOperationContext context,
        StartSetupSessionCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionOperationsWriteResult> CompleteSetupSessionAsync(
        ProductionOrderOperationContext context,
        CompleteSetupSessionCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionOperationsListResult<SetupSessionReadModel>> ListSetupSessionsAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        int limit,
        CancellationToken cancellationToken = default);

    // ── Manual Cycle ──────────────────────────────────────────────────────────

    Task<ProductionOperationsWriteResult> RecordManualCycleAsync(
        ProductionOrderOperationContext context,
        RecordManualCycleCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionOperationsListResult<CycleEntryReadModel>> ListCycleEntriesAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        int limit,
        CancellationToken cancellationToken = default);

    // ── Runtime Summary ───────────────────────────────────────────────────────

    Task<ProductionRuntimeSummaryResult> GetRuntimeSummaryAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default);

    // ── 4L: Recovery ──────────────────────────────────────────────────────────

    Task<ProductionOperationsWriteResult> RecoverDowntimeAsync(
        ProductionOrderOperationContext context,
        RecoverDowntimeCommand command,
        CancellationToken cancellationToken = default);

    Task<ProductionOperationsWriteResult> RecoverSetupSessionAsync(
        ProductionOrderOperationContext context,
        RecoverSetupSessionCommand command,
        CancellationToken cancellationToken = default);

    // ── 4L: Health Summary ────────────────────────────────────────────────────

    Task<ProductionHealthSummaryResult> GetHealthSummaryAsync(
        ProductionOrderOperationContext context,
        Guid productionOrderId,
        ProductionExecutionOptions options,
        CancellationToken cancellationToken = default);
}
