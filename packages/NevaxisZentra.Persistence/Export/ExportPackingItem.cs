using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Export;

public sealed class ExportPackingItem : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ExportCaseId { get; set; }
    public Guid PackingUnitId { get; set; }
    public Guid? SourceOrderLineId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string? LotBatchReference { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
}
