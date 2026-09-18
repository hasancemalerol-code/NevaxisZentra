using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Production;

/// <summary>
/// Represents a setup or changeover session recorded against a production order.
/// At most one active (CompletedAtUtc IS NULL) setup session may exist per ProductionOrder at any time.
/// Records are never deleted.
/// </summary>
public sealed class ProductionSetupSession : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionOrderId { get; set; }

    /// <summary>Typed setup type code.</summary>
    public string SetupType { get; set; } = string.Empty;

    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>Derived when session is completed: (CompletedAtUtc - StartedAtUtc).TotalSeconds.</summary>
    public long? DurationSeconds { get; set; }

    public string? PreviousMachineReference { get; set; }
    public string? PreviousMoldReference { get; set; }
    public string? TargetMachineReference { get; set; }
    public string? TargetMoldReference { get; set; }
    public string? Comment { get; set; }

    public string StartedBySubjectId { get; set; } = string.Empty;
    public string? CompletedBySubjectId { get; set; }

    // ── 4L: Recovery metadata ─────────────────────────────────────────────────
    // Populated only when a stale setup session is manually recovered via the recovery endpoint.
    // Kept separate from CompletedAtUtc / CompletedBySubjectId to preserve recovery provenance.

    public DateTimeOffset? RecoveredAtUtc { get; set; }
    public string? RecoveredBySubjectId { get; set; }
    public string? RecoveryReason { get; set; }

    public Guid CorrelationId { get; set; }
    public uint Version { get; set; } = 1;
}
