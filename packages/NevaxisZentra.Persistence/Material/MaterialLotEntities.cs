namespace NevaxisZentra.Persistence.Material;

public sealed class MaterialLot
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MaterialId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public string LotNumberNormalized { get; set; } = string.Empty;
    public string? SupplierLotNumber { get; set; }
    public string? ManufacturerLotNumber { get; set; }
    public string? SupplierReference { get; set; }
    public string? PurchaseOrderReference { get; set; }
    public string? GoodsReceiptReference { get; set; }
    public string? ExternalSystem { get; set; }
    public string? ExternalLotReference { get; set; }
    public DateOnly? ManufactureDate { get; set; }
    public DateTimeOffset? ReceivedAtUtc { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public DateOnly? RetestDate { get; set; }
    public decimal? InitialQuantity { get; set; }
    public decimal? CurrentObservedQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = "kg";
    public MaterialLotStatus Status { get; set; } = MaterialLotStatus.Draft;
    public MaterialLotQualityStatus QualityStatus { get; set; } = MaterialLotQualityStatus.Pending;
    public Guid? CurrentStorageLocationId { get; set; }
    public string? CurrentWarehouseReference { get; set; }
    public string? CurrentLocationReference { get; set; }
    public MaterialSourceType SourceType { get; set; } = MaterialSourceType.Manual;
    public string? SourceReference { get; set; }
    public DateTimeOffset? DataRecordedAtUtc { get; set; }
    public string? Notes { get; set; }
    public string? CreateIdempotencyKey { get; set; }
    public string? CreateIdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public string UpdatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;

    public MaterialRegistryEntry? Material { get; set; }
    public MaterialStorageLocation? CurrentStorageLocation { get; set; }
}

public sealed class MaterialStorageLocation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseCodeNormalized { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string LocationCodeNormalized { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public MaterialSourceType SourceType { get; set; } = MaterialSourceType.Manual;
    public string? SourceReference { get; set; }
    public DateTimeOffset? DataRecordedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public string UpdatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;
}

public sealed class MaterialLotTraceabilityEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MaterialLotId { get; set; }
    public MaterialLotEventType EventType { get; set; }
    public DateTimeOffset EventAtUtc { get; set; }
    public decimal? Quantity { get; set; }
    public string? UnitOfMeasure { get; set; }
    public string? FromWarehouseReference { get; set; }
    public string? FromLocationReference { get; set; }
    public string? ToWarehouseReference { get; set; }
    public string? ToLocationReference { get; set; }
    public MaterialLotStatus? StatusBefore { get; set; }
    public MaterialLotStatus? StatusAfter { get; set; }
    public MaterialLotQualityStatus? QualityStatusBefore { get; set; }
    public MaterialLotQualityStatus? QualityStatusAfter { get; set; }
    public string? Reason { get; set; }
    public MaterialSourceType SourceType { get; set; } = MaterialSourceType.Manual;
    public string? SourceReference { get; set; }
    public string? ExternalEventReference { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? PayloadFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;

    public MaterialLot? MaterialLot { get; set; }
}

public sealed class MaterialLotCertificateReference
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MaterialLotId { get; set; }
    public MaterialLotCertificateType CertificateType { get; set; } = MaterialLotCertificateType.Other;
    public string DocumentReference { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public string? Issuer { get; set; }
    public DateOnly? IssuedDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public MaterialSourceType SourceType { get; set; } = MaterialSourceType.Manual;
    public string? SourceReference { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? PayloadFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;

    public MaterialLot? MaterialLot { get; set; }
}

public enum MaterialLotStatus
{
    Draft,
    Received,
    Available,
    QualityHold,
    Blocked,
    Closed,
}

public enum MaterialLotQualityStatus
{
    Pending,
    Accepted,
    ConditionallyAccepted,
    Rejected,
    NotRequired,
    Unknown,
}

public enum MaterialLotEventType
{
    LotCreated,
    ReceiptObserved,
    LocationTransfer,
    StatusChanged,
    QualityStatusRecorded,
    CertificateLinked,
    NoteRecorded,
}

public enum MaterialLotCertificateType
{
    CertificateOfAnalysis,
    MaterialCertificate,
    SafetyDataSheet,
    TechnicalDataSheet,
    ConformityCertificate,
    Other,
}
