using NevaxisZentra.Persistence.SaasBilling.Payments;

namespace NevaxisZentra.Persistence.SaasBilling;

public interface ISaasBillingRepository
{
    Task<SaasBillingWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        SaasBillingProviderStatusReadModel providerStatus,
        CancellationToken cancellationToken = default);

    Task<SaasBillingAccountReadModel?> GetBillingAccountAsync(
        CancellationToken cancellationToken = default);

    Task<SaasBillingInvoiceReadModel?> GetInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaasBillingInvoiceSummaryReadModel>> ListInvoicesAsync(
        string? statusFilter,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string? search,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaasBillingPaymentAttemptReadModel>> ListPaymentAttemptsAsync(
        Guid? invoiceId,
        CancellationToken cancellationToken = default);

    Task<SaasBillingWriteResult> UpdateBillingAccountAsync(
        UpdateSaasBillingAccountCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<SaasBillingWriteResult> CreateDraftInvoiceAsync(
        CreateSaasBillingInvoiceCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<SaasBillingWriteResult> TransitionInvoiceAsync(
        TransitionSaasBillingInvoiceCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<SaasBillingWriteResult> CreatePaymentAttemptAsync(
        CreateSaasBillingPaymentAttemptCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<SaasBillingWriteResult> VerifyBankTransferAsync(
        VerifySaasBillingBankTransferCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<SaasBillingWriteResult<InitializeCardCheckoutResult>> InitializeCardCheckoutAsync(
        InitializeCardCheckoutCommand command,
        ISaasPaymentProvider paymentProvider,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<SaasBillingWriteResult> VerifyCheckoutReturnAsync(
        VerifyCheckoutReturnCommand command,
        ISaasPaymentProvider paymentProvider,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<SaasBillingWriteResult> ProcessProviderWebhookAsync(
        ProcessProviderWebhookCommand command,
        string actorReference,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<SaasBillingPaymentAttemptDetailReadModel?> GetPaymentAttemptDetailAsync(
        Guid paymentAttemptId,
        CancellationToken cancellationToken = default);

    Task<SaasBillingCheckoutSession?> FindCheckoutSessionByReferenceAsync(
        string checkoutSessionReferenceMasked,
        CancellationToken cancellationToken = default);
}
