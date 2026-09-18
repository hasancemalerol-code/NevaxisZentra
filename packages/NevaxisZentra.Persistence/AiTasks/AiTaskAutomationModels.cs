namespace NevaxisZentra.Persistence.AiTasks;

public sealed record AiTaskAutomationCondition(
    string Field,
    string Operator,
    string? Value,
    IReadOnlyList<string>? Values);

public sealed record AiTaskAutomationAction(
    string Type,
    string? Value,
    Guid? GuidValue,
    int? IntValue,
    bool? BoolValue,
    IReadOnlyList<string>? Values);

public sealed record AiTaskAutomationRuleSummaryReadModel(
    Guid Id,
    string RuleKey,
    string Name,
    string? Description,
    Guid? FacilityId,
    string TriggerType,
    int SortOrder,
    bool Enabled,
    int Version,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? LastMatchedAtUtc,
    IReadOnlyList<string> ActionSummaries);

public sealed record AiTaskAutomationRuleDetailReadModel(
    Guid Id,
    string RuleKey,
    string Name,
    string? Description,
    Guid? FacilityId,
    string TriggerType,
    int SortOrder,
    bool Enabled,
    int Version,
    IReadOnlyList<AiTaskAutomationCondition> Conditions,
    IReadOnlyList<AiTaskAutomationAction> Actions,
    DateTimeOffset? EffectiveFromUtc,
    DateTimeOffset? EffectiveUntilUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? LastMatchedAtUtc,
    string CreatedBy,
    string UpdatedBy);

public sealed record AiTaskAutomationRuleRevisionReadModel(
    Guid Id,
    int Version,
    string Name,
    bool Enabled,
    string ChangedBy,
    DateTimeOffset ChangedAtUtc,
    string ChangeReason);

public sealed record AiTaskAutomationExecutionReadModel(
    Guid Id,
    Guid TaskId,
    Guid RuleId,
    string RuleName,
    int RuleVersion,
    string TriggerType,
    DateTimeOffset ExecutedAtUtc,
    IReadOnlyList<string> ActionsApplied,
    bool Simulated);

public sealed record AiTaskAutomationSkippedRule(
    Guid RuleId,
    string RuleName,
    string Reason);

public sealed record AiTaskAutomationMatchedRule(
    Guid RuleId,
    string RuleName,
    int RuleVersion,
    int SortOrder,
    IReadOnlyList<AiTaskAutomationCondition> MatchedConditions);

public sealed record AiTaskAutomationProposedChange(
    string Field,
    string? CurrentValue,
    string? ProposedValue,
    Guid? SourceRuleId,
    string? SourceRuleName);

public sealed record AiTaskAutomationConflict(
    string Field,
    IReadOnlyList<string> CompetingValues,
    string Resolution,
    Guid? WinningRuleId);

public sealed record AiTaskAutomationEvaluationResult(
    string TriggerType,
    IReadOnlyList<AiTaskAutomationMatchedRule> MatchedRules,
    IReadOnlyList<AiTaskAutomationSkippedRule> SkippedRules,
    IReadOnlyList<AiTaskAutomationProposedChange> ProposedChanges,
    IReadOnlyList<AiTaskAutomationConflict> Conflicts,
    IReadOnlyList<AiTaskAutomationAction> ActionsApplied,
    bool Simulated,
    string OutcomeSummary);

public sealed record CreateAiTaskAutomationRuleCommand(
    string RuleKey,
    string Name,
    string? Description,
    Guid? FacilityId,
    string TriggerType,
    int SortOrder,
    bool Enabled,
    IReadOnlyList<AiTaskAutomationCondition> Conditions,
    IReadOnlyList<AiTaskAutomationAction> Actions,
    DateTimeOffset? EffectiveFromUtc,
    DateTimeOffset? EffectiveUntilUtc);

public sealed record UpdateAiTaskAutomationRuleCommand(
    Guid RuleId,
    uint ExpectedVersion,
    string Name,
    string? Description,
    string TriggerType,
    int SortOrder,
    bool Enabled,
    IReadOnlyList<AiTaskAutomationCondition> Conditions,
    IReadOnlyList<AiTaskAutomationAction> Actions,
    DateTimeOffset? EffectiveFromUtc,
    DateTimeOffset? EffectiveUntilUtc,
    string ChangeReason);

public sealed record SimulateAiTaskAutomationCommand(
    Guid? RuleId,
    Guid TaskId,
    string TriggerType);

public sealed record AiTaskAutomationOverrideState(
    IReadOnlyList<string> Scopes,
    string ActorId,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    bool AllowAutomationResume);

public sealed record AiTaskAutomationWriteResult(
    bool Succeeded,
    string? FailureCode,
    string? Message,
    Guid? RuleId,
    uint? Version)
{
    public static AiTaskAutomationWriteResult Success(Guid ruleId, uint version) =>
        new(true, null, null, ruleId, version);

    public static AiTaskAutomationWriteResult Fail(string code, string message) =>
        new(false, code, message, null, null);
}
