using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Identity;

public sealed class TenantMembership : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid IdentitySubjectId { get; set; }

    public TenantMembershipStatus Status { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
