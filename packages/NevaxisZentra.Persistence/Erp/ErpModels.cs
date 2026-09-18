namespace NevaxisZentra.Persistence.Erp;

public sealed record ErpOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public static class ErpFailureCodes
{
    public const string InvalidScope = "erp.invalid-scope";
    public const string NotFound = "erp.not-found";
    public const string ConcurrencyConflict = "erp.concurrency-conflict";
    public const string ValidationFailed = "erp.validation-failed";
    public const string SyncAlreadyRunning = "erp.sync-already-running";
    public const string CapabilityUnsupported = "erp.capability-unsupported";
    public const string ProviderUnsupported = "erp.provider-unsupported";
    public const string SourceUnavailable = "erp.source-unavailable";
    public const string WritebackNotApproved = "erp.writeback-not-approved";
    public const string WritebackBlocked = "erp.writeback-blocked";
    public const string WritebackAlreadyExecuted = "erp.writeback-already-executed";
}

public sealed class ErpWriteResult
{
    public bool Succeeded { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ErpWriteResult Success(Guid entityId, uint version) =>
        new() { Succeeded = true, EntityId = entityId, Version = version };

    public static ErpWriteResult Fail(string code, string? reason = null) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public sealed class ErpSyncResult
{
    public bool Succeeded { get; init; }
    public Guid? SyncRunId { get; init; }
    public string? Status { get; init; }
    public int RecordsRead { get; init; }
    public int RecordsCreated { get; init; }
    public int RecordsUpdated { get; init; }
    public int RecordsSkipped { get; init; }
    public int RecordsFailed { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }

    public static ErpSyncResult Ok(
        Guid syncRunId,
        string status,
        int read,
        int created,
        int updated,
        int skipped,
        int failed) =>
        new()
        {
            Succeeded = true,
            SyncRunId = syncRunId,
            Status = status,
            RecordsRead = read,
            RecordsCreated = created,
            RecordsUpdated = updated,
            RecordsSkipped = skipped,
            RecordsFailed = failed,
        };

    public static ErpSyncResult Fail(string code, string? reason = null, Guid? syncRunId = null) =>
        new()
        {
            Succeeded = false,
            SyncRunId = syncRunId,
            FailureCode = code,
            FailureReason = reason,
        };
}

public sealed record ErpConnectionSummaryReadModel(
    Guid Id,
    Guid FacilityId,
    string Provider,
    string Name,
    string? ExternalCompanyId,
    string? ExternalCompanyCode,
    string ConnectionMode,
    string Status,
    DateTimeOffset? LastSuccessfulConnectionAtUtc,
    DateTimeOffset? LastFailedConnectionAtUtc,
    DateTimeOffset? LastHealthCheckAtUtc,
    int? LastHealthLatencyMs,
    string? LastErrorCode,
    string? LastErrorMessage,
    DateTimeOffset? LastSuccessfulSyncAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version,
    string? CredentialReference);

public sealed record ErpHealthCheckReadModel(
    Guid ConnectionProfileId,
    string Provider,
    string Status,
    bool IsHealthy,
    DateTimeOffset CheckedAtUtc,
    int? LatencyMs,
    string? ErrorCode,
    string? ErrorMessage,
    uint Version,
    IReadOnlyDictionary<string, string>? IntegrationMetadata = null);

public sealed record ErpSyncRunSummaryReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string EntityType,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string Status,
    int RecordsRead,
    int RecordsCreated,
    int RecordsUpdated,
    int RecordsSkipped,
    int RecordsFailed,
    string? ErrorCode,
    string? ErrorMessage,
    string TriggeredBySubjectId,
    IReadOnlyList<ErpIntegrationErrorReadModel> Errors);

public sealed record ErpIntegrationErrorReadModel(
    string Code,
    string EntityType,
    string? ExternalId,
    string Message,
    DateTimeOffset OccurredAtUtc);

public sealed record CanonicalErpCustomerReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string Provider,
    string ExternalId,
    string? ExternalCode,
    string Name,
    string? TaxNumber,
    string? TaxOffice,
    string? Email,
    string? Phone,
    string? CountryCode,
    bool IsActive,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc,
    DateTimeOffset ImportedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CanonicalErpProductReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string Provider,
    string ExternalId,
    string? ExternalCode,
    string Name,
    string? Unit,
    string? Barcode,
    bool IsActive,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc,
    DateTimeOffset ImportedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ErpWorkspaceSummaryReadModel(
    int ConnectionCount,
    int HealthyCount,
    int ProblematicCount,
    DateTimeOffset? LastSyncAtUtc,
    int CustomerCount,
    int ProductCount,
    int OrderCount);

public sealed record CreateErpConnectionCommand(
    string Provider,
    string Name,
    string? ExternalCompanyId,
    string? ExternalCompanyCode,
    string? BaseEndpoint,
    string ConnectionMode,
    string? CredentialReference,
    string Status);

public sealed record UpdateErpConnectionCommand(
    Guid ConnectionId,
    uint ExpectedVersion,
    string Name,
    string? ExternalCompanyId,
    string? ExternalCompanyCode,
    string? BaseEndpoint,
    string ConnectionMode,
    string? CredentialReference,
    string Status);

public sealed record CanonicalErpOrderLineReadModel(
    Guid Id,
    string ExternalLineId,
    int LineNumber,
    string ExternalProductId,
    string? ExternalProductCode,
    string? Description,
    decimal Quantity,
    string Unit,
    DateTimeOffset? RequestedDeliveryDateUtc,
    string? SourceVersion,
    string ProductMappingStatus,
    Guid? MappedInternalProductId);

public sealed record CanonicalErpOrderSummaryReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string Provider,
    string ExternalId,
    string? ExternalCode,
    string OrderNumber,
    string ExternalCustomerId,
    string? ExternalCustomerCode,
    DateTimeOffset OrderDateUtc,
    DateTimeOffset? RequestedDeliveryDateUtc,
    string? CurrencyCode,
    string Status,
    string? Reference,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc,
    DateTimeOffset ImportedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int LineCount,
    string CustomerMappingStatus,
    bool ReadyForOperationalUse,
    string SourceFreshness);

public sealed record CanonicalErpOrderDetailReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string Provider,
    string ExternalId,
    string? ExternalCode,
    string OrderNumber,
    string ExternalCustomerId,
    string? ExternalCustomerCode,
    DateTimeOffset OrderDateUtc,
    DateTimeOffset? RequestedDeliveryDateUtc,
    string? CurrencyCode,
    string Status,
    string? Reference,
    string? Notes,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc,
    DateTimeOffset ImportedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<CanonicalErpOrderLineReadModel> Lines,
    ErpOrderReadinessReadModel Readiness);

public sealed record ErpOrderReadinessReadModel(
    Guid OrderId,
    bool CustomerMapped,
    bool AllProductsMapped,
    int MissingProductMappings,
    string SourceFreshness,
    bool ReadyForOperationalUse,
    IReadOnlyList<ErpReadinessBlockingReason> BlockingReasons);

public sealed record ErpReadinessBlockingReason(string Code, string Message);

public sealed record ErpMappingReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string EntityType,
    string ExternalId,
    Guid? CanonicalEntityId,
    Guid? InternalEntityId,
    Guid? SuggestedInternalEntityId,
    string? SuggestedReason,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record ErpCustomerReconciliationReadModel(
    Guid CanonicalCustomerId,
    Guid ConnectionProfileId,
    string Provider,
    string ExternalId,
    string? ExternalCode,
    string Name,
    string? TaxNumber,
    string MappingStatus,
    Guid? MappingId,
    Guid? MappedInternalEntityId,
    Guid? SuggestedInternalEntityId,
    string? SuggestedReason,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint? MappingVersion);

public sealed record ErpProductReconciliationReadModel(
    Guid CanonicalProductId,
    Guid ConnectionProfileId,
    string Provider,
    string ExternalId,
    string? ExternalCode,
    string Name,
    string? Unit,
    string? Barcode,
    string MappingStatus,
    Guid? MappingId,
    Guid? MappedInternalEntityId,
    Guid? SuggestedInternalEntityId,
    string? SuggestedReason,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint? MappingVersion);

