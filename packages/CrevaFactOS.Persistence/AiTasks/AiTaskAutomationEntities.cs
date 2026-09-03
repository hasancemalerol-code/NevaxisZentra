namespace CrevaFactOS.Persistence.AiTasks;

public sealed class AiTaskAutomationRule
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? FacilityId { get; set; }
    public string RuleKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Enabled { get; set; } = true;
    public int SortOrder { get; set; }
    public string TriggerType { get; set; } = AiTaskAutomationTriggers.TaskCreated;
    public string ConditionsJson { get; set; } = "[]";
    public string ActionsJson { get; set; } = "[]";
    public DateTimeOffset? EffectiveFromUtc { get; set; }
    public DateTimeOffset? EffectiveUntilUtc { get; set; }
    public int Version { get; set; } = 1;
    public Guid? CurrentRevisionId { get; set; }
    public DateTimeOffset? LastMatchedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<AiTaskAutomationRuleRevision> Revisions { get; set; } = [];
    public ICollection<AiTaskAutomationExecution> Executions { get; set; } = [];
}

public sealed class AiTaskAutomationRuleRevision
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RuleId { get; set; }
    public int Version { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Enabled { get; set; }
    public int SortOrder { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public string ConditionsJson { get; set; } = "[]";
    public string ActionsJson { get; set; } = "[]";
    public DateTimeOffset? EffectiveFromUtc { get; set; }
    public DateTimeOffset? EffectiveUntilUtc { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTimeOffset ChangedAtUtc { get; set; }
    public string ChangeReason { get; set; } = string.Empty;

    public AiTaskAutomationRule? Rule { get; set; }
}

public sealed class AiTaskAutomationExecution
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid TaskId { get; set; }
    public Guid RuleId { get; set; }
    public int RuleVersion { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string MatchedConditionsJson { get; set; } = "[]";
    public string ActionsAppliedJson { get; set; } = "[]";
    public string ConflictsJson { get; set; } = "[]";
    public string OutcomeJson { get; set; } = "{}";
    public bool Simulated { get; set; }
    public Guid? CorrelationId { get; set; }
    public DateTimeOffset ExecutedAtUtc { get; set; }
    public string ActorType { get; set; } = AiTaskActorTypes.System;
    public string ActorId { get; set; } = "automation";

    public AiTaskAutomationRule? Rule { get; set; }
    public AiTask? Task { get; set; }
}
