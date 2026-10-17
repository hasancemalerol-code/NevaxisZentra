namespace NevaxisZentra.Persistence.AiTasks;

public sealed record AiTaskOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record AiTaskRecommendationModel(
    string Title,
    string Description,
    string ActionType,
    bool RequiresApproval,
    bool Reversible,
    string? TargetSystem);

public sealed record AiTaskConfidenceModel(
    decimal Score,
    string Explanation,
    string? ModelVersion);

public sealed record AiTaskImpactModel(
    decimal? FinancialAmount,
    string? Currency,
    decimal? ProductionQuantity,
    string? ProductionUnit,
    int? EstimatedDelayMinutes,
    int? EstimatedDowntimeMinutes,
    decimal? EstimatedSavingsAmount,
    string Summary);

public sealed record AiTaskSourceReferenceModel(
    string SourceType,
    string? SourceId,
    string SourceLabel,
    DateTimeOffset? ObservedAt,
    string? ValueSummary);

public sealed record AiTaskAssigneeModel(
    string UserId,
    string DisplayName,
    string? Role);

public sealed record AiTaskApprovalReadModel(
    string State,
    DateTimeOffset? RequestedAt,
    string? RequestedBy,
    DateTimeOffset? DecidedAt,
    string? DecidedBy,
    string? DecisionNote);

public sealed record AiTaskExecutionReadModel(
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? TargetSystem,
    string? ExternalReferenceId,
    string? PreviousValue,
    string? AppliedValue,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record AiTaskAuditEventReadModel(
    Guid Id,
    Guid TaskId,
    string EventType,
    DateTimeOffset OccurredAt,
    string ActorType,
    string? ActorId,
    string ActorLabel,
    string? Note,
    string? PreviousStatus,
    string? NextStatus,
    string? MetadataJson);

public sealed record AiTaskSummaryReadModel(
    Guid Id,
    string TaskReference,
    string Title,
    string Summary,
    string Category,
    string Priority,
    string RiskLevel,
    string Status,
    string ApprovalState,
    string? SourceModule,
    string? SourceEntityId,
    string ListOriginKind,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version,
    AiTaskAssigneeModel? Assignee,
    Guid? AssignedMembershipId,
    Guid? AssignedTeamChannelId,
    DateTimeOffset? AcknowledgedAtUtc,
    int EscalationLevel,
    string EscalationState,
    string SlaState,
    string ClosureState,
    int EvidenceCount,
    bool ClosureApprovalRequired,
    string? SlaBreachType = null);

public sealed record AiTaskDetailReadModel(
    Guid Id,
    string TaskReference,
    string Title,
    string Summary,
    string Category,
    string Priority,
    string RiskLevel,
    string Status,
    string DetectedIssue,
    string Reasoning,
    AiTaskRecommendationModel Recommendation,
    AiTaskConfidenceModel Confidence,
    AiTaskImpactModel Impact,
    IReadOnlyList<AiTaskSourceReferenceModel> Sources,
    AiTaskAssigneeModel? Assignee,
    AiTaskApprovalReadModel Approval,
    AiTaskExecutionReadModel? Execution,
    string? SiteId,
    string? ProductionLineId,
    string? MachineId,
    string? MoldId,
    string? ProductionOrderId,
    string? SalesOrderId,
    string? MaterialId,
    string? SupplierId,
    string? CustomerId,
    string? SourceModule,
    string? SourceEntityId,
    string RuleSetVersion,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? AcknowledgedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    int EscalationLevel,
    string EscalationState,
    DateTimeOffset? LastEscalatedAtUtc,
    string? AssignmentReason,
    string? AssignmentSource,
    Guid? AssignedMembershipId,
    Guid? AssignedTeamChannelId,
    Guid? SlaPolicyId,
    AiTaskSlaStateReadModel Sla,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version,
    AiTaskClosureReadModel Closure,
    AiTaskClosurePolicyReadModel ClosurePolicy,
    AiTaskClosureValidationResult ClosureValidation,
    IReadOnlyList<AiTaskEvidenceReadModel> Evidence,
    IReadOnlyList<AiTaskAuditEventReadModel> AuditTrail,
    Guid? AutomationRuleId = null,
    int? AutomationRuleVersion = null,
    int MinimumEvidenceRequired = 0,
    bool FourEyesRequired = false,
    string? AutomationMetadataJson = null,
    string? AutomationOverrideJson = null);

public sealed record AiTaskListReadModel(
    IReadOnlyList<AiTaskSummaryReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AiTaskListFilters(
    string? Status,
    string? Category,
    string? Priority,
    string? SourceModule,
    string? ClosureState,
    string? ClosureQueue,
    int Page,
    int PageSize);

public sealed record CreateAiTaskCommand(
    string Title,
    string Summary,
    string Category,
    string Priority,
    string RiskLevel,
    string DetectedIssue,
    string Reasoning,
    AiTaskRecommendationModel Recommendation,
    AiTaskConfidenceModel Confidence,
    AiTaskImpactModel Impact,
    IReadOnlyList<AiTaskSourceReferenceModel> Sources,
    string? SiteId,
    string? ProductionLineId,
    string? MachineId,
    string? MoldId,
    string? ProductionOrderId,
    string? SalesOrderId,
    string? MaterialId,
    string? SupplierId,
    string? CustomerId,
    DateTimeOffset? DueAtUtc);

public sealed record CreateSourceLinkedAiTaskCommand(
    string Title,
    string Summary,
    string Category,
    string Priority,
    string RiskLevel,
    string DetectedIssue,
    string Reasoning,
    AiTaskRecommendationModel Recommendation,
    AiTaskConfidenceModel Confidence,
    AiTaskImpactModel Impact,
    IReadOnlyList<AiTaskSourceReferenceModel> Sources,
    string SourceModule,
    string SourceEntityId,
    string IdempotencyKey,
    string? SiteId,
    string? ProductionLineId,
    string? MachineId,
    string? MoldId,
    string? ProductionOrderId,
    string? SalesOrderId,
    string? MaterialId,
    string? SupplierId,
    string? CustomerId,
    DateTimeOffset? DueAtUtc);

public sealed record TransitionAiTaskCommand(
    Guid TaskId,
    uint ExpectedVersion,
    string TargetStatus,
    string? Note);

public sealed class AiTaskWriteResult
{
    public bool Succeeded { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public bool IdempotentReplay { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static AiTaskWriteResult Success(Guid entityId, uint version, bool idempotentReplay = false) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
        };

    public static AiTaskWriteResult Fail(string code, string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = code,
            FailureReason = reason,
        };
}