public sealed record ConfirmErpMappingCommand(Guid MappingId, Guid TargetEntityId, uint ExpectedVersion);
public sealed record IgnoreErpMappingCommand(Guid MappingId, uint ExpectedVersion);
public sealed record RemapErpMappingCommand(Guid MappingId, Guid TargetEntityId, uint ExpectedVersion);

public sealed record CanonicalErpWarehouseReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string Provider,
    string ExternalId,
    string? ExternalCode,
    string Name,
    bool IsActive,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc,
    DateTimeOffset ImportedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string Freshness);

public sealed record CanonicalErpInventoryBalanceReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string Provider,
    string ExternalProductId,
    string? ExternalProductCode,
    string? ProductName,
    string ExternalWarehouseId,
    string? ExternalWarehouseCode,
    string? WarehouseName,
    decimal QuantityOnHand,
    decimal? QuantityAvailable,
    decimal? QuantityReserved,
    decimal? QuantityOnOrder,
    string Unit,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc,
    DateTimeOffset ImportedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string Freshness);

public sealed record ErpInventoryWarehouseBreakdownReadModel(
    string ExternalWarehouseId,
    string? ExternalWarehouseCode,
    string? WarehouseName,
    decimal OnHand,
    decimal? Available,
    decimal? Reserved,
    decimal? OnOrder);

public sealed record ErpInventoryAvailabilityReadModel(
    string ExternalProductId,
    string? ExternalProductCode,
    string? ProductName,
    decimal TotalOnHand,
    decimal? TotalAvailable,
    IReadOnlyList<ErpInventoryWarehouseBreakdownReadModel> WarehouseBreakdown,
    string Freshness,
    DateTimeOffset? LastSuccessfulSyncAtUtc);

public sealed record CanonicalErpPurchaseOrderSummaryReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string Provider,
    string ExternalId,
    string? ExternalCode,
    string PurchaseOrderNumber,
    string? ExternalSupplierId,
    string? ExternalSupplierCode,
    string? SupplierName,
    DateTimeOffset OrderDateUtc,
    DateTimeOffset? ExpectedDeliveryDateUtc,
    string Status,
    string? CurrencyCode,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc,
    DateTimeOffset ImportedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int LineCount,
    int OpenLineCount,
    string Freshness);

public sealed record CanonicalErpPurchaseOrderLineReadModel(
    Guid Id,
    string ExternalLineId,
    int LineNumber,
    string ExternalProductId,
    string? ExternalProductCode,
    string? Description,
    decimal OrderedQuantity,
    decimal? ReceivedQuantity,
    decimal? OpenQuantity,
    string Unit,
    DateTimeOffset? ExpectedDeliveryDateUtc,
    string? SourceVersion);

public sealed record CanonicalErpPurchaseOrderDetailReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string Provider,
    string ExternalId,
    string? ExternalCode,
    string PurchaseOrderNumber,
    string? ExternalSupplierId,
    string? ExternalSupplierCode,
    string? SupplierName,
    DateTimeOffset OrderDateUtc,
    DateTimeOffset? ExpectedDeliveryDateUtc,
    string Status,
    string? CurrencyCode,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc,
    DateTimeOffset ImportedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string Freshness,
    IReadOnlyList<CanonicalErpPurchaseOrderLineReadModel> Lines);

public sealed record ErpSyncReliabilityReadModel(
    string EntityType,
    DateTimeOffset? LastSuccessfulSyncAtUtc,
    DateTimeOffset? LastFailedSyncAtUtc,
    string? LastRunStatus,
    int LastRecordsRead,
    int LastRecordsFailed,
    int ConsecutiveFailures,
    string FreshnessStatus,
    string? CurrentErrorCode,
    string? CurrentErrorMessage);

public sealed record ErpPurchaseCoverageReadModel(
    decimal OpenPurchaseQuantity,
    DateTimeOffset? NearestExpectedDeliveryDate,
    string CoverageStatus);

public sealed record ErpMaterialAvailabilityLineReadModel(
    string ExternalLineId,
    int LineNumber,
    string ExternalProductId,
    string? ExternalProductCode,
    string? Description,
    decimal RequiredQuantity,
    decimal? AvailableQuantity,
    decimal? ShortageQuantity,
    string Status,
    ErpPurchaseCoverageReadModel? PurchaseCoverage);

