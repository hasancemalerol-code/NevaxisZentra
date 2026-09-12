using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Facilities;

public sealed class FacilityMembership : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid FacilityId { get; set; }

    public Guid TenantMembershipId { get; set; }

    public FacilityMembershipStatus Status { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public uint Version { get; set; }
}
