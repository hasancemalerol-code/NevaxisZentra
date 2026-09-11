namespace CrevaFactOS.Persistence.DigitalWorkforce;

public sealed class DigitalWorker
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? FacilityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Mission { get; set; } = string.Empty;
    public string Status { get; set; } = DigitalWorkforceWorkerStatuses.Idle;
    public string AutonomyMode { get; set; } = DigitalWorkforceAutonomyModes.Observe;
    public bool Enabled { get; set; } = true;
    public bool IsPaused { get; set; }
    public bool IsKilled { get; set; }
    public bool IsHumanControlled { get; set; }
    public bool GlobalPauseActive { get; set; }
    public string SourceClassification { get; set; } = DigitalWorkforceSourceClassifications.Real;
    public string DefaultPolicyProfileJson { get; set; } = "{}";
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }

    public DigitalWorkerPolicy? Policy { get; set; }
    public DigitalWorkerBudget? Budget { get; set; }
}

public sealed class DigitalWorkerPolicy
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WorkerId { get; set; }
    public string AllowedToolsJson { get; set; } = "[]";
    public string AllowedConnectorsJson { get; set; } = "[]";
    public string DataScopeJson { get; set; } = "{}";
    public string TenantScopeJson { get; set; } = "{}";
    public string FacilityScopeJson { get; set; } = "{}";
    public string LineScopeJson { get; set; } = "[]";
    public string MachineScopeJson { get; set; } = "[]";
    public string AllowedActionsJson { get; set; } = "[]";
    public string ProhibitedActionsJson { get; set; } = "[]";
    public string ApprovalRequiredForJson { get; set; } = "[]";
    public decimal MaxTransactionValue { get; set; }
    public int MaxRiskLevel { get; set; } = 3;
    public string ExternalCommunicationPolicy { get; set; } = DigitalWorkApprovalPolicies.AskAboveThreshold;
    public string WorkingHoursJson { get; set; } = "{}";
    public int RetryLimit { get; set; } = 3;
    public string EscalationPolicyJson { get; set; } = "{}";
    public decimal AiBudgetDaily { get; set; } = 50m;
    public decimal AiBudgetMonthly { get; set; } = 500m;
    public decimal AiBudgetWarningThreshold { get; set; } = 0.8m;
    public bool KillSwitch { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }

    public DigitalWorker? Worker { get; set; }
}

public sealed class DigitalWorkCase
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? FacilityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string SourceClassification { get; set; } = DigitalWorkforceSourceClassifications.Real;
    public string Severity { get; set; } = "medium";
    public string Status { get; set; } = DigitalWorkCaseStatuses.Detected;
    public string CurrentStage { get; set; } = DigitalWorkCaseStages.Detect;
    public Guid PrimaryWorkerId { get; set; }
    public string AutonomyModeAtStart { get; set; } = DigitalWorkforceAutonomyModes.Observe;
    public bool RequiresApproval { get; set; }
    public bool IsHumanControlled { get; set; }
    public Guid? LinkedAiTaskId { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? TargetAtUtc { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string BusinessImpact { get; set; } = string.Empty;
    public decimal? EstimatedTimeSavedMinutes { get; set; }
    public decimal AiCost { get; set; }
    public string VerificationState { get; set; } = DigitalWorkVerificationResults.Pending;
    public string ClosureContractKey { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
    public Guid? OrchestrationRootCaseId { get; set; }
    public Guid? ParentCaseId { get; set; }
    public string? CorrelationKey { get; set; }
    public string PrimaryDomain { get; set; } = string.Empty;
    public string ParticipatingDomainsJson { get; set; } = "[]";
    public string CurrentBlockerCode { get; set; } = "none";
    public string RecoveryPlanJson { get; set; } = "{}";
    public string ImpactGraphJson { get; set; } = "{}";
    public string DependencyGraphJson { get; set; } = "{}";
    public string OutcomeVerificationJson { get; set; } = "{}";
    public bool AdministrativelyClosed { get; set; }
    public int ReopenCount { get; set; }
    public DateTimeOffset? BlockerSinceUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }

    public DigitalWorker? PrimaryWorker { get; set; }
    public ICollection<DigitalWorkCaseParticipant> Participants { get; set; } = new List<DigitalWorkCaseParticipant>();
    public ICollection<DigitalWorkActivity> Activities { get; set; } = new List<DigitalWorkActivity>();
    public ICollection<DigitalWorkVerification> Verifications { get; set; } = new List<DigitalWorkVerification>();
    public ICollection<DigitalWorkApproval> Approvals { get; set; } = new List<DigitalWorkApproval>();
    public ICollection<DigitalWorkHandoff> Handoffs { get; set; } = new List<DigitalWorkHandoff>();
}

public sealed class DigitalWorkCaseParticipant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public Guid WorkerId { get; set; }
    public string Role { get; set; } = DigitalWorkforceParticipantRoles.Primary;
    public DateTimeOffset JoinedAtUtc { get; set; }
    public DateTimeOffset? LeftAtUtc { get; set; }
    public string? HandoffReason { get; set; }

    public DigitalWorkCase? Case { get; set; }
    public DigitalWorker? Worker { get; set; }
}

