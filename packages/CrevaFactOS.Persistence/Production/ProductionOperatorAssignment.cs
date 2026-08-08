using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Production;

/// <summary>
/// Operator assignment to a production order for a specific role.
/// At most one active primary-operator (ReleasedAtUtc IS NULL) per ProductionOrder.
/// Multiple operator and supervisor assignments may be active simultaneously.
/// </summary>
public sealed class ProductionOperatorAssignment : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionOrderId { get; set; }

    /// <summary>Identity subject (user) being assigned. Must be a facility member.</summary>
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>
    /// Role of this assignment.
    /// Allowed values: "primary-operator" | "operator" | "supervisor".
    /// Validated against <see cref="ProductionOperatorRoles.All"/>.
    /// </summary>
    public string AssignmentRole { get; set; } = string.Empty;

    public DateTimeOffset AssignedAtUtc { get; set; }
    public string AssignedBySubjectId { get; set; } = string.Empty;

    /// <summary>Set on explicit release or atomically on order completion.</summary>
    public DateTimeOffset? ReleasedAtUtc { get; set; }
    public string? ReleasedBySubjectId { get; set; }
    public string? ReleaseReason { get; set; }

    public Guid CorrelationId { get; set; }
    public uint Version { get; set; } = 1;
}

/// <summary>
/// Allowed assignment roles for production operator assignments.
/// Machine-comparable string set.
/// </summary>
public static class ProductionOperatorRoles
{
    public const string PrimaryOperator = "primary-operator";
    public const string Operator = "operator";
    public const string Supervisor = "supervisor";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        PrimaryOperator,
        Operator,
        Supervisor,
    };
}
