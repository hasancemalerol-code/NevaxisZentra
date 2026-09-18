using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Production;

/// <summary>
/// Immutable record of a manually observed production cycle.
/// Source is always "manual" in 3L; future Machine/Edge packages may add "machine" source.
/// CycleNumber is a per-order sequence number incremented atomically alongside the order version.
/// Records are never updated or deleted.
/// </summary>
public sealed class ProductionCycleEntry : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionOrderId { get; set; }
    public int CycleNumber { get; set; }
    public DateTimeOffset CycleStartedAtUtc { get; set; }
    public DateTimeOffset CycleEndedAtUtc { get; set; }
    public long CycleDurationMilliseconds { get; set; }
    public decimal? QuantityProduced { get; set; }
    public string RecordedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset RecordedAtUtc { get; set; }

    /// <summary>Always "manual" in 3L. Future: "machine".</summary>
    public string Source { get; set; } = "manual";

    public string? Comment { get; set; }
    public Guid CorrelationId { get; set; }
}
