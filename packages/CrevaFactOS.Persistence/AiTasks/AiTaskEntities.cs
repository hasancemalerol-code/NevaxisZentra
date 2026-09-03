namespace CrevaFactOS.Persistence.AiTasks;

public sealed class AiTask
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string TaskReference { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = AiTaskPriorities.Medium;
    public string RiskLevel { get; set; } = AiTaskRiskLevels.Medium;
    public string Status { get; set; } = AiTaskStatuses.Draft;
    public string DetectedIssue { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public string RecommendationJson { get; set; } = "{}";
    public string ConfidenceJson { get; set; } = "{}";
    public string ImpactJson { get; set; } = "{}";
    public string SourcesJson { get; set; } = "[]";
    public string? AssigneeJson { get; set; }
    public Guid? AssignedMembershipId { get; set; }
    public Guid? AssignedTeamChannelId { get; set; }
    public DateTimeOffset? AcknowledgedAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public int EscalationLevel { get; set; }
    public string EscalationState { get; set; } = AiTaskEscalationStates.None;
    public Guid? SlaPolicyId { get; set; }
    public DateTimeOffset? LastEscalatedAtUtc { get; set; }
    public string? AssignmentReason { get; set; }
    public string? AssignmentSource { get; set; }
    public Guid? RoutingRuleId { get; set; }
    public string? RoutingMetadataJson { get; set; }
    public string ApprovalState { get; set; } = AiTaskApprovalStates.NotRequired;
    public DateTimeOffset? ApprovalRequestedAtUtc { get; set; }
    public string? ApprovalRequestedBy { get; set; }
    public DateTimeOffset? ApprovalDecidedAtUtc { get; set; }
    public string? ApprovalDecidedBy { get; set; }
    public string? ApprovalDecisionNote { get; set; }
    public string? ExecutionJson { get; set; }
    public string? SiteId { get; set; }
    public string? ProductionLineId { get; set; }
    public string? MachineId { get; set; }
    public string? MoldId { get; set; }
    public string? ProductionOrderId { get; set; }
    public string? SalesOrderId { get; set; }
    public string? MaterialId { get; set; }
    public string? SupplierId { get; set; }
    public string? CustomerId { get; set; }
    public string? SourceModule { get; set; }
    public string? SourceEntityId { get; set; }
    public string RuleSetVersion { get; set; } = AiTaskConstants.RuleSetVersion;
    public string? IdempotencyKey { get; set; }
    public string? IdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset? DueAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    public ICollection<AiTaskAuditEvent> AuditEvents { get; set; } = [];
}

public sealed class AiTaskAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid TaskId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string ActorType { get; set; } = AiTaskActorTypes.User;
    public string? ActorId { get; set; }
    public string ActorLabel { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? PreviousStatus { get; set; }
    public string? NextStatus { get; set; }
    public Guid? CorrelationId { get; set; }
    public string? MetadataJson { get; set; }

    public AiTask? Task { get; set; }
}

public sealed class AiTaskSlaPolicy
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? FacilityId { get; set; }
    public string PolicyKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Priority { get; set; } = AiTaskPriorities.Medium;
    public string? Category { get; set; }
    public string? SourceModule { get; set; }
    public int AcknowledgmentTargetMinutes { get; set; }
    public int ResponseTargetMinutes { get; set; }
    public int ResolutionTargetMinutes { get; set; }
    public bool Enabled { get; set; } = true;
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class AiTaskRoutingRule
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string RuleKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? SourceModule { get; set; }
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public string TargetType { get; set; } = AiTaskRoutingTargetTypes.UnassignedQueue;
    public Guid? TargetMembershipId { get; set; }
    public Guid? TargetTeamChannelId { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class AiTaskEscalationEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid TaskId { get; set; }
    public int EscalationLevel { get; set; }
    public string BreachType { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string? Note { get; set; }

    public AiTask? Task { get; set; }
}
