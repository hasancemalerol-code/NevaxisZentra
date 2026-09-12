namespace NevaxisFactOS.Persistence.SaasSubscription;

public interface ISaasSubscriptionRepository
{
    Task<SaasSubscriptionSummaryReadModel?> GetCurrentSubscriptionSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaasSubscriptionTransitionHistoryReadModel>> ListTransitionHistoryAsync(
        Guid subscriptionId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaasSubscriptionAuditEventReadModel>> ListAuditEventsAsync(
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<SaasSubscriptionWriteResult> AssignOrChangeDevelopmentSubscriptionAsync(
        AssignDevelopmentSaasSubscriptionCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<SaasSubscriptionWriteResult> TransitionSubscriptionAsync(
        TransitionSaasSubscriptionCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);
}
