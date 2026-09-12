using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Facilities;

public sealed class Facility : ITenantScopedEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public FacilityStatus Status { get; set; }

    public string TimeZoneId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public uint Version { get; set; }
}
