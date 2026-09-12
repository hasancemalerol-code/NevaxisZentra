using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Authorization;

public sealed class TenantRole : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Key { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public string? SystemTemplateOrigin { get; set; }
    public bool IsProtected { get; set; }
    public AuthorizationStatus Status { get; set; }
    public uint Version { get; set; } = 1;
    public string? CreatedByActorReference { get; set; }
    public string? UpdatedByActorReference { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
