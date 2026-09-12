namespace NevaxisFactOS.Persistence.AiTasks;

public sealed class UnavailableAiTaskPlaybookRepository : IAiTaskPlaybookRepository, IAiTaskPlaybookOrchestrator
{
    private static AiTaskPlaybookWriteResult Unavailable() =>
        AiTaskPlaybookWriteResult.Fail(AiTaskFailureCodes.Unavailable, "Playbook orchestration is unavailable.");

    public Task<IReadOnlyList<AiTaskPlaybookSummaryReadModel>> ListPlaybooksAsync(
        AiTaskOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AiTaskPlaybookSummaryReadModel>>([]);

    public Task<AiTaskPlaybookDetailReadModel?> GetPlaybookAsync(
        AiTaskOperationContext context,
        Guid playbookId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AiTaskPlaybookDetailReadModel?>(null);

    public Task<AiTaskPlaybookWriteResult> CreatePlaybookAsync(
        AiTaskOperationContext context,
        CreateAiTaskPlaybookCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<AiTaskPlaybookWriteResult> UpdatePlaybookAsync(
        AiTaskOperationContext context,
        UpdateAiTaskPlaybookCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<AiTaskPlaybookWriteResult> SetPlaybookEnabledAsync(
        AiTaskOperationContext context,
        Guid playbookId,
        uint expectedVersion,
        bool enabled,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<IReadOnlyList<AiTaskPlaybookRunSummaryReadModel>> ListRunsAsync(
        AiTaskOperationContext context,
        Guid? playbookId,
        string? statusFilter,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AiTaskPlaybookRunSummaryReadModel>>([]);

    public Task<AiTaskPlaybookRunDetailReadModel?> GetRunAsync(
        AiTaskOperationContext context,
        Guid runId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AiTaskPlaybookRunDetailReadModel?>(null);

    public Task<AiTaskPlaybookWriteResult> StartRunAsync(
        AiTaskOperationContext context,
        StartAiTaskPlaybookRunCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<AiTaskPlaybookWriteResult> CancelRunAsync(
        AiTaskOperationContext context,
        CancelAiTaskPlaybookRunCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<AiTaskPlaybookWriteResult> BlockRunStepAsync(
        AiTaskOperationContext context,
        BlockAiTaskPlaybookRunStepCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<AiTaskPlaybookWriteResult> UnblockRunStepAsync(
        AiTaskOperationContext context,
        UnblockAiTaskPlaybookRunStepCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<AiTaskPlaybookWriteResult> SkipRunStepAsync(
        AiTaskOperationContext context,
        SkipAiTaskPlaybookRunStepCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<AiTaskPlaybookGraphValidationResult> ValidateGraphAsync(
        IReadOnlyList<AiTaskPlaybookStepDefinition> steps,
        IReadOnlyList<AiTaskPlaybookDependencyDefinition> dependencies) =>
        Task.FromResult(new AiTaskPlaybookGraphValidationResult(false, ["Playbook orchestration is unavailable."]));

    public Task NotifyTaskTransitionAsync(
        AiTaskOperationContext context,
        AiTask task,
        string previousStatus,
        string targetStatus,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyTaskReopenedAsync(
        AiTaskOperationContext context,
        AiTask task,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
