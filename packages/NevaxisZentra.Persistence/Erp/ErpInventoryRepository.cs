using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Erp;

public interface IErpInventoryRepository
{
    Task<ErpSyncResult> SyncWarehousesAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default);

    Task<ErpSyncResult> SyncInventoryAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default);

    Task<ErpSyncResult> SyncPurchasingAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CanonicalErpWarehouseReadModel>> ListWarehousesAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default);

    Task<CanonicalErpWarehouseReadModel?> GetWarehouseAsync(
        ErpOperationContext context,
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CanonicalErpInventoryBalanceReadModel>> ListInventoryBalancesAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? productExternalId = null,
        string? warehouseExternalId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ErpInventoryAvailabilityReadModel>> ListInventoryAvailabilityAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? productExternalId = null,
        string? warehouseExternalId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CanonicalErpPurchaseOrderSummaryReadModel>> ListPurchaseOrdersAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default);

    Task<CanonicalErpPurchaseOrderDetailReadModel?> GetPurchaseOrderAsync(
        ErpOperationContext context,
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default);

    Task<ErpOrderMaterialAvailabilityReadModel?> GetOrderMaterialAvailabilityAsync(
        ErpOperationContext context,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ErpSyncReliabilityReadModel>> ListSyncReliabilityAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default);
}

public sealed class ErpInventoryRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IErpAdapterResolver adapterResolver) : IErpInventoryRepository
{
    internal const string MaterialAvailabilityFreshnessNote = "ERP stok projeksiyonuna dayalı bilgi";

