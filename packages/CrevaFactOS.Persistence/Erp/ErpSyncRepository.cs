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
            return;
        }

        Guid? canonicalId = null;
        if (entityType == ErpSyncEntityTypes.Customers)
        {
            canonicalId = dbContext.CanonicalErpCustomers.Local
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
        else
        {
            canonicalId = dbContext.CanonicalErpProducts.Local
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

        dbContext.ErpEntityMappings.Add(new ErpEntityMapping
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ConnectionProfileId = connectionProfileId,
            EntityType = entityType,
            ExternalId = externalId,
            CanonicalEntityId = canonicalId,
            Status = canonicalId.HasValue ? ErpMappingStatuses.Mapped : ErpMappingStatuses.Unmapped,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
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
}
