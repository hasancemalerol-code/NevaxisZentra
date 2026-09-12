namespace NevaxisFactOS.Persistence.AiTasks;

public sealed class UnavailableAiTaskAutomationRepository : IAiTaskAutomationRepository
{
    public Task<IReadOnlyList<AiTaskAutomationRuleSummaryReadModel>> ListRulesAsync(
        AiTaskOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AiTaskAutomationRuleSummaryReadModel>>([]);

    public Task<AiTaskAutomationRuleDetailReadModel?> GetRuleAsync(
        AiTaskOperationContext context,
        Guid ruleId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AiTaskAutomationRuleDetailReadModel?>(null);

    public Task<AiTaskAutomationWriteResult> CreateRuleAsync(
        AiTaskOperationContext context,
        CreateAiTaskAutomationRuleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AiTaskAutomationWriteResult.Fail(AiTaskFailureCodes.Unavailable, "Automation is unavailable."));

    public Task<AiTaskAutomationWriteResult> UpdateRuleAsync(
        AiTaskOperationContext context,
        UpdateAiTaskAutomationRuleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AiTaskAutomationWriteResult.Fail(AiTaskFailureCodes.Unavailable, "Automation is unavailable."));

    public Task<AiTaskAutomationWriteResult> SetRuleEnabledAsync(
        AiTaskOperationContext context,
        Guid ruleId,
        uint expectedVersion,
        bool enabled,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AiTaskAutomationWriteResult.Fail(AiTaskFailureCodes.Unavailable, "Automation is unavailable."));

    public Task<IReadOnlyList<AiTaskAutomationRuleRevisionReadModel>> ListRuleHistoryAsync(
        AiTaskOperationContext context,
        Guid ruleId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AiTaskAutomationRuleRevisionReadModel>>([]);

    public Task<AiTaskAutomationEvaluationResult> SimulateAsync(
        AiTaskOperationContext context,
        SimulateAiTaskAutomationCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiTaskAutomationEvaluationResult(
            command.TriggerType, [], [], [], [], [], true, "Automation unavailable."));

    public Task<AiTaskAutomationEvaluationResult> EvaluateAndApplyToTaskAsync(
        AiTaskOperationContext context,
        AiTask task,
        string triggerType,
        DateTimeOffset now,
        string actor,
        Guid? correlationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiTaskAutomationEvaluationResult(
            triggerType, [], [], [], [], [], false, "Automation unavailable."));

    public Task<AiTaskAutomationEvaluationResult> EvaluateAndApplyAsync(
        AiTaskOperationContext context,
        AiTask task,
        string triggerType,
        DateTimeOffset now,
        string actor,
        Guid? correlationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiTaskAutomationEvaluationResult(
            triggerType, [], [], [], [], [], false, "Automation unavailable."));

    public Task<IReadOnlyList<AiTaskAutomationExecutionReadModel>> ListExecutionsAsync(
        AiTaskOperationContext context,
        Guid? taskId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AiTaskAutomationExecutionReadModel>>([]);

    public Task RecordManualOverrideAsync(
        AiTaskOperationContext context,
        AiTask task,
        IReadOnlyList<string> scopes,
        string reason,
        bool allowAutomationResume,
        DateTimeOffset now,
        string actor,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
