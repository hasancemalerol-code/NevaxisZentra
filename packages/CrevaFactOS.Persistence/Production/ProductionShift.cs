using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Production;

/// <summary>
/// Recurring shift definition for a facility.
/// Represents the schedule template (e.g. 08:00–16:00), not a calendar occurrence.
/// Actual UTC windows are derived at assignment time using the facility's TimeZoneId.
/// </summary>
public sealed class ProductionShift : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }

    /// <summary>Short code displayed in UI and filters (e.g. "V1", "V2", "V3").</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Local wall-clock start time of the shift (e.g. "08:00").
    /// Stored as a TimeSpan from midnight.
    /// Overnight shifts: StartLocalTime > EndLocalTime.
    /// </summary>
    public TimeSpan StartLocalTime { get; set; }

    /// <summary>
    /// Local wall-clock end time of the shift (e.g. "16:00").
    /// For overnight shifts (e.g. V3 00:00–08:00) EndLocalTime < StartLocalTime.
    /// </summary>
    public TimeSpan EndLocalTime { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public uint Version { get; set; } = 1;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
