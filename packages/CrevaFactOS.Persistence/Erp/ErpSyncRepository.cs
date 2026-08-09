using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Erp;

public interface IErpSyncRepository
{
    Task<ErpSyncResult> SyncCustomersAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default);

    Task<ErpSyncResult> SyncProductsAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default);

    Task<ErpSyncResult> SyncOrdersAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default);

    Task<ErpSyncResult> RetrySyncRunAsync(
        ErpOperationContext context,
        Guid syncRunId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ErpSyncRunSummaryReadModel>> ListSyncRunsAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default);

    Task<ErpSyncRunSummaryReadModel?> GetSyncRunAsync(
        ErpOperationContext context,
        Guid syncRunId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CanonicalErpCustomerReadModel>> ListCustomersAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CanonicalErpProductReadModel>> ListProductsAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CanonicalErpOrderSummaryReadModel>> ListOrdersAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<CanonicalErpOrderDetailReadModel?> GetOrderAsync(
        ErpOperationContext context,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<ErpOrderReadinessReadModel?> GetOrderReadinessAsync(
        ErpOperationContext context,
        Guid orderId,
        CancellationToken cancellationToken = default);
}

public sealed class ErpSyncRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IErpAdapterResolver adapterResolver) : IErpSyncRepository
{
    public Task<ErpSyncResult> SyncCustomersAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default) =>
        SyncEntityAsync(
            context,
            connectionProfileId,
            ErpSyncEntityTypes.Customers,
            ErpCapabilities.CustomersRead,
            retriedFromSyncRunId,
            cancellationToken);

    public Task<ErpSyncResult> SyncProductsAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default) =>
        SyncEntityAsync(
            context,
            connectionProfileId,
            ErpSyncEntityTypes.Products,
            ErpCapabilities.ProductsRead,
            retriedFromSyncRunId,
            cancellationToken);

    public async Task<ErpSyncResult> RetrySyncRunAsync(
        ErpOperationContext context,
        Guid syncRunId,
        CancellationToken cancellationToken = default)
    {
        var existing = await tenantDatabaseSession.ExecuteReadAsync(
            async ct => await dbContext.ErpSyncRuns.AsNoTracking()
                .SingleOrDefaultAsync(
                    s => s.TenantId == context.TenantId
                        && s.FacilityId == context.FacilityId
                        && s.Id == syncRunId,
                    ct),
            cancellationToken);

        if (existing is null)
        {
            return ErpSyncResult.Fail(ErpFailureCodes.NotFound, "Sync run not found.");
        }

        if (existing.Status is not (ErpSyncRunStatuses.Failed or ErpSyncRunStatuses.CompletedWithErrors))
        {
            return ErpSyncResult.Fail(
                ErpFailureCodes.ValidationFailed,
                "Only failed or completed-with-errors sync runs can be retried.");
        }

        return existing.EntityType switch
        {
            ErpSyncEntityTypes.Customers => await SyncCustomersAsync(
                context, existing.ConnectionProfileId, existing.Id, cancellationToken),
            ErpSyncEntityTypes.Products => await SyncProductsAsync(
                context, existing.ConnectionProfileId, existing.Id, cancellationToken),
            ErpSyncEntityTypes.Orders => await SyncOrdersAsync(
                context, existing.ConnectionProfileId, existing.Id, cancellationToken),
            ErpSyncEntityTypes.Warehouses or ErpSyncEntityTypes.Inventory or ErpSyncEntityTypes.Purchasing =>
                ErpSyncResult.Fail(
                    ErpFailureCodes.ValidationFailed,
                    "Retry for this entity type must be routed through inventory sync."),
            _ => ErpSyncResult.Fail(ErpFailureCodes.ValidationFailed, "Unsupported entity type for retry."),
        };
    }

    public Task<IReadOnlyList<ErpSyncRunSummaryReadModel>> ListSyncRunsAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.ErpSyncRuns.AsNoTracking()
                .Include(s => s.Errors)
                .Where(s => s.TenantId == context.TenantId && s.FacilityId == context.FacilityId);

            if (connectionProfileId.HasValue)
            {
                query = query.Where(s => s.ConnectionProfileId == connectionProfileId.Value);
            }

            var rows = await query
                .OrderByDescending(s => s.StartedAtUtc)
                .Take(50)
                .ToListAsync(ct);

            return (IReadOnlyList<ErpSyncRunSummaryReadModel>)rows.Select(MapSyncRun).ToList();
        }, cancellationToken);

    public Task<ErpSyncRunSummaryReadModel?> GetSyncRunAsync(
        ErpOperationContext context,
        Guid syncRunId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var row = await dbContext.ErpSyncRuns.AsNoTracking()
                .Include(s => s.Errors)
                .SingleOrDefaultAsync(
                    s => s.TenantId == context.TenantId
                        && s.FacilityId == context.FacilityId
                        && s.Id == syncRunId,
                    ct);

            return row is null ? null : MapSyncRun(row);
        }, cancellationToken);

    public Task<IReadOnlyList<CanonicalErpCustomerReadModel>> ListCustomersAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.CanonicalErpCustomers.AsNoTracking()
                .Where(c => c.TenantId == context.TenantId && c.FacilityId == context.FacilityId);

            if (connectionProfileId.HasValue)
            {
                query = query.Where(c => c.ConnectionProfileId == connectionProfileId.Value);
            }

            var rows = await query.OrderBy(c => c.ExternalCode ?? c.ExternalId).ToListAsync(ct);
            return (IReadOnlyList<CanonicalErpCustomerReadModel>)rows.Select(c => new CanonicalErpCustomerReadModel(
                c.Id,
                c.ConnectionProfileId,
                c.Provider,
                c.ExternalId,
                c.ExternalCode,
                c.Name,
                c.TaxNumber,
                c.TaxOffice,
                c.Email,
                c.Phone,
                c.CountryCode,
                c.IsActive,
                c.SourceVersion,
                c.SourceUpdatedAtUtc,
                c.ImportedAtUtc,
                c.UpdatedAtUtc)).ToList();
        }, cancellationToken);

    public Task<IReadOnlyList<CanonicalErpProductReadModel>> ListProductsAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.CanonicalErpProducts.AsNoTracking()
                .Where(c => c.TenantId == context.TenantId && c.FacilityId == context.FacilityId);

            if (connectionProfileId.HasValue)
            {
                query = query.Where(c => c.ConnectionProfileId == connectionProfileId.Value);
            }

            var rows = await query.OrderBy(c => c.ExternalCode ?? c.ExternalId).ToListAsync(ct);
            return (IReadOnlyList<CanonicalErpProductReadModel>)rows.Select(c => new CanonicalErpProductReadModel(
                c.Id,
                c.ConnectionProfileId,
                c.Provider,
                c.ExternalId,
                c.ExternalCode,
                c.Name,
                c.Unit,
                c.Barcode,
                c.IsActive,
                c.SourceVersion,
                c.SourceUpdatedAtUtc,
                c.ImportedAtUtc,
                c.UpdatedAtUtc)).ToList();
        }, cancellationToken);

    public Task<ErpSyncResult> SyncOrdersAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default) =>
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
                            && s.EntityType == ErpSyncEntityTypes.Orders
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

                if (!adapter.Capabilities.Contains(ErpCapabilities.OrdersRead))
                {
                    return ErpSyncResult.Fail(
                        ErpFailureCodes.CapabilityUnsupported,
                        $"Provider '{profile.Provider}' does not support '{ErpCapabilities.OrdersRead}'.");
                }

                var now = DateTimeOffset.UtcNow;
                var syncRun = new ErpSyncRun
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ConnectionProfileId = profile.Id,
                    EntityType = ErpSyncEntityTypes.Orders,
                    StartedAtUtc = now,
                    Status = ErpSyncRunStatuses.Running,
                    TriggeredBySubjectId = context.IdentitySubjectId.ToString(),
                    RetriedFromSyncRunId = retriedFromSyncRunId,
                };

                dbContext.ErpSyncRuns.Add(syncRun);
                ErpRepositorySupport.AddAudit(
                    dbContext,
                    context,
                    ErpAuditActions.SyncOrdersStarted,
                    ErpAuditEventTypes.SyncOrdersStarted,
                    "erp.sync",
                    syncRun.Id,
                    now,
                    new { syncRunId = syncRun.Id, connectionProfileId = profile.Id, entityType = ErpSyncEntityTypes.Orders });

                await dbContext.SaveChangesAsync(ct);

                try
                {
                    var adapterContext = ErpRepositorySupport.ToAdapterContext(profile);
                    var records = await adapter.ReadOrdersAsync(adapterContext, ct);
                    int created = 0, updated = 0, skipped = 0, failed = 0;
                    var read = records.Count;

                    foreach (var record in records)
                    {
                        ApplyOrder(context, profile, record, now, ref created, ref updated, ref skipped);
                    }

                    var completedAt = DateTimeOffset.UtcNow;
                    syncRun.RecordsRead = read;
                    syncRun.RecordsCreated = created;
                    syncRun.RecordsUpdated = updated;
                    syncRun.RecordsSkipped = skipped;
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
                        ErpAuditActions.SyncOrdersCompleted,
                        ErpAuditEventTypes.SyncOrdersCompleted,
                        "erp.sync",
                        syncRun.Id,
                        completedAt,
                        new { syncRun.Id, entityType = ErpSyncEntityTypes.Orders, read, created, updated, skipped, failed });
                    ErpRepositorySupport.AddOutbox(
                        dbContext,
                        context,
                        "erp-sync",
                        syncRun.Id,
                        1,
                        ErpOutboxMessageTypes.SyncOrdersCompleted,
                        completedAt,
                        new
                        {
                            syncRunId = syncRun.Id.ToString(),
                            connectionProfileId = profile.Id.ToString(),
                            entityType = ErpSyncEntityTypes.Orders,
                            status = syncRun.Status,
                            recordsRead = read,
                            recordsCreated = created,
                            recordsUpdated = updated,
                            recordsSkipped = skipped,
                            recordsFailed = failed,
                        });

                    await dbContext.SaveChangesAsync(ct);

                    return ErpSyncResult.Ok(
                        syncRun.Id,
                        syncRun.Status,
                        read,
                        created,
                        updated,
                        skipped,
                        failed);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    var failedAt = DateTimeOffset.UtcNow;
                    syncRun.CompletedAtUtc = failedAt;
                    syncRun.Status = ErpSyncRunStatuses.Failed;
                    syncRun.ErrorCode = "erp.sync.orders.failed";
                    syncRun.ErrorMessage = SanitizeError(ex.Message);
                    syncRun.RecordsFailed = Math.Max(syncRun.RecordsFailed, 1);

                    dbContext.ErpIntegrationErrors.Add(new ErpIntegrationError
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        SyncRunId = syncRun.Id,
                        Code = syncRun.ErrorCode,
                        EntityType = ErpSyncEntityTypes.Orders,
                        Message = syncRun.ErrorMessage,
                        OccurredAtUtc = failedAt,
                    });

                    ErpRepositorySupport.AddAudit(
                        dbContext,
                        context,
                        ErpAuditActions.SyncOrdersFailed,
                        ErpAuditEventTypes.SyncOrdersFailed,
                        "erp.sync",
                        syncRun.Id,
                        failedAt,
                        new { syncRun.Id, entityType = ErpSyncEntityTypes.Orders, syncRun.ErrorCode });
                    ErpRepositorySupport.AddOutbox(
                        dbContext,
                        context,
                        "erp-sync",
                        syncRun.Id,
                        1,
                        ErpOutboxMessageTypes.SyncOrdersFailed,
                        failedAt,
                        new
                        {
                            syncRunId = syncRun.Id.ToString(),
                            connectionProfileId = profile.Id.ToString(),
                            entityType = ErpSyncEntityTypes.Orders,
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

    public Task<IReadOnlyList<CanonicalErpOrderSummaryReadModel>> ListOrdersAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? status = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.CanonicalErpOrders.AsNoTracking()
                .Include(o => o.Lines)
                .Where(o => o.TenantId == context.TenantId && o.FacilityId == context.FacilityId);

            if (connectionProfileId.HasValue)
            {
                query = query.Where(o => o.ConnectionProfileId == connectionProfileId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalized = status.Trim().ToLowerInvariant();
                query = query.Where(o => o.Status == normalized);
            }

            var rows = await query.OrderByDescending(o => o.OrderDateUtc).ToListAsync(ct);
            var mappings = await dbContext.ErpEntityMappings.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId && m.FacilityId == context.FacilityId)
                .ToListAsync(ct);

            return (IReadOnlyList<CanonicalErpOrderSummaryReadModel>)rows
                .Select(o => MapOrderSummary(o, mappings, DateTimeOffset.UtcNow))
                .ToList();
        }, cancellationToken);

    public Task<CanonicalErpOrderDetailReadModel?> GetOrderAsync(
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

            var mappings = await dbContext.ErpEntityMappings.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId
                    && m.FacilityId == context.FacilityId
                    && m.ConnectionProfileId == order.ConnectionProfileId)
                .ToListAsync(ct);

            return MapOrderDetail(order, mappings, DateTimeOffset.UtcNow);
        }, cancellationToken);

    public Task<ErpOrderReadinessReadModel?> GetOrderReadinessAsync(
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

            var mappings = await dbContext.ErpEntityMappings.AsNoTracking()
                .Where(m => m.TenantId == context.TenantId
                    && m.FacilityId == context.FacilityId
                    && m.ConnectionProfileId == order.ConnectionProfileId)
                .ToListAsync(ct);

            return BuildReadiness(order, mappings, DateTimeOffset.UtcNow);
        }, cancellationToken);

    private Task<ErpSyncResult> SyncEntityAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        string entityType,
        string requiredCapability,
        Guid? retriedFromSyncRunId,
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
                    ErpAuditActions.SyncStarted,
                    ErpAuditEventTypes.SyncStarted,
                    "erp.sync",
                    syncRun.Id,
                    now,
                    new { syncRunId = syncRun.Id, connectionProfileId = profile.Id, entityType });

                await dbContext.SaveChangesAsync(ct);

                try
                {
                    var adapterContext = ErpRepositorySupport.ToAdapterContext(profile);
                    int created = 0, updated = 0, skipped = 0, failed = 0, read = 0;

                    if (entityType == ErpSyncEntityTypes.Customers)
                    {
                        var records = await adapter.ReadCustomersAsync(adapterContext, ct);
                        read = records.Count;
                        foreach (var record in records)
                        {
                            ApplyCustomer(context, profile, record, now, ref created, ref updated, ref skipped);
                            EnsureMapping(
                                context,
                                profile.Id,
                                ErpSyncEntityTypes.Customers,
                                record.ExternalId,
                                now);
                        }
                    }
                    else
                    {
                        var records = await adapter.ReadProductsAsync(adapterContext, ct);
                        read = records.Count;
                        foreach (var record in records)
                        {
                            ApplyProduct(context, profile, record, now, ref created, ref updated, ref skipped);
                            EnsureMapping(
                                context,
                                profile.Id,
                                ErpSyncEntityTypes.Products,
                                record.ExternalId,
                                now);
                        }
                    }

                    var completedAt = DateTimeOffset.UtcNow;
                    syncRun.RecordsRead = read;
                    syncRun.RecordsCreated = created;
                    syncRun.RecordsUpdated = updated;
                    syncRun.RecordsSkipped = skipped;
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
                        ErpAuditActions.SyncCompleted,
                        ErpAuditEventTypes.SyncCompleted,
                        "erp.sync",
                        syncRun.Id,
                        completedAt,
                        new { syncRun.Id, entityType, read, created, updated, skipped, failed });
                    ErpRepositorySupport.AddOutbox(
                        dbContext,
                        context,
                        "erp-sync",
                        syncRun.Id,
                        1,
                        ErpOutboxMessageTypes.SyncCompleted,
                        completedAt,
                        new
                        {
                            syncRunId = syncRun.Id.ToString(),
                            connectionProfileId = profile.Id.ToString(),
                            entityType,
                            status = syncRun.Status,
                            recordsRead = read,
                            recordsCreated = created,
                            recordsUpdated = updated,
                            recordsSkipped = skipped,
                            recordsFailed = failed,
                        });

                    await dbContext.SaveChangesAsync(ct);

                    return ErpSyncResult.Ok(
                        syncRun.Id,
                        syncRun.Status,
                        read,
                        created,
                        updated,
                        skipped,
                        failed);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    var failedAt = DateTimeOffset.UtcNow;
                    syncRun.CompletedAtUtc = failedAt;
                    syncRun.Status = ErpSyncRunStatuses.Failed;
                    syncRun.ErrorCode = "erp.sync.failed";
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
                        ErpAuditActions.SyncFailed,
                        ErpAuditEventTypes.SyncFailed,
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
                        ErpOutboxMessageTypes.SyncFailed,
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

    private void ApplyCustomer(
        ErpOperationContext context,
        ErpConnectionProfile profile,
        ErpExternalCustomerRecord record,
        DateTimeOffset now,
        ref int created,
        ref int updated,
        ref int skipped)
    {
        var existing = dbContext.CanonicalErpCustomers
            .SingleOrDefault(c =>
                c.TenantId == context.TenantId
                && c.FacilityId == context.FacilityId
                && c.ConnectionProfileId == profile.Id
                && c.ExternalId == record.ExternalId);

        if (existing is null)
        {
            dbContext.CanonicalErpCustomers.Add(new CanonicalErpCustomer
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ConnectionProfileId = profile.Id,
                Provider = profile.Provider,
                ExternalId = record.ExternalId,
                ExternalCode = record.ExternalCode,
                Name = record.Name,
                TaxNumber = record.TaxNumber,
                TaxOffice = record.TaxOffice,
                Email = record.Email,
                Phone = record.Phone,
                CountryCode = record.CountryCode,
                IsActive = record.IsActive,
                SourceVersion = record.SourceVersion,
                SourceUpdatedAtUtc = record.SourceUpdatedAtUtc,
                ImportedAtUtc = now,
                UpdatedAtUtc = now,
            });
            created++;
            return;
        }

        var changed =
            existing.Name != record.Name
            || existing.ExternalCode != record.ExternalCode
            || existing.TaxNumber != record.TaxNumber
            || existing.TaxOffice != record.TaxOffice
            || existing.Email != record.Email
            || existing.Phone != record.Phone
            || existing.CountryCode != record.CountryCode
            || existing.IsActive != record.IsActive
            || existing.SourceVersion != record.SourceVersion
            || existing.SourceUpdatedAtUtc != record.SourceUpdatedAtUtc;

        if (!changed)
        {
            skipped++;
            return;
        }

        existing.ExternalCode = record.ExternalCode;
        existing.Name = record.Name;
        existing.TaxNumber = record.TaxNumber;
        existing.TaxOffice = record.TaxOffice;
        existing.Email = record.Email;
        existing.Phone = record.Phone;
        existing.CountryCode = record.CountryCode;
        existing.IsActive = record.IsActive;
        existing.SourceVersion = record.SourceVersion;
        existing.SourceUpdatedAtUtc = record.SourceUpdatedAtUtc;
        existing.UpdatedAtUtc = now;
        updated++;
    }

    private void ApplyProduct(
        ErpOperationContext context,
        ErpConnectionProfile profile,
        ErpExternalProductRecord record,
        DateTimeOffset now,
        ref int created,
        ref int updated,
        ref int skipped)
    {
        var existing = dbContext.CanonicalErpProducts
            .SingleOrDefault(c =>
                c.TenantId == context.TenantId
                && c.FacilityId == context.FacilityId
                && c.ConnectionProfileId == profile.Id
                && c.ExternalId == record.ExternalId);

        if (existing is null)
        {
            dbContext.CanonicalErpProducts.Add(new CanonicalErpProduct
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ConnectionProfileId = profile.Id,
                Provider = profile.Provider,
                ExternalId = record.ExternalId,
                ExternalCode = record.ExternalCode,
                Name = record.Name,
                Unit = record.Unit,
                Barcode = record.Barcode,
                IsActive = record.IsActive,
                SourceVersion = record.SourceVersion,
                SourceUpdatedAtUtc = record.SourceUpdatedAtUtc,
                ImportedAtUtc = now,
                UpdatedAtUtc = now,
            });
            created++;
            return;
        }

        var changed =
            existing.Name != record.Name
            || existing.ExternalCode != record.ExternalCode
            || existing.Unit != record.Unit
            || existing.Barcode != record.Barcode
            || existing.IsActive != record.IsActive
            || existing.SourceVersion != record.SourceVersion
            || existing.SourceUpdatedAtUtc != record.SourceUpdatedAtUtc;

        if (!changed)
        {
            skipped++;
            return;
        }

        existing.ExternalCode = record.ExternalCode;
        existing.Name = record.Name;
        existing.Unit = record.Unit;
        existing.Barcode = record.Barcode;
        existing.IsActive = record.IsActive;
        existing.SourceVersion = record.SourceVersion;
        existing.SourceUpdatedAtUtc = record.SourceUpdatedAtUtc;
        existing.UpdatedAtUtc = now;
        updated++;
    }

    private void ApplyOrder(
        ErpOperationContext context,
        ErpConnectionProfile profile,
        ErpExternalOrderRecord record,
        DateTimeOffset now,
        ref int created,
        ref int updated,
        ref int skipped)
    {
        var existing = dbContext.CanonicalErpOrders
            .Include(o => o.Lines)
            .SingleOrDefault(o =>
                o.TenantId == context.TenantId
                && o.FacilityId == context.FacilityId
                && o.ConnectionProfileId == profile.Id
                && o.ExternalId == record.ExternalId);

        var normalizedStatus = NormalizeOrderStatus(record.Status);

        if (existing is null)
        {
            var orderId = Guid.NewGuid();
            var order = new CanonicalErpOrder
            {
                Id = orderId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ConnectionProfileId = profile.Id,
                Provider = profile.Provider,
                ExternalId = record.ExternalId,
                ExternalCode = record.ExternalCode,
                OrderNumber = record.OrderNumber,
                ExternalCustomerId = record.ExternalCustomerId,
                ExternalCustomerCode = record.ExternalCustomerCode,
                OrderDateUtc = record.OrderDateUtc,
                RequestedDeliveryDateUtc = record.RequestedDeliveryDateUtc,
                CurrencyCode = record.CurrencyCode,
                Status = normalizedStatus,
                Reference = record.Reference,
                Notes = record.Notes,
                SourceVersion = record.SourceVersion,
                SourceUpdatedAtUtc = record.SourceUpdatedAtUtc,
                ImportedAtUtc = now,
                UpdatedAtUtc = now,
            };

            foreach (var line in record.Lines)
            {
                order.Lines.Add(CreateOrderLine(context, profile.Id, orderId, line));
            }

            dbContext.CanonicalErpOrders.Add(order);
            created += 1 + record.Lines.Count;
            return;
        }

        var headerUnchanged =
            existing.SourceVersion is not null
            && record.SourceVersion is not null
            && string.Equals(existing.SourceVersion, record.SourceVersion, StringComparison.Ordinal)
            && existing.ExternalCode == record.ExternalCode
            && existing.OrderNumber == record.OrderNumber
            && existing.ExternalCustomerId == record.ExternalCustomerId
            && existing.ExternalCustomerCode == record.ExternalCustomerCode
            && existing.OrderDateUtc == record.OrderDateUtc
            && existing.RequestedDeliveryDateUtc == record.RequestedDeliveryDateUtc
            && existing.CurrencyCode == record.CurrencyCode
            && existing.Status == normalizedStatus
            && existing.Reference == record.Reference
            && existing.Notes == record.Notes
            && existing.SourceUpdatedAtUtc == record.SourceUpdatedAtUtc;

        var linesUnchanged = LinesMatch(existing.Lines, record.Lines);

        if (headerUnchanged && linesUnchanged)
        {
            skipped++;
            return;
        }

        existing.ExternalCode = record.ExternalCode;
        existing.OrderNumber = record.OrderNumber;
        existing.ExternalCustomerId = record.ExternalCustomerId;
        existing.ExternalCustomerCode = record.ExternalCustomerCode;
        existing.OrderDateUtc = record.OrderDateUtc;
        existing.RequestedDeliveryDateUtc = record.RequestedDeliveryDateUtc;
        existing.CurrencyCode = record.CurrencyCode;
        existing.Status = normalizedStatus;
        existing.Reference = record.Reference;
        existing.Notes = record.Notes;
        existing.SourceVersion = record.SourceVersion;
        existing.SourceUpdatedAtUtc = record.SourceUpdatedAtUtc;
        existing.UpdatedAtUtc = now;

        foreach (var line in record.Lines)
        {
            var existingLine = existing.Lines.SingleOrDefault(l => l.ExternalLineId == line.ExternalLineId);
            if (existingLine is null)
            {
                existing.Lines.Add(CreateOrderLine(context, profile.Id, existing.Id, line));
                created++;
                continue;
            }

            var lineChanged =
                existingLine.LineNumber != line.LineNumber
                || existingLine.ExternalProductId != line.ExternalProductId
                || existingLine.ExternalProductCode != line.ExternalProductCode
                || existingLine.Description != line.Description
                || existingLine.Quantity != line.Quantity
                || existingLine.Unit != line.Unit
                || existingLine.RequestedDeliveryDateUtc != line.RequestedDeliveryDateUtc
                || existingLine.SourceVersion != line.SourceVersion;

            if (!lineChanged)
            {
                continue;
            }

            existingLine.LineNumber = line.LineNumber;
            existingLine.ExternalProductId = line.ExternalProductId;
            existingLine.ExternalProductCode = line.ExternalProductCode;
            existingLine.Description = line.Description;
            existingLine.Quantity = line.Quantity;
            existingLine.Unit = line.Unit;
            existingLine.RequestedDeliveryDateUtc = line.RequestedDeliveryDateUtc;
            existingLine.SourceVersion = line.SourceVersion;
            updated++;
        }

        updated++;
    }

    private static CanonicalErpOrderLine CreateOrderLine(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid orderId,
        ErpExternalOrderLineRecord line) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ConnectionProfileId = connectionProfileId,
            OrderId = orderId,
            ExternalLineId = line.ExternalLineId,
            LineNumber = line.LineNumber,
            ExternalProductId = line.ExternalProductId,
            ExternalProductCode = line.ExternalProductCode,
            Description = line.Description,
            Quantity = line.Quantity,
            Unit = line.Unit,
            RequestedDeliveryDateUtc = line.RequestedDeliveryDateUtc,
            SourceVersion = line.SourceVersion,
        };

    private static bool LinesMatch(
        ICollection<CanonicalErpOrderLine> existing,
        IReadOnlyList<ErpExternalOrderLineRecord> incoming)
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
                || match.Quantity != line.Quantity
                || match.Unit != line.Unit
                || match.RequestedDeliveryDateUtc != line.RequestedDeliveryDateUtc
                || match.SourceVersion != line.SourceVersion)
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizeOrderStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return ErpOrderStatuses.Unknown;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            ErpOrderStatuses.Open
                or ErpOrderStatuses.Confirmed
                or ErpOrderStatuses.PartiallyFulfilled
                or ErpOrderStatuses.Fulfilled
                or ErpOrderStatuses.Cancelled
                or ErpOrderStatuses.Unknown => normalized,
            _ => ErpOrderStatuses.Unknown,
        };
    }

    private static CanonicalErpOrderSummaryReadModel MapOrderSummary(
        CanonicalErpOrder order,
        IReadOnlyList<ErpEntityMapping> mappings,
        DateTimeOffset now)
    {
        var readiness = BuildReadiness(order, mappings, now);
        var customerMapping = FindMapping(mappings, order.ConnectionProfileId, ErpSyncEntityTypes.Customers, order.ExternalCustomerId);

        return new CanonicalErpOrderSummaryReadModel(
            order.Id,
            order.ConnectionProfileId,
            order.Provider,
            order.ExternalId,
            order.ExternalCode,
            order.OrderNumber,
            order.ExternalCustomerId,
            order.ExternalCustomerCode,
            order.OrderDateUtc,
            order.RequestedDeliveryDateUtc,
            order.CurrencyCode,
            order.Status,
            order.Reference,
            order.SourceVersion,
            order.SourceUpdatedAtUtc,
            order.ImportedAtUtc,
            order.UpdatedAtUtc,
            order.Lines.Count,
            customerMapping?.Status ?? ErpMappingStatuses.Unmapped,
            readiness.ReadyForOperationalUse,
            readiness.SourceFreshness);
    }

    private static CanonicalErpOrderDetailReadModel MapOrderDetail(
        CanonicalErpOrder order,
        IReadOnlyList<ErpEntityMapping> mappings,
        DateTimeOffset now)
    {
        var readiness = BuildReadiness(order, mappings, now);
        var lines = order.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l =>
            {
                var productMapping = FindMapping(
                    mappings,
                    order.ConnectionProfileId,
                    ErpSyncEntityTypes.Products,
                    l.ExternalProductId);

                return new CanonicalErpOrderLineReadModel(
                    l.Id,
                    l.ExternalLineId,
                    l.LineNumber,
                    l.ExternalProductId,
                    l.ExternalProductCode,
                    l.Description,
                    l.Quantity,
                    l.Unit,
                    l.RequestedDeliveryDateUtc,
                    l.SourceVersion,
                    productMapping?.Status ?? ErpMappingStatuses.Unmapped,
                    productMapping?.InternalEntityId);
            })
            .ToList();

        return new CanonicalErpOrderDetailReadModel(
            order.Id,
            order.ConnectionProfileId,
            order.Provider,
            order.ExternalId,
            order.ExternalCode,
            order.OrderNumber,
            order.ExternalCustomerId,
            order.ExternalCustomerCode,
            order.OrderDateUtc,
            order.RequestedDeliveryDateUtc,
            order.CurrencyCode,
            order.Status,
            order.Reference,
            order.Notes,
            order.SourceVersion,
            order.SourceUpdatedAtUtc,
            order.ImportedAtUtc,
            order.UpdatedAtUtc,
            lines,
            readiness);
    }

    internal static ErpOrderReadinessReadModel BuildReadiness(
        CanonicalErpOrder order,
        IReadOnlyList<ErpEntityMapping> mappings,
        DateTimeOffset now)
    {
        var blocking = new List<ErpReadinessBlockingReason>();
        var freshness = ResolveFreshness(order.SourceUpdatedAtUtc, now);

        var customerMapping = FindMapping(
            mappings,
            order.ConnectionProfileId,
            ErpSyncEntityTypes.Customers,
            order.ExternalCustomerId);

        var customerMapped = customerMapping?.Status == ErpMappingStatuses.Mapped
            && customerMapping.InternalEntityId.HasValue;

        if (customerMapping?.Status == ErpMappingStatuses.Conflict)
        {
            blocking.Add(new ErpReadinessBlockingReason(
                ErpReadinessBlockingCodes.MappingConflict,
                "Customer mapping is in conflict."));
        }
        else if (!customerMapped)
        {
            blocking.Add(new ErpReadinessBlockingReason(
                ErpReadinessBlockingCodes.CustomerUnmapped,
                "Customer is not mapped to an operational reference."));
        }

        var missingProducts = 0;
        foreach (var line in order.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.ExternalProductId))
            {
                missingProducts++;
                blocking.Add(new ErpReadinessBlockingReason(
                    ErpReadinessBlockingCodes.MissingProductReference,
                    $"Line {line.LineNumber} is missing an external product reference."));
                continue;
            }

            if (line.Quantity <= 0)
            {
                blocking.Add(new ErpReadinessBlockingReason(
                    ErpReadinessBlockingCodes.InvalidQuantity,
                    $"Line {line.LineNumber} has an invalid quantity."));
            }

            var productMapping = FindMapping(
                mappings,
                order.ConnectionProfileId,
                ErpSyncEntityTypes.Products,
                line.ExternalProductId);

            if (productMapping?.Status == ErpMappingStatuses.Conflict)
            {
                blocking.Add(new ErpReadinessBlockingReason(
                    ErpReadinessBlockingCodes.MappingConflict,
                    $"Product mapping conflict for {line.ExternalProductId}."));
            }
            else if (productMapping?.Status != ErpMappingStatuses.Mapped || !productMapping.InternalEntityId.HasValue)
            {
                missingProducts++;
                blocking.Add(new ErpReadinessBlockingReason(
                    ErpReadinessBlockingCodes.ProductUnmapped,
                    $"Product {line.ExternalProductId} is not mapped."));
            }
        }

        if (freshness == ErpSourceFreshness.Stale)
        {
            blocking.Add(new ErpReadinessBlockingReason(
                ErpReadinessBlockingCodes.SourceStale,
                "Source order record is stale."));
        }

        var distinctBlocking = blocking
            .GroupBy(b => (b.Code, b.Message))
            .Select(g => g.First())
            .ToList();

        return new ErpOrderReadinessReadModel(
            order.Id,
            customerMapped,
            missingProducts == 0,
            missingProducts,
            freshness,
            distinctBlocking.Count == 0,
            distinctBlocking);
    }

    internal static string ResolveFreshness(DateTimeOffset? sourceUpdatedAtUtc, DateTimeOffset now)
    {
        if (!sourceUpdatedAtUtc.HasValue)
        {
            return ErpSourceFreshness.Unknown;
        }

        return now - sourceUpdatedAtUtc.Value > TimeSpan.FromDays(30)
            ? ErpSourceFreshness.Stale
            : ErpSourceFreshness.Fresh;
    }

    private static ErpEntityMapping? FindMapping(
        IReadOnlyList<ErpEntityMapping> mappings,
        Guid connectionProfileId,
        string entityType,
        string externalId) =>
        mappings.FirstOrDefault(m =>
            m.ConnectionProfileId == connectionProfileId
            && m.EntityType == entityType
            && m.ExternalId == externalId);

    private void EnsureMapping(
        ErpOperationContext context,
        Guid connectionProfileId,
        string entityType,
        string externalId,
        DateTimeOffset now)
    {
        var existing = dbContext.ErpEntityMappings
            .SingleOrDefault(m =>
                m.TenantId == context.TenantId
                && m.FacilityId == context.FacilityId
                && m.ConnectionProfileId == connectionProfileId
                && m.EntityType == entityType
                && m.ExternalId == externalId);

        if (existing is not null)
        {
            if (!existing.CanonicalEntityId.HasValue)
            {
                existing.CanonicalEntityId = ResolveCanonicalId(context, connectionProfileId, entityType, externalId);
                existing.UpdatedAtUtc = now;
            }

            return;
        }

        var canonicalId = ResolveCanonicalId(context, connectionProfileId, entityType, externalId);

        dbContext.ErpEntityMappings.Add(new ErpEntityMapping
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ConnectionProfileId = connectionProfileId,
            EntityType = entityType,
            ExternalId = externalId,
            CanonicalEntityId = canonicalId,
            InternalEntityId = null,
            SuggestedInternalEntityId = null,
            SuggestedReason = null,
            Status = ErpMappingStatuses.Unmapped,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1,
        });
    }

    private Guid? ResolveCanonicalId(
        ErpOperationContext context,
        Guid connectionProfileId,
        string entityType,
        string externalId)
    {
        if (entityType == ErpSyncEntityTypes.Customers)
        {
            return dbContext.CanonicalErpCustomers.Local
                .FirstOrDefault(c =>
                    c.ConnectionProfileId == connectionProfileId
                    && c.ExternalId == externalId)?.Id
                ?? dbContext.CanonicalErpCustomers
                    .FirstOrDefault(c =>
                        c.TenantId == context.TenantId
                        && c.FacilityId == context.FacilityId
                        && c.ConnectionProfileId == connectionProfileId
                        && c.ExternalId == externalId)?.Id;
        }

        return dbContext.CanonicalErpProducts.Local
            .FirstOrDefault(c =>
                c.ConnectionProfileId == connectionProfileId
                && c.ExternalId == externalId)?.Id
            ?? dbContext.CanonicalErpProducts
                .FirstOrDefault(c =>
                    c.TenantId == context.TenantId
                    && c.FacilityId == context.FacilityId
                    && c.ConnectionProfileId == connectionProfileId
                    && c.ExternalId == externalId)?.Id;
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

    private static ErpSyncRunSummaryReadModel MapSyncRun(ErpSyncRun s) =>
        new(
            s.Id,
            s.ConnectionProfileId,
            s.EntityType,
            s.StartedAtUtc,
            s.CompletedAtUtc,
            s.Status,
            s.RecordsRead,
            s.RecordsCreated,
            s.RecordsUpdated,
            s.RecordsSkipped,
            s.RecordsFailed,
            s.ErrorCode,
            s.ErrorMessage,
            s.TriggeredBySubjectId,
            s.Errors.OrderBy(e => e.OccurredAtUtc)
                .Select(e => new ErpIntegrationErrorReadModel(
                    e.Code,
                    e.EntityType,
                    e.ExternalId,
                    e.Message,
                    e.OccurredAtUtc))
                .ToList());
}

