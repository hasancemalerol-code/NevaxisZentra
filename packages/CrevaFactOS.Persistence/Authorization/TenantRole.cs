using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Authorization;

public sealed class TenantRole : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Key { get; set; }
    public required string DisplayName { get; set; }
    public AuthorizationStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
