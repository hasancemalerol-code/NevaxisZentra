using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Production;

public sealed class ProductionOrder : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionPlanHandoffId { get; set; }
    public Guid SourceProductionPlanId { get; set; }
    public uint SourceProductionPlanVersion { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public ProductionOrderStatus Status { get; set; }
    public DateTimeOffset PlannedStartAtUtc { get; set; }
    public DateTimeOffset PlannedEndAtUtc { get; set; }
    public DateTimeOffset? ActualStartAtUtc { get; set; }
    public DateTimeOffset? ActualEndAtUtc { get; set; }
    public string? ShiftName { get; set; }
    public string? MachineReference { get; set; }
    public string? MoldReference { get; set; }
    public string? ProductReference { get; set; }
    public decimal? PlannedQuantity { get; set; }
    public string? PlannedQuantityUnit { get; set; }
    public decimal GoodQuantity { get; set; }
    public decimal ScrapQuantity { get; set; }
    public DateTimeOffset? LastProductionEntryAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public string LastModifiedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string HistoryJson { get; set; } = "[]";
}
