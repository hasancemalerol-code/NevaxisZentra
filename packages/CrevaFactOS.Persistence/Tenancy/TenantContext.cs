namespace CrevaFactOS.Persistence.Tenancy;

public sealed record TenantContext(Guid TenantId, string TenantKey, Guid MembershipId);
