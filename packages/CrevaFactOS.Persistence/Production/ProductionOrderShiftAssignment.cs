using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Production;

/// <summary>
/// A point-in-time shift assignment for a production order.
/// Immutable provenance record: once assigned, history is preserved via ReleasedAtUtc.
/// At most one active assignment (ReleasedAtUtc IS NULL) may exist per ProductionOrder.
/// </summary>
public sealed class ProductionOrderShiftAssignment : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid ProductionShiftId { get; set; }

    /// <summary>Snapshot of shift code at assignment time for read-model efficiency.</summary>
    public string ShiftCode { get; set; } = string.Empty;

    /// <summary>
    /// Calendar date of the shift occurrence (facility-local date, e.g. 2026-08-08).
    /// Stored as DateOnly-compatible UTC midnight to avoid timezone ambiguity in storage.
    /// </summary>
    public DateOnly ShiftDate { get; set; }

    /// <summary>UTC start of the resolved shift window for this date.</summary>
    public DateTimeOffset ShiftStartAtUtc { get; set; }

    /// <summary>UTC end of the resolved shift window for this date.</summary>
    public DateTimeOffset ShiftEndAtUtc { get; set; }

    public DateTimeOffset AssignedAtUtc { get; set; }
    public string AssignedBySubjectId { get; set; } = string.Empty;

    /// <summary>Set when a subsequent shift assignment replaces this one, or at order completion.</summary>
    public DateTimeOffset? ReleasedAtUtc { get; set; }
    public string? ReleasedBySubjectId { get; set; }
    public string? ReleaseReason { get; set; }

    public Guid CorrelationId { get; set; }
    public uint Version { get; set; } = 1;
}
