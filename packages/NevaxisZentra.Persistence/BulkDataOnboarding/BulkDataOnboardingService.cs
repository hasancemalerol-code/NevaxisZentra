using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.MixedProduction;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.BulkDataOnboarding;

public interface IBulkDataOnboardingService
{
    Task<BulkDataImportJob> CreateJobAsync(
        BulkDataOperationContext context,
        CreateBulkImportJobRequest request,
        CancellationToken cancellationToken = default);

    Task<BulkImportJobWorkspace> GetWorkspaceAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<BulkDataImportJob> DiscoverAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<BulkDataImportJob> StageAndValidateAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<BulkDryRunResult> DryRunAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<BulkDataImportJob> CommitAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<BulkDataImportJob> RollbackAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<BulkSyntheticImportPerformance> RunSyntheticFoundationBenchmarkAsync(
        BulkDataOperationContext context,
        CancellationToken cancellationToken = default);

    Task<UploadBulkImportFilesResult> UploadFilesAsync(
        BulkDataOperationContext context,
        Guid jobId,
        IReadOnlyList<BulkImportUploadFile> files,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BulkImportJobSummary>> ListJobsAsync(
        BulkDataOperationContext context,
        CancellationToken cancellationToken = default);

    Task<FactoryDataMappingProfile> UpsertMappingProfileAsync(
        BulkDataOperationContext context,
        Guid? profileId,
        UpsertBulkMappingProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BulkMappingProfileSummary>> ListMappingProfilesAsync(
        BulkDataOperationContext context,
        CancellationToken cancellationToken = default);

    Task<BulkDataImportJob> ApplyAutoMappingAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken = default);
}

public sealed class BulkDataOnboardingService(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IBulkDataSourceProviderResolver sourceProviderResolver,
    IBulkDataCanonicalizationService canonicalizationService,
    IBulkImportFileStore fileStore,
    IBulkImportSourceContextAccessor sourceContextAccessor) : IBulkDataOnboardingService
{
    private static readonly Dictionary<string, string> RequiredFields = new(StringComparer.OrdinalIgnoreCase)
    {
        [BulkCanonicalEntityTypes.ManufacturingArea] = "code,name",
        [BulkCanonicalEntityTypes.ProductionLine] = "code,name,area_code",
        [BulkCanonicalEntityTypes.ProductionStation] = "code,name,line_code",
        [BulkCanonicalEntityTypes.ManufacturingResource] = "code,name,station_code,resource_type",
        [BulkCanonicalEntityTypes.Material] = "code,name",
        [BulkCanonicalEntityTypes.ProductionEvent] = "resource_code,event_type,occurred_at",
    };

    private static readonly Dictionary<string, string> TableToEntity = new(StringComparer.OrdinalIgnoreCase)
    {
        ["areas"] = BulkCanonicalEntityTypes.ManufacturingArea,
        ["lines"] = BulkCanonicalEntityTypes.ProductionLine,
        ["stations"] = BulkCanonicalEntityTypes.ProductionStation,
        ["resources"] = BulkCanonicalEntityTypes.ManufacturingResource,
        ["materials"] = BulkCanonicalEntityTypes.Material,
        ["orders"] = BulkCanonicalEntityTypes.ProductionOrder,
        ["events"] = BulkCanonicalEntityTypes.ProductionEvent,
        ["quality"] = BulkCanonicalEntityTypes.QualityRecord,
        ["maintenance"] = BulkCanonicalEntityTypes.MaintenanceRecord,
        ["molds"] = BulkCanonicalEntityTypes.MoldTool,
    };

    public Task<BulkDataImportJob> CreateJobAsync(
        BulkDataOperationContext context,
        CreateBulkImportJobRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => CreateJobCoreAsync(context, request, ct),
            cancellationToken);

    private async Task<BulkDataImportJob> CreateJobCoreAsync(
        BulkDataOperationContext context,
        CreateBulkImportJobRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await dbContext.BulkDataImportJobs
                .FirstOrDefaultAsync(
                    j => j.TenantId == context.TenantId
                        && j.FacilityId == context.FacilityId
                        && j.IdempotencyKey == request.IdempotencyKey,
                    cancellationToken);
            if (existing is not null)
            {
                return existing;
            }
        }

        var job = new BulkDataImportJob
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            Name = request.Name,
            Status = BulkDataImportJobStatuses.Created,
            SourceProviderKind = request.SourceProviderKind,
            SourceSystem = request.SourceSystem,
            MappingProfileId = request.MappingProfileId,
            IdempotencyKey = request.IdempotencyKey,
            SafetyMode = BulkOnboardingSafetyModes.Observe,
            CreatedBySubjectId = context.SubjectId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.BulkDataImportJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return job;
    }

