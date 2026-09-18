namespace NevaxisZentra.Persistence.Erp;

public sealed class ErpConnectionProfile
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string Provider { get; set; } = ErpProviderKeys.Simulator;
    public string Name { get; set; } = string.Empty;
    public string? ExternalCompanyId { get; set; }
    public string? ExternalCompanyCode { get; set; }
    public string? BaseEndpoint { get; set; }
    public string ConnectionMode { get; set; } = ErpConnectionModes.Simulator;
    public string? CredentialReference { get; set; }
    public string Status { get; set; } = ErpConnectionStatuses.Draft;
    public DateTimeOffset? LastSuccessfulConnectionAtUtc { get; set; }
    public DateTimeOffset? LastFailedConnectionAtUtc { get; set; }
    public DateTimeOffset? LastHealthCheckAtUtc { get; set; }
    public int? LastHealthLatencyMs { get; set; }
    public string? LastErrorCode { get; set; }
    public string? LastErrorMessage { get; set; }
    public DateTimeOffset? LastSuccessfulSyncAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;
}

public sealed class ErpSyncRun
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string Status { get; set; } = ErpSyncRunStatuses.Running;
    public int RecordsRead { get; set; }
    public int RecordsCreated { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsSkipped { get; set; }
    public int RecordsFailed { get; set; }
    public string? Cursor { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string TriggeredBySubjectId { get; set; } = string.Empty;
    public Guid? RetriedFromSyncRunId { get; set; }

    public ErpConnectionProfile? ConnectionProfile { get; set; }
    public ICollection<ErpIntegrationError> Errors { get; set; } = new List<ErpIntegrationError>();
}

public sealed class ErpIntegrationError
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid SyncRunId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }

    public ErpSyncRun? SyncRun { get; set; }
}

