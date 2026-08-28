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

    public AiTask? Task { get; set; }
}
