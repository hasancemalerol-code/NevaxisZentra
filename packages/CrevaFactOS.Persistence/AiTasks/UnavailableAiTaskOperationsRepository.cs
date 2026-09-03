namespace CrevaFactOS.Persistence.AiTasks;

public sealed class UnavailableAiTaskOperationsRepository : IAiTaskOperationsRepository
{
    public Task<AiTaskWriteResult> AssignTaskAsync(
        AiTaskOperationContext context,
        AssignAiTaskCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AiTaskWriteResult.Fail(AiTaskFailureCodes.Unavailable, "AI task operations are unavailable."));

    public Task<AiTaskWriteResult> AcknowledgeTaskAsync(
        AiTaskOperationContext context,
        AcknowledgeAiTaskCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AiTaskWriteResult.Fail(AiTaskFailureCodes.Unavailable, "AI task operations are unavailable."));

    public Task<AiTaskWriteResult> ChangePriorityAsync(
        AiTaskOperationContext context,
        ChangeAiTaskPriorityCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AiTaskWriteResult.Fail(AiTaskFailureCodes.Unavailable, "AI task operations are unavailable."));

    public Task<AiTaskWriteResult> ChangeDueDateAsync(
        AiTaskOperationContext context,
        ChangeAiTaskDueDateCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AiTaskWriteResult.Fail(AiTaskFailureCodes.Unavailable, "AI task operations are unavailable."));

    public Task<AiTaskWriteResult> EscalateTaskAsync(
        AiTaskOperationContext context,
        EscalateAiTaskCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AiTaskWriteResult.Fail(AiTaskFailureCodes.Unavailable, "AI task operations are unavailable."));

    public Task<AiTaskEscalationEvaluationResult> EvaluateEscalationsAsync(
        AiTaskOperationContext context,
        int maxTasks,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiTaskEscalationEvaluationResult(0, 0, []));

    public Task ApplyRoutingOnCreateAsync(
        AiTaskOperationContext context,
        AiTask task,
        DateTimeOffset now,
        string actor,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
