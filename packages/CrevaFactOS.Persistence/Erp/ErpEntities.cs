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
/// Lightweight external-to-canonical mapping foundation (unmapped/mapped/ignored).
/// No AI matching in ERP-1L.
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
    public string Status { get; set; } = ErpMappingStatuses.Unmapped;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
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
}

public static class ErpMappingStatuses
{
    public const string Unmapped = "unmapped";
    public const string Mapped = "mapped";
    public const string Ignored = "ignored";
}

public static class ErpCapabilities
{
    public const string CustomersRead = "customers.read";
    public const string ProductsRead = "products.read";

    // Future only — not implemented in ERP-1L
    public const string OrdersRead = "orders.read";
    public const string InventoryRead = "inventory.read";
    public const string PurchasingRead = "purchasing.read";
    public const string ProductionRead = "production.read";
    public const string DocumentsRead = "documents.read";
    public const string Writeback = "writeback";
}
