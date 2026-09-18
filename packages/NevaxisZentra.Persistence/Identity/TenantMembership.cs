using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Identity;

public sealed class TenantMembership : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid IdentitySubjectId { get; set; }

    public string MembershipReference { get; set; } = string.Empty;

    public TenantMembershipStatus Status { get; set; }

    public uint AuthorizationVersion { get; set; } = 1;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public static string CreateReference(Guid membershipId) => $"mem_{membershipId:N}";
}