public sealed class UnavailableErpSyncRepository : IErpSyncRepository
{
    public Task<ErpSyncResult> SyncCustomersAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpSyncResult.Fail(ErpFailureCodes.SourceUnavailable));

    public Task<ErpSyncResult> SyncProductsAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpSyncResult.Fail(ErpFailureCodes.SourceUnavailable));

    public Task<ErpSyncResult> SyncOrdersAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpSyncResult.Fail(ErpFailureCodes.SourceUnavailable));

    public Task<ErpSyncResult> RetrySyncRunAsync(
        ErpOperationContext context,
        Guid syncRunId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpSyncResult.Fail(ErpFailureCodes.SourceUnavailable));

    public Task<IReadOnlyList<ErpSyncRunSummaryReadModel>> ListSyncRunsAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ErpSyncRunSummaryReadModel>>([]);

    public Task<ErpSyncRunSummaryReadModel?> GetSyncRunAsync(
        ErpOperationContext context,
        Guid syncRunId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ErpSyncRunSummaryReadModel?>(null);

    public Task<IReadOnlyList<CanonicalErpCustomerReadModel>> ListCustomersAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CanonicalErpCustomerReadModel>>([]);

    public Task<IReadOnlyList<CanonicalErpProductReadModel>> ListProductsAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CanonicalErpProductReadModel>>([]);

    public Task<IReadOnlyList<CanonicalErpOrderSummaryReadModel>> ListOrdersAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? status = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CanonicalErpOrderSummaryReadModel>>([]);

    public Task<CanonicalErpOrderDetailReadModel?> GetOrderAsync(
        ErpOperationContext context,
        Guid orderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CanonicalErpOrderDetailReadModel?>(null);

    public Task<ErpOrderReadinessReadModel?> GetOrderReadinessAsync(
        ErpOperationContext context,
        Guid orderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ErpOrderReadinessReadModel?>(null);
}
