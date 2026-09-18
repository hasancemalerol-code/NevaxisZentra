namespace NevaxisZentra.Persistence.AiTasks;

public interface IAiTaskPlaybookRepository
{
    Task<IReadOnlyList<AiTaskPlaybookSummaryReadModel>> ListPlaybooksAsync(
        AiTaskOperationContext context,
        CancellationToken cancellationToken = default);

    Task<AiTaskPlaybookDetailReadModel?> GetPlaybookAsync(
        AiTaskOperationContext context,
        Guid playbookId,
        CancellationToken cancellationToken = default);

    Task<AiTaskPlaybookWriteResult> CreatePlaybookAsync(
        AiTaskOperationContext context,
        CreateAiTaskPlaybookCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskPlaybookWriteResult> UpdatePlaybookAsync(
        AiTaskOperationContext context,
        UpdateAiTaskPlaybookCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskPlaybookWriteResult> SetPlaybookEnabledAsync(
        AiTaskOperationContext context,
        Guid playbookId,
        uint expectedVersion,
        bool enabled,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiTaskPlaybookRunSummaryReadModel>> ListRunsAsync(
        AiTaskOperationContext context,
        Guid? playbookId,
        string? statusFilter,
        CancellationToken cancellationToken = default);

    Task<AiTaskPlaybookRunDetailReadModel?> GetRunAsync(
        AiTaskOperationContext context,
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<AiTaskPlaybookWriteResult> StartRunAsync(
        AiTaskOperationContext context,
        StartAiTaskPlaybookRunCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskPlaybookWriteResult> CancelRunAsync(
        AiTaskOperationContext context,
        CancelAiTaskPlaybookRunCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskPlaybookWriteResult> BlockRunStepAsync(
        AiTaskOperationContext context,
        BlockAiTaskPlaybookRunStepCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskPlaybookWriteResult> UnblockRunStepAsync(
        AiTaskOperationContext context,
        UnblockAiTaskPlaybookRunStepCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskPlaybookWriteResult> SkipRunStepAsync(
        AiTaskOperationContext context,
        SkipAiTaskPlaybookRunStepCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskPlaybookGraphValidationResult> ValidateGraphAsync(
        IReadOnlyList<AiTaskPlaybookStepDefinition> steps,
        IReadOnlyList<AiTaskPlaybookDependencyDefinition> dependencies);
}

public interface IAiTaskPlaybookOrchestrator
{
    Task NotifyTaskTransitionAsync(
        AiTaskOperationContext context,
        AiTask task,
        string previousStatus,
        string targetStatus,
        CancellationToken cancellationToken = default);

    Task NotifyTaskReopenedAsync(
        AiTaskOperationContext context,
        AiTask task,
        CancellationToken cancellationToken = default);
}
