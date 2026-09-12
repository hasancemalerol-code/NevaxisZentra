namespace NevaxisFactOS.Persistence.AiTasks;

public sealed class AiTaskPlaybook
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? FacilityId { get; set; }
    public string PlaybookKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = AiTaskPlaybookCategories.MachineIncident;
    public string? TriggerContext { get; set; }
    public bool Enabled { get; set; } = true;
    public int Version { get; set; } = 1;
    public Guid? CurrentRevisionId { get; set; }
    public DateTimeOffset? EffectiveFromUtc { get; set; }
    public DateTimeOffset? EffectiveUntilUtc { get; set; }
    public Guid? CreatedByMembershipId { get; set; }
    public Guid? UpdatedByMembershipId { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<AiTaskPlaybookRevision> Revisions { get; set; } = [];
    public ICollection<AiTaskPlaybookRun> Runs { get; set; } = [];
}

public sealed class AiTaskPlaybookRevision
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlaybookId { get; set; }
    public int Version { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? TriggerContext { get; set; }
    public string DefinitionSnapshotJson { get; set; } = "{}";
    public string ChangedBy { get; set; } = string.Empty;
    public DateTimeOffset ChangedAtUtc { get; set; }
    public string ChangeReason { get; set; } = string.Empty;

    public AiTaskPlaybook? Playbook { get; set; }
    public ICollection<AiTaskPlaybookStep> Steps { get; set; } = [];
    public ICollection<AiTaskPlaybookStepDependency> Dependencies { get; set; } = [];
}

public sealed class AiTaskPlaybookStep
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlaybookRevisionId { get; set; }
    public string StepKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public string StepType { get; set; } = AiTaskPlaybookStepTypes.Task;
    public bool Required { get; set; } = true;
    public string DefaultPriority { get; set; } = AiTaskPriorities.Medium;
    public string? AssignmentPolicyJson { get; set; }
    public string? SlaPolicyJson { get; set; }
    public string? EvidencePolicyJson { get; set; }
    public string? ClosurePolicyJson { get; set; }
    public string? ConditionJson { get; set; }
    public int? TargetDurationMinutes { get; set; }

    public AiTaskPlaybookRevision? Revision { get; set; }
}

public sealed class AiTaskPlaybookStepDependency
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlaybookRevisionId { get; set; }
    public Guid StepId { get; set; }
    public Guid DependsOnStepId { get; set; }
    public string DependencyType { get; set; } = AiTaskPlaybookDependencyTypes.RequiredPredecessor;

    public AiTaskPlaybookRevision? Revision { get; set; }
    public AiTaskPlaybookStep? Step { get; set; }
    public AiTaskPlaybookStep? DependsOnStep { get; set; }
}

public sealed class AiTaskPlaybookRun
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid PlaybookId { get; set; }
    public int PlaybookVersion { get; set; }
    public Guid PlaybookRevisionId { get; set; }
    public string TriggerSource { get; set; } = AiTaskPlaybookTriggerSources.Manual;
    public Guid? SourceTaskId { get; set; }
    public string? SourceEntityJson { get; set; }
    public string Status { get; set; } = AiTaskPlaybookRunStatuses.Pending;
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid? CorrelationId { get; set; }
    public Guid? ParentTaskId { get; set; }
    public int RequiredStepsCompleted { get; set; }
    public int RequiredStepsTotal { get; set; }
    public int OptionalStepsCompleted { get; set; }
    public int OptionalStepsTotal { get; set; }
    public int BlockedStepsCount { get; set; }
    public int ApprovalsPending { get; set; }
    public bool AtRisk { get; set; }
    public string? CriticalPathJson { get; set; }
    public string? BlockReason { get; set; }
    public string StartedBy { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public string? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }

    public AiTaskPlaybook? Playbook { get; set; }
    public ICollection<AiTaskPlaybookRunStep> Steps { get; set; } = [];
}

public sealed class AiTaskPlaybookRunStep
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid RunId { get; set; }
    public Guid TemplateStepId { get; set; }
    public string StepKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Required { get; set; } = true;
    public string StepType { get; set; } = AiTaskPlaybookStepTypes.Task;
    public string Status { get; set; } = AiTaskPlaybookRunStepStatuses.Pending;
    public Guid? LinkedTaskId { get; set; }
    public string DependencyState { get; set; } = AiTaskPlaybookDependencyStates.Waiting;
    public string? BlockReason { get; set; }
    public string? BlockedBy { get; set; }
    public DateTimeOffset? BlockedAtUtc { get; set; }
    public DateTimeOffset? ReadyAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public int SortOrder { get; set; }

    public AiTaskPlaybookRun? Run { get; set; }
}
