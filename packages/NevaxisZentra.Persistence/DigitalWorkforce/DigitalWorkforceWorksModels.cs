namespace NevaxisZentra.Persistence.DigitalWorkforce;

public sealed record WorksListItem(
    Guid Id,
    string Title,
    string ProblemSummary,
    string DisplayState,
    string PriorityLabel,
    string WorkTypeLabel,
    string FactoryContextLabel,
    string OwnerLabel,
    string ActingSystemLabel,
    string? SlaLabel,
    bool HasPendingApproval,
    bool IsBlocked,
    string? BlockerLabel,
    string? ExpectedImpact,
    string LastUpdateLabel,
    DateTimeOffset UpdatedAtUtc,
    string Status,
    string VerificationState,
    string OutcomeLabel,
    string Severity,
    string PrimaryDomain,
    long ConcurrencyToken);

public sealed record WorksQueueCounts(
    int Assigned,
    int Team,
    int PendingApproval,
    int Critical,
    int Blocked,
    int InProgress,
    int Observing,
    int VerificationPending,
    int CompletedToday);

public sealed record WorksWorkspaceSnapshot(
    WorksQueueCounts QueueCounts,
    IReadOnlyList<WorksListItem> Items,
    string ActiveQueue,
    int TotalCount);

public sealed record WorksApprovalPanelItem(
    Guid Id,
    string Action,
    string BusinessReason,
    string Impact,
    int RiskLevel,
    string Status,
    long ConcurrencyToken);

public sealed record WorksBlockerPanel(
    string Code,
    string Label,
    DateTimeOffset? SinceUtc,
    string Detail);

public sealed record WorksDependencyItem(
    string Label,
    string Relation,
    string Detail);

public sealed record WorksRelatedWorkItem(
    Guid Id,
    string Title,
    string Relation,
    string DisplayState);

public sealed record WorksEvidenceItem(
    string Kind,
    string Label,
    string Detail,
    DateTimeOffset? OccurredAtUtc);

public sealed record WorksScenarioContextPanel(
    Guid RunId,
    string LineCode,
    string CurrentPhase,
    string? SelectedOptionLabel,
    string? VerificationOutcome,
    decimal? BaselineThroughput,
    decimal? PredictedThroughput,
    decimal? ActualThroughput,
    string ScenarioHref);

public sealed record WorksCaseDetailSnapshot(
    WorksListItem Summary,
    string Description,
    string ImpactSummary,
    string NextStepLabel,
    string? ActiveActionLabel,
    WorksApprovalPanelItem? PendingApproval,
    WorksBlockerPanel? Blocker,
    IReadOnlyList<WorksDependencyItem> Dependencies,
    IReadOnlyList<WorksRelatedWorkItem> RelatedWork,
    IReadOnlyList<WorksTimelineEntry> Timeline,
    WorksVerificationSummary? Verification,
    WorksScenarioContextPanel? ScenarioContext,
    IReadOnlyList<WorksEvidenceItem> Evidence,
    IReadOnlyList<string> PermittedActions,
    string? FactoryHref,
    string? PlanHref,
    string? ScenarioHref,
    string? DigitalWorkforceHref);
