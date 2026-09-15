namespace NevaxisFactOS.Persistence.FactoryOutcomeExecution;

public sealed class FactoryOutcomeExecutionAction
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid RootCaseId { get; set; }
    public Guid? ChildCaseId { get; set; }
    public string ActionKey { get; set; } = string.Empty;
    public string WorkerCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int ExecutionOrder { get; set; }
    public string Status { get; set; } = FactoryOutcomeExecutionActionStatuses.Pending;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string DependsOnActionKeysJson { get; set; } = "[]";
    public Guid? LinkedAiTaskId { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 3;
    public string? LastError { get; set; }
    public DateTimeOffset? NextRetryAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }
}

public sealed class FactoryOutcomeEventReceipt
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public Guid RootCaseId { get; set; }
    public bool CreatedNewCase { get; set; }
    public DateTimeOffset ProcessedAtUtc { get; set; }
}

public sealed class FactoryOutcomeLearningRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid SourceCaseId { get; set; }
    public string ProblemPattern { get; set; } = string.Empty;
    public string CausePattern { get; set; } = string.Empty;
    public string SelectedStrategyKey { get; set; } = string.Empty;
    public string? AlternativeStrategyKey { get; set; }
    public string ActionsJson { get; set; } = "[]";
    public int? ActualRecoveredQuantity { get; set; }
    public int? TimeToRecoveryMinutes { get; set; }
    public string VerificationResult { get; set; } = string.Empty;
    public string? FailureSummary { get; set; }
    public DateTimeOffset VerifiedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