public sealed class DigitalWorkActivity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public Guid WorkerId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? EvidenceReference { get; set; }
    public string SourceClassification { get; set; } = DigitalWorkforceSourceClassifications.Real;
    public string? CorrelationId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public DigitalWorkCase? Case { get; set; }
    public DigitalWorker? Worker { get; set; }
}

public sealed class DigitalWorkVerification
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public string VerificationType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? EvidenceReference { get; set; }
    public string ExpectedState { get; set; } = string.Empty;
    public string ObservedState { get; set; } = string.Empty;
    public string? VerifiedBy { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public decimal Confidence { get; set; }
    public string Result { get; set; } = DigitalWorkVerificationResults.Pending;
    public string SourceClassification { get; set; } = DigitalWorkforceSourceClassifications.Real;

    public DigitalWorkCase? Case { get; set; }
}

public sealed class DigitalWorkApproval
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public Guid WorkerId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string BusinessReason { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public int RiskLevel { get; set; }
    public decimal? Amount { get; set; }
    public string? EvidenceJson { get; set; }
    public string Status { get; set; } = DigitalWorkApprovalStatuses.Pending;
    public string? Decision { get; set; }
    public string? DecidedBySubjectId { get; set; }
    public DateTimeOffset? DecidedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }

    public DigitalWorkCase? Case { get; set; }
    public DigitalWorker? Worker { get; set; }
}

public sealed class DigitalWorkHandoff
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public Guid FromWorkerId { get; set; }
    public Guid ToWorkerId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Dependency { get; set; }
    public string Status { get; set; } = DigitalWorkforceHandoffStatuses.Pending;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }

    public DigitalWorkCase? Case { get; set; }
}

public sealed class DigitalWorkerUsage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WorkerId { get; set; }
    public Guid? CaseId { get; set; }
    public string ModelTier { get; set; } = "standard";
    public string ModelName { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal EstimatedCost { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public DigitalWorker? Worker { get; set; }
}

public sealed class DigitalWorkerBudget
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WorkerId { get; set; }
    public decimal DailyLimit { get; set; }
    public decimal MonthlyLimit { get; set; }
    public decimal WarningThreshold { get; set; } = 0.8m;
    public decimal DailyConsumed { get; set; }
    public decimal MonthlyConsumed { get; set; }
    public string State { get; set; } = DigitalWorkforceBudgetStates.Normal;
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public DigitalWorker? Worker { get; set; }
}

public sealed class DigitalWorkforceRuntimeState
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public bool GlobalPauseActive { get; set; }
    public bool GlobalKillActive { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }
}
