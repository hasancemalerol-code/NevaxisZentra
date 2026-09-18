namespace NevaxisZentra.Persistence.SaasBilling;

public sealed record SaasBillingOperationContext(
    Guid TenantId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record SaasBillingAccountReadModel(
    Guid BillingAccountId,
    string LegalName,
    string DisplayName,
    string BillingContactMasked,
    string BillingAddressSnapshot,
    string? TaxCountryCode,
    string? TaxNumberMasked,
    string PreferredBillingCadence,
    string? PreferredPaymentMethod,
    long Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record SaasBillingInvoiceLineReadModel(
    Guid LineId,
    string DescriptionSnapshot,
    string ComponentType,
    decimal Quantity,
    decimal UnitAmount,
    decimal LineSubtotalAmount,
    string SourceProvenance,
    int SortOrder);

public sealed record SaasBillingInvoiceReadModel(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid BillingAccountId,
    Guid SubscriptionId,
    Guid CommercialPlanVersionId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    string CurrencyCode,
    decimal SubtotalAmount,
    decimal? TaxAmount,
    decimal TotalAmount,
    string InvoiceStatus,
    DateTimeOffset? IssuedAtUtc,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset? PaidAtUtc,
    DateTimeOffset? VoidedAtUtc,
    string SourceProvenance,
    long Version,
    IReadOnlyList<SaasBillingInvoiceLineReadModel> Lines);

public sealed record SaasBillingPaymentAttemptReadModel(
    Guid PaymentAttemptId,
    Guid InvoiceId,
    string PaymentMethod,
    string? ProviderKey,
    string? ExternalReferenceMasked,
    decimal Amount,
    string CurrencyCode,
    string PaymentStatus,
    string? FailureCode,
    long Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record SaasBillingAuditEventReadModel(
    Guid AuditEventId,
    string EventType,
    string EntityType,
    string EntityReference,
    string ActorReference,
    string PayloadJson,
    Guid? CorrelationId,
    DateTimeOffset OccurredAtUtc);

public sealed record SaasBillingWorkspaceSummaryReadModel(
    SaasBillingAccountReadModel? BillingAccount,
    IReadOnlyList<SaasBillingInvoiceSummaryReadModel> Invoices,
    IReadOnlyList<SaasBillingPaymentAttemptReadModel> RecentPaymentAttempts,
    SaasBillingProviderStatusReadModel ProviderStatus,
    int OpenInvoiceCount,
    int OverdueInvoiceCount,
    int AwaitingVerificationCount);

public sealed record SaasBillingInvoiceSummaryReadModel(
    Guid InvoiceId,
    string InvoiceNumber,
    string InvoiceStatus,
    string CurrencyCode,
    decimal TotalAmount,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset? PaidAtUtc);

public sealed record SaasBillingProviderStatusReadModel(
    string ProviderKey,
    string ProviderMode,
    bool IsProduction,
    bool CardCheckoutAvailable,
    string CardCheckoutStatus,
    IReadOnlyList<string> SupportedPaymentMethods);

public sealed record UpdateSaasBillingAccountCommand(
    string LegalName,
    string DisplayName,
    string BillingContactMasked,
    string BillingAddressSnapshot,
    string? TaxCountryCode,
    string? TaxNumberMasked,
    string PreferredBillingCadence,
    string? PreferredPaymentMethod,
    long ExpectedVersion,
    string? IdempotencyKey);

public sealed record CreateSaasBillingInvoiceCommand(
    Guid SubscriptionId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    string? IdempotencyKey);

public sealed record TransitionSaasBillingInvoiceCommand(
    Guid InvoiceId,
    string TargetStatus,
    long ExpectedVersion,
    string? IdempotencyKey);

public sealed record CreateSaasBillingPaymentAttemptCommand(
    Guid InvoiceId,
    string PaymentMethod,
    decimal Amount,
    string CurrencyCode,
    string? ExternalReferenceMasked,
    string? IdempotencyKey);

public sealed record VerifySaasBillingBankTransferCommand(
    Guid PaymentAttemptId,
    long ExpectedVersion,
    string? IdempotencyKey);

public sealed record InitializeCardCheckoutCommand(
    Guid InvoiceId,
    long InvoiceExpectedVersion,
    string? IdempotencyKey);

public sealed record InitializeCardCheckoutResult(
    Guid PaymentAttemptId,
    Guid CheckoutSessionId,
    string HostedCheckoutUrl,
    long PaymentAttemptVersion);

public sealed record VerifyCheckoutReturnCommand(
    Guid PaymentAttemptId,
    string CheckoutSessionToken,
    string? IdempotencyKey);

public sealed record ProcessProviderWebhookCommand(
    string ProviderKey,
    string ProviderEventIdentityHash,
    string NormalizedStatus,
    string? ProviderPaymentReferenceMasked,
    string? CheckoutSessionReferenceMasked,
    decimal? Amount,
    string? CurrencyCode,
    string SanitizedEvidenceJson,
    string? IdempotencyKey);

public sealed record SaasBillingPaymentAttemptDetailReadModel(
    Guid PaymentAttemptId,
    Guid InvoiceId,
    string PaymentMethod,
    string? ProviderKey,
    string? ExternalReferenceMasked,
    decimal Amount,
    string CurrencyCode,
    string PaymentStatus,
    string? FailureCode,
    long Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    Guid? CheckoutSessionId,
    string? CheckoutSessionStatus);

public sealed record SaasBillingWriteResult(
    bool Succeeded,
    string? FailureCode = null,
    string? FailureMessage = null,
    bool IdempotentReplay = false,
    Guid? EntityId = null)
{
    public static SaasBillingWriteResult Ok(Guid? entityId = null, bool idempotentReplay = false) =>
        new(true, EntityId: entityId, IdempotentReplay: idempotentReplay);

    public static SaasBillingWriteResult Fail(string failureCode, string failureMessage) =>
        new(false, failureCode, failureMessage);
}

public sealed record SaasBillingWriteResult<T>(
    bool Succeeded,
    T? Value,
    string? FailureCode = null,
    string? FailureMessage = null,
    bool IdempotentReplay = false)
{
#pragma warning disable CA1000 // Do not declare static members on generic types
    public static SaasBillingWriteResult<T> Ok(T value, bool idempotentReplay = false) =>
        new(true, value, IdempotentReplay: idempotentReplay);

    public static SaasBillingWriteResult<T> Fail(string failureCode, string failureMessage) =>
        new(false, default, failureCode, failureMessage);
#pragma warning restore CA1000
}

public sealed record BillingPaymentProviderResult(
    string Status,
    string? FailureCode,
    string? ExplanationKey,
    bool Deferred);

public interface IBillingPaymentProvider
{
    string ProviderKey { get; }

    bool IsProduction { get; }

    Task<BillingPaymentProviderResult> InitiateCardCheckoutAsync(
        Guid invoiceId,
        decimal amount,
        string currencyCode,
        CancellationToken cancellationToken = default);
}
