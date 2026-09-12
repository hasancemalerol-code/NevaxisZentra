using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Erp;

public interface IErpProductionRepository
{
    Task<ErpSyncResult> SyncProductionAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default);

    Task<ErpSyncResult> SyncDocumentsAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CanonicalErpProductionOrderReadModel>> ListProductionOrdersAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? status = null,
        string? productExternalId = null,
        CancellationToken cancellationToken = default);

    Task<CanonicalErpProductionOrderReadModel?> GetProductionOrderAsync(
        ErpOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CanonicalErpDocumentReadModel>> ListDocumentsAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? documentType = null,
        string? relatedEntityType = null,
        string? relatedExternalEntityId = null,
        CancellationToken cancellationToken = default);

    Task<CanonicalErpDocumentReadModel?> GetDocumentAsync(
        ErpOperationContext context,
        Guid documentId,
        CancellationToken cancellationToken = default);
}

public sealed class ErpProductionRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IErpAdapterResolver adapterResolver) : IErpProductionRepository
{
    private sealed class SyncCounters
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
    }

    public Task<ErpSyncResult> SyncProductionAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default) =>
        SyncProjectionAsync(
            context,
            connectionProfileId,
            ErpSyncEntityTypes.Production,
            ErpCapabilities.ProductionRead,
            retriedFromSyncRunId,
            static (adapter, adapterContext, ct) => adapter.ReadProductionOrdersAsync(adapterContext, ct),
            ApplyProductionOrder,
            ErpAuditActions.SyncProductionStarted,
            ErpAuditEventTypes.SyncProductionStarted,
            ErpAuditActions.SyncProductionCompleted,
            ErpAuditEventTypes.SyncProductionCompleted,
            ErpAuditActions.SyncProductionFailed,
            ErpAuditEventTypes.SyncProductionFailed,
            ErpOutboxMessageTypes.SyncProductionCompleted,
            ErpOutboxMessageTypes.SyncProductionFailed,
            "erp.sync.production.failed",
            cancellationToken);

    public Task<ErpSyncResult> SyncDocumentsAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default) =>
        SyncProjectionAsync(
            context,
            connectionProfileId,
            ErpSyncEntityTypes.Documents,
            ErpCapabilities.DocumentsRead,
            retriedFromSyncRunId,
            static (adapter, adapterContext, ct) => adapter.ReadDocumentsAsync(adapterContext, ct),
            ApplyDocument,
            ErpAuditActions.SyncDocumentsStarted,
            ErpAuditEventTypes.SyncDocumentsStarted,
            ErpAuditActions.SyncDocumentsCompleted,
            ErpAuditEventTypes.SyncDocumentsCompleted,
            ErpAuditActions.SyncDocumentsFailed,
            ErpAuditEventTypes.SyncDocumentsFailed,
            ErpOutboxMessageTypes.SyncDocumentsCompleted,
            ErpOutboxMessageTypes.SyncDocumentsFailed,
            "erp.sync.documents.failed",
            cancellationToken);

    public Task<IReadOnlyList<CanonicalErpProductionOrderReadModel>> ListProductionOrdersAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? status = null,
        string? productExternalId = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.CanonicalErpProductionOrders.AsNoTracking()
                .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId);

            query = ApplyProductionOrderFilters(query, connectionProfileId, status, productExternalId);

            var now = DateTimeOffset.UtcNow;
            var rows = await query
                .OrderByDescending(p => p.PlannedStartUtc ?? p.UpdatedAtUtc)
                .ThenBy(p => p.ProductionOrderNumber)
                .ToListAsync(ct);

            return (IReadOnlyList<CanonicalErpProductionOrderReadModel>)rows
                .Select(p => MapProductionOrder(p, now))
                .ToList();
        }, cancellationToken);

    public Task<CanonicalErpProductionOrderReadModel?> GetProductionOrderAsync(
        ErpOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var row = await dbContext.CanonicalErpProductionOrders.AsNoTracking()
                .SingleOrDefaultAsync(
                    p => p.TenantId == context.TenantId
                        && p.FacilityId == context.FacilityId
                        && p.Id == productionOrderId,
                    ct);

            return row is null ? null : MapProductionOrder(row, DateTimeOffset.UtcNow);
        }, cancellationToken);

    public Task<IReadOnlyList<CanonicalErpDocumentReadModel>> ListDocumentsAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? documentType = null,
        string? relatedEntityType = null,
        string? relatedExternalEntityId = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.CanonicalErpDocuments.AsNoTracking()
                .Where(d => d.TenantId == context.TenantId && d.FacilityId == context.FacilityId);

            query = ApplyDocumentFilters(
                query,
                connectionProfileId,
                documentType,
                relatedEntityType,
                relatedExternalEntityId);

            var now = DateTimeOffset.UtcNow;
            var rows = await query
                .OrderByDescending(d => d.DocumentDateUtc ?? d.UpdatedAtUtc)
                .ThenBy(d => d.DocumentNumber ?? d.ExternalCode ?? d.ExternalId)
                .ToListAsync(ct);

            return (IReadOnlyList<CanonicalErpDocumentReadModel>)rows
                .Select(d => MapDocument(d, now))
                .ToList();
        }, cancellationToken);

    public Task<CanonicalErpDocumentReadModel?> GetDocumentAsync(
        ErpOperationContext context,
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var row = await dbContext.CanonicalErpDocuments.AsNoTracking()
                .SingleOrDefaultAsync(
                    d => d.TenantId == context.TenantId
                        && d.FacilityId == context.FacilityId
                        && d.Id == documentId,
                    ct);

            return row is null ? null : MapDocument(row, DateTimeOffset.UtcNow);
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

    private void ApplyProductionOrder(
        ErpOperationContext context,
        ErpConnectionProfile profile,
        ErpExternalProductionOrderRecord record,
        DateTimeOffset now,
        SyncCounters counters)
    {
        var existing = dbContext.CanonicalErpProductionOrders
            .SingleOrDefault(p =>
                p.TenantId == context.TenantId
                && p.FacilityId == context.FacilityId
                && p.ConnectionProfileId == profile.Id
                && p.ExternalId == record.ExternalId);

        var normalizedStatus = NormalizeProductionOrderStatus(record.Status);

        if (existing is null)
        {
            dbContext.CanonicalErpProductionOrders.Add(new CanonicalErpProductionOrder
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ConnectionProfileId = profile.Id,
                Provider = profile.Provider,
                ExternalId = record.ExternalId,
                ExternalCode = record.ExternalCode,
                ProductionOrderNumber = record.ProductionOrderNumber,
                ExternalProductId = record.ExternalProductId,
                ExternalProductCode = record.ExternalProductCode,
                ProductDescription = record.ProductDescription,
                PlannedQuantity = record.PlannedQuantity,
                CompletedQuantity = record.CompletedQuantity,
                ScrapQuantity = record.ScrapQuantity,
                Unit = record.Unit,
                Status = normalizedStatus,
                PlannedStartUtc = record.PlannedStartUtc,
                PlannedEndUtc = record.PlannedEndUtc,
                ActualStartUtc = record.ActualStartUtc,
                ActualEndUtc = record.ActualEndUtc,
                ExternalSalesOrderId = record.ExternalSalesOrderId,
                ExternalSalesOrderCode = record.ExternalSalesOrderCode,
                SourceVersion = record.SourceVersion,
                SourceUpdatedAtUtc = record.SourceUpdatedAtUtc,
                ImportedAtUtc = now,
                UpdatedAtUtc = now,
            });
            counters.Created++;
            return;
        }

        var changed =
            existing.ExternalCode != record.ExternalCode
            || existing.ProductionOrderNumber != record.ProductionOrderNumber
            || existing.ExternalProductId != record.ExternalProductId
            || existing.ExternalProductCode != record.ExternalProductCode
            || existing.ProductDescription != record.ProductDescription
            || existing.PlannedQuantity != record.PlannedQuantity
            || existing.CompletedQuantity != record.CompletedQuantity
            || existing.ScrapQuantity != record.ScrapQuantity
            || existing.Unit != record.Unit
            || existing.Status != normalizedStatus
            || existing.PlannedStartUtc != record.PlannedStartUtc
            || existing.PlannedEndUtc != record.PlannedEndUtc
            || existing.ActualStartUtc != record.ActualStartUtc
            || existing.ActualEndUtc != record.ActualEndUtc
            || existing.ExternalSalesOrderId != record.ExternalSalesOrderId
            || existing.ExternalSalesOrderCode != record.ExternalSalesOrderCode
            || existing.SourceVersion != record.SourceVersion
            || existing.SourceUpdatedAtUtc != record.SourceUpdatedAtUtc;

        if (!changed)
        {
            counters.Skipped++;
            return;
        }

        existing.ExternalCode = record.ExternalCode;
        existing.ProductionOrderNumber = record.ProductionOrderNumber;
        existing.ExternalProductId = record.ExternalProductId;
        existing.ExternalProductCode = record.ExternalProductCode;
        existing.ProductDescription = record.ProductDescription;
        existing.PlannedQuantity = record.PlannedQuantity;
        existing.CompletedQuantity = record.CompletedQuantity;
        existing.ScrapQuantity = record.ScrapQuantity;
        existing.Unit = record.Unit;
        existing.Status = normalizedStatus;
        existing.PlannedStartUtc = record.PlannedStartUtc;
        existing.PlannedEndUtc = record.PlannedEndUtc;
        existing.ActualStartUtc = record.ActualStartUtc;
        existing.ActualEndUtc = record.ActualEndUtc;
        existing.ExternalSalesOrderId = record.ExternalSalesOrderId;
        existing.ExternalSalesOrderCode = record.ExternalSalesOrderCode;
        existing.SourceVersion = record.SourceVersion;
        existing.SourceUpdatedAtUtc = record.SourceUpdatedAtUtc;
        existing.UpdatedAtUtc = now;
        counters.Updated++;
    }

    private void ApplyDocument(
        ErpOperationContext context,
        ErpConnectionProfile profile,
        ErpExternalDocumentRecord record,
        DateTimeOffset now,
        SyncCounters counters)
    {
        var existing = dbContext.CanonicalErpDocuments
            .SingleOrDefault(d =>
                d.TenantId == context.TenantId
                && d.FacilityId == context.FacilityId
                && d.ConnectionProfileId == profile.Id
                && d.ExternalId == record.ExternalId);

        var normalizedDocumentType = NormalizeDocumentType(record.DocumentType);

        if (existing is null)
        {
            dbContext.CanonicalErpDocuments.Add(new CanonicalErpDocument
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ConnectionProfileId = profile.Id,
                Provider = profile.Provider,
                ExternalId = record.ExternalId,
                ExternalCode = record.ExternalCode,
                DocumentType = normalizedDocumentType,
                DocumentNumber = record.DocumentNumber,
                DocumentDateUtc = record.DocumentDateUtc,
                Status = record.Status,
                RelatedEntityType = record.RelatedEntityType,
                RelatedExternalEntityId = record.RelatedExternalEntityId,
                RelatedExternalEntityCode = record.RelatedExternalEntityCode,
                FileName = record.FileName,
                MimeType = record.MimeType,
                ContentAvailable = record.ContentAvailable,
                ExternalReference = record.ExternalReference,
                SourceVersion = record.SourceVersion,
                SourceUpdatedAtUtc = record.SourceUpdatedAtUtc,
                ImportedAtUtc = now,
                UpdatedAtUtc = now,
            });
            counters.Created++;
            return;
        }

        var changed =
            existing.ExternalCode != record.ExternalCode
            || existing.DocumentType != normalizedDocumentType
            || existing.DocumentNumber != record.DocumentNumber
            || existing.DocumentDateUtc != record.DocumentDateUtc
            || existing.Status != record.Status
            || existing.RelatedEntityType != record.RelatedEntityType
            || existing.RelatedExternalEntityId != record.RelatedExternalEntityId
            || existing.RelatedExternalEntityCode != record.RelatedExternalEntityCode
            || existing.FileName != record.FileName
            || existing.MimeType != record.MimeType
            || existing.ContentAvailable != record.ContentAvailable
            || existing.ExternalReference != record.ExternalReference
            || existing.SourceVersion != record.SourceVersion
            || existing.SourceUpdatedAtUtc != record.SourceUpdatedAtUtc;

        if (!changed)
        {
            counters.Skipped++;
            return;
        }

        existing.ExternalCode = record.ExternalCode;
        existing.DocumentType = normalizedDocumentType;
        existing.DocumentNumber = record.DocumentNumber;
        existing.DocumentDateUtc = record.DocumentDateUtc;
        existing.Status = record.Status;
        existing.RelatedEntityType = record.RelatedEntityType;
        existing.RelatedExternalEntityId = record.RelatedExternalEntityId;
        existing.RelatedExternalEntityCode = record.RelatedExternalEntityCode;
        existing.FileName = record.FileName;
        existing.MimeType = record.MimeType;
        existing.ContentAvailable = record.ContentAvailable;
        existing.ExternalReference = record.ExternalReference;
        existing.SourceVersion = record.SourceVersion;
        existing.SourceUpdatedAtUtc = record.SourceUpdatedAtUtc;
        existing.UpdatedAtUtc = now;
        counters.Updated++;
    }

    private static IQueryable<CanonicalErpProductionOrder> ApplyProductionOrderFilters(
        IQueryable<CanonicalErpProductionOrder> query,
        Guid? connectionProfileId,
        string? status,
        string? productExternalId)
    {
        if (connectionProfileId.HasValue)
        {
            query = query.Where(p => p.ConnectionProfileId == connectionProfileId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToLowerInvariant();
            query = query.Where(p => p.Status == normalized);
        }

        if (!string.IsNullOrWhiteSpace(productExternalId))
        {
            query = query.Where(p => p.ExternalProductId == productExternalId.Trim());
        }

        return query;
    }

    private static IQueryable<CanonicalErpDocument> ApplyDocumentFilters(
        IQueryable<CanonicalErpDocument> query,
        Guid? connectionProfileId,
        string? documentType,
        string? relatedEntityType,
        string? relatedExternalEntityId)
    {
        if (connectionProfileId.HasValue)
        {
            query = query.Where(d => d.ConnectionProfileId == connectionProfileId.Value);
        }

        if (!string.IsNullOrWhiteSpace(documentType))
        {
            var normalized = documentType.Trim().ToLowerInvariant();
            query = query.Where(d => d.DocumentType == normalized);
        }

        if (!string.IsNullOrWhiteSpace(relatedEntityType))
        {
            query = query.Where(d => d.RelatedEntityType == relatedEntityType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(relatedExternalEntityId))
        {
            query = query.Where(d => d.RelatedExternalEntityId == relatedExternalEntityId.Trim());
        }

        return query;
    }

    private static string NormalizeProductionOrderStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return ErpProductionOrderStatuses.Unknown;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            ErpProductionOrderStatuses.Planned
                or ErpProductionOrderStatuses.Released
                or ErpProductionOrderStatuses.InProgress
                or ErpProductionOrderStatuses.Completed
                or ErpProductionOrderStatuses.Cancelled
                or ErpProductionOrderStatuses.Closed
                or ErpProductionOrderStatuses.Unknown => normalized,
            _ => ErpProductionOrderStatuses.Unknown,
        };
    }

    private static string NormalizeDocumentType(string? documentType)
    {
        if (string.IsNullOrWhiteSpace(documentType))
        {
            return ErpDocumentTypes.Generic;
        }

        var normalized = documentType.Trim().ToLowerInvariant();
        return normalized switch
        {
            ErpDocumentTypes.SalesOrder
                or ErpDocumentTypes.PurchaseOrder
                or ErpDocumentTypes.DeliveryNote
                or ErpDocumentTypes.Invoice
                or ErpDocumentTypes.ProductionOrder
                or ErpDocumentTypes.QualityDocument
                or ErpDocumentTypes.Generic => normalized,
            _ => ErpDocumentTypes.Generic,
        };
    }

    private static CanonicalErpProductionOrderReadModel MapProductionOrder(
        CanonicalErpProductionOrder productionOrder,
        DateTimeOffset now) =>
        new(
            productionOrder.Id,
            productionOrder.ConnectionProfileId,
            productionOrder.Provider,
            productionOrder.ExternalId,
            productionOrder.ExternalCode,
            productionOrder.ProductionOrderNumber,
            productionOrder.ExternalProductId,
            productionOrder.ExternalProductCode,
            productionOrder.ProductDescription,
            productionOrder.PlannedQuantity,
            productionOrder.CompletedQuantity,
            productionOrder.ScrapQuantity,
            productionOrder.Unit,
            productionOrder.Status,
            productionOrder.PlannedStartUtc,
            productionOrder.PlannedEndUtc,
            productionOrder.ActualStartUtc,
            productionOrder.ActualEndUtc,
            productionOrder.ExternalSalesOrderId,
            productionOrder.ExternalSalesOrderCode,
            productionOrder.SourceVersion,
            productionOrder.SourceUpdatedAtUtc,
            productionOrder.ImportedAtUtc,
            productionOrder.UpdatedAtUtc,
            ErpSyncRepository.ResolveFreshness(productionOrder.SourceUpdatedAtUtc, now));

    private static CanonicalErpDocumentReadModel MapDocument(
        CanonicalErpDocument document,
        DateTimeOffset now) =>
        new(
            document.Id,
            document.ConnectionProfileId,
            document.Provider,
            document.ExternalId,
            document.ExternalCode,
            document.DocumentType,
            document.DocumentNumber,
            document.DocumentDateUtc,
            document.Status,
            document.RelatedEntityType,
            document.RelatedExternalEntityId,
            document.RelatedExternalEntityCode,
            document.FileName,
            document.MimeType,
            document.ContentAvailable,
            document.ExternalReference,
            document.SourceVersion,
            document.SourceUpdatedAtUtc,
            document.ImportedAtUtc,
            document.UpdatedAtUtc,
            ErpSyncRepository.ResolveFreshness(document.SourceUpdatedAtUtc, now));

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

