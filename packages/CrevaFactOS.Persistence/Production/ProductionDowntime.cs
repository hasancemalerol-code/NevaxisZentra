using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Production;

/// <summary>
/// Represents a single downtime session recorded against a production order.
/// A downtime session also transitions the order to Paused (start) and back to InProgress (end).
/// At most one active (EndedAtUtc IS NULL) downtime may exist per ProductionOrder at any time.
/// Records are never deleted; only INSERT and UPDATE (to end the session) are permitted.
/// </summary>
public sealed class ProductionDowntime : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionOrderId { get; set; }

    /// <summary>"planned" or "unplanned"</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Typed downtime reason code.</summary>
    public string ReasonCode { get; set; } = string.Empty;

    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }

    /// <summary>Derived when session is ended: (EndedAtUtc - StartedAtUtc).TotalSeconds.</summary>
    public long? DurationSeconds { get; set; }

    /// <summary>Required when ReasonCode is "other".</summary>
    public string? Comment { get; set; }

    public string StartedBySubjectId { get; set; } = string.Empty;
    public string? EndedBySubjectId { get; set; }

    // ── 4L: Recovery metadata ─────────────────────────────────────────────────
    // Populated only when a stale downtime is manually recovered via the recovery endpoint.
    // Kept separate from EndedAtUtc / EndedBySubjectId to preserve recovery provenance.

    public DateTimeOffset? RecoveredAtUtc { get; set; }
    public string? RecoveredBySubjectId { get; set; }
    public string? RecoveryReason { get; set; }

    public Guid CorrelationId { get; set; }
    public uint Version { get; set; } = 1;
}
