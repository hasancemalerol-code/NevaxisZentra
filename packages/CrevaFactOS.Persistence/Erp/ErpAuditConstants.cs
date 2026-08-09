namespace CrevaFactOS.Persistence.Erp;

public static class ErpAuditActions
{
    public const string ConnectionCreated = "erp.connection.created";
    public const string ConnectionUpdated = "erp.connection.updated";
    public const string ConnectionHealthChecked = "erp.connection.health.checked";
    public const string SyncStarted = "erp.sync.started";
    public const string SyncCompleted = "erp.sync.completed";
    public const string SyncFailed = "erp.sync.failed";
    public const string MappingConfirmed = "erp.mapping.confirmed";
    public const string MappingIgnored = "erp.mapping.ignored";
    public const string SyncOrdersStarted = "erp.sync.orders.started";
    public const string SyncOrdersCompleted = "erp.sync.orders.completed";
    public const string SyncOrdersFailed = "erp.sync.orders.failed";
}

public static class ErpAuditEventTypes
{
    public const string ConnectionCreated = "erp.connection.created";
    public const string ConnectionUpdated = "erp.connection.updated";
    public const string ConnectionHealthChecked = "erp.connection.health.checked";
    public const string SyncStarted = "erp.sync.started";
    public const string SyncCompleted = "erp.sync.completed";
    public const string SyncFailed = "erp.sync.failed";
    public const string MappingConfirmed = "erp.mapping.confirmed";
    public const string MappingIgnored = "erp.mapping.ignored";
    public const string SyncOrdersStarted = "erp.sync.orders.started";
    public const string SyncOrdersCompleted = "erp.sync.orders.completed";
    public const string SyncOrdersFailed = "erp.sync.orders.failed";
}

public static class ErpOutboxMessageTypes
{
    public const string ConnectionCreated = "erp-connection-created";
    public const string SyncCompleted = "erp-sync-completed";
    public const string SyncFailed = "erp-sync-failed";
    public const string MappingConfirmed = "erp-mapping-confirmed";
    public const string SyncOrdersCompleted = "erp-sync-orders-completed";
    public const string SyncOrdersFailed = "erp-sync-orders-failed";
}
