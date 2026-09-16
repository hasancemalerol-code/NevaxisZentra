namespace NevaxisFactOS.Persistence.DigitalWorkforce;

public sealed record EnqueueDigitalWorkExecutionRequest(
    Guid CaseId,
    Guid WorkerId,
    string ActionType,
    string? Tool,
    string InputContractJson,
    decimal? Amount,
    int RiskLevel,
    string? IdempotencyKeyOverride = null);

public sealed record EnqueueDigitalWorkExecutionResult(
    bool Accepted,
    bool Duplicate,
    Guid? ExecutionId,
    string? ErrorCode,
    string? SafeMessage);

public sealed record DigitalWorkExecutionSummary(
    Guid Id,
    Guid CaseId,
    Guid WorkerId,
    string ActionType,
    string ActionClassification,
    string? Tool,
    string Status,
    string? CurrentBlocker,
    int AttemptCount,
    string? ExternalActionStatus,
    string? ExternalReferenceId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record DigitalWorkExecutionProcessingResult(
    int ProcessedCount,
    int FailedCount,
    int WaitingApprovalCount,
    int ReclaimedCount);

public sealed record DigitalWorkforceRuntimeControlCenterSnapshot(
    DigitalWorkforceControlCenterSnapshot Base,
    int ActiveWorkers,
    int RunningExecutions,
    int QueuedExecutions,
    int BlockedExecutions,
    int PausedWorkers,
    int FailedExecutions);