/// <summary>
/// External ERP customer projection. Does not replace operational customer domain entities.
/// External ERP remains system of record for ERP customer master.
/// </summary>
public sealed class CanonicalErpCustomer
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string? ExternalCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? CountryCode { get; set; }
    public bool IsActive { get; set; } = true;
    public string? SourceVersion { get; set; }
    public DateTimeOffset? SourceUpdatedAtUtc { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>
/// External ERP product projection. Does not create duplicate factory product truth.
/// External ERP remains system of record for ERP product master.
/// </summary>
public sealed class CanonicalErpProduct
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string? ExternalCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? Barcode { get; set; }
    public bool IsActive { get; set; } = true;
    public string? SourceVersion { get; set; }
    public DateTimeOffset? SourceUpdatedAtUtc { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>
/// External ERP order projection. Not a ProductionOrder / SalesOrder SoR.
/// </summary>
public sealed class CanonicalErpOrder
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string? ExternalCode { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ExternalCustomerId { get; set; } = string.Empty;
    public string? ExternalCustomerCode { get; set; }
    public DateTimeOffset OrderDateUtc { get; set; }
    public DateTimeOffset? RequestedDeliveryDateUtc { get; set; }
    public string? CurrencyCode { get; set; }
    public string Status { get; set; } = ErpOrderStatuses.Unknown;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public string? SourceVersion { get; set; }
    public DateTimeOffset? SourceUpdatedAtUtc { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<CanonicalErpOrderLine> Lines { get; set; } = new List<CanonicalErpOrderLine>();
}

public sealed class CanonicalErpOrderLine
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid OrderId { get; set; }
    public string ExternalLineId { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string ExternalProductId { get; set; } = string.Empty;
    public string? ExternalProductCode { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTimeOffset? RequestedDeliveryDateUtc { get; set; }
    public string? SourceVersion { get; set; }

    public CanonicalErpOrder? Order { get; set; }
}

/// <summary>
/// External ERP warehouse projection. Does not replace NevaxisZentra warehouse SoR.
/// </summary>
public sealed class CanonicalErpWarehouse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string? ExternalCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? SourceVersion { get; set; }
    public DateTimeOffset? SourceUpdatedAtUtc { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>
/// External ERP inventory balance projection. Quantities are ERP source values only.
/// </summary>
public sealed class CanonicalErpInventoryBalance
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalProductId { get; set; } = string.Empty;
    public string? ExternalProductCode { get; set; }
    public string ExternalWarehouseId { get; set; } = string.Empty;
    public string? ExternalWarehouseCode { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal? QuantityAvailable { get; set; }
    public decimal? QuantityReserved { get; set; }
    public decimal? QuantityOnOrder { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? SourceVersion { get; set; }
    public DateTimeOffset? SourceUpdatedAtUtc { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>
/// External ERP purchase order projection. No purchasing writeback.
/// </summary>
public sealed class CanonicalErpPurchaseOrder
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string? ExternalCode { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public string? ExternalSupplierId { get; set; }
    public string? ExternalSupplierCode { get; set; }
    public string? SupplierName { get; set; }
    public DateTimeOffset OrderDateUtc { get; set; }
    public DateTimeOffset? ExpectedDeliveryDateUtc { get; set; }
    public string Status { get; set; } = ErpPurchaseOrderStatuses.Unknown;
    public string? CurrencyCode { get; set; }
    public string? SourceVersion { get; set; }
    public DateTimeOffset? SourceUpdatedAtUtc { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<CanonicalErpPurchaseOrderLine> Lines { get; set; } =
        new List<CanonicalErpPurchaseOrderLine>();
}

public sealed class CanonicalErpPurchaseOrderLine
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public string ExternalLineId { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string ExternalProductId { get; set; } = string.Empty;
    public string? ExternalProductCode { get; set; }
    public string? Description { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal? ReceivedQuantity { get; set; }
    public decimal? OpenQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTimeOffset? ExpectedDeliveryDateUtc { get; set; }
    public string? SourceVersion { get; set; }

    public CanonicalErpPurchaseOrder? PurchaseOrder { get; set; }
}

/// <summary>
/// External-to-internal mapping. CanonicalEntityId links the ERP projection;
/// InternalEntityId is the human-confirmed operational reference (no auto-create).
/// </summary>
public sealed class ErpEntityMapping
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public Guid? CanonicalEntityId { get; set; }
    public Guid? InternalEntityId { get; set; }
    public Guid? SuggestedInternalEntityId { get; set; }
    public string? SuggestedReason { get; set; }
    public string Status { get; set; } = ErpMappingStatuses.Unmapped;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;
}

public static class ErpProviderKeys
{
    public const string Logo = "logo";
    public const string Mikro = "mikro";
    public const string Netsis = "netsis";
    public const string Simulator = "simulator";
}

public static class ErpConnectionModes
{
    public const string Api = "api";
    public const string Database = "database";
    public const string File = "file";
    public const string Simulator = "simulator";
}

public static class ErpConnectionStatuses
{
    public const string Draft = "draft";
    public const string Configured = "configured";
    public const string Healthy = "healthy";
    public const string Degraded = "degraded";
    public const string Unavailable = "unavailable";
    public const string Disabled = "disabled";
}

public static class ErpSyncRunStatuses
{
    public const string Running = "running";
    public const string Completed = "completed";
    public const string CompletedWithErrors = "completed-with-errors";
    public const string Failed = "failed";
}

public static class ErpSyncEntityTypes
{
    public const string Customers = "customers";
    public const string Products = "products";
    public const string Orders = "orders";
    public const string Warehouses = "warehouses";
    public const string Inventory = "inventory";
    public const string Purchasing = "purchasing";
    public const string Production = "production";
    public const string Documents = "documents";
}

public static class ErpMappingStatuses
{
    public const string Unmapped = "unmapped";
    public const string Suggested = "suggested";
    public const string Mapped = "mapped";
    public const string Ignored = "ignored";
    public const string Conflict = "conflict";
}

public static class ErpOrderStatuses
{
    public const string Open = "open";
    public const string Confirmed = "confirmed";
    public const string PartiallyFulfilled = "partially-fulfilled";
    public const string Fulfilled = "fulfilled";
    public const string Cancelled = "cancelled";
    public const string Unknown = "unknown";
}

public static class ErpSourceFreshness
{
    public const string Fresh = "fresh";
    public const string Stale = "stale";
    public const string Unknown = "unknown";
}

public static class ErpReadinessBlockingCodes
{
    public const string CustomerUnmapped = "customer-unmapped";
    public const string ProductUnmapped = "product-unmapped";
    public const string MappingConflict = "mapping-conflict";
    public const string SourceStale = "source-stale";
    public const string MissingProductReference = "missing-product-reference";
    public const string InvalidQuantity = "invalid-quantity";
}

public static class ErpPurchaseOrderStatuses
{
    public const string Open = "open";
    public const string PartiallyReceived = "partially-received";
    public const string Received = "received";
    public const string Cancelled = "cancelled";
    public const string Closed = "closed";
    public const string Unknown = "unknown";
}

public static class ErpShortageStatuses
{
    public const string Sufficient = "sufficient";
    public const string Insufficient = "insufficient";
    public const string Unknown = "unknown";
    public const string StaleSource = "stale-source";
}

public static class ErpCoverageStatuses
{
    public const string Covered = "covered";
    public const string PartiallyCovered = "partially-covered";
    public const string NotCovered = "not-covered";
    public const string Unknown = "unknown";
}

public static class ErpCapabilities
{
    public const string CustomersRead = "customers.read";
    public const string ProductsRead = "products.read";
    public const string OrdersRead = "orders.read";
    public const string WarehousesRead = "warehouses.read";
    public const string InventoryRead = "inventory.read";
    public const string PurchasingRead = "purchasing.read";
    public const string ProductionRead = "production.read";
    public const string DocumentsRead = "documents.read";
    public const string SalesOrdersWriteDraft = "sales-orders.write-draft";
}

/// <summary>
/// External ERP production order projection. Does not create or mutate internal Production orders.
/// </summary>
public sealed class CanonicalErpProductionOrder
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string? ExternalCode { get; set; }
    public string ProductionOrderNumber { get; set; } = string.Empty;
    public string ExternalProductId { get; set; } = string.Empty;
    public string? ExternalProductCode { get; set; }
    public string? ProductDescription { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal? CompletedQuantity { get; set; }
    public decimal? ScrapQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = ErpProductionOrderStatuses.Unknown;
    public DateTimeOffset? PlannedStartUtc { get; set; }
    public DateTimeOffset? PlannedEndUtc { get; set; }
    public DateTimeOffset? ActualStartUtc { get; set; }
    public DateTimeOffset? ActualEndUtc { get; set; }
    public string? ExternalSalesOrderId { get; set; }
    public string? ExternalSalesOrderCode { get; set; }
    public string? SourceVersion { get; set; }
    public DateTimeOffset? SourceUpdatedAtUtc { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>
/// External ERP document metadata projection. Not an official document ledger.
/// </summary>
public sealed class CanonicalErpDocument
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string? ExternalCode { get; set; }
    public string DocumentType { get; set; } = ErpDocumentTypes.Generic;
    public string? DocumentNumber { get; set; }
    public DateTimeOffset? DocumentDateUtc { get; set; }
    public string? Status { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? RelatedExternalEntityId { get; set; }
    public string? RelatedExternalEntityCode { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public bool ContentAvailable { get; set; }
    public string? ExternalReference { get; set; }
    public string? SourceVersion { get; set; }
    public DateTimeOffset? SourceUpdatedAtUtc { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>
/// Human-in-the-loop ERP writeback orchestration request. Not an ERP SoR record.
/// </summary>
public sealed class ErpWritebackRequest
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectionProfileId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string CommandType { get; set; } = string.Empty;
    public string? TargetEntityType { get; set; }
    public string? TargetExternalId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string PayloadHash { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Status { get; set; } = ErpWritebackStatuses.Draft;
    public string RequestedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset RequestedAtUtc { get; set; }
    public DateTimeOffset? SubmittedAtUtc { get; set; }
    public string? ApprovedPayloadHash { get; set; }
    public uint? ApprovedVersion { get; set; }
    public DateTimeOffset? ExecutedAtUtc { get; set; }
    public string? ExternalExecutionReference { get; set; }
    public string? LastErrorCode { get; set; }
    public string? LastErrorMessage { get; set; }
    public uint Version { get; set; } = 1;

    public ICollection<ErpWritebackApproval> Approvals { get; set; } = new List<ErpWritebackApproval>();
}

/// <summary>
/// Append-only human approval record for ERP writeback requests.
/// </summary>
public sealed class ErpWritebackApproval
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid WritebackRequestId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string DecidedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset DecidedAtUtc { get; set; }
    public uint RequestVersion { get; set; }
    public string PayloadHash { get; set; } = string.Empty;
    public string? Comment { get; set; }

    public ErpWritebackRequest? WritebackRequest { get; set; }
}

public static class ErpProductionOrderStatuses
{
    public const string Planned = "planned";
    public const string Released = "released";
    public const string InProgress = "in-progress";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
    public const string Closed = "closed";
    public const string Unknown = "unknown";
}

public static class ErpDocumentTypes
{
    public const string SalesOrder = "sales-order";
    public const string PurchaseOrder = "purchase-order";
    public const string DeliveryNote = "delivery-note";
    public const string Invoice = "invoice";
    public const string ProductionOrder = "production-order";
    public const string QualityDocument = "quality-document";
    public const string Generic = "generic";
}

public static class ErpWritebackStatuses
{
    public const string Draft = "draft";
    public const string PendingApproval = "pending-approval";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Executing = "executing";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Blocked = "blocked";
    public const string Superseded = "superseded";
}

public static class ErpWritebackDecisions
{
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}

public static class ErpWritebackCommandTypes
{
    public const string SalesOrderCreateDraft = "sales-order.create-draft";
}
