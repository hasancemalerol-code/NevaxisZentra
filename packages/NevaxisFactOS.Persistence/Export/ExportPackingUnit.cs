using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Export;

public sealed class ExportPackingUnit : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ExportCaseId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;
    public Guid? ParentPackingUnitId { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal? NetWeight { get; set; }
    public string? WeightUnit { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? DimensionUnit { get; set; }
    public decimal? Volume { get; set; }
    public string? MarksAndNumbers { get; set; }
    public ExportPackingStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string LastModifiedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; }
}
