using NevaxisZentra.Persistence.FactoryRecoveryIntelligence;

namespace NevaxisZentra.Persistence.FactoryOutcomeExecution;

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
    public int RecordVersion { get; set; } = 1;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string LogicVersion { get; set; } = string.Empty;
    public string LearningDisposition { get; set; } = FactoryRecoveryIntelligence.FactoryRecoveryLearningDisposition.VerifiedSuccess;
    public string ProblemPattern { get; set; } = string.Empty;
    public string CausePattern { get; set; } = string.Empty;
    public string OutcomeType { get; set; } = string.Empty;
    public string EventCategory { get; set; } = string.Empty;
    public string? MachineCode { get; set; }
    public string? OrderReference { get; set; }
    public string? MaterialCode { get; set; }
    public string? MoldCode { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string PatternKey { get; set; } = string.Empty;
    public string PatternJson { get; set; } = "{}";
    public string SelectedStrategyKey { get; set; } = string.Empty;
    public string? AlternativeStrategyKey { get; set; }
    public string ActionsJson { get; set; } = "[]";
    public string WorkersJson { get; set; } = "[]";
    public string BlockersJson { get; set; } = "[]";
    public string FailureModesJson { get; set; } = "[]";
    public string? ImpactBeforeJson { get; set; }
    public int? ActualRecoveredQuantity { get; set; }
    public int? TimeToRecoveryMinutes { get; set; }
    public int? TimeToFirstActionMinutes { get; set; }
    public int? TimeToVerifiedMinutes { get; set; }
    public bool? DeliveryProtected { get; set; }
    public string EvidenceStrength { get; set; } = string.Empty;
    public string VerificationResult { get; set; } = string.Empty;
    public string? FailureSummary { get; set; }
    public DateTimeOffset VerifiedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
