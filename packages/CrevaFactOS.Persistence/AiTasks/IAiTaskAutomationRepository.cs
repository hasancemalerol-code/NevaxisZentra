namespace CrevaFactOS.Persistence.AiTasks;

public interface IAiTaskAutomationRepository
{
    Task<IReadOnlyList<AiTaskAutomationRuleSummaryReadModel>> ListRulesAsync(
        AiTaskOperationContext context,
        CancellationToken cancellationToken = default);

    Task<AiTaskAutomationRuleDetailReadModel?> GetRuleAsync(
        AiTaskOperationContext context,
        Guid ruleId,
        CancellationToken cancellationToken = default);

    Task<AiTaskAutomationWriteResult> CreateRuleAsync(
        AiTaskOperationContext context,
        CreateAiTaskAutomationRuleCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskAutomationWriteResult> UpdateRuleAsync(
        AiTaskOperationContext context,
        UpdateAiTaskAutomationRuleCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskAutomationWriteResult> SetRuleEnabledAsync(
        AiTaskOperationContext context,
        Guid ruleId,
        uint expectedVersion,
        bool enabled,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiTaskAutomationRuleRevisionReadModel>> ListRuleHistoryAsync(
        AiTaskOperationContext context,
        Guid ruleId,
        CancellationToken cancellationToken = default);

    Task<AiTaskAutomationEvaluationResult> SimulateAsync(
        AiTaskOperationContext context,
        SimulateAiTaskAutomationCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskAutomationEvaluationResult> EvaluateAndApplyToTaskAsync(
        AiTaskOperationContext context,
        AiTask task,
        string triggerType,
        DateTimeOffset now,
        string actor,
        Guid? correlationId,
        CancellationToken cancellationToken = default);

    Task<AiTaskAutomationEvaluationResult> EvaluateAndApplyAsync(
        AiTaskOperationContext context,
        AiTask task,
        string triggerType,
        DateTimeOffset now,
        string actor,
        Guid? correlationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiTaskAutomationExecutionReadModel>> ListExecutionsAsync(
        AiTaskOperationContext context,
        Guid? taskId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task RecordManualOverrideAsync(
        AiTaskOperationContext context,
        AiTask task,
        IReadOnlyList<string> scopes,
        string reason,
        bool allowAutomationResume,
        DateTimeOffset now,
        string actor,
        CancellationToken cancellationToken = default);
}
