using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.SaasBilling;

public static class SaasBillingCheckoutSessionStatuses
{
    public const string Pending = "pending";
    public const string Completed = "completed";
    public const string Expired = "expired";
    public const string Cancelled = "cancelled";
}

public sealed class SaasBillingCheckoutSession : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid PaymentAttemptId { get; set; }

    public Guid InvoiceId { get; set; }

    public string ProviderKey { get; set; } = string.Empty;

    public string ConversationId { get; set; } = string.Empty;

    public string CheckoutSessionReferenceMasked { get; set; } = string.Empty;

    public string? HostedCheckoutUrl { get; set; }

    public string SessionStatus { get; set; } = SaasBillingCheckoutSessionStatuses.Pending;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public long Version { get; set; } = 1;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class SaasBillingProviderEvent : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string ProviderKey { get; set; } = string.Empty;

    public string ProviderEventIdentityHash { get; set; } = string.Empty;

    public Guid? PaymentAttemptId { get; set; }

    public Guid? InvoiceId { get; set; }

    public string NormalizedStatus { get; set; } = string.Empty;

    public decimal? Amount { get; set; }

    public string? CurrencyCode { get; set; }

    public string SanitizedEvidenceJson { get; set; } = "{}";

    public string? IdempotencyKey { get; set; }

    public DateTimeOffset ProcessedAtUtc { get; set; }
}
