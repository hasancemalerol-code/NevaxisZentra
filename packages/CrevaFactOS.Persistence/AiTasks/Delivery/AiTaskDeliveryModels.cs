namespace CrevaFactOS.Persistence.AiTasks.Delivery;

public sealed record EnqueueAiTaskDeliveryCommand(
    Guid AiTaskId,
    string Channel,
    string RecipientKind,
    Guid? RecipientUserId,
    string? MaskedRecipient,
    string IdempotencyKey,
    string? DeliveryProjectionJson,
    Guid? SecureAccessGrantId,
    int? MaxAttempts = null);

public sealed record AiTaskDeliveryReadModel(
    Guid Id,
    Guid AiTaskId,
    string Channel,
    string RecipientKind,
    Guid? RecipientUserId,
    string? MaskedRecipient,
    string Status,
    string IdempotencyKey,
    int AttemptCount,
    int MaxAttempts,
    DateTimeOffset? NextAttemptAtUtc,
    string? LastErrorCode,
    string? LastErrorCategory,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset? CancelledAtUtc,
    Guid? SecureAccessGrantId,
    DateTimeOffset CreatedAtUtc);

public sealed record AiTaskDeliveryAttemptReadModel(
    Guid Id,
    int AttemptNumber,
    string AdapterKey,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string Outcome,
    string? ProviderStatusCode,
    string? ProviderMessageId,
    string? ErrorCategory,
    string? SanitizedErrorCode,
    DateTimeOffset? RetryScheduledForUtc);

public sealed record AiTaskDeliveryDetailReadModel(
    AiTaskDeliveryReadModel Delivery,
    IReadOnlyList<AiTaskDeliveryAttemptReadModel> Attempts);

public sealed record AiTaskDeliveryListFilters(
    string? Channel,
    string? Status,
    Guid? TaskId,
    Guid? RecipientUserId,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    int Page = 1,
    int PageSize = 25);

public sealed record AiTaskDeliveryListReadModel(
    IReadOnlyList<AiTaskDeliveryReadModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AiTaskDeliveryWriteResult(
    bool Success,
    string? FailureCode,
    string? FailureMessage,
    AiTaskDeliveryReadModel? Delivery);

public sealed record IssueAiTaskSecureAccessCommand(
    Guid AiTaskId,
    string TtlPolicy,
    string Scope = AiTaskSecureAccessScopes.ReadOnly);

public sealed record AiTaskSecureAccessGrantReadModel(
    Guid Id,
    Guid AiTaskId,
    string Scope,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc,
    int AccessCount,
    DateTimeOffset? LastAccessedAtUtc,
    bool IsActive);

public sealed record AiTaskSecureAccessIssueResult(
    bool Success,
    string? FailureCode,
    string? FailureMessage,
    AiTaskSecureAccessGrantReadModel? Grant,
    string? RawToken,
    string? SecureLinkPath);

public sealed record AiTaskSecureAccessResolveResult(
    bool Success,
    string? FailureCode,
    string? FailureMessage,
    AiTaskSafeExternalProjection? Projection);

public sealed record AiTaskSafeExternalProjection(
    string ProductName,
    string TaskTitle,
    string SafeSummary,
    string Priority,
    string StatusLabel,
    DateTimeOffset? DueAt,
    string? FacilityDisplayName,
    string? MachineDisplayName,
    string? AssignedDepartment,
    string PublicReference,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);

public sealed record AiTaskDeliveryWorkerHealthReadModel(
    bool WorkerEnabled,
    DateTimeOffset? LastHeartbeatUtc,
    int QueuedCount,
    int RetryableCount,
    int DeadLetterCount,
    TimeSpan? OldestQueuedAge,
    IReadOnlyList<AiTaskDeliveryChannelReadiness> ChannelReadiness);

public interface IAiTaskDeliveryRepository
{
    Task<AiTaskDeliveryWriteResult> EnqueueAsync(
        AiTaskOperationContext context,
        EnqueueAiTaskDeliveryCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskDeliveryDetailReadModel?> GetByIdAsync(
        AiTaskOperationContext context,
        Guid deliveryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiTaskDeliveryReadModel>> ListForTaskAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task<AiTaskDeliveryListReadModel> ListAsync(
        AiTaskOperationContext context,
        AiTaskDeliveryListFilters filters,
        CancellationToken cancellationToken = default);

    Task<AiTaskDeliveryWriteResult> RetryAsync(
        AiTaskOperationContext context,
        Guid deliveryId,
        CancellationToken cancellationToken = default);

    Task<AiTaskDeliveryWriteResult> CancelAsync(
        AiTaskOperationContext context,
        Guid deliveryId,
        CancellationToken cancellationToken = default);

    Task<AiTaskDeliveryWorkerHealthReadModel> GetWorkerHealthAsync(
        AiTaskOperationContext context,
        bool includeDevelopmentChannels,
        CancellationToken cancellationToken = default);
}

public interface IAiTaskSecureAccessRepository
{
    Task<AiTaskSecureAccessIssueResult> IssueAsync(
        AiTaskOperationContext context,
        IssueAiTaskSecureAccessCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiTaskSecureAccessGrantReadModel>> ListForTaskAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task<AiTaskSecureAccessIssueResult> RevokeAsync(
        AiTaskOperationContext context,
        Guid grantId,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<AiTaskSecureAccessIssueResult> RotateAsync(
        AiTaskOperationContext context,
        Guid grantId,
        string ttlPolicy,
        CancellationToken cancellationToken = default);

    Task<AiTaskSecureAccessResolveResult> ResolvePublicAsync(
        string rawToken,
        CancellationToken cancellationToken = default);
}

public interface IAiTaskDeliveryWorker
{
    Task<int> ProcessBatchAsync(string workerId, int batchSize, CancellationToken cancellationToken = default);

    Task RecordHeartbeatAsync(CancellationToken cancellationToken = default);
}
