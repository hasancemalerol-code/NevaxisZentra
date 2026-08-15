using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.SaasBilling;

public sealed class SaasTenantBillingAccount : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string LegalName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string BillingContactMasked { get; set; } = string.Empty;

    public string BillingAddressSnapshot { get; set; } = string.Empty;

    public string? TaxCountryCode { get; set; }

    public string? TaxNumberMasked { get; set; }

    public string PreferredBillingCadence { get; set; } = string.Empty;

    public string? PreferredPaymentMethod { get; set; }

    public long Version { get; set; } = 1;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class SaasBillingInvoice : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid BillingAccountId { get; set; }

    public Guid SubscriptionId { get; set; }

    public Guid CommercialPlanVersionId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTimeOffset PeriodStartUtc { get; set; }

    public DateTimeOffset PeriodEndUtc { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public decimal SubtotalAmount { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string InvoiceStatus { get; set; } = SaasBillingInvoiceStatuses.Draft;

    public DateTimeOffset? IssuedAtUtc { get; set; }

    public DateTimeOffset? DueAtUtc { get; set; }

    public DateTimeOffset? PaidAtUtc { get; set; }

    public DateTimeOffset? VoidedAtUtc { get; set; }

    public string SourceProvenance { get; set; } = string.Empty;

    public string? IdempotencyKey { get; set; }

    public int InvoiceSequence { get; set; }

    public long Version { get; set; } = 1;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<SaasBillingInvoiceLine> Lines { get; set; } = [];
}

public sealed class SaasBillingInvoiceLine : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid InvoiceId { get; set; }

    public string DescriptionSnapshot { get; set; } = string.Empty;

    public string ComponentType { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitAmount { get; set; }

    public decimal LineSubtotalAmount { get; set; }

    public string SourceProvenance { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SaasBillingPaymentAttempt : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid InvoiceId { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string? ProviderKey { get; set; }

    public string? ExternalReferenceMasked { get; set; }

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = SaasBillingPaymentStatuses.Pending;

    public string? FailureCode { get; set; }

    public string? IdempotencyKey { get; set; }

    public long Version { get; set; } = 1;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class SaasBillingIdempotencyRecord : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public string RequestHash { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SaasBillingAuditEvent : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string EntityReference { get; set; } = string.Empty;

    public string ActorReference { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public Guid? CorrelationId { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }
}