public sealed class UnavailableErpProductionRepository : IErpProductionRepository
{
    public Task<ErpSyncResult> SyncProductionAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpSyncResult.Fail(ErpFailureCodes.SourceUnavailable));

    public Task<ErpSyncResult> SyncDocumentsAsync(
        ErpOperationContext context,
        Guid connectionProfileId,
        Guid? retriedFromSyncRunId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpSyncResult.Fail(ErpFailureCodes.SourceUnavailable));

    public Task<IReadOnlyList<CanonicalErpProductionOrderReadModel>> ListProductionOrdersAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? status = null,
        string? productExternalId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CanonicalErpProductionOrderReadModel>>([]);

    public Task<CanonicalErpProductionOrderReadModel?> GetProductionOrderAsync(
        ErpOperationContext context,
        Guid productionOrderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CanonicalErpProductionOrderReadModel?>(null);

    public Task<IReadOnlyList<CanonicalErpDocumentReadModel>> ListDocumentsAsync(
        ErpOperationContext context,
        Guid? connectionProfileId = null,
        string? documentType = null,
        string? relatedEntityType = null,
        string? relatedExternalEntityId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CanonicalErpDocumentReadModel>>([]);

    public Task<CanonicalErpDocumentReadModel?> GetDocumentAsync(
        ErpOperationContext context,
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CanonicalErpDocumentReadModel?>(null);
}
