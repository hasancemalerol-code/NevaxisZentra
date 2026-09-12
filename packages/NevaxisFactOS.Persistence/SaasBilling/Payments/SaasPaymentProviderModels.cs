namespace NevaxisFactOS.Persistence.SaasBilling.Payments;

public static class SaasPaymentProviderKeys
{
    public const string Iyzico = "iyzico";
}

public static class SaasPaymentProviderModes
{
    public const string NotConfigured = "not-configured";
    public const string Sandbox = "sandbox";
    public const string Live = "live";
}

public static class SaasPaymentNormalizedStatuses
{
    public const string Pending = "pending";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
    public const string Refunded = "refunded";
    public const string ReviewRequired = "review-required";
}

public sealed record SaasPaymentProviderStatus(
    string ProviderKey,
    string Mode,
    bool IsConfigured,
    bool IsProduction,
    bool CardCheckoutAvailable,
    string CardCheckoutStatus);

public sealed record SaasPaymentCheckoutInitializationRequest(
    Guid TenantId,
    Guid InvoiceId,
    Guid PaymentAttemptId,
    string ConversationId,
    decimal Amount,
    string CurrencyCode,
    string BuyerReferenceMasked,
    string CallbackUrl);

public sealed record SaasPaymentCheckoutInitializationResult(
    bool Succeeded,
    string? FailureCode,
    string? CheckoutSessionReferenceMasked,
    string? HostedCheckoutUrl,
    DateTimeOffset? ExpiresAtUtc);

public sealed record SaasPaymentCheckoutVerificationRequest(
    string CheckoutSessionToken,
    string ConversationId);

public sealed record SaasPaymentCheckoutVerificationResult(
    bool Succeeded,
    string NormalizedStatus,
    string? FailureCode,
    decimal? PaidAmount,
    string? CurrencyCode,
    string? ProviderPaymentReferenceMasked,
    string? SanitizedEvidenceJson);

public sealed record SaasPaymentWebhookValidationRequest(
    string RawBody,
    IReadOnlyDictionary<string, string> Headers);

public sealed record SaasPaymentWebhookValidationResult(
    bool IsValid,
    string? FailureCode);

public sealed record SaasPaymentNormalizedEvent(
    string ProviderEventIdentity,
    string NormalizedStatus,
    string? ProviderPaymentReferenceMasked,
    string? CheckoutSessionReferenceMasked,
    decimal? Amount,
    string? CurrencyCode,
    string SanitizedEvidenceJson);

public sealed record SaasPaymentStateQueryRequest(
    string ProviderPaymentReferenceMasked,
    string ConversationId);

public sealed record SaasPaymentStateQueryResult(
    bool Succeeded,
    string NormalizedStatus,
    string? FailureCode,
    decimal? Amount,
    string? CurrencyCode,
    string? SanitizedEvidenceJson);

public interface ISaasPaymentProvider
{
    string ProviderKey { get; }

    SaasPaymentProviderStatus GetStatus();

    Task<SaasPaymentCheckoutInitializationResult> InitializeHostedCheckoutAsync(
        SaasPaymentCheckoutInitializationRequest request,
        CancellationToken cancellationToken = default);

    Task<SaasPaymentCheckoutVerificationResult> RetrieveCheckoutResultAsync(
        SaasPaymentCheckoutVerificationRequest request,
        CancellationToken cancellationToken = default);

    SaasPaymentWebhookValidationResult ValidateWebhookSignature(
        SaasPaymentWebhookValidationRequest request);

    SaasPaymentNormalizedEvent? NormalizeProviderEvent(string rawBody);

    Task<SaasPaymentStateQueryResult> QueryPaymentStateAsync(
        SaasPaymentStateQueryRequest request,
        CancellationToken cancellationToken = default);
}
