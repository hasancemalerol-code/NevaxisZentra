namespace NevaxisFactOS.Persistence.FactoryDecisionCenter;

public sealed record FactoryDecisionCenterExecutiveSummary(
    int CriticalDecisions,
    int OutcomesAtRisk,
    int PendingHumanApproval,
    int RecoveryOperations,
    int PendingVerification);

public sealed record FactoryDecisionCaseQueueItem(
    Guid CaseId,
    string Title,
    string Severity,
    string Status,
    string OrderReference,
    string CustomerName,
    string DeliveryCommitmentLabel,
    string RiskSummary,
    int RequiredQuantity,
    int ProjectedQuantity,
    int GapQuantity,
    string RecommendedStrategy,
    IReadOnlyList<string> DigitalWorkerNames,
    string? DecisionRequired,
    string? CurrentBlockerCode,
    string VerificationState,
    DateTimeOffset UpdatedAtUtc,
    long ConcurrencyToken);

public sealed record FactoryDecisionCenterLiveQueues(
    IReadOnlyList<FactoryDecisionCaseQueueItem> CriticalDecisions,
    IReadOnlyList<FactoryDecisionCaseQueueItem> ActiveRecoveryOperations,
    IReadOnlyList<FactoryDecisionCaseQueueItem> PendingHumanApproval,
    IReadOnlyList<FactoryDecisionCaseQueueItem> BlockedOrEscalated,
    IReadOnlyList<FactoryDecisionCaseQueueItem> PendingVerification,
    IReadOnlyList<FactoryDecisionCaseQueueItem> VerifiedClosedToday);

public sealed record FactoryDecisionCenterWorkspaceSnapshot(
    FactoryDecisionCenterExecutiveSummary Summary,
    IReadOnlyList<FactoryDecisionCaseQueueItem> PriorityQueue,
    FactoryDecisionCenterLiveQueues LiveQueues,
    DateTimeOffset GeneratedAtUtc);

public sealed record FactoryDecisionRecoveryStep(
    int Order,
    string Title,
    string WorkerCode,
    string WorkerName,
    string Status,
    Guid? LinkedCaseId,
    string? ActionKey);

public sealed record FactoryDecisionWorkerAssignment(
    string WorkerCode,
    string WorkerName,
    string Responsibility,
    string Status,
    string? BlockerCode);

public sealed record FactoryDecisionEvidenceItem(
    string Category,
    string Label,
    string Status,
    string Summary,
    DateTimeOffset? RecordedAtUtc);

public sealed record FactoryDecisionApprovalCard(
    Guid ApprovalId,
    string Decision,
    string Impact,
    string Status,
    long ConcurrencyToken);

public sealed record FactoryDecisionOutcomeRisk(
    string Summary,
    int RequiredQuantity,
    int ProjectedQuantity,
    int GapQuantity,
    string TimeRemainingLabel,
    decimal Confidence);

public sealed record FactoryDecisionAssessment(
    string PrimaryCause,
    IReadOnlyList<string> ContributingFactors);

public sealed record FactoryDecisionVerifiedOutcome(
    bool IsVerified,
    int RequiredQuantity,
    int VerifiedProjection,
    bool DeliveryProtected,
    int RecoveryGain,
    DateTimeOffset? VerifiedAtUtc);

public sealed record FactoryDecisionCaseDetailSnapshot(
    Guid CaseId,
    string Title,
    string Severity,
    string Status,
    string OperationalProblem,
    string BusinessOutcomeAtRisk,
    string FacilityLabel,
    string OrderReference,
    string CustomerName,
    string MachineCode,
    string MoldCode,
    string MaterialCode,
    string DeliveryCommitmentLabel,
    FactoryDecisionOutcomeRisk OutcomeRisk,
    FactoryDecisionAssessment Assessment,
    IReadOnlyList<FactoryDecisionRecoveryStep> RecoveryPlan,
    IReadOnlyList<FactoryDecisionWorkerAssignment> DigitalWorkers,
    FactoryDecisionApprovalCard? HumanDecision,
    IReadOnlyList<FactoryDecisionWorkerAssignment> ExecutionProgress,
    IReadOnlyList<FactoryDecisionEvidenceItem> Evidence,
    FactoryDecisionVerifiedOutcome VerifiedOutcome,
    IReadOnlyList<FactoryDecisionTimelineEntry> Timeline,
    long ConcurrencyToken);

public sealed record FactoryDecisionTimelineEntry(
    string Label,
    string Description,
    DateTimeOffset OccurredAtUtc);

public sealed record FactoryDecisionCenterApprovalRequest(
    bool Approved,
    string? Note,
    long ConcurrencyToken);

public sealed record FactoryDecisionCenterVerifyRequest(
    long ConcurrencyToken,
    int? VerifiedProjection = null);

public sealed class FactoryDecisionCenterException : Exception
{
    public FactoryDecisionCenterException(string failureCode, string message) : base(message)
    {
        FailureCode = failureCode;
    }

    public string FailureCode { get; }
}

public static class FactoryDecisionCenterFailureCodes
{
    public const string NotFound = "not-found";
    public const string ConcurrencyConflict = "concurrency-conflict";
    public const string Forbidden = "forbidden";
    public const string ApprovalRequired = "approval-required";
    public const string OutcomeNotVerified = "outcome-not-verified";
    public const string TasksNotComplete = "tasks-not-complete";
    public const string DevelopmentOnly = "development-only";
}
