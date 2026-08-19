namespace CrevaFactOS.Persistence.SaasBilling.Payments;

public static class SaasPaymentFailureCodes
{
    public const string ProviderNotConfigured = "saas.payment.provider-not-configured";
    public const string ProviderUnavailable = "saas.payment.provider-unavailable";
    public const string ValidationFailed = "saas.payment.validation-failed";
    public const string NotFound = "saas.payment.not-found";
    public const string Conflict = "saas.payment.conflict";
    public const string IdempotencyConflict = "saas.payment.idempotency-conflict";
    public const string IdempotencyUnverifiable = "saas.payment.idempotency-unverifiable";
    public const string ConcurrencyConflict = "saas.payment.concurrency-conflict";
    public const string CurrencyMismatch = "saas.payment.currency-mismatch";
    public const string AmountMismatch = "saas.payment.amount-mismatch";
    public const string ReconciliationReviewRequired = "saas.payment.reconciliation-review-required";
    public const string WebhookSignatureInvalid = "saas.payment.webhook-signature-invalid";
    public const string WebhookRejected = "saas.payment.webhook-rejected";
    public const string CheckoutExpired = "saas.payment.checkout-expired";
    public const string CheckoutVerificationFailed = "saas.payment.checkout-verification-failed";
    public const string Unauthorized = "saas.payment.unauthorized";
    public const string Forbidden = "saas.payment.forbidden";
}
