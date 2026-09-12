namespace NevaxisFactOS.Persistence.AiTasks.Delivery;

public sealed class UnavailableAiTaskDeliveryRepository : IAiTaskDeliveryRepository
{
    public Task<AiTaskDeliveryWriteResult> EnqueueAsync(
        AiTaskOperationContext context,
        EnqueueAiTaskDeliveryCommand command,
        CancellationToken cancellationToken = default) =>
        Unavailable();

    public Task<AiTaskDeliveryDetailReadModel?> GetByIdAsync(
        AiTaskOperationContext context,
        Guid deliveryId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AiTaskDeliveryDetailReadModel?>(null);

    public Task<IReadOnlyList<AiTaskDeliveryReadModel>> ListForTaskAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AiTaskDeliveryReadModel>>([]);

    public Task<AiTaskDeliveryListReadModel> ListAsync(
        AiTaskOperationContext context,
        AiTaskDeliveryListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiTaskDeliveryListReadModel([], 0, 1, 25));

    public Task<AiTaskDeliveryWriteResult> RetryAsync(
        AiTaskOperationContext context,
        Guid deliveryId,
        CancellationToken cancellationToken = default) =>
        Unavailable();

    public Task<AiTaskDeliveryWriteResult> CancelAsync(
        AiTaskOperationContext context,
        Guid deliveryId,
        CancellationToken cancellationToken = default) =>
        Unavailable();

    public Task<AiTaskDeliveryWorkerHealthReadModel> GetWorkerHealthAsync(
        AiTaskOperationContext context,
        bool includeDevelopmentChannels,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiTaskDeliveryWorkerHealthReadModel(
            false, null, 0, 0, 0, null, []));

    private static Task<AiTaskDeliveryWriteResult> Unavailable() =>
        Task.FromResult(new AiTaskDeliveryWriteResult(
            false,
            AiTaskDeliveryFailureCodes.Unavailable,
            "Delivery services are unavailable.",
            null));
}

public sealed class UnavailableAiTaskSecureAccessRepository : IAiTaskSecureAccessRepository
{
    public Task<AiTaskSecureAccessIssueResult> IssueAsync(
        AiTaskOperationContext context,
        IssueAiTaskSecureAccessCommand command,
        CancellationToken cancellationToken = default) =>
        Unavailable();

    public Task<IReadOnlyList<AiTaskSecureAccessGrantReadModel>> ListForTaskAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AiTaskSecureAccessGrantReadModel>>([]);

    public Task<AiTaskSecureAccessIssueResult> RevokeAsync(
        AiTaskOperationContext context,
        Guid grantId,
        string? reason,
        CancellationToken cancellationToken = default) =>
        Unavailable();

    public Task<AiTaskSecureAccessIssueResult> RotateAsync(
        AiTaskOperationContext context,
        Guid grantId,
        string ttlPolicy,
        CancellationToken cancellationToken = default) =>
        Unavailable();

    public Task<AiTaskSecureAccessResolveResult> ResolvePublicAsync(
        string rawToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiTaskSecureAccessResolveResult(
            false,
            AiTaskSecureAccessFailureCodes.Unavailable,
            "Secure access is unavailable.",
            null));

    private static Task<AiTaskSecureAccessIssueResult> Unavailable() =>
        Task.FromResult(new AiTaskSecureAccessIssueResult(
            false,
            AiTaskSecureAccessFailureCodes.Unavailable,
            "Secure access is unavailable.",
            null,
            null,
            null));
}

public sealed class UnavailableAiTaskDeliveryWorker : IAiTaskDeliveryWorker
{
    public Task<int> ProcessBatchAsync(string workerId, int batchSize, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task RecordHeartbeatAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
