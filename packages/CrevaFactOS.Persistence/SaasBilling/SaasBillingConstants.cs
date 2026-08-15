namespace CrevaFactOS.Persistence.SaasBilling;

public static class SaasBillingInvoiceStatuses
{
    public const string Draft = "draft";
    public const string Open = "open";
    public const string Paid = "paid";
    public const string Overdue = "overdue";
    public const string Void = "void";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Draft, Open, Paid, Overdue, Void,
    };

    public static readonly IReadOnlySet<string> MutableLineStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        Draft,
    };

    public static readonly IReadOnlySet<string> TerminalStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        Paid, Void,
    };
}

public static class SaasBillingPaymentStatuses
{
    public const string Pending = "pending";
    public const string AwaitingManualVerification = "awaiting-manual-verification";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
    public const string Refunded = "refunded";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Pending, AwaitingManualVerification, Succeeded, Failed, Cancelled, Refunded,
    };
}

public static class SaasBillingPaymentMethods
{
    public const string Card = "card";
    public const string BankTransfer = "bank-transfer";
    public const string EnterpriseInvoice = "enterprise-invoice";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Card, BankTransfer, EnterpriseInvoice,
    };
}

public static class SaasBillingProvenance
{
    public const string LocalDevelopmentPilot = "local-development-pilot";
    public const string NonProduction = "non-production";
    public const string BillingSystem = "billing-system";
    public const string ManualOverride = "manual-override";
}

public static class SaasBillingAuditEventTypes
{
    public const string BillingAccountUpdated = "saas-billing.account.updated";
    public const string InvoiceCreated = "saas-billing.invoice.created";
    public const string InvoiceOpened = "saas-billing.invoice.opened";
    public const string InvoiceVoided = "saas-billing.invoice.voided";
    public const string PaymentAttemptCreated = "saas-billing.payment-attempt.created";
    public const string PaymentAttemptVerified = "saas-billing.payment-attempt.verified";
    public const string PaymentAttemptFailed = "saas-billing.payment-attempt.failed";
}

public static class SaasBillingAuditEntityTypes
{
    public const string BillingAccount = "saas-billing-account";
    public const string Invoice = "saas-billing-invoice";
    public const string PaymentAttempt = "saas-billing-payment-attempt";
}

public static class SaasBillingFailureCodes
{
    public const string Conflict = "saas.billing.conflict";
    public const string ValidationFailed = "saas.billing.validation-failed";
    public const string NotFound = "saas.billing.not-found";
    public const string ConcurrencyConflict = "saas.billing.concurrency-conflict";
    public const string IdempotencyConflict = "saas.billing.idempotency-conflict";
    public const string IdempotencyUnverifiable = "saas.billing.idempotency-unverifiable";
    public const string InvalidTransition = "saas.billing.invalid-transition";
    public const string Unavailable = "saas.billing.unavailable";
    public const string AmountExceeded = "saas.billing.amount-exceeded";
    public const string CurrencyMismatch = "saas.billing.currency-mismatch";
    public const string ReconciliationFailed = "saas.billing.reconciliation-failed";
    public const string ProviderNotConfigured = "saas.billing.provider-not-configured";
}

public static class SaasBillingValueConversions
{
    public static bool IsValidInvoiceStatus(string status) =>
        SaasBillingInvoiceStatuses.All.Contains(status);

    public static bool IsValidPaymentStatus(string status) =>
        SaasBillingPaymentStatuses.All.Contains(status);

    public static bool IsValidPaymentMethod(string method) =>
        SaasBillingPaymentMethods.All.Contains(method);

    public static bool IsValidInvoiceTransition(string fromStatus, string toStatus) =>
        fromStatus switch
        {
            SaasBillingInvoiceStatuses.Draft => toStatus is SaasBillingInvoiceStatuses.Open
                or SaasBillingInvoiceStatuses.Void,
            SaasBillingInvoiceStatuses.Open => toStatus is SaasBillingInvoiceStatuses.Paid
                or SaasBillingInvoiceStatuses.Overdue
                or SaasBillingInvoiceStatuses.Void,
            SaasBillingInvoiceStatuses.Overdue => toStatus is SaasBillingInvoiceStatuses.Paid
                or SaasBillingInvoiceStatuses.Void,
            _ => false,
        };

    public static bool CanMutateInvoiceLines(string invoiceStatus) =>
        SaasBillingInvoiceStatuses.MutableLineStatuses.Contains(invoiceStatus);

    public static bool IsInvoiceImmutable(string invoiceStatus) =>
        !SaasBillingInvoiceStatuses.MutableLineStatuses.Contains(invoiceStatus);

    public static string FormatInvoiceNumber(Guid tenantId, int sequence) =>
        $"INV-{tenantId.ToString("N")[..8].ToUpperInvariant()}-{sequence:D6}";
}
