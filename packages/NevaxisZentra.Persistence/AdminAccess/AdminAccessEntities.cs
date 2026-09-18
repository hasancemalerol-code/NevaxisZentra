using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.AdminAccess;

public sealed class TenantMembershipPermissionOverride : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid TenantMembershipId { get; set; }

    public Guid PermissionDefinitionId { get; set; }

    public required string Effect { get; set; }

    public string? RecordScope { get; set; }

    public DateTimeOffset? EffectiveFromUtc { get; set; }

    public DateTimeOffset? EffectiveUntilUtc { get; set; }

    public required string Status { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public uint Version { get; set; }
}

public sealed class AuthorizationAuditEvent : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public required string EventType { get; set; }

    public required string EntityType { get; set; }

    public required string EntityReference { get; set; }

    public required string ActorReference { get; set; }

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }

    public Guid? CorrelationId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class AdminAccessIdempotencyRecord : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public required string IdempotencyKey { get; set; }

    public required string RequestHash { get; set; }

    public required string Operation { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
