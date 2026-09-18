namespace NevaxisZentra.Api.Export;

// ─── Packing DTO types ────────────────────────────────────────────────────────

public sealed record CreatePackingUnitRequest(
    string UnitCode,
    string UnitType,
    Guid? ParentPackingUnitId,
    decimal? GrossWeight,
    decimal? NetWeight,
    string? WeightUnit,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    string? DimensionUnit,
    string? MarksAndNumbers);

public sealed record UpdatePackingUnitRequest(
    uint ExpectedVersion,
    decimal? GrossWeight,
    decimal? NetWeight,
    string? WeightUnit,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    string? DimensionUnit,
    string? MarksAndNumbers);

public sealed record AddPackingItemRequest(
    string ProductCode,
    string ProductName,
    decimal Quantity,
    string UnitOfMeasure,
    Guid? SourceOrderLineId,
    string? LotBatchReference);

public sealed record CompletePackingRequest(
    uint ExpectedCaseVersion);

public sealed record VerifyPackingRequest(
    uint ExpectedCaseVersion);

// ─── Shipment DTO types ───────────────────────────────────────────────────────

public sealed record CreateShipmentRequest(
    string ShipmentReference,
    string? TransportMode,
    string? IncotermCode,
    string DestinationCountryCode,
    string? DestinationCity,
    string? DeliveryAddress,
    string? ForwarderName,
    string? CarrierName,
    DateTimeOffset? PlannedPickupAtUtc,
    DateTimeOffset? PlannedDepartureAtUtc,
    DateTimeOffset? PlannedArrivalAtUtc,
    string? BookingReference);

public sealed record UpdateShipmentRequest(
    uint ExpectedVersion,
    string? TransportMode,
    string? IncotermCode,
    string? DestinationCountryCode,
    string? DestinationCity,
    string? DeliveryAddress,
    string? ForwarderName,
    string? CarrierName,
    DateTimeOffset? PlannedPickupAtUtc,
    DateTimeOffset? PlannedDepartureAtUtc,
    DateTimeOffset? PlannedArrivalAtUtc,
    DateTimeOffset? ActualPickupAtUtc,
    DateTimeOffset? ActualDepartureAtUtc,
    DateTimeOffset? ActualArrivalAtUtc,
    string? BookingReference,
    string? TrackingReference,
    decimal? TotalGrossWeight,
    decimal? TotalNetWeight,
    int? TotalPackageCount);

public sealed record TransitionShipmentRequest(
    uint ExpectedVersion,
    string TargetStatus,
    string? CancellationReason);

// ─── Broker DTO types ─────────────────────────────────────────────────────────

public sealed record CreateBrokerWorkspaceRequest(
    Guid? ShipmentId,
    string BrokerCompanyName,
    string? BrokerContactName,
    string? BrokerEmail,
    string? BrokerPhone,
    string? Notes);

public sealed record PreparePackageRequest(
    uint ExpectedWorkspaceVersion,
    IReadOnlyList<Guid> DocumentIds,
    string? Notes);

public sealed record SendPackageRequest(
    uint ExpectedWorkspaceVersion);

public sealed record AcknowledgeBrokerRequest(
    uint ExpectedWorkspaceVersion,
    string? BrokerReference);

public sealed record CreateCorrectionRequest(
    string Category,
    string Title,
    string Description,
    string Severity,
    Guid? RelatedDocumentId);

public sealed record ResolveCorrectionRequest(
    uint ExpectedWorkspaceVersion,
    string Resolution,
    string? ResolutionNote);
