using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Production;

/// <summary>
/// Immutable operational record of a single quantity write against a production order.
/// Records are never updated or deleted; corrections use a Reversal entry referencing the original.
/// </summary>
public sealed class ProductionExecutionEntry : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionOrderId { get; set; }
    public ProductionExecutionEntryType EntryType { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "piece";
    public string? ReasonCode { get; set; }
    public string? Comment { get; set; }
    public string RecordedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset RecordedAtUtc { get; set; }
    public Guid? ReversesEntryId { get; set; }
    public Guid CorrelationId { get; set; }
    public uint Version { get; set; } = 1;
}
