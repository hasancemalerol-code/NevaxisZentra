using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.SaasSubscription;

public sealed class SaasTenantSubscription : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid CommercialPlanVersionId { get; set; }

    public string SubscriptionStatus { get; set; } = SaasSubscriptionStatuses.Trialing;

    public string BillingCadence { get; set; } = string.Empty;

    public DateTimeOffset StartsAtUtc { get; set; }

    public DateTimeOffset CurrentPeriodStartUtc { get; set; }

    public DateTimeOffset CurrentPeriodEndUtc { get; set; }

    public DateTimeOffset? TrialEndsAtUtc { get; set; }

    public DateTimeOffset? GraceUntilUtc { get; set; }

    public DateTimeOffset? SuspendedAtUtc { get; set; }

    public DateTimeOffset? CancelledAtUtc { get; set; }

    public bool CancelAtPeriodEnd { get; set; }

    public string SourceProvenance { get; set; } = string.Empty;

    public bool IsCurrent { get; set; } = true;

    public long Version { get; set; } = 1;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class SaasTenantSubscriptionTransitionHistory : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid SubscriptionId { get; set; }

    public string FromStatus { get; set; } = string.Empty;

    public string ToStatus { get; set; } = string.Empty;

    public string TransitionReason { get; set; } = string.Empty;

    public string ActorReference { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string? IdempotencyKey { get; set; }

    public string EvidenceJson { get; set; } = "{}";
}

public sealed class SaasSubscriptionIdempotencyRecord : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public required string IdempotencyKey { get; set; }

    public required string RequestHash { get; set; }

    public required string Operation { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SaasSubscriptionAuditEvent : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public required string EventType { get; set; }

    public required string EntityType { get; set; }

    public required string EntityReference { get; set; }

    public required string ActorReference { get; set; }

    public string PayloadJson { get; set; } = "{}";

    public Guid? CorrelationId { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }
}
