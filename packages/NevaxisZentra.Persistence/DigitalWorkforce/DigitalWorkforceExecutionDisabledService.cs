namespace NevaxisZentra.Persistence.DigitalWorkforce;

public static class DigitalWorkforceExecutionSettings
{
    public const string ConfigurationKey = "DigitalWorkforce:ExecutionEnabled";

    public const string DisabledErrorCode = "digital-workforce.execution-disabled";

    /// <summary>
    /// The execution runtime still runs stub tools that report success without performing the action, so it is on only in
    /// Development unless an operator enables it explicitly.
    /// </summary>
    public static bool IsEnabled(bool isDevelopment, bool? configured) => isDevelopment || configured == true;
}

/// <summary>
/// Fail-closed stand-in for the execution runtime. Nothing is queued, claimed, executed or reported as done, so a stub
/// tool can never produce a fake success. Stopping work (pause, cancel) and reading state still reach the real service.
/// </summary>
public sealed class DigitalWorkforceExecutionDisabledService(
    DigitalWorkforceExecutionRuntimeService inner) : IDigitalWorkforceExecutionRuntimeService
{
    public Task<EnqueueDigitalWorkExecutionResult> EnqueueAsync(
        EnqueueDigitalWorkExecutionRequest request,
        string subjectId,
        CancellationToken cancellationToken) =>
        Task.FromResult(new EnqueueDigitalWorkExecutionResult(
            false,
            false,
            null,
            DigitalWorkforceExecutionSettings.DisabledErrorCode,
            "Dijital iş gücü yürütmesi bu kurulumda etkin değil."));

    public Task<DigitalWorkExecutionProcessingResult> ProcessBatchAsync(
        string owner,
        int batchSize,
        CancellationToken cancellationToken) =>
        Task.FromResult(new DigitalWorkExecutionProcessingResult(0, 0, 0, 0));

    public Task<DigitalWorkExecutionProcessingResult> ReclaimExpiredLeasesAsync(
        string owner,
        int batchSize,
        CancellationToken cancellationToken) =>
        Task.FromResult(new DigitalWorkExecutionProcessingResult(0, 0, 0, 0));

    public Task ResumeAfterApprovalAsync(Guid approvalId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task CancelWorkerExecutionsAsync(Guid workerId, string reason, CancellationToken cancellationToken) =>
        inner.CancelWorkerExecutionsAsync(workerId, reason, cancellationToken);

    public Task PauseWorkerExecutionsAsync(Guid workerId, CancellationToken cancellationToken) =>
        inner.PauseWorkerExecutionsAsync(workerId, cancellationToken);

    public Task<IReadOnlyList<DigitalWorkExecutionSummary>> ListExecutionsAsync(
        Guid? caseId,
        Guid? workerId,
        string? status,
        int limit,
        CancellationToken cancellationToken) =>
        inner.ListExecutionsAsync(caseId, workerId, status, limit, cancellationToken);

    public Task<DigitalWorkerHealthSnapshot> GetWorkerHealthAsync(Guid workerId, CancellationToken cancellationToken) =>
        inner.GetWorkerHealthAsync(workerId, cancellationToken);
}
