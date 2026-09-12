namespace NevaxisFactOS.Persistence.DigitalWorkforce;

public sealed record DigitalWorkerSummary(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Mission,
    string Status,
    string AutonomyMode,
    bool Enabled,
    bool IsPaused,
    bool IsKilled,
    bool IsHumanControlled,
    string SourceClassification,
    int ActiveCaseCount,
    int QueueCount,
    int CompletedToday,
    int CompletedThisMonth,
    int VerifiedClosures,
    int PendingApprovals,
    string? LastActivityAt,
    decimal AiCostToday,
    string PermissionStatus,
    long ConcurrencyToken);

public sealed record DigitalWorkforceControlCenterSnapshot(
    IReadOnlyList<DigitalWorkerSummary> Workers,
    bool GlobalPauseActive,
    bool GlobalKillActive,
    int OpenCases,
    int EscalatedCases,
    int PendingApprovals,
    decimal VerificationRate,
    decimal TotalAiCostToday,
    decimal TotalAiCostMonth);

public sealed record DigitalWorkCaseSummary(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string CurrentStage,
    string Severity,
    Guid PrimaryWorkerId,
    string PrimaryWorkerCode,
    string PrimaryWorkerName,
    bool RequiresApproval,
    bool IsHumanControlled,
    string VerificationState,
    string SourceClassification,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    DateTimeOffset? VerifiedAtUtc,
    string Outcome,
    long ConcurrencyToken);

public sealed record DigitalWorkActivitySummary(
    Guid Id,
    Guid CaseId,
    Guid WorkerId,
    string WorkerCode,
    string ActionType,
    string Description,
    string Result,
    string SourceClassification,
    DateTimeOffset CreatedAtUtc);

public sealed record DigitalWorkApprovalSummary(
    Guid Id,
    Guid CaseId,
    Guid WorkerId,
    string WorkerCode,
    string Action,
    string BusinessReason,
    string Impact,
    int RiskLevel,
    decimal? Amount,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    long ConcurrencyToken);

public sealed record DigitalWorkforceMetrics(
    int CompletedCases,
    int VerifiedCases,
    decimal VerificationRate,
    int OpenCases,
    int EscalatedCases,
    double? AverageClosureDurationHours);

public sealed record UpdateAutonomyRequest(string AutonomyMode, long ConcurrencyToken);

public sealed record UpdatePolicyRequest(
    string AllowedToolsJson,
    string AllowedActionsJson,
    string ProhibitedActionsJson,
    string ApprovalRequiredForJson,
    decimal MaxTransactionValue,
    int MaxRiskLevel,
    string ExternalCommunicationPolicy,
    decimal AiBudgetDaily,
    decimal AiBudgetMonthly,
    long ConcurrencyToken);

public sealed record ApprovalDecisionRequest(string Decision, string? Comment, long ConcurrencyToken);

public sealed record TriggerWorkCaseRequest(
    string Title,
    string Description,
    string Source,
    string SourceClassification,
    string Severity,
    Guid PrimaryWorkerId,
    Guid? FacilityId,
    string? MetadataJson);

public sealed record AdvanceWorkCaseRequest(
    string TargetStatus,
    string? Action,
    string? Tool,
    decimal? Amount,
    int RiskLevel,
    bool AllowEmergencyClose);

public sealed record RecordVerificationRequest(
    string VerificationType,
    string Source,
    string ExpectedState,
    string ObservedState,
    string SourceClassification,
    decimal Confidence,
    string Result);

public sealed record HandoffRequest(Guid ToWorkerId, string Reason, string? Dependency);

public sealed record RecordAiUsageRequest(
    Guid WorkerId,
    Guid? CaseId,
    string ModelTier,
    string ModelName,
    int InputTokens,
    int OutputTokens,
    decimal EstimatedCost);

public sealed record DigitalWorkforceWorkerDetailSnapshot(
    Guid WorkerId,
    string Code,
    string Name,
    string Description,
    string Mission,
    string Status,
    string AutonomyMode,
    bool IsPaused,
    bool IsKilled,
    bool IsHumanControlled,
    string SourceClassification,
    long ConcurrencyToken,
    DigitalWorkerPolicy? Policy,
    DigitalWorkerBudget? Budget,
    DigitalWorkCaseSummary? CurrentCase,
    IReadOnlyList<DigitalWorkCaseSummary> OpenCases,
    IReadOnlyList<DigitalWorkCaseSummary> ClosedCases,
    IReadOnlyList<DigitalWorkActivitySummary> Activity,
    IReadOnlyList<DigitalWorkApprovalSummary> Approvals);

public sealed record TriggerDomainEventRequest(
    string EventType,
    string Title,
    string Description,
    string Severity,
    string SourceClassification,
    Guid? FacilityId,
    string? MetadataJson);
