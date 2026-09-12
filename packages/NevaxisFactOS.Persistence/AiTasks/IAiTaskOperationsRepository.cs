namespace NevaxisFactOS.Persistence.AiTasks;

public interface IAiTaskOperationsRepository
{
    Task<AiTaskWriteResult> AssignTaskAsync(
        AiTaskOperationContext context,
        AssignAiTaskCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskWriteResult> AcknowledgeTaskAsync(
        AiTaskOperationContext context,
        AcknowledgeAiTaskCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskWriteResult> ChangePriorityAsync(
        AiTaskOperationContext context,
        ChangeAiTaskPriorityCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskWriteResult> ChangeDueDateAsync(
        AiTaskOperationContext context,
        ChangeAiTaskDueDateCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskWriteResult> EscalateTaskAsync(
        AiTaskOperationContext context,
        EscalateAiTaskCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskEscalationEvaluationResult> EvaluateEscalationsAsync(
        AiTaskOperationContext context,
        int maxTasks,
        CancellationToken cancellationToken = default);

    Task ApplyRoutingOnCreateAsync(
        AiTaskOperationContext context,
        AiTask task,
        DateTimeOffset now,
        string actor,
        CancellationToken cancellationToken = default);
}
