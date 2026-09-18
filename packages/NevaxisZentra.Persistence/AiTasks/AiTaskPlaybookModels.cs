namespace NevaxisZentra.Persistence.AiTasks;

public sealed record AiTaskPlaybookConditionDefinition(
    string Field,
    string Operator,
    string Value);

public sealed record AiTaskPlaybookStepDefinition(
    string StepKey,
    string Name,
    string? Description,
    int SortOrder,
    string StepType,
    bool Required,
    string DefaultPriority,
    string? AssignmentPolicyJson,
    string? SlaPolicyJson,
    string? EvidencePolicyJson,
    string? ClosurePolicyJson,
    AiTaskPlaybookConditionDefinition? Condition,
    int? TargetDurationMinutes);

public sealed record AiTaskPlaybookDependencyDefinition(
    string StepKey,
    string DependsOnStepKey,
    string DependencyType);

public sealed record CreateAiTaskPlaybookCommand(
    string PlaybookKey,
    string Name,
    string? Description,
    string Category,
    string? TriggerContext,
    Guid? FacilityId,
    bool Enabled,
    DateTimeOffset? EffectiveFromUtc,
    DateTimeOffset? EffectiveUntilUtc,
    IReadOnlyList<AiTaskPlaybookStepDefinition> Steps,
    IReadOnlyList<AiTaskPlaybookDependencyDefinition> Dependencies);

public sealed record UpdateAiTaskPlaybookCommand(
    Guid PlaybookId,
    uint ExpectedVersion,
    string Name,
    string? Description,
    string Category,
    string? TriggerContext,
    bool Enabled,
    DateTimeOffset? EffectiveFromUtc,
    DateTimeOffset? EffectiveUntilUtc,
    IReadOnlyList<AiTaskPlaybookStepDefinition> Steps,
    IReadOnlyList<AiTaskPlaybookDependencyDefinition> Dependencies,
    string ChangeReason);

public sealed record StartAiTaskPlaybookRunCommand(
    Guid PlaybookId,
    string IdempotencyKey,
    string TriggerSource,
    Guid? SourceTaskId,
    string? SourceEntityJson,
    Guid? CorrelationId,
    IReadOnlyDictionary<string, string>? ContextValues);

public sealed record CancelAiTaskPlaybookRunCommand(
    Guid RunId,
    string Reason);

public sealed record BlockAiTaskPlaybookRunStepCommand(
    Guid RunId,
    Guid StepId,
    string Reason);

public sealed record UnblockAiTaskPlaybookRunStepCommand(
    Guid RunId,
    Guid StepId,
    string Reason);

public sealed record SkipAiTaskPlaybookRunStepCommand(
    Guid RunId,
    Guid StepId,
    string Reason);

public sealed record AiTaskPlaybookWriteResult(
    bool Success,
    string? FailureCode,
    string? Message,
    Guid? EntityId,
    uint? Version,
    Guid? RunId)
{
    public static AiTaskPlaybookWriteResult SuccessResult(Guid entityId, uint? version = null, Guid? runId = null) =>
        new(true, null, null, entityId, version, runId);

    public static AiTaskPlaybookWriteResult Fail(string failureCode, string message) =>
        new(false, failureCode, message, null, null, null);
}

public sealed record AiTaskPlaybookSummaryReadModel(
    Guid Id,
    string PlaybookKey,
    string Name,
    string Category,
    Guid? FacilityId,
    int Version,
    bool Enabled,
    int StepCount,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? LastRunAtUtc);

public sealed record AiTaskPlaybookDetailReadModel(
    Guid Id,
    string PlaybookKey,
    string Name,
    string? Description,
    string Category,
    string? TriggerContext,
    Guid? FacilityId,
    int Version,
    bool Enabled,
    DateTimeOffset? EffectiveFromUtc,
    DateTimeOffset? EffectiveUntilUtc,
    IReadOnlyList<AiTaskPlaybookStepReadModel> Steps,
    IReadOnlyList<AiTaskPlaybookDependencyReadModel> Dependencies,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record AiTaskPlaybookStepReadModel(
    Guid Id,
    string StepKey,
    string Name,
    string? Description,
    int SortOrder,
    string StepType,
    bool Required,
    string DefaultPriority,
    string? AssignmentPolicyJson,
    string? SlaPolicyJson,
    string? EvidencePolicyJson,
    string? ClosurePolicyJson,
    AiTaskPlaybookConditionDefinition? Condition,
    int? TargetDurationMinutes);

public sealed record AiTaskPlaybookDependencyReadModel(
    Guid Id,
    string StepKey,
    string DependsOnStepKey,
    string DependencyType);

public sealed record AiTaskPlaybookRunSummaryReadModel(
    Guid Id,
    Guid PlaybookId,
    string PlaybookName,
    string Status,
    Guid FacilityId,
    string TriggerSource,
    Guid? SourceTaskId,
    int RequiredStepsCompleted,
    int RequiredStepsTotal,
    int OptionalStepsCompleted,
    int OptionalStepsTotal,
    int BlockedStepsCount,
    int ApprovalsPending,
    bool AtRisk,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record AiTaskPlaybookRunDetailReadModel(
    Guid Id,
    Guid PlaybookId,
    string PlaybookName,
    int PlaybookVersion,
    string Status,
    Guid FacilityId,
    string TriggerSource,
    Guid? SourceTaskId,
    string? SourceEntityJson,
    Guid? ParentTaskId,
    int RequiredStepsCompleted,
    int RequiredStepsTotal,
    int OptionalStepsCompleted,
    int OptionalStepsTotal,
    int BlockedStepsCount,
    int ApprovalsPending,
    bool AtRisk,
    string? BlockReason,
    string? CriticalPathJson,
    IReadOnlyList<AiTaskPlaybookRunStepReadModel> Steps,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason);

public sealed record AiTaskPlaybookRunStepReadModel(
    Guid Id,
    string StepKey,
    string Name,
    bool Required,
    string StepType,
    string Status,
    string DependencyState,
    Guid? LinkedTaskId,
    string? BlockReason,
    int SortOrder,
    DateTimeOffset? ReadyAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record AiTaskPlaybookProgressReadModel(
    int RequiredCompleted,
    int RequiredTotal,
    int OptionalCompleted,
    int OptionalTotal,
    int BlockedCount,
    int ApprovalsPending,
    int? OverallPercent);

public sealed record AiTaskPlaybookCriticalPathReadModel(
    bool Available,
    IReadOnlyList<string> BlockingStepKeys,
    IReadOnlyList<string> ImpactedStepKeys,
    string? LimitationReason);

public sealed record AiTaskPlaybookGraphValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors);
