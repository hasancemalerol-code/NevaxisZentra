using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Export;

public sealed class ExportShipment : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ExportCaseId { get; set; }
    public string ShipmentReference { get; set; } = string.Empty;
    public ExportShipmentStatus Status { get; set; }
    public string? TransportMode { get; set; }
    public string? IncotermCode { get; set; }
    public string DestinationCountryCode { get; set; } = string.Empty;
    public string? DestinationCity { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? ForwarderName { get; set; }
    public string? CarrierName { get; set; }
    public DateTimeOffset? PlannedPickupAtUtc { get; set; }
    public DateTimeOffset? PlannedDepartureAtUtc { get; set; }
    public DateTimeOffset? PlannedArrivalAtUtc { get; set; }
    public DateTimeOffset? ActualPickupAtUtc { get; set; }
    public DateTimeOffset? ActualDepartureAtUtc { get; set; }
    public DateTimeOffset? ActualArrivalAtUtc { get; set; }
    public string? BookingReference { get; set; }
    public string? TrackingReference { get; set; }
    public decimal? TotalGrossWeight { get; set; }
    public decimal? TotalNetWeight { get; set; }
    public int? TotalPackageCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string LastModifiedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; }
    public string? CancellationReason { get; set; }
}
