using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Production;

/// <summary>
/// Represents an operational issue on a production order that requires human attention.
/// Distinct from downtime (session-oriented) and quality NCR (out of scope in 4L).
/// Records are never deleted; only INSERT and status transitions (acknowledge, resolve) are permitted.
/// Named "Record" to avoid the CA1711 restriction on types ending in "Exception".
/// </summary>
public sealed class ProductionExecutionExceptionRecord : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionOrderId { get; set; }

    /// <summary>
    /// Typed exception type. Allowed values defined in <see cref="ProductionExceptionTypes"/>.
    /// </summary>
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>
    /// Typed severity. Allowed values defined in <see cref="ProductionExceptionSeverities"/>.
    /// Critical severity does NOT automatically stop production; human action is required.
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Typed status. Allowed values defined in <see cref="ProductionExceptionStatuses"/>.
    /// Transitions: open → acknowledged, open → resolved, acknowledged → resolved.
    /// </summary>
    public string Status { get; set; } = ProductionExceptionStatuses.Open;

    /// <summary>Human-readable description of the issue. Max 500 chars.</summary>
    public string Message { get; set; } = string.Empty;

    public DateTimeOffset RaisedAtUtc { get; set; }
    public string RaisedBySubjectId { get; set; } = string.Empty;

    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public string? ResolvedBySubjectId { get; set; }

    /// <summary>Required when resolving. Documents how the exception was addressed.</summary>
    public string? ResolutionNote { get; set; }

    public Guid CorrelationId { get; set; }
    public uint Version { get; set; } = 1;

    // ── 5L: Exception ownership ───────────────────────────────────────────────
    // Populated when an exception is assigned to a specific operator for resolution.

    public string? AssignedToSubjectId { get; set; }
    public DateTimeOffset? AssignedAtUtc { get; set; }
    public string? AssignedBySubjectId { get; set; }

    // ── AI Task Center follow-up integration ────────────────────────────────────
    public Guid? LinkedAiTaskId { get; set; }
    public string FollowUpStatus { get; set; } = ProductionFollowUpStatuses.RequiresReview;
    public string? FollowUpUpdatedBy { get; set; }
}

public sealed class ProductionFollowUpAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ExceptionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public ProductionExecutionExceptionRecord? Exception { get; set; }
}

/// <summary>
/// Typed exception type allowlist for <see cref="ProductionExecutionException.ExceptionType"/>.
/// </summary>
public static class ProductionExceptionTypes
{
    public const string StaleDowntime = "stale-downtime";
    public const string StaleSetup = "stale-setup";
    public const string QuantityAnomaly = "quantity-anomaly";
    public const string CycleAnomaly = "cycle-anomaly";
    public const string ScheduleOverrun = "schedule-overrun";
    public const string HandoffInconsistency = "handoff-inconsistency";
    public const string OperatorAttention = "operator-attention";
    public const string Other = "other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        StaleDowntime, StaleSetup, QuantityAnomaly, CycleAnomaly,
        ScheduleOverrun, HandoffInconsistency, OperatorAttention, Other,
    };
}

/// <summary>
/// Typed severity allowlist for <see cref="ProductionExecutionException.Severity"/>.
/// </summary>
public static class ProductionExceptionSeverities
{
    public const string Info = "info";
    public const string Warning = "warning";
    public const string Critical = "critical";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Info, Warning, Critical,
    };
}

/// <summary>
/// Typed status allowlist for <see cref="ProductionExecutionException.Status"/>.
/// </summary>
public static class ProductionExceptionStatuses
{
    public const string Open = "open";
    public const string Acknowledged = "acknowledged";
    public const string Resolved = "resolved";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Open, Acknowledged, Resolved,
    };

    /// <summary>Statuses that represent an unresolved exception for lifecycle guards.</summary>
    public static readonly IReadOnlySet<string> Unresolved = new HashSet<string>(StringComparer.Ordinal)
    {
        Open, Acknowledged,
    };
}