    private sealed class SyncCounters
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
    }

    private static readonly string[] SyncReliabilityEntityTypes =
    [
        ErpSyncEntityTypes.Customers,
        ErpSyncEntityTypes.Products,
        ErpSyncEntityTypes.Orders,
        ErpSyncEntityTypes.Warehouses,
        ErpSyncEntityTypes.Inventory,
        ErpSyncEntityTypes.Purchasing,
        ErpSyncEntityTypes.Production,
        ErpSyncEntityTypes.Documents,
    ];

    public Task<ErpSyncResult> SyncWarehousesAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default) =>
        SyncProjectionAsync(
            context,
            connectionProfileId,
            ErpSyncEntityTypes.Warehouses,
            ErpCapabilities.WarehousesRead,
            retriedFromSyncRunId,
            static (adapter, adapterContext, ct) => adapter.ReadWarehousesAsync(adapterContext, ct),
            ApplyWarehouse,
            ErpAuditActions.SyncWarehousesStarted,
            ErpAuditEventTypes.SyncWarehousesStarted,
            ErpAuditActions.SyncWarehousesCompleted,
            ErpAuditEventTypes.SyncWarehousesCompleted,
            ErpAuditActions.SyncWarehousesFailed,
            ErpAuditEventTypes.SyncWarehousesFailed,
            ErpOutboxMessageTypes.SyncWarehousesCompleted,
            ErpOutboxMessageTypes.SyncWarehousesFailed,
            "erp.sync.warehouses.failed",
            cancellationToken);

    public Task<ErpSyncResult> SyncInventoryAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default) =>
        SyncProjectionAsync(
            context,
            connectionProfileId,
            ErpSyncEntityTypes.Inventory,
            ErpCapabilities.InventoryRead,
            retriedFromSyncRunId,
            static (adapter, adapterContext, ct) => adapter.ReadInventoryAsync(adapterContext, ct),
            ApplyInventoryBalance,
            ErpAuditActions.SyncInventoryStarted,
            ErpAuditEventTypes.SyncInventoryStarted,
            ErpAuditActions.SyncInventoryCompleted,
            ErpAuditEventTypes.SyncInventoryCompleted,
            ErpAuditActions.SyncInventoryFailed,
            ErpAuditEventTypes.SyncInventoryFailed,
            ErpOutboxMessageTypes.SyncInventoryCompleted,
            ErpOutboxMessageTypes.SyncInventoryFailed,
            "erp.sync.inventory.failed",
            cancellationToken);

    public Task<ErpSyncResult> SyncPurchasingAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default) =>
        SyncProjectionAsync(
            context,
            connectionProfileId,
            ErpSyncEntityTypes.Purchasing,
            ErpCapabilities.PurchasingRead,
            retriedFromSyncRunId,
            static (adapter, adapterContext, ct) => adapter.ReadPurchaseOrdersAsync(adapterContext, ct),
            ApplyPurchaseOrder,
            ErpAuditActions.SyncPurchasingStarted,
            ErpAuditEventTypes.SyncPurchasingStarted,
            ErpAuditActions.SyncPurchasingCompleted,
            ErpAuditEventTypes.SyncPurchasingCompleted,
            ErpAuditActions.SyncPurchasingFailed,
            ErpAuditEventTypes.SyncPurchasingFailed,
            ErpOutboxMessageTypes.SyncPurchasingCompleted,
            ErpOutboxMessageTypes.SyncPurchasingFailed,
            "erp.sync.purchasing.failed",
            cancellationToken);

    public Task<IReadOnlyList<CanonicalErpWarehouseReadModel>> ListWarehousesAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.CanonicalErpWarehouses.AsNoTracking()
                .Where(w => w.TenantId == context.TenantId && w.FacilityId == context.FacilityId);

            if (connectionProfileId.HasValue)
            {
                query = query.Where(w => w.ConnectionProfileId == connectionProfileId.Value);
            }

            var now = DateTimeOffset.UtcNow;
            var rows = await query.OrderBy(w => w.ExternalCode ?? w.ExternalId).ToListAsync(ct);
            return (IReadOnlyList<CanonicalErpWarehouseReadModel>)rows
                .Select(w => MapWarehouse(w, now))
                .ToList();
        }, cancellationToken);

    public Task<CanonicalErpWarehouseReadModel?> GetWarehouseAsync(
        ErpOperationContext context,
        Guid warehouseId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var row = await dbContext.CanonicalErpWarehouses.AsNoTracking()
                .SingleOrDefaultAsync(
                    w => w.TenantId == context.TenantId
                        && w.FacilityId == context.FacilityId
                        && w.Id == warehouseId,
                    ct);

            return row is null ? null : MapWarehouse(row, DateTimeOffset.UtcNow);
        }, cancellationToken);

    public Task<IReadOnlyList<CanonicalErpInventoryBalanceReadModel>> ListInventoryBalancesAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? productExternalId = null,
        string? warehouseExternalId = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.CanonicalErpInventoryBalances.AsNoTracking()
                .Where(b => b.TenantId == context.TenantId && b.FacilityId == context.FacilityId);

            query = ApplyInventoryFilters(query, connectionProfileId, productExternalId, warehouseExternalId);

            var now = DateTimeOffset.UtcNow;
            var rows = await query
                .OrderBy(b => b.ExternalProductCode ?? b.ExternalProductId)
                .ThenBy(b => b.ExternalWarehouseCode ?? b.ExternalWarehouseId)
                .ToListAsync(ct);

            var products = await LoadProductsForBalancesAsync(context, rows, ct);
            var warehouses = await LoadWarehousesForBalancesAsync(context, rows, ct);

            return (IReadOnlyList<CanonicalErpInventoryBalanceReadModel>)rows
                .Select(b => MapInventoryBalance(b, products, warehouses, now))
                .ToList();
        }, cancellationToken);

    public Task<IReadOnlyList<ErpInventoryAvailabilityReadModel>> ListInventoryAvailabilityAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? productExternalId = null,
        string? warehouseExternalId = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.CanonicalErpInventoryBalances.AsNoTracking()
                .Where(b => b.TenantId == context.TenantId && b.FacilityId == context.FacilityId);

            query = ApplyInventoryFilters(query, connectionProfileId, productExternalId, warehouseExternalId);

            var now = DateTimeOffset.UtcNow;
            var rows = await query.ToListAsync(ct);
            var products = await LoadProductsForBalancesAsync(context, rows, ct);
            var warehouses = await LoadWarehousesForBalancesAsync(context, rows, ct);
            var lastInventorySyncAt = await ResolveLastSuccessfulSyncAtUtcAsync(
                context,
                connectionProfileId,
                ErpSyncEntityTypes.Inventory,
                ct);

            var grouped = rows
                .GroupBy(b => b.ExternalProductId)
                .OrderBy(g => g.First().ExternalProductCode ?? g.Key);

            return (IReadOnlyList<ErpInventoryAvailabilityReadModel>)grouped
                .Select(group =>
                {
                    var sample = group.First();
                    var product = products.FirstOrDefault(p =>
                        p.ConnectionProfileId == sample.ConnectionProfileId
                        && p.ExternalId == sample.ExternalProductId);

                    var breakdown = group
                        .OrderBy(b => b.ExternalWarehouseCode ?? b.ExternalWarehouseId)
                        .Select(b =>
                        {
                            var warehouse = warehouses.FirstOrDefault(w =>
                                w.ConnectionProfileId == b.ConnectionProfileId
                                && w.ExternalId == b.ExternalWarehouseId);

                            return new ErpInventoryWarehouseBreakdownReadModel(
                                b.ExternalWarehouseId,
                                b.ExternalWarehouseCode ?? warehouse?.ExternalCode,
                                warehouse?.Name,
                                b.QuantityOnHand,
                                b.QuantityAvailable,
                                b.QuantityReserved,
                                b.QuantityOnOrder);
                        })
                        .ToList();

                    var totalOnHand = group.Sum(b => b.QuantityOnHand);
                    decimal? totalAvailable = group.Any(b => b.QuantityAvailable.HasValue)
                        ? group.Where(b => b.QuantityAvailable.HasValue).Sum(b => b.QuantityAvailable!.Value)
                        : null;

                    var latestSourceUpdated = group
                        .Select(b => b.SourceUpdatedAtUtc)
                        .Where(d => d.HasValue)
                        .OrderByDescending(d => d)
                        .FirstOrDefault();

                    return new ErpInventoryAvailabilityReadModel(
                        sample.ExternalProductId,
                        sample.ExternalProductCode ?? product?.ExternalCode,
                        product?.Name,
                        totalOnHand,
                        totalAvailable,
                        breakdown,
                        ErpSyncRepository.ResolveFreshness(latestSourceUpdated, now),
                        lastInventorySyncAt);
                })
                .ToList();
        }, cancellationToken);

    public Task<IReadOnlyList<CanonicalErpPurchaseOrderSummaryReadModel>> ListPurchaseOrdersAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.CanonicalErpPurchaseOrders.AsNoTracking()
                .Include(p => p.Lines)
                .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId);

            if (connectionProfileId.HasValue)
            {
                query = query.Where(p => p.ConnectionProfileId == connectionProfileId.Value);
            }

            var now = DateTimeOffset.UtcNow;
            var rows = await query.OrderByDescending(p => p.OrderDateUtc).ToListAsync(ct);
            return (IReadOnlyList<CanonicalErpPurchaseOrderSummaryReadModel>)rows
                .Select(p => MapPurchaseOrderSummary(p, now))
                .ToList();
        }, cancellationToken);

    public Task<CanonicalErpPurchaseOrderDetailReadModel?> GetPurchaseOrderAsync(
        ErpOperationContext context,
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var row = await dbContext.CanonicalErpPurchaseOrders.AsNoTracking()
                .Include(p => p.Lines)
                .SingleOrDefaultAsync(
                    p => p.TenantId == context.TenantId
                        && p.FacilityId == context.FacilityId
                        && p.Id == purchaseOrderId,
                    ct);

            return row is null ? null : MapPurchaseOrderDetail(row, DateTimeOffset.UtcNow);
        }, cancellationToken);

    public Task<ErpOrderMaterialAvailabilityReadModel?> GetOrderMaterialAvailabilityAsync(
        ErpOperationContext context,
        Guid orderId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var order = await dbContext.CanonicalErpOrders.AsNoTracking()
                .Include(o => o.Lines)
                .SingleOrDefaultAsync(
                    o => o.TenantId == context.TenantId
                        && o.FacilityId == context.FacilityId
                        && o.Id == orderId,
                    ct);

            if (order is null)
            {
                return null;
            }

            var now = DateTimeOffset.UtcNow;
            var inventoryBalances = await dbContext.CanonicalErpInventoryBalances.AsNoTracking()
                .Where(b => b.TenantId == context.TenantId
                    && b.FacilityId == context.FacilityId
                    && b.ConnectionProfileId == order.ConnectionProfileId)
                .ToListAsync(ct);

            var openPurchaseLines = await dbContext.CanonicalErpPurchaseOrderLines.AsNoTracking()
                .Where(l => l.TenantId == context.TenantId
                    && l.FacilityId == context.FacilityId
                    && l.ConnectionProfileId == order.ConnectionProfileId)
                .Join(
                    dbContext.CanonicalErpPurchaseOrders.AsNoTracking()
                        .Where(p => p.TenantId == context.TenantId
                            && p.FacilityId == context.FacilityId
                            && p.ConnectionProfileId == order.ConnectionProfileId
                            && (p.Status == ErpPurchaseOrderStatuses.Open
                                || p.Status == ErpPurchaseOrderStatuses.PartiallyReceived)),
                    line => line.PurchaseOrderId,
                    purchaseOrder => purchaseOrder.Id,
                    (line, _) => line)
                .ToListAsync(ct);

            var lines = order.Lines
                .OrderBy(l => l.LineNumber)
                .Select(line => BuildMaterialAvailabilityLine(
                    line,
                    inventoryBalances,
                    openPurchaseLines,
                    now))
                .ToList();

            return new ErpOrderMaterialAvailabilityReadModel(
                order.Id,
                order.OrderNumber,
                MaterialAvailabilityFreshnessNote,
                lines);
        }, cancellationToken);

    public Task<IReadOnlyList<ErpSyncReliabilityReadModel>> ListSyncReliabilityAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var query = dbContext.ErpSyncRuns.AsNoTracking()
                .Where(s => s.TenantId == context.TenantId && s.FacilityId == context.FacilityId);

            if (connectionProfileId.HasValue)
            {
                query = query.Where(s => s.ConnectionProfileId == connectionProfileId.Value);
            }

            var runs = await query
                .OrderByDescending(s => s.StartedAtUtc)
                .Take(200)
                .ToListAsync(ct);

            return (IReadOnlyList<ErpSyncReliabilityReadModel>)SyncReliabilityEntityTypes
                .Select(entityType => BuildSyncReliability(entityType, runs, now))
                .ToList();
        }, cancellationToken);

    private Task<ErpSyncResult> SyncProjectionAsync<TRecord>(
        ErpOperationContext context,
        Guid connectionProfileId,
        string entityType,
        string requiredCapability,
        Guid? retriedFromSyncRunId,
        Func<IErpAdapter, ErpAdapterConnectionContext, CancellationToken, Task<IReadOnlyList<TRecord>>> readAsync,
        Action<ErpOperationContext, ErpConnectionProfile, TRecord, DateTimeOffset, SyncCounters> apply,
        string startedAuditAction,
        string startedAuditEventType,
        string completedAuditAction,
        string completedAuditEventType,
        string failedAuditAction,
        string failedAuditEventType,
        string completedOutboxType,
        string failedOutboxType,
        string failureErrorCode,
        CancellationToken cancellationToken) =>
        ErpRepositorySupport.ExecuteSyncAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                var profile = await dbContext.ErpConnectionProfiles
                    .SingleOrDefaultAsync(
                        c => c.TenantId == context.TenantId
                            && c.FacilityId == context.FacilityId
                            && c.Id == connectionProfileId,
                        ct);

                if (profile is null)
                {
                    return ErpSyncResult.Fail(ErpFailureCodes.NotFound, "Connection profile not found.");
                }

                var runningExists = await dbContext.ErpSyncRuns
                    .AnyAsync(
                        s => s.TenantId == context.TenantId
                            && s.FacilityId == context.FacilityId
                            && s.ConnectionProfileId == connectionProfileId
                            && s.EntityType == entityType
                            && s.Status == ErpSyncRunStatuses.Running,
                        ct);

                if (runningExists)
                {
                    return ErpSyncResult.Fail(
                        ErpFailureCodes.SyncAlreadyRunning,
                        "A sync is already running for this connection and entity type.");
                }

                IErpAdapter adapter;
                try
                {
                    adapter = adapterResolver.Resolve(profile.Provider);
                }
                catch (InvalidOperationException)
                {
                    return ErpSyncResult.Fail(ErpFailureCodes.ProviderUnsupported, "Unknown ERP provider.");
                }

                if (!adapter.Capabilities.Contains(requiredCapability))
                {
                    return ErpSyncResult.Fail(
                        ErpFailureCodes.CapabilityUnsupported,
                        $"Provider '{profile.Provider}' does not support '{requiredCapability}'.");
                }

                var now = DateTimeOffset.UtcNow;
                var syncRun = new ErpSyncRun
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ConnectionProfileId = profile.Id,
                    EntityType = entityType,
                    StartedAtUtc = now,
                    Status = ErpSyncRunStatuses.Running,
                    TriggeredBySubjectId = context.IdentitySubjectId.ToString(),
                    RetriedFromSyncRunId = retriedFromSyncRunId,
                };

                dbContext.ErpSyncRuns.Add(syncRun);
                ErpRepositorySupport.AddAudit(
                    dbContext,
                    context,
                    startedAuditAction,
                    startedAuditEventType,
                    "erp.sync",
                    syncRun.Id,
                    now,
                    new { syncRunId = syncRun.Id, connectionProfileId = profile.Id, entityType });

                await dbContext.SaveChangesAsync(ct);

                try
                {
                    var adapterContext = ErpRepositorySupport.ToAdapterContext(profile);
                    var records = await readAsync(adapter, adapterContext, ct);
                    var counters = new SyncCounters();
                    const int failed = 0;
                    var read = records.Count;

                    foreach (var record in records)
                    {
                        apply(context, profile, record, now, counters);
                    }

                    var completedAt = DateTimeOffset.UtcNow;
                    syncRun.RecordsRead = read;
                    syncRun.RecordsCreated = counters.Created;
                    syncRun.RecordsUpdated = counters.Updated;
                    syncRun.RecordsSkipped = counters.Skipped;
                    syncRun.RecordsFailed = failed;
                    syncRun.CompletedAtUtc = completedAt;
                    syncRun.Status = failed > 0
                        ? ErpSyncRunStatuses.CompletedWithErrors
                        : ErpSyncRunStatuses.Completed;

                    profile.LastSuccessfulSyncAtUtc = completedAt;
                    profile.UpdatedAtUtc = completedAt;

                    ErpRepositorySupport.AddAudit(
                        dbContext,
                        context,
                        completedAuditAction,
                        completedAuditEventType,
                        "erp.sync",
                        syncRun.Id,
                        completedAt,
                        new
                        {
                            syncRun.Id,
                            entityType,
                            read,
                            created = counters.Created,
                            updated = counters.Updated,
                            skipped = counters.Skipped,
                            failed,
                        });
                    ErpRepositorySupport.AddOutbox(
                        dbContext,
                        context,
                        "erp-sync",
                        syncRun.Id,
                        1,
                        completedOutboxType,
                        completedAt,
                        new
                        {
                            syncRunId = syncRun.Id.ToString(),
                            connectionProfileId = profile.Id.ToString(),
                            entityType,
                            status = syncRun.Status,
                            recordsRead = read,
                            recordsCreated = counters.Created,
                            recordsUpdated = counters.Updated,
                            recordsSkipped = counters.Skipped,
                            recordsFailed = failed,
                        });

                    await dbContext.SaveChangesAsync(ct);

                    return ErpSyncResult.Ok(
                        syncRun.Id,
                        syncRun.Status,
                        read,
                        counters.Created,
                        counters.Updated,
                        counters.Skipped,
                        failed);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    var failedAt = DateTimeOffset.UtcNow;
                    syncRun.CompletedAtUtc = failedAt;
                    syncRun.Status = ErpSyncRunStatuses.Failed;
                    syncRun.ErrorCode = failureErrorCode;
                    syncRun.ErrorMessage = SanitizeError(ex.Message);
                    syncRun.RecordsFailed = Math.Max(syncRun.RecordsFailed, 1);

                    dbContext.ErpIntegrationErrors.Add(new ErpIntegrationError
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        SyncRunId = syncRun.Id,
                        Code = syncRun.ErrorCode,
                        EntityType = entityType,
                        Message = syncRun.ErrorMessage,
                        OccurredAtUtc = failedAt,
                    });

                    ErpRepositorySupport.AddAudit(
                        dbContext,
                        context,
                        failedAuditAction,
                        failedAuditEventType,
                        "erp.sync",
                        syncRun.Id,
                        failedAt,
                        new { syncRun.Id, entityType, syncRun.ErrorCode });
                    ErpRepositorySupport.AddOutbox(
                        dbContext,
                        context,
                        "erp-sync",
                        syncRun.Id,
                        1,
                        failedOutboxType,
                        failedAt,
                        new
                        {
                            syncRunId = syncRun.Id.ToString(),
                            connectionProfileId = profile.Id.ToString(),
                            entityType,
                            errorCode = syncRun.ErrorCode,
                        });

                    await dbContext.SaveChangesAsync(ct);
                    return ErpSyncResult.Fail(
                        ErpFailureCodes.SourceUnavailable,
                        syncRun.ErrorMessage,
                        syncRun.Id);
                }
            },
            cancellationToken);

    private void ApplyWarehouse(
        ErpOperationContext context,
        ErpConnectionProfile profile,
        ErpExternalWarehouseRecord record,
        DateTimeOffset now,
        SyncCounters counters)
    {
        var existing = dbContext.CanonicalErpWarehouses
            .SingleOrDefault(w =>
                w.TenantId == context.TenantId
                && w.FacilityId == context.FacilityId
                && w.ConnectionProfileId == profile.Id
                && w.ExternalId == record.ExternalId);

        if (existing is null)
        {
            dbContext.CanonicalErpWarehouses.Add(new CanonicalErpWarehouse
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ConnectionProfileId = profile.Id,
                Provider = profile.Provider,
                ExternalId = record.ExternalId,
                ExternalCode = record.ExternalCode,
                Name = record.Name,
                IsActive = record.IsActive,
                SourceVersion = record.SourceVersion,
                SourceUpdatedAtUtc = record.SourceUpdatedAtUtc,
                ImportedAtUtc = now,
                UpdatedAtUtc = now,
            });
            counters.Created++;
            return;
        }

        var changed =
            existing.Name != record.Name
            || existing.ExternalCode != record.ExternalCode
            || existing.IsActive != record.IsActive
            || existing.SourceVersion != record.SourceVersion
            || existing.SourceUpdatedAtUtc != record.SourceUpdatedAtUtc;

        if (!changed)
        {
            counters.Skipped++;
            return;
        }

        existing.ExternalCode = record.ExternalCode;
        existing.Name = record.Name;
        existing.IsActive = record.IsActive;
        existing.SourceVersion = record.SourceVersion;
        existing.SourceUpdatedAtUtc = record.SourceUpdatedAtUtc;
        existing.UpdatedAtUtc = now;
        counters.Updated++;
    }

    private void ApplyInventoryBalance(
        ErpOperationContext context,
        ErpConnectionProfile profile,
        ErpExternalInventoryBalanceRecord record,
        DateTimeOffset now,
        SyncCounters counters)
    {
        var existing = dbContext.CanonicalErpInventoryBalances
            .SingleOrDefault(b =>
                b.TenantId == context.TenantId
                && b.FacilityId == context.FacilityId
                && b.ConnectionProfileId == profile.Id
                && b.ExternalProductId == record.ExternalProductId
                && b.ExternalWarehouseId == record.ExternalWarehouseId);

        if (existing is null)
        {
            dbContext.CanonicalErpInventoryBalances.Add(new CanonicalErpInventoryBalance
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ConnectionProfileId = profile.Id,
                Provider = profile.Provider,
                ExternalProductId = record.ExternalProductId,
                ExternalProductCode = record.ExternalProductCode,
                ExternalWarehouseId = record.ExternalWarehouseId,
                ExternalWarehouseCode = record.ExternalWarehouseCode,
                QuantityOnHand = record.QuantityOnHand,
                QuantityAvailable = record.QuantityAvailable,
                QuantityReserved = record.QuantityReserved,
                QuantityOnOrder = record.QuantityOnOrder,
                Unit = record.Unit,
                SourceVersion = record.SourceVersion,
                SourceUpdatedAtUtc = record.SourceUpdatedAtUtc,
                ImportedAtUtc = now,
                UpdatedAtUtc = now,
            });
            counters.Created++;
            return;
        }

        var changed =
            existing.ExternalProductCode != record.ExternalProductCode
            || existing.ExternalWarehouseCode != record.ExternalWarehouseCode
            || existing.QuantityOnHand != record.QuantityOnHand
            || existing.QuantityAvailable != record.QuantityAvailable
            || existing.QuantityReserved != record.QuantityReserved
            || existing.QuantityOnOrder != record.QuantityOnOrder
            || existing.Unit != record.Unit
            || existing.SourceVersion != record.SourceVersion
            || existing.SourceUpdatedAtUtc != record.SourceUpdatedAtUtc;

        if (!changed)
        {
            counters.Skipped++;
            return;
        }

        existing.ExternalProductCode = record.ExternalProductCode;
        existing.ExternalWarehouseCode = record.ExternalWarehouseCode;
        existing.QuantityOnHand = record.QuantityOnHand;
        existing.QuantityAvailable = record.QuantityAvailable;
        existing.QuantityReserved = record.QuantityReserved;
        existing.QuantityOnOrder = record.QuantityOnOrder;
        existing.Unit = record.Unit;
        existing.SourceVersion = record.SourceVersion;
        existing.SourceUpdatedAtUtc = record.SourceUpdatedAtUtc;
        existing.UpdatedAtUtc = now;
        counters.Updated++;
    }

    private void ApplyPurchaseOrder(
        ErpOperationContext context,
        ErpConnectionProfile profile,
        ErpExternalPurchaseOrderRecord record,
        DateTimeOffset now,
        SyncCounters counters)
    {
        var existing = dbContext.CanonicalErpPurchaseOrders
            .Include(p => p.Lines)
            .SingleOrDefault(p =>
                p.TenantId == context.TenantId
                && p.FacilityId == context.FacilityId
                && p.ConnectionProfileId == profile.Id
                && p.ExternalId == record.ExternalId);

        var normalizedStatus = NormalizePurchaseOrderStatus(record.Status);

        if (existing is null)
        {
            var purchaseOrderId = Guid.NewGuid();
            var purchaseOrder = new CanonicalErpPurchaseOrder
            {
                Id = purchaseOrderId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ConnectionProfileId = profile.Id,
                Provider = profile.Provider,
                ExternalId = record.ExternalId,
                ExternalCode = record.ExternalCode,
                PurchaseOrderNumber = record.PurchaseOrderNumber,
                ExternalSupplierId = record.ExternalSupplierId,
                ExternalSupplierCode = record.ExternalSupplierCode,
                SupplierName = record.SupplierName,
                OrderDateUtc = record.OrderDateUtc,
                ExpectedDeliveryDateUtc = record.ExpectedDeliveryDateUtc,
                Status = normalizedStatus,
                CurrencyCode = record.CurrencyCode,
                SourceVersion = record.SourceVersion,
                SourceUpdatedAtUtc = record.SourceUpdatedAtUtc,
                ImportedAtUtc = now,
                UpdatedAtUtc = now,
            };

            foreach (var line in record.Lines)
            {
                purchaseOrder.Lines.Add(CreatePurchaseOrderLine(context, profile.Id, purchaseOrderId, line));
            }

            dbContext.CanonicalErpPurchaseOrders.Add(purchaseOrder);
            counters.Created += 1 + record.Lines.Count;
            return;
        }

        var headerUnchanged =
            existing.SourceVersion is not null
            && record.SourceVersion is not null
            && string.Equals(existing.SourceVersion, record.SourceVersion, StringComparison.Ordinal)
            && existing.ExternalCode == record.ExternalCode
            && existing.PurchaseOrderNumber == record.PurchaseOrderNumber
            && existing.ExternalSupplierId == record.ExternalSupplierId
            && existing.ExternalSupplierCode == record.ExternalSupplierCode
            && existing.SupplierName == record.SupplierName
            && existing.OrderDateUtc == record.OrderDateUtc
            && existing.ExpectedDeliveryDateUtc == record.ExpectedDeliveryDateUtc
            && existing.Status == normalizedStatus
            && existing.CurrencyCode == record.CurrencyCode
            && existing.SourceUpdatedAtUtc == record.SourceUpdatedAtUtc;

        var linesUnchanged = PurchaseOrderLinesMatch(existing.Lines, record.Lines);

        if (headerUnchanged && linesUnchanged)
        {
            counters.Skipped++;
            return;
        }

        existing.ExternalCode = record.ExternalCode;
        existing.PurchaseOrderNumber = record.PurchaseOrderNumber;
        existing.ExternalSupplierId = record.ExternalSupplierId;
        existing.ExternalSupplierCode = record.ExternalSupplierCode;
        existing.SupplierName = record.SupplierName;
        existing.OrderDateUtc = record.OrderDateUtc;
        existing.ExpectedDeliveryDateUtc = record.ExpectedDeliveryDateUtc;
        existing.Status = normalizedStatus;
        existing.CurrencyCode = record.CurrencyCode;
        existing.SourceVersion = record.SourceVersion;
        existing.SourceUpdatedAtUtc = record.SourceUpdatedAtUtc;
        existing.UpdatedAtUtc = now;

        foreach (var line in record.Lines)
        {
            var existingLine = existing.Lines.SingleOrDefault(l => l.ExternalLineId == line.ExternalLineId);
            if (existingLine is null)
            {
                existing.Lines.Add(CreatePurchaseOrderLine(context, profile.Id, existing.Id, line));
                counters.Created++;
                continue;
            }

            var lineChanged =
                existingLine.LineNumber != line.LineNumber
                || existingLine.ExternalProductId != line.ExternalProductId
                || existingLine.ExternalProductCode != line.ExternalProductCode
                || existingLine.Description != line.Description
                || existingLine.OrderedQuantity != line.OrderedQuantity
                || existingLine.ReceivedQuantity != line.ReceivedQuantity
                || existingLine.OpenQuantity != line.OpenQuantity
                || existingLine.Unit != line.Unit
                || existingLine.ExpectedDeliveryDateUtc != line.ExpectedDeliveryDateUtc
                || existingLine.SourceVersion != line.SourceVersion;

            if (!lineChanged)
            {
                continue;
            }

            existingLine.LineNumber = line.LineNumber;
            existingLine.ExternalProductId = line.ExternalProductId;
            existingLine.ExternalProductCode = line.ExternalProductCode;
            existingLine.Description = line.Description;
            existingLine.OrderedQuantity = line.OrderedQuantity;
            existingLine.ReceivedQuantity = line.ReceivedQuantity;
            existingLine.OpenQuantity = line.OpenQuantity;
            existingLine.Unit = line.Unit;
            existingLine.ExpectedDeliveryDateUtc = line.ExpectedDeliveryDateUtc;
            existingLine.SourceVersion = line.SourceVersion;
            counters.Updated++;
        }

        counters.Updated++;
    }

    private static CanonicalErpPurchaseOrderLine CreatePurchaseOrderLine(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid purchaseOrderId,
        ErpExternalPurchaseOrderLineRecord line) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ConnectionProfileId = connectionProfileId,
            PurchaseOrderId = purchaseOrderId,
            ExternalLineId = line.ExternalLineId,
            LineNumber = line.LineNumber,
            ExternalProductId = line.ExternalProductId,
            ExternalProductCode = line.ExternalProductCode,
            Description = line.Description,
            OrderedQuantity = line.OrderedQuantity,
            ReceivedQuantity = line.ReceivedQuantity,
            OpenQuantity = line.OpenQuantity,
            Unit = line.Unit,
            ExpectedDeliveryDateUtc = line.ExpectedDeliveryDateUtc,
            SourceVersion = line.SourceVersion,
        };

    private static bool PurchaseOrderLinesMatch(
        ICollection<CanonicalErpPurchaseOrderLine> existing,
        IReadOnlyList<ErpExternalPurchaseOrderLineRecord> incoming)
    {
        if (existing.Count != incoming.Count)
        {
            return false;
        }

        foreach (var line in incoming)
        {
            var match = existing.SingleOrDefault(l => l.ExternalLineId == line.ExternalLineId);
            if (match is null)
            {
                return false;
            }

            if (match.LineNumber != line.LineNumber
                || match.ExternalProductId != line.ExternalProductId
                || match.ExternalProductCode != line.ExternalProductCode
                || match.Description != line.Description
                || match.OrderedQuantity != line.OrderedQuantity
                || match.ReceivedQuantity != line.ReceivedQuantity
                || match.OpenQuantity != line.OpenQuantity
                || match.Unit != line.Unit
                || match.ExpectedDeliveryDateUtc != line.ExpectedDeliveryDateUtc
                || match.SourceVersion != line.SourceVersion)
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizePurchaseOrderStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return ErpPurchaseOrderStatuses.Unknown;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            ErpPurchaseOrderStatuses.Open
                or ErpPurchaseOrderStatuses.PartiallyReceived
                or ErpPurchaseOrderStatuses.Received
                or ErpPurchaseOrderStatuses.Cancelled
                or ErpPurchaseOrderStatuses.Closed
                or ErpPurchaseOrderStatuses.Unknown => normalized,
            _ => ErpPurchaseOrderStatuses.Unknown,
        };
    }

    private static IQueryable<CanonicalErpInventoryBalance> ApplyInventoryFilters(
        IQueryable<CanonicalErpInventoryBalance> query,
        Guid? connectionProfileId,
        string? productExternalId,
        string? warehouseExternalId)
    {
        if (connectionProfileId.HasValue)
        {
            query = query.Where(b => b.ConnectionProfileId == connectionProfileId.Value);
        }

        if (!string.IsNullOrWhiteSpace(productExternalId))
        {
            query = query.Where(b => b.ExternalProductId == productExternalId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(warehouseExternalId))
        {
            query = query.Where(b => b.ExternalWarehouseId == warehouseExternalId.Trim());
        }

        return query;
    }

    private async Task<IReadOnlyList<CanonicalErpProduct>> LoadProductsForBalancesAsync(
        ErpOperationContext context,
        List<CanonicalErpInventoryBalance> balances,
        CancellationToken cancellationToken)
    {
        if (balances.Count == 0)
        {
            return [];
        }

        var profileIds = balances.Select(b => b.ConnectionProfileId).Distinct().ToList();
        var productIds = balances.Select(b => b.ExternalProductId).Distinct().ToList();

        return await dbContext.CanonicalErpProducts.AsNoTracking()
            .Where(p => p.TenantId == context.TenantId
                && p.FacilityId == context.FacilityId
                && profileIds.Contains(p.ConnectionProfileId)
                && productIds.Contains(p.ExternalId))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<CanonicalErpWarehouse>> LoadWarehousesForBalancesAsync(
        ErpOperationContext context,
        List<CanonicalErpInventoryBalance> balances,
        CancellationToken cancellationToken)
    {
        if (balances.Count == 0)
        {
            return [];
        }

        var profileIds = balances.Select(b => b.ConnectionProfileId).Distinct().ToList();
        var warehouseIds = balances.Select(b => b.ExternalWarehouseId).Distinct().ToList();

        return await dbContext.CanonicalErpWarehouses.AsNoTracking()
            .Where(w => w.TenantId == context.TenantId
                && w.FacilityId == context.FacilityId
                && profileIds.Contains(w.ConnectionProfileId)
                && warehouseIds.Contains(w.ExternalId))
            .ToListAsync(cancellationToken);
    }

    private async Task<DateTimeOffset?> ResolveLastSuccessfulSyncAtUtcAsync(
        ErpOperationContext context,
        Guid? connectionProfileId,
        string entityType,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ErpSyncRuns.AsNoTracking()
            .Where(s => s.TenantId == context.TenantId
                && s.FacilityId == context.FacilityId
                && s.EntityType == entityType
                && s.Status == ErpSyncRunStatuses.Completed);

        if (connectionProfileId.HasValue)
        {
            query = query.Where(s => s.ConnectionProfileId == connectionProfileId.Value);
        }

        return await query
            .OrderByDescending(s => s.CompletedAtUtc)
            .Select(s => s.CompletedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static CanonicalErpWarehouseReadModel MapWarehouse(CanonicalErpWarehouse warehouse, DateTimeOffset now) =>
        new(
            warehouse.Id,
            warehouse.ConnectionProfileId,
            warehouse.Provider,
            warehouse.ExternalId,
            warehouse.ExternalCode,
            warehouse.Name,
            warehouse.IsActive,
            warehouse.SourceVersion,
            warehouse.SourceUpdatedAtUtc,
            warehouse.ImportedAtUtc,
            warehouse.UpdatedAtUtc,
            ErpSyncRepository.ResolveFreshness(warehouse.SourceUpdatedAtUtc, now));

    private static CanonicalErpInventoryBalanceReadModel MapInventoryBalance(
        CanonicalErpInventoryBalance balance,
        IReadOnlyList<CanonicalErpProduct> products,
        IReadOnlyList<CanonicalErpWarehouse> warehouses,
        DateTimeOffset now)
    {
        var product = products.FirstOrDefault(p =>
            p.ConnectionProfileId == balance.ConnectionProfileId
            && p.ExternalId == balance.ExternalProductId);
        var warehouse = warehouses.FirstOrDefault(w =>
            w.ConnectionProfileId == balance.ConnectionProfileId
            && w.ExternalId == balance.ExternalWarehouseId);

        return new CanonicalErpInventoryBalanceReadModel(
            balance.Id,
            balance.ConnectionProfileId,
            balance.Provider,
            balance.ExternalProductId,
            balance.ExternalProductCode ?? product?.ExternalCode,
            product?.Name,
            balance.ExternalWarehouseId,
            balance.ExternalWarehouseCode ?? warehouse?.ExternalCode,
            warehouse?.Name,
            balance.QuantityOnHand,
            balance.QuantityAvailable,
            balance.QuantityReserved,
            balance.QuantityOnOrder,
            balance.Unit,
            balance.SourceVersion,
            balance.SourceUpdatedAtUtc,
            balance.ImportedAtUtc,
            balance.UpdatedAtUtc,
            ErpSyncRepository.ResolveFreshness(balance.SourceUpdatedAtUtc, now));
    }

    private static CanonicalErpPurchaseOrderSummaryReadModel MapPurchaseOrderSummary(
        CanonicalErpPurchaseOrder purchaseOrder,
        DateTimeOffset now)
    {
        var openLineCount = purchaseOrder.Lines.Count(l => l.OpenQuantity is > 0);

        return new CanonicalErpPurchaseOrderSummaryReadModel(
            purchaseOrder.Id,
            purchaseOrder.ConnectionProfileId,
            purchaseOrder.Provider,
            purchaseOrder.ExternalId,
            purchaseOrder.ExternalCode,
            purchaseOrder.PurchaseOrderNumber,
            purchaseOrder.ExternalSupplierId,
            purchaseOrder.ExternalSupplierCode,
            purchaseOrder.SupplierName,
            purchaseOrder.OrderDateUtc,
            purchaseOrder.ExpectedDeliveryDateUtc,
            purchaseOrder.Status,
            purchaseOrder.CurrencyCode,
            purchaseOrder.SourceVersion,
            purchaseOrder.SourceUpdatedAtUtc,
            purchaseOrder.ImportedAtUtc,
            purchaseOrder.UpdatedAtUtc,
            purchaseOrder.Lines.Count,
            openLineCount,
            ErpSyncRepository.ResolveFreshness(purchaseOrder.SourceUpdatedAtUtc, now));
    }

    private static CanonicalErpPurchaseOrderDetailReadModel MapPurchaseOrderDetail(
        CanonicalErpPurchaseOrder purchaseOrder,
        DateTimeOffset now)
    {
        var lines = purchaseOrder.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => new CanonicalErpPurchaseOrderLineReadModel(
                l.Id,
                l.ExternalLineId,
                l.LineNumber,
                l.ExternalProductId,
                l.ExternalProductCode,
                l.Description,
                l.OrderedQuantity,
                l.ReceivedQuantity,
                l.OpenQuantity,
                l.Unit,
                l.ExpectedDeliveryDateUtc,
                l.SourceVersion))
            .ToList();

        return new CanonicalErpPurchaseOrderDetailReadModel(
            purchaseOrder.Id,
            purchaseOrder.ConnectionProfileId,
            purchaseOrder.Provider,
            purchaseOrder.ExternalId,
            purchaseOrder.ExternalCode,
            purchaseOrder.PurchaseOrderNumber,
            purchaseOrder.ExternalSupplierId,
            purchaseOrder.ExternalSupplierCode,
            purchaseOrder.SupplierName,
            purchaseOrder.OrderDateUtc,
            purchaseOrder.ExpectedDeliveryDateUtc,
            purchaseOrder.Status,
            purchaseOrder.CurrencyCode,
            purchaseOrder.SourceVersion,
            purchaseOrder.SourceUpdatedAtUtc,
            purchaseOrder.ImportedAtUtc,
            purchaseOrder.UpdatedAtUtc,
            ErpSyncRepository.ResolveFreshness(purchaseOrder.SourceUpdatedAtUtc, now),
            lines);
    }

    private static ErpMaterialAvailabilityLineReadModel BuildMaterialAvailabilityLine(
        CanonicalErpOrderLine line,
        IReadOnlyList<CanonicalErpInventoryBalance> inventoryBalances,
        IReadOnlyList<CanonicalErpPurchaseOrderLine> openPurchaseLines,
        DateTimeOffset now)
    {
        var productBalances = inventoryBalances
            .Where(b => b.ExternalProductId == line.ExternalProductId)
            .ToList();

        decimal? availableQuantity = productBalances.Any(b => b.QuantityAvailable.HasValue)
            ? productBalances.Where(b => b.QuantityAvailable.HasValue).Sum(b => b.QuantityAvailable!.Value)
            : null;

        var latestSourceUpdated = productBalances
            .Select(b => b.SourceUpdatedAtUtc)
            .Where(d => d.HasValue)
            .OrderByDescending(d => d)
            .FirstOrDefault();

        var freshness = ErpSyncRepository.ResolveFreshness(latestSourceUpdated, now);
        var requiredQuantity = line.Quantity;
        decimal? shortageQuantity = null;
        string status;

        if (!availableQuantity.HasValue)
        {
            status = ErpShortageStatuses.Unknown;
        }
        else if (freshness == ErpSourceFreshness.Stale)
        {
            status = ErpShortageStatuses.StaleSource;
            if (availableQuantity.Value < requiredQuantity)
            {
                shortageQuantity = requiredQuantity - availableQuantity.Value;
            }
        }
        else if (availableQuantity.Value >= requiredQuantity)
        {
            status = ErpShortageStatuses.Sufficient;
        }
        else
        {
            status = ErpShortageStatuses.Insufficient;
            shortageQuantity = requiredQuantity - availableQuantity.Value;
        }

        ErpPurchaseCoverageReadModel? purchaseCoverage = null;
        if (shortageQuantity is > 0)
        {
            var matchingOpenLines = openPurchaseLines
                .Where(l => l.ExternalProductId == line.ExternalProductId && l.OpenQuantity is > 0)
                .ToList();

            if (matchingOpenLines.Count == 0)
            {
                purchaseCoverage = new ErpPurchaseCoverageReadModel(
                    0m,
                    null,
                    ErpCoverageStatuses.NotCovered);
            }
            else
            {
                var openPurchaseQuantity = matchingOpenLines.Sum(l => l.OpenQuantity!.Value);
                var nearestExpectedDeliveryDate = matchingOpenLines
                    .Select(l => l.ExpectedDeliveryDateUtc)
                    .Where(d => d.HasValue)
                    .OrderBy(d => d)
                    .FirstOrDefault();

                var coverageStatus = openPurchaseQuantity >= shortageQuantity.Value
                    ? ErpCoverageStatuses.Covered
                    : openPurchaseQuantity > 0
                        ? ErpCoverageStatuses.PartiallyCovered
                        : ErpCoverageStatuses.NotCovered;

                purchaseCoverage = new ErpPurchaseCoverageReadModel(
                    openPurchaseQuantity,
                    nearestExpectedDeliveryDate,
                    coverageStatus);
            }
        }
        else if (!availableQuantity.HasValue)
        {
            var matchingOpenLines = openPurchaseLines
                .Where(l => l.ExternalProductId == line.ExternalProductId && l.OpenQuantity is > 0)
                .ToList();

            if (matchingOpenLines.Count > 0)
            {
                purchaseCoverage = new ErpPurchaseCoverageReadModel(
                    matchingOpenLines.Sum(l => l.OpenQuantity!.Value),
                    matchingOpenLines
                        .Select(l => l.ExpectedDeliveryDateUtc)
                        .Where(d => d.HasValue)
                        .OrderBy(d => d)
                        .FirstOrDefault(),
                    ErpCoverageStatuses.Unknown);
            }
        }

        return new ErpMaterialAvailabilityLineReadModel(
            line.ExternalLineId,
            line.LineNumber,
            line.ExternalProductId,
            line.ExternalProductCode,
            line.Description,
            requiredQuantity,
            availableQuantity,
            shortageQuantity,
            status,
            purchaseCoverage);
    }

    private static ErpSyncReliabilityReadModel BuildSyncReliability(
        string entityType,
        IReadOnlyList<ErpSyncRun> runs,
        DateTimeOffset now)
    {
        var entityRuns = runs
            .Where(r => r.EntityType == entityType)
            .OrderByDescending(r => r.StartedAtUtc)
            .ToList();

        var lastRun = entityRuns.FirstOrDefault();
        var lastSuccessful = entityRuns
            .FirstOrDefault(r => r.Status == ErpSyncRunStatuses.Completed);
        var lastFailed = entityRuns
            .FirstOrDefault(r => r.Status is ErpSyncRunStatuses.Failed or ErpSyncRunStatuses.CompletedWithErrors);

        var consecutiveFailures = 0;
        foreach (var run in entityRuns)
        {
            if (run.Status == ErpSyncRunStatuses.Completed)
            {
                break;
            }

            if (run.Status is ErpSyncRunStatuses.Failed or ErpSyncRunStatuses.CompletedWithErrors)
            {
                consecutiveFailures++;
            }
        }

        return new ErpSyncReliabilityReadModel(
            entityType,
            lastSuccessful?.CompletedAtUtc,
            lastFailed?.CompletedAtUtc ?? lastFailed?.StartedAtUtc,
            lastRun?.Status,
            lastRun?.RecordsRead ?? 0,
            lastRun?.RecordsFailed ?? 0,
            consecutiveFailures,
            ErpSyncRepository.ResolveFreshness(lastSuccessful?.CompletedAtUtc, now),
            lastFailed?.ErrorCode,
            lastFailed?.ErrorMessage);
    }

    private static string SanitizeError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "ERP sync failed.";
        }

        var trimmed = message.Trim();
        if (trimmed.Length > 500)
        {
            trimmed = trimmed[..500];
        }

        return trimmed
            .Replace("password", "[redacted]", StringComparison.OrdinalIgnoreCase)
            .Replace("token", "[redacted]", StringComparison.OrdinalIgnoreCase)
            .Replace("apikey", "[redacted]", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class UnavailableErpInventoryRepository : IErpInventoryRepository
{
    public Task<ErpSyncResult> SyncWarehousesAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpSyncResult.Fail(ErpFailureCodes.SourceUnavailable));

    public Task<ErpSyncResult> SyncInventoryAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpSyncResult.Fail(ErpFailureCodes.SourceUnavailable));

    public Task<ErpSyncResult> SyncPurchasingAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpSyncResult.Fail(ErpFailureCodes.SourceUnavailable));

    public Task<IReadOnlyList<CanonicalErpWarehouseReadModel>> ListWarehousesAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CanonicalErpWarehouseReadModel>>([]);

    public Task<CanonicalErpWarehouseReadModel?> GetWarehouseAsync(
        ErpOperationContext context,
        Guid warehouseId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CanonicalErpWarehouseReadModel?>(null);

    public Task<IReadOnlyList<CanonicalErpInventoryBalanceReadModel>> ListInventoryBalancesAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? productExternalId = null,
        string? warehouseExternalId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CanonicalErpInventoryBalanceReadModel>>([]);

    public Task<IReadOnlyList<ErpInventoryAvailabilityReadModel>> ListInventoryAvailabilityAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? productExternalId = null,
        string? warehouseExternalId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ErpInventoryAvailabilityReadModel>>([]);

    public Task<IReadOnlyList<CanonicalErpPurchaseOrderSummaryReadModel>> ListPurchaseOrdersAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CanonicalErpPurchaseOrderSummaryReadModel>>([]);

    public Task<CanonicalErpPurchaseOrderDetailReadModel?> GetPurchaseOrderAsync(
        ErpOperationContext context,
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CanonicalErpPurchaseOrderDetailReadModel?>(null);

    public Task<ErpOrderMaterialAvailabilityReadModel?> GetOrderMaterialAvailabilityAsync(
        ErpOperationContext context,
        Guid orderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ErpOrderMaterialAvailabilityReadModel?>(null);

    public Task<IReadOnlyList<ErpSyncReliabilityReadModel>> ListSyncReliabilityAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ErpSyncReliabilityReadModel>>([]);
}
