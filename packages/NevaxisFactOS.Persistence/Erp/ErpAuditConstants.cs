namespace NevaxisFactOS.Persistence.Erp;

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
    public const string SyncWarehousesStarted = "erp.sync.warehouses.started";
    public const string SyncWarehousesCompleted = "erp.sync.warehouses.completed";
    public const string SyncWarehousesFailed = "erp.sync.warehouses.failed";
    public const string SyncInventoryStarted = "erp.sync.inventory.started";
    public const string SyncInventoryCompleted = "erp.sync.inventory.completed";
    public const string SyncInventoryFailed = "erp.sync.inventory.failed";
    public const string SyncPurchasingStarted = "erp.sync.purchasing.started";
    public const string SyncPurchasingCompleted = "erp.sync.purchasing.completed";
    public const string SyncPurchasingFailed = "erp.sync.purchasing.failed";
    public const string SyncProductionStarted = "erp.sync.production.started";
    public const string SyncProductionCompleted = "erp.sync.production.completed";
    public const string SyncProductionFailed = "erp.sync.production.failed";
    public const string SyncDocumentsStarted = "erp.sync.documents.started";
    public const string SyncDocumentsCompleted = "erp.sync.documents.completed";
    public const string SyncDocumentsFailed = "erp.sync.documents.failed";
    public const string WritebackCreated = "erp.writeback.created";
    public const string WritebackSubmitted = "erp.writeback.submitted";
    public const string WritebackApproved = "erp.writeback.approved";
    public const string WritebackRejected = "erp.writeback.rejected";
    public const string WritebackExecutionStarted = "erp.writeback.execution.started";
    public const string WritebackExecutionSucceeded = "erp.writeback.execution.succeeded";
    public const string WritebackExecutionFailed = "erp.writeback.execution.failed";
    public const string WritebackExecutionBlocked = "erp.writeback.execution.blocked";
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
    public const string SyncWarehousesStarted = "erp.sync.warehouses.started";
    public const string SyncWarehousesCompleted = "erp.sync.warehouses.completed";
    public const string SyncWarehousesFailed = "erp.sync.warehouses.failed";
    public const string SyncInventoryStarted = "erp.sync.inventory.started";
    public const string SyncInventoryCompleted = "erp.sync.inventory.completed";
    public const string SyncInventoryFailed = "erp.sync.inventory.failed";
    public const string SyncPurchasingStarted = "erp.sync.purchasing.started";
    public const string SyncPurchasingCompleted = "erp.sync.purchasing.completed";
    public const string SyncPurchasingFailed = "erp.sync.purchasing.failed";
    public const string SyncProductionStarted = "erp.sync.production.started";
    public const string SyncProductionCompleted = "erp.sync.production.completed";
    public const string SyncProductionFailed = "erp.sync.production.failed";
    public const string SyncDocumentsStarted = "erp.sync.documents.started";
    public const string SyncDocumentsCompleted = "erp.sync.documents.completed";
    public const string SyncDocumentsFailed = "erp.sync.documents.failed";
    public const string WritebackCreated = "erp.writeback.created";
    public const string WritebackSubmitted = "erp.writeback.submitted";
    public const string WritebackApproved = "erp.writeback.approved";
    public const string WritebackRejected = "erp.writeback.rejected";
    public const string WritebackExecutionStarted = "erp.writeback.execution.started";
    public const string WritebackExecutionSucceeded = "erp.writeback.execution.succeeded";
    public const string WritebackExecutionFailed = "erp.writeback.execution.failed";
    public const string WritebackExecutionBlocked = "erp.writeback.execution.blocked";
}

public static class ErpOutboxMessageTypes
{
    public const string ConnectionCreated = "erp-connection-created";
    public const string SyncCompleted = "erp-sync-completed";
    public const string SyncFailed = "erp-sync-failed";
    public const string MappingConfirmed = "erp-mapping-confirmed";
    public const string SyncOrdersCompleted = "erp-sync-orders-completed";
    public const string SyncOrdersFailed = "erp-sync-orders-failed";
    public const string SyncWarehousesCompleted = "erp-sync-warehouses-completed";
    public const string SyncWarehousesFailed = "erp-sync-warehouses-failed";
    public const string SyncInventoryCompleted = "erp-sync-inventory-completed";
    public const string SyncInventoryFailed = "erp-sync-inventory-failed";
    public const string SyncPurchasingCompleted = "erp-sync-purchasing-completed";
    public const string SyncPurchasingFailed = "erp-sync-purchasing-failed";
    public const string SyncProductionCompleted = "erp-sync-production-completed";
    public const string SyncProductionFailed = "erp-sync-production-failed";
    public const string SyncDocumentsCompleted = "erp-sync-documents-completed";
    public const string SyncDocumentsFailed = "erp-sync-documents-failed";
    public const string WritebackSubmitted = "erp-writeback-submitted";
    public const string WritebackApproved = "erp-writeback-approved";
    public const string WritebackRejected = "erp-writeback-rejected";
    public const string WritebackSucceeded = "erp-writeback-succeeded";
    public const string WritebackFailed = "erp-writeback-failed";
}
