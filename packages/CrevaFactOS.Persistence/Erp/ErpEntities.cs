namespace CrevaFactOS.Persistence.Erp;

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

public static class ErpCapabilities
{
    public const string CustomersRead = "customers.read";
    public const string ProductsRead = "products.read";
    public const string OrdersRead = "orders.read";

    // Future only
    public const string InventoryRead = "inventory.read";
    public const string PurchasingRead = "purchasing.read";
    public const string ProductionRead = "production.read";
    public const string DocumentsRead = "documents.read";
    public const string Writeback = "writeback";
}
