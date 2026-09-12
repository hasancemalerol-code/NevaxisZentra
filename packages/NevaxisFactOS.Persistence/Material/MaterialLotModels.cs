namespace NevaxisFactOS.Persistence.Material;

public static class MaterialLotFailureCodes
{
    public const string InvalidScope = "invalid-scope";
    public const string NotFound = "not-found";
    public const string LotNumberConflict = "lot-number-conflict";
    public const string ConcurrencyConflict = "lot-concurrency-conflict";
    public const string IdempotencyConflict = "lot-idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "lot-idempotency-legacy-unverifiable";
    public const string InvalidTransition = "lot-invalid-transition";
    public const string InvalidLocation = "lot-invalid-location";
    public const string InvalidQuantity = "lot-invalid-quantity";
    public const string ValidationFailed = "validation-failed";
    public const string SourceUnavailable = "source-unavailable";
}

public sealed class MaterialLotWriteResult
{
    public bool Succeeded { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public bool IdempotentReplay { get; init; }

    public static MaterialLotWriteResult Success(Guid entityId, uint version, bool idempotentReplay = false) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
        };

    public static MaterialLotWriteResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public sealed record MaterialLotListFilters(
    Guid? MaterialId,
    string? Search,
    string? SupplierLotSearch,
    string? WarehouseReference,
    string? LocationReference,
    string? Status,
    string? QualityStatus,
    string? ExpiryState,
    int Page,
    int PageSize);

public sealed record MaterialLotSummaryReadModel(
    Guid Id,
    Guid MaterialId,
    string MaterialCode,
    string MaterialName,
    string LotNumber,
    string? SupplierLotNumber,
    string? ManufacturerLotNumber,
    string Status,
    string QualityStatus,
    string? CurrentWarehouseReference,
    string? CurrentLocationReference,
    decimal? CurrentObservedQuantity,
    string UnitOfMeasure,
    DateOnly? ExpiryDate,
    DateOnly? RetestDate,
    bool IsExpiryApproaching,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record MaterialLotDetailReadModel(
    Guid Id,
    Guid FacilityId,
    Guid MaterialId,
    string MaterialCode,
    string MaterialName,
    string LotNumber,
    string? SupplierLotNumber,
    string? ManufacturerLotNumber,
    string? SupplierReference,
    string? PurchaseOrderReference,
    string? GoodsReceiptReference,
    string? ExternalSystem,
    string? ExternalLotReference,
    DateOnly? ManufactureDate,
    DateTimeOffset? ReceivedAtUtc,
    DateOnly? ExpiryDate,
    DateOnly? RetestDate,
    decimal? InitialQuantity,
    decimal? CurrentObservedQuantity,
    string UnitOfMeasure,
    string Status,
    string QualityStatus,
    Guid? CurrentStorageLocationId,
    string? CurrentWarehouseReference,
    string? CurrentLocationReference,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record MaterialStorageLocationReadModel(
    Guid Id,
    string WarehouseCode,
    string WarehouseName,
    string LocationCode,
    string LocationName,
    bool IsActive,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    uint Version);

public sealed record MaterialLotTimelineEntryReadModel(
    Guid Id,
    string EventType,
    DateTimeOffset EventAtUtc,
    decimal? Quantity,
    string? UnitOfMeasure,
    string? FromWarehouseReference,
    string? FromLocationReference,
    string? ToWarehouseReference,
    string? ToLocationReference,
    string? StatusBefore,
    string? StatusAfter,
    string? QualityStatusBefore,
    string? QualityStatusAfter,
    string? Reason,
    string SourceType,
    string? SourceReference,
    string ActorDisplayName,
    DateTimeOffset CreatedAtUtc,
    uint Version);

public sealed record MaterialLotCertificateReadModel(
    Guid Id,
    string CertificateType,
    string DocumentReference,
    string? DocumentNumber,
    string? Issuer,
    DateOnly? IssuedDate,
    DateOnly? ExpiryDate,
    string SourceType,
    string? SourceReference,
    DateTimeOffset CreatedAtUtc,
    uint Version);

public sealed record MaterialLotBackwardTraceabilityReadModel(
    Guid LotId,
    string LotNumber,
    Guid MaterialId,
    string MaterialCode,
    string MaterialName,
    string? SupplierLotNumber,
    string? ManufacturerLotNumber,
    string? SupplierReference,
    string? PurchaseOrderReference,
    string? GoodsReceiptReference,
    string? ExternalSystem,
    string? ExternalLotReference,
    string? InitialWarehouseReference,
    string? InitialLocationReference,
    IReadOnlyList<MaterialLotCertificateReadModel> Certificates,
    IReadOnlyList<MaterialLotTimelineEntryReadModel> SourceEvents);

public sealed record MaterialLotForwardTraceabilityReadModel(
    Guid LotId,
    string LotNumber,
    string? CurrentWarehouseReference,
    string? CurrentLocationReference,
    string Status,
    string QualityStatus,
    IReadOnlyList<MaterialLotTimelineEntryReadModel> LocationTransfers,
    IReadOnlyList<MaterialLotTimelineEntryReadModel> StatusChanges,
    IReadOnlyList<MaterialLotTimelineEntryReadModel> QualityChanges,
    IReadOnlyList<MaterialLotCertificateReadModel> Certificates);

public sealed record MaterialLotWorkspaceSummaryReadModel(
    int TotalCount,
    int AvailableCount,
    int QualityHoldCount,
    int BlockedCount,
    int ExpiryApproachingCount);

public sealed record CreateMaterialLotCommand(
    Guid MaterialId,
    string LotNumber,
    string? SupplierLotNumber,
    string? ManufacturerLotNumber,
    string? SupplierReference,
    string? PurchaseOrderReference,
    string? GoodsReceiptReference,
    string? ExternalSystem,
    string? ExternalLotReference,
    DateOnly? ManufactureDate,
    DateOnly? ExpiryDate,
    DateOnly? RetestDate,
    decimal? InitialQuantity,
    string UnitOfMeasure,
    string QualityStatus,
    Guid? StorageLocationId,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    string? Notes,
    string? IdempotencyKey);

public sealed record ObserveMaterialLotReceiptCommand(
    Guid LotId,
    uint ExpectedVersion,
    DateTimeOffset ReceivedAtUtc,
    decimal ObservedQuantity,
    string? GoodsReceiptReference,
    string? SourceReference,
    string IdempotencyKey);

public sealed record TransferMaterialLotCommand(
    Guid LotId,
    uint ExpectedVersion,
    Guid TargetStorageLocationId,
    string? Reason,
    string? SourceReference,
    string IdempotencyKey);

public sealed record TransitionMaterialLotStatusCommand(
    Guid LotId,
    uint ExpectedVersion,
    string TargetStatus,
    string Reason,
    string? SourceReference,
    string IdempotencyKey);

public sealed record RecordMaterialLotQualityStatusCommand(
    Guid LotId,
    uint ExpectedVersion,
    string QualityStatus,
    string? Reason,
    string? SourceReference,
    string IdempotencyKey);

public sealed record LinkMaterialLotCertificateCommand(
    Guid LotId,
    string CertificateType,
    string DocumentReference,
    string? DocumentNumber,
    string? Issuer,
    DateOnly? IssuedDate,
    DateOnly? ExpiryDate,
    string SourceType,
    string? SourceReference,
    string IdempotencyKey);