public sealed record ErpOrderMaterialAvailabilityReadModel(
    Guid OrderId,
    string OrderNumber,
    string FreshnessNote,
    IReadOnlyList<ErpMaterialAvailabilityLineReadModel> Lines);

public sealed record CanonicalErpProductionOrderReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string Provider,
    string ExternalId,
    string? ExternalCode,
    string ProductionOrderNumber,
    string ExternalProductId,
    string? ExternalProductCode,
    string? ProductDescription,
    decimal PlannedQuantity,
    decimal? CompletedQuantity,
    decimal? ScrapQuantity,
    string Unit,
    string Status,
    DateTimeOffset? PlannedStartUtc,
    DateTimeOffset? PlannedEndUtc,
    DateTimeOffset? ActualStartUtc,
    DateTimeOffset? ActualEndUtc,
    string? ExternalSalesOrderId,
    string? ExternalSalesOrderCode,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc,
    DateTimeOffset ImportedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string Freshness);

public sealed record CanonicalErpDocumentReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string Provider,
    string ExternalId,
    string? ExternalCode,
    string DocumentType,
    string? DocumentNumber,
    DateTimeOffset? DocumentDateUtc,
    string? Status,
    string? RelatedEntityType,
    string? RelatedExternalEntityId,
    string? RelatedExternalEntityCode,
    string? FileName,
    string? MimeType,
    bool ContentAvailable,
    string? ExternalReference,
    string? SourceVersion,
    DateTimeOffset? SourceUpdatedAtUtc,
    DateTimeOffset ImportedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string Freshness);

public sealed record CreateErpWritebackCommand(
    Guid ConnectionProfileId,
    string CommandType,
    string PayloadJson,
    string IdempotencyKey,
    string? TargetEntityType = null,
    string? TargetExternalId = null);

public sealed record SubmitErpWritebackCommand(Guid WritebackId, uint ExpectedVersion);

public sealed record ApproveErpWritebackCommand(Guid WritebackId, uint ExpectedVersion, string? Comment = null);

public sealed record RejectErpWritebackCommand(Guid WritebackId, uint ExpectedVersion, string? Comment = null);

public sealed record ExecuteErpWritebackCommand(Guid WritebackId, uint ExpectedVersion);

public sealed record ErpWritebackLineSummaryReadModel(
    string ProductExternalId,
    decimal Quantity,
    string Unit);

public sealed record ErpWritebackPayloadSummaryReadModel(
    string? CustomerExternalId,
    string? OrderReference,
    DateTimeOffset? RequestedDeliveryDateUtc,
    int LineCount,
    IReadOnlyList<ErpWritebackLineSummaryReadModel> Lines);

public sealed record ErpWritebackApprovalReadModel(
    Guid Id,
    string Decision,
    string DecidedBySubjectId,
    DateTimeOffset DecidedAtUtc,
    uint RequestVersion,
    string PayloadHash,
    string? Comment);

public sealed record ErpWritebackSummaryReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string Provider,
    string CommandType,
    string Status,
    string RequestedBySubjectId,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    string? LatestApprovalDecision,
    string? LatestApproverSubjectId,
    DateTimeOffset? LatestApprovalAtUtc,
    string? ExternalExecutionReference,
    string? LastErrorCode,
    string? CustomerExternalId,
    string? OrderReference,
    int LineCount,
    uint Version);

public sealed record ErpWritebackDetailReadModel(
    Guid Id,
    Guid ConnectionProfileId,
    string Provider,
    string CommandType,
    string? TargetEntityType,
    string? TargetExternalId,
    string Status,
    string PayloadHash,
    string IdempotencyKey,
    string RequestedBySubjectId,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    string? ApprovedPayloadHash,
    uint? ApprovedVersion,
    DateTimeOffset? ExecutedAtUtc,
    string? ExternalExecutionReference,
    string? LastErrorCode,
    string? LastErrorMessage,
    uint Version,
    ErpWritebackPayloadSummaryReadModel PayloadSummary,
    IReadOnlyList<ErpWritebackApprovalReadModel> Approvals);