    public Task<BulkImportJobWorkspace> GetWorkspaceAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetWorkspaceCoreAsync(context, jobId, ct),
            cancellationToken);

    private async Task<BulkImportJobWorkspace> GetWorkspaceCoreAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await RequireJobAsync(context, jobId, cancellationToken);
        var catalog = await dbContext.BulkImportSourceCatalogEntries
            .Where(c => c.ImportJobId == jobId)
            .ToListAsync(cancellationToken);

        BulkImportCheckpoint? checkpoint = null;
        if (!string.IsNullOrWhiteSpace(job.CheckpointJson))
        {
            checkpoint = JsonSerializer.Deserialize<BulkImportCheckpoint>(job.CheckpointJson);
        }

        BulkImpactPreview? impact = null;
        if (!string.IsNullOrWhiteSpace(job.ImpactPreviewJson))
        {
            impact = JsonSerializer.Deserialize<BulkImpactPreview>(job.ImpactPreviewJson);
        }

        BulkReconciliationSummary? reconciliation = null;
        if (!string.IsNullOrWhiteSpace(job.ReconciliationJson))
        {
            reconciliation = JsonSerializer.Deserialize<BulkReconciliationSummary>(job.ReconciliationJson);
        }

        var quarantine = await dbContext.BulkImportQuarantineRecords
            .Where(q => q.ImportJobId == jobId)
            .OrderByDescending(q => q.OccurredAtUtc)
            .Take(200)
            .Select(q => new BulkQuarantineSummary(
                q.Id,
                q.SourceTable,
                q.SourceKey,
                q.ReasonCode,
                q.SafeReason,
                q.Severity,
                q.Status,
                q.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        var mappingRules = await ResolveMappingRulesAsync(context, job, catalog, cancellationToken);
        var uploadedFiles = fileStore.ReadManifest(job.ObservabilityJson);

        return new BulkImportJobWorkspace(
            ToSummary(job),
            catalog.Select(c => new BulkSourceTableDescriptor(
                c.SourceDatabase,
                c.SourceSchema,
                c.SourceTable,
                JsonSerializer.Deserialize<List<BulkColumnDescriptor>>(c.ColumnsJson) ?? [],
                c.EstimatedRowCount,
                (c.PrimaryKeyColumns ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                (c.TimestampColumns ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList())).ToList(),
            impact,
            reconciliation,
            checkpoint,
            quarantine,
            mappingRules,
            uploadedFiles);
    }

    public Task<BulkDataImportJob> DiscoverAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => DiscoverCoreAsync(context, jobId, ct),
            cancellationToken);

    private async Task<BulkDataImportJob> DiscoverCoreAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await RequireJobAsync(context, jobId, cancellationToken);
        job.Status = BulkDataImportJobStatuses.Discovering;
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;

        using var sourceScope = SetSourceContext(job);
        var provider = sourceProviderResolver.Resolve(job.SourceProviderKind);
        var discovery = await provider.DiscoverAsync(cancellationToken);
        job.SourceSystem = discovery.SourceSystem;
        job.SourceVersion = discovery.SourceVersion;
        job.TotalSourceRows = discovery.Tables.Sum(t => (int)Math.Min(t.EstimatedRowCount, int.MaxValue));

        var existingCatalog = await dbContext.BulkImportSourceCatalogEntries
            .Where(c => c.ImportJobId == jobId)
            .ToListAsync(cancellationToken);
        dbContext.BulkImportSourceCatalogEntries.RemoveRange(existingCatalog);

        foreach (var table in discovery.Tables)
        {
            dbContext.BulkImportSourceCatalogEntries.Add(new BulkImportSourceCatalogEntry
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                ImportJobId = job.Id,
                SourceDatabase = table.SourceDatabase,
                SourceSchema = table.SourceSchema,
                SourceTable = table.SourceTable,
                RoleHint = BulkSchemaProfilerEngine.InferRoleHint(table.SourceTable, table.Columns),
                ColumnsJson = JsonSerializer.Serialize(table.Columns),
                EstimatedRowCount = table.EstimatedRowCount,
                PrimaryKeyColumns = string.Join(',', table.PrimaryKeyColumns),
                TimestampColumns = string.Join(',', table.TimestampColumns),
                DiscoveredAtUtc = DateTimeOffset.UtcNow,
            });
        }

        job.Status = BulkDataImportJobStatuses.Profiling;
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await EnsureMappingProfileAsync(context, job, cancellationToken);
        return job;
    }

    public Task<BulkDataImportJob> StageAndValidateAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => StageAndValidateCoreAsync(context, jobId, ct),
            cancellationToken);

    private async Task<BulkDataImportJob> StageAndValidateCoreAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await RequireJobAsync(context, jobId, cancellationToken);
        job.Status = BulkDataImportJobStatuses.Validating;
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;

        using var sourceScope = SetSourceContext(job);
        var provider = sourceProviderResolver.Resolve(job.SourceProviderKind);
        BulkImportCheckpoint? checkpoint = string.IsNullOrWhiteSpace(job.CheckpointJson)
            ? null
            : JsonSerializer.Deserialize<BulkImportCheckpoint>(job.CheckpointJson);

        var mapping = await ResolveMappingProfileDefinitionAsync(context, job, cancellationToken);
        var sourceDatabase = IsFileProvider(job.SourceProviderKind)
            ? "customer_file_import"
            : BulkDataOnboardingSyntheticFixture.SourceDatabase;

        var existingStaging = await dbContext.BulkImportStagingRecords
            .Where(s => s.ImportJobId == jobId)
            .ToListAsync(cancellationToken);
        if (existingStaging.Count == 0)
        {
            var checksums = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var batch = 0;
            var processed = 0;
            await foreach (var record in provider.ReadRecordsAsync(
                mapping,
                checkpoint,
                cancellationToken))
            {
                var entityType = TableToEntity.GetValueOrDefault(record.SourceTable, BulkCanonicalEntityTypes.ManufacturingArea);
                var checksum = BulkIdempotencyEngine.BuildRecordChecksum(record.Fields);
                var duplicate = !checksums.Add($"{record.SourceTable}:{record.SourceKey}:{checksum}");

                var quality = BulkDataQualityGateEngine.Evaluate(
                    record with { CanonicalEntityType = entityType },
                    context.TenantId,
                    context.FacilityId,
                    RequiredFields,
                    DateTimeOffset.UtcNow);

                var validationState = duplicate
                    ? BulkStagingValidationStates.Duplicate
                    : quality.Accepted
                        ? BulkStagingValidationStates.Valid
                        : BulkStagingValidationStates.Quarantined;
                var firstIssue = quality.Issues.Count > 0 ? quality.Issues[0] : null;

                var staging = new BulkImportStagingRecord
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    ImportJobId = job.Id,
                    SourceSystem = job.SourceSystem,
                    SourceDatabase = sourceDatabase,
                    SourceTable = record.SourceTable,
                    SourceKey = record.SourceKey,
                    RecordChecksum = checksum,
                    CanonicalEntityType = entityType,
                    ValidationState = validationState,
                    ValidationReasonCode = firstIssue?.ReasonCode,
                    PayloadJson = JsonSerializer.Serialize(record.Fields),
                    SourceTimestampRaw = record.SourceTimestampRaw,
                    SourceTimestampUtc = record.SourceTimestampUtc,
                    IngestedAtUtc = DateTimeOffset.UtcNow,
                    HistoricalTier = BulkHistoricalTierEngine.Classify(record.SourceTimestampUtc, DateTimeOffset.UtcNow),
                    BatchNumber = batch,
                };
                dbContext.BulkImportStagingRecords.Add(staging);

                if (!quality.Accepted && firstIssue is not null)
                {
                    dbContext.BulkImportQuarantineRecords.Add(new BulkImportQuarantineRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ImportJobId = job.Id,
                        StagingRecordId = staging.Id,
                        SourceTable = record.SourceTable,
                        SourceKey = record.SourceKey,
                        ReasonCode = firstIssue.ReasonCode,
                        SafeReason = firstIssue.SafeMessage,
                        Severity = firstIssue.Severity,
                        Status = BulkQuarantineStatuses.Open,
                        OccurredAtUtc = DateTimeOffset.UtcNow,
                    });
                }

                processed++;
                if (processed % 250 == 0)
                {
                    batch++;
                    job.CheckpointJson = JsonSerializer.Serialize(new BulkImportCheckpoint(
                        record.SourceTable,
                        batch,
                        processed,
                        DateTimeOffset.UtcNow));
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            job.StagedRows = processed;
        }

        var stagingRecords = await dbContext.BulkImportStagingRecords
            .Where(s => s.ImportJobId == jobId)
            .ToListAsync(cancellationToken);

        job.StagedRows = stagingRecords.Count;
        job.ValidRows = stagingRecords.Count(s => s.ValidationState == BulkStagingValidationStates.Valid);
        job.InvalidRows = stagingRecords.Count(s => s.ValidationState == BulkStagingValidationStates.Invalid);
        job.DuplicateRows = stagingRecords.Count(s => s.ValidationState == BulkStagingValidationStates.Duplicate);
        job.QuarantinedRows = stagingRecords.Count(s => s.ValidationState == BulkStagingValidationStates.Quarantined);
        job.Status = BulkDataImportJobStatuses.ReadyForDryRun;
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return job;
    }

    public Task<BulkDryRunResult> DryRunAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => DryRunCoreAsync(context, jobId, ct),
            cancellationToken);

    private async Task<BulkDryRunResult> DryRunCoreAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await RequireJobAsync(context, jobId, cancellationToken);
        var staging = await dbContext.BulkImportStagingRecords
            .Where(s => s.ImportJobId == jobId)
            .ToListAsync(cancellationToken);

        var existingKeys = await dbContext.BulkImportLineageRecords
            .Where(l => l.FacilityId == context.FacilityId)
            .Select(l => l.CanonicalEntityType + ":" + l.SourceKey)
            .ToHashSetAsync(cancellationToken);

        var impact = BulkImpactPreviewEngine.Build(staging, existingKeys);
        job.ImpactPreviewJson = JsonSerializer.Serialize(impact);
        job.Status = BulkDataImportJobStatuses.ReadyToCommit;
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new BulkDryRunResult(
            true,
            job.TotalSourceRows,
            job.ValidRows,
            job.InvalidRows,
            job.DuplicateRows,
            job.QuarantinedRows,
            impact,
            null,
            null);
    }

    public Task<BulkDataImportJob> CommitAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => CommitCoreAsync(context, jobId, ct),
            cancellationToken);

    private async Task<BulkDataImportJob> CommitCoreAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await RequireJobAsync(context, jobId, cancellationToken);
        if (job.Status != BulkDataImportJobStatuses.ReadyToCommit
            && job.Status != BulkDataImportJobStatuses.ReadyForDryRun)
        {
            throw new InvalidOperationException(BulkFailureCodes.InvalidState);
        }

        job.Status = BulkDataImportJobStatuses.Importing;
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var commitOrder = new[]
        {
            BulkCanonicalEntityTypes.ManufacturingArea,
            BulkCanonicalEntityTypes.ProductionLine,
            BulkCanonicalEntityTypes.ProductionStation,
            BulkCanonicalEntityTypes.ManufacturingResource,
            BulkCanonicalEntityTypes.Material,
            BulkCanonicalEntityTypes.ProductionEvent,
        };

        var staging = await dbContext.BulkImportStagingRecords
            .Where(s => s.ImportJobId == jobId && s.ValidationState == BulkStagingValidationStates.Valid)
            .ToListAsync(cancellationToken);

        var imported = 0;
        var updated = 0;
        var failed = 0;

        foreach (var entityType in commitOrder)
        {
            foreach (var record in staging.Where(s => s.CanonicalEntityType == entityType))
            {
                var result = await canonicalizationService.CommitRecordAsync(context, record, cancellationToken);
                if (!result.Success)
                {
                    failed++;
                    continue;
                }

                if (result.Created)
                {
                    imported++;
                    dbContext.BulkImportEntityMutations.Add(new BulkImportEntityMutation
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ImportJobId = job.Id,
                        CanonicalEntityType = record.CanonicalEntityType,
                        CanonicalEntityId = result.CanonicalEntityId ?? Guid.Empty,
                        MutationKind = "created",
                        RecordedAtUtc = DateTimeOffset.UtcNow,
                        RollbackEligible = true,
                    });
                }
                else if (result.Updated)
                {
                    updated++;
                }

                if (result.CanonicalEntityId is Guid entityId)
                {
                    dbContext.BulkImportLineageRecords.Add(new BulkImportLineageRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ImportJobId = job.Id,
                        CanonicalEntityType = record.CanonicalEntityType,
                        CanonicalEntityId = entityId,
                        SourceSystem = record.SourceSystem,
                        SourceDatabase = record.SourceDatabase,
                        SourceTable = record.SourceTable,
                        SourceKey = record.SourceKey,
                        RecordedAtUtc = DateTimeOffset.UtcNow,
                    });
                }

                if ((imported + updated + failed) % 100 == 0)
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
        }

        job.ImportedRows = imported;
        job.UpdatedRows = updated;
        job.FailedRows = failed;
        job.Status = BulkDataImportJobStatuses.Reconciling;
        job.ReconciliationJson = JsonSerializer.Serialize(BulkReconciliationEngine.Build(job));
        job.Status = BulkDataImportJobStatuses.Completed;
        job.CompletedAtUtc = DateTimeOffset.UtcNow;
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return job;
    }

    public Task<BulkDataImportJob> RollbackAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RollbackCoreAsync(context, jobId, ct),
            cancellationToken);

    private async Task<BulkDataImportJob> RollbackCoreAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await RequireJobAsync(context, jobId, cancellationToken);
        var mutations = await dbContext.BulkImportEntityMutations
            .Where(m => m.ImportJobId == jobId && m.RollbackEligible && !m.RolledBack)
            .ToListAsync(cancellationToken);

        foreach (var mutation in mutations.OrderByDescending(m => m.RecordedAtUtc))
        {
            var removed = await RemoveMutationEntityAsync(dbContext, mutation, cancellationToken);
            if (!removed)
            {
                throw new InvalidOperationException(BulkFailureCodes.RollbackBlocked);
            }

            mutation.RolledBack = true;
        }

        job.Status = BulkDataImportJobStatuses.Cancelled;
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return job;
    }

    public Task<BulkSyntheticImportPerformance> RunSyntheticFoundationBenchmarkAsync(
        BulkDataOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RunSyntheticFoundationBenchmarkCoreAsync(context, ct),
            cancellationToken);

    private async Task<BulkSyntheticImportPerformance> RunSyntheticFoundationBenchmarkCoreAsync(
        BulkDataOperationContext context,
        CancellationToken cancellationToken)
    {
        var job = await CreateJobCoreAsync(
            context,
            new CreateBulkImportJobRequest(
                "Synthetic Foundation Benchmark",
                BulkDataSourceProviderKinds.SyntheticStructured,
                BulkDataOnboardingSyntheticFixture.SourceSystem,
                $"benchmark-{Guid.NewGuid():N}",
                null),
            cancellationToken);

        await DiscoverCoreAsync(context, job.Id, cancellationToken);

        var dryRunStopwatch = Stopwatch.StartNew();
        await StageAndValidateCoreAsync(context, job.Id, cancellationToken);
        await DryRunCoreAsync(context, job.Id, cancellationToken);
        dryRunStopwatch.Stop();

        var importStopwatch = Stopwatch.StartNew();
        await CommitCoreAsync(context, job.Id, cancellationToken);
        importStopwatch.Stop();

        job = await RequireJobAsync(context, job.Id, cancellationToken);
        var reconciliation = BulkReconciliationEngine.Build(job);
        var durationSeconds = Math.Max(importStopwatch.Elapsed.TotalSeconds, 0.001);
        var catalogCount = await dbContext.BulkImportSourceCatalogEntries.CountAsync(c => c.ImportJobId == job.Id, cancellationToken);

        return new BulkSyntheticImportPerformance(
            catalogCount,
            job.StagedRows,
            dryRunStopwatch.ElapsedMilliseconds,
            importStopwatch.ElapsedMilliseconds,
            job.ImportedRows / durationSeconds,
            job.QuarantinedRows,
            reconciliation.Balanced);
    }

    public Task<UploadBulkImportFilesResult> UploadFilesAsync(
        BulkDataOperationContext context,
        Guid jobId,
        IReadOnlyList<BulkImportUploadFile> files,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => UploadFilesCoreAsync(context, jobId, files, ct),
            cancellationToken);

    private async Task<UploadBulkImportFilesResult> UploadFilesCoreAsync(
        BulkDataOperationContext context,
        Guid jobId,
        IReadOnlyList<BulkImportUploadFile> files,
        CancellationToken cancellationToken)
    {
        var job = await RequireJobAsync(context, jobId, cancellationToken);
        if (!IsFileProvider(job.SourceProviderKind))
        {
            throw new InvalidOperationException(BulkFailureCodes.InvalidState);
        }

        var manifest = await fileStore.SaveFilesAsync(context.TenantId, context.FacilityId, jobId, files, cancellationToken);
        job.ObservabilityJson = fileStore.WriteManifest(manifest);
        job.Status = BulkDataImportJobStatuses.Created;
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new UploadBulkImportFilesResult(jobId, manifest.Files.Count, manifest.Files);
    }

    public Task<IReadOnlyList<BulkImportJobSummary>> ListJobsAsync(
        BulkDataOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var jobs = await dbContext.BulkDataImportJobs
                    .Where(j => j.TenantId == context.TenantId && j.FacilityId == context.FacilityId)
                    .OrderByDescending(j => j.CreatedAtUtc)
                    .Take(50)
                    .ToListAsync(ct);
                return (IReadOnlyList<BulkImportJobSummary>)jobs.Select(ToSummary).ToList();
            },
            cancellationToken);

    public Task<FactoryDataMappingProfile> UpsertMappingProfileAsync(
        BulkDataOperationContext context,
        Guid? profileId,
        UpsertBulkMappingProfileRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => UpsertMappingProfileCoreAsync(context, profileId, request, ct),
            cancellationToken);

    private async Task<FactoryDataMappingProfile> UpsertMappingProfileCoreAsync(
        BulkDataOperationContext context,
        Guid? profileId,
        UpsertBulkMappingProfileRequest request,
        CancellationToken cancellationToken)
    {
        FactoryDataMappingProfile profile;
        if (profileId is Guid existingId)
        {
            profile = await dbContext.FactoryDataMappingProfiles
                .SingleOrDefaultAsync(
                    p => p.Id == existingId
                        && p.TenantId == context.TenantId
                        && p.FacilityId == context.FacilityId,
                    cancellationToken)
                ?? throw new InvalidOperationException(BulkFailureCodes.NotFound);
        }
        else
        {
            profile = new FactoryDataMappingProfile
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                CreatedBySubjectId = context.SubjectId,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };
            dbContext.FactoryDataMappingProfiles.Add(profile);
        }

        profile.Name = request.Name;
        profile.SourceSystem = request.SourceSystem;
        profile.Status = "active";
        profile.MappingJson = BulkMappingProfileEngine.SerializeRules(request.Rules);
        profile.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public Task<IReadOnlyList<BulkMappingProfileSummary>> ListMappingProfilesAsync(
        BulkDataOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var profiles = await dbContext.FactoryDataMappingProfiles
                    .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
                    .OrderByDescending(p => p.UpdatedAtUtc)
                    .Take(50)
                    .ToListAsync(ct);
                return (IReadOnlyList<BulkMappingProfileSummary>)profiles
                    .Select(p => new BulkMappingProfileSummary(
                        p.Id,
                        p.Name,
                        p.SourceSystem,
                        p.Status,
                        JsonSerializer.Deserialize<List<BulkMappingRule>>(p.MappingJson)?.Count ?? 0,
                        p.UpdatedAtUtc))
                    .ToList();
            },
            cancellationToken);

    public Task<BulkDataImportJob> ApplyAutoMappingAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var job = await RequireJobAsync(context, jobId, ct);
                await EnsureMappingProfileAsync(context, job, ct);
                job.Status = BulkDataImportJobStatuses.MappingRequired;
                job.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync(ct);
                return job;
            },
            cancellationToken);

    private async Task EnsureMappingProfileAsync(
        BulkDataOperationContext context,
        BulkDataImportJob job,
        CancellationToken cancellationToken)
    {
        if (job.MappingProfileId is not null)
        {
            return;
        }

        var catalog = await dbContext.BulkImportSourceCatalogEntries
            .Where(c => c.ImportJobId == job.Id)
            .ToListAsync(cancellationToken);
        if (catalog.Count == 0)
        {
            return;
        }

        var rules = new List<BulkMappingRule>();
        foreach (var entry in catalog)
        {
            var columns = JsonSerializer.Deserialize<List<BulkColumnDescriptor>>(entry.ColumnsJson) ?? [];
            rules.AddRange(BulkMappingProfileEngine.BuildAutoMappingRules(
                entry.SourceTable,
                columns.Select(c => c.Name).ToList()));
        }

        var profile = new FactoryDataMappingProfile
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            Name = $"{job.Name} Mapping",
            SourceSystem = job.SourceSystem,
            SourceVersion = job.SourceVersion,
            Status = "active",
            MappingJson = BulkMappingProfileEngine.SerializeRules(rules),
            CreatedBySubjectId = context.SubjectId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.FactoryDataMappingProfiles.Add(profile);
        job.MappingProfileId = profile.Id;
        job.Status = BulkDataImportJobStatuses.MappingRequired;
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<BulkMappingProfileDefinition> ResolveMappingProfileDefinitionAsync(
        BulkDataOperationContext context,
        BulkDataImportJob job,
        CancellationToken cancellationToken)
    {
        if (job.MappingProfileId is Guid profileId)
        {
            var profile = await dbContext.FactoryDataMappingProfiles
                .SingleOrDefaultAsync(
                    p => p.Id == profileId
                        && p.TenantId == context.TenantId
                        && p.FacilityId == context.FacilityId,
                    cancellationToken);
            if (profile is not null)
            {
                return BulkMappingProfileEngine.DeserializeProfile(
                    profile.MappingJson,
                    profile.SourceSystem,
                    profile.SourceVersion);
            }
        }

        if (IsFileProvider(job.SourceProviderKind))
        {
            return new BulkMappingProfileDefinition(job.SourceSystem, job.SourceVersion, []);
        }

        return BulkDataOnboardingSyntheticFixture.BuildDefaultMapping();
    }

    private async Task<IReadOnlyList<BulkMappingRule>> ResolveMappingRulesAsync(
        BulkDataOperationContext context,
        BulkDataImportJob job,
        IReadOnlyList<BulkImportSourceCatalogEntry> catalog,
        CancellationToken cancellationToken)
    {
        var definition = await ResolveMappingProfileDefinitionAsync(context, job, cancellationToken);
        if (definition.Rules.Count > 0)
        {
            return definition.Rules;
        }

        return catalog
            .SelectMany(entry =>
            {
                var columns = JsonSerializer.Deserialize<List<BulkColumnDescriptor>>(entry.ColumnsJson) ?? [];
                return BulkMappingProfileEngine.BuildAutoMappingRules(
                    entry.SourceTable,
                    columns.Select(c => c.Name).ToList());
            })
            .ToList();
    }

    private BulkImportSourceContextScope SetSourceContext(BulkDataImportJob job)
    {
        sourceContextAccessor.Current = new BulkImportSourceContext(
            job.Id,
            job.TenantId,
            job.FacilityId,
            job.SourceProviderKind,
            job.ObservabilityJson,
            job.SourceSystem);
        return new BulkImportSourceContextScope(sourceContextAccessor);
    }

    private static bool IsFileProvider(string providerKind) =>
        string.Equals(providerKind, BulkDataSourceProviderKinds.CsvFile, StringComparison.OrdinalIgnoreCase)
        || string.Equals(providerKind, BulkDataSourceProviderKinds.XlsxFile, StringComparison.OrdinalIgnoreCase);

    private sealed class BulkImportSourceContextScope(IBulkImportSourceContextAccessor accessor) : IDisposable
    {
        public void Dispose() => accessor.Current = null;
    }

    private async Task<BulkDataImportJob> RequireJobAsync(
        BulkDataOperationContext context,
        Guid jobId,
        CancellationToken cancellationToken) =>
        await dbContext.BulkDataImportJobs
            .SingleOrDefaultAsync(
                j => j.TenantId == context.TenantId
                    && j.FacilityId == context.FacilityId
                    && j.Id == jobId,
                cancellationToken)
        ?? throw new InvalidOperationException(BulkFailureCodes.NotFound);

    private static BulkImportJobSummary ToSummary(BulkDataImportJob job) =>
        new(
            job.Id,
            job.Name,
            job.Status,
            job.SourceSystem,
            job.SourceProviderKind,
            job.TotalSourceRows,
            job.ValidRows,
            job.QuarantinedRows,
            job.ImportedRows,
            job.SafetyMode,
            job.CreatedAtUtc,
            job.UpdatedAtUtc);

    private static async Task<bool> RemoveMutationEntityAsync(
        NevaxisZentraDbContext dbContext,
        BulkImportEntityMutation mutation,
        CancellationToken cancellationToken)
    {
        switch (mutation.CanonicalEntityType)
        {
            case BulkCanonicalEntityTypes.ManufacturingArea:
            {
                var entity = await dbContext.ManufacturingAreas.FirstOrDefaultAsync(e => e.Id == mutation.CanonicalEntityId, cancellationToken);
                if (entity is null) return true;
                dbContext.ManufacturingAreas.Remove(entity);
                return true;
            }
            case BulkCanonicalEntityTypes.ProductionLine:
            {
                var entity = await dbContext.ProductionLines.FirstOrDefaultAsync(e => e.Id == mutation.CanonicalEntityId, cancellationToken);
                if (entity is null) return true;
                dbContext.ProductionLines.Remove(entity);
                return true;
            }
            case BulkCanonicalEntityTypes.ProductionStation:
            {
                var entity = await dbContext.ProductionStations.FirstOrDefaultAsync(e => e.Id == mutation.CanonicalEntityId, cancellationToken);
                if (entity is null) return true;
                dbContext.ProductionStations.Remove(entity);
                return true;
            }
            case BulkCanonicalEntityTypes.ManufacturingResource:
            {
                var entity = await dbContext.ManufacturingResources.FirstOrDefaultAsync(e => e.Id == mutation.CanonicalEntityId, cancellationToken);
                if (entity is null) return true;
                dbContext.ManufacturingResources.Remove(entity);
                return true;
            }
            case BulkCanonicalEntityTypes.Material:
            {
                var entity = await dbContext.Materials.FirstOrDefaultAsync(e => e.Id == mutation.CanonicalEntityId, cancellationToken);
                if (entity is null) return true;
                dbContext.Materials.Remove(entity);
                return true;
            }
            case BulkCanonicalEntityTypes.ProductionEvent:
            {
                var entity = await dbContext.CanonicalProductionEvents.FirstOrDefaultAsync(e => e.Id == mutation.CanonicalEntityId, cancellationToken);
                if (entity is null) return true;
                dbContext.CanonicalProductionEvents.Remove(entity);
                return true;
            }
            default:
                return false;
        }
    }
}
