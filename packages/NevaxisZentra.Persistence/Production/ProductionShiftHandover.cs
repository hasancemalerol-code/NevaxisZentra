using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Production;

/// <summary>
/// Immutable handover record capturing human operational context when production crosses shifts.
/// Created by the outgoing operator/supervisor before shift reassignment.
/// Once created, this record is never modified.
/// </summary>
public sealed class ProductionShiftHandover : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionOrderId { get; set; }

    /// <summary>The shift assignment being handed over from.</summary>
    public Guid FromShiftAssignmentId { get; set; }

    /// <summary>
    /// The next shift assignment, if already assigned at handover creation time.
    /// May be null if the next shift was not yet assigned.
    /// </summary>
    public Guid? ToShiftAssignmentId { get; set; }

    /// <summary>Mandatory operational note. Must describe production state at handover.</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>Optional summary of open issues for the incoming shift.</summary>
    public string? OpenIssuesSummary { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;

    public Guid CorrelationId { get; set; }
}
