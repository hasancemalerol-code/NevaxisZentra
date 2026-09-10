using CrevaFactOS.Persistence.Integration.FileImport;
using CrevaFactOS.Persistence.Integration.Mapping;
using CrevaFactOS.Persistence.Integration.Projections;
using CrevaFactOS.Persistence.Material;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Integration.Sync;

public sealed record IntegrationRecordIngestionRequest(
    Guid ConnectorId,
    Guid? MappingVersionId,
    IntegrationFileTable Table,
    string SourceType,
    string? SourceFingerprint,
    string? ImportIdempotencyKey,
    bool Mutate,
    string? CheckpointBeforeJson,
    string? CheckpointAfterJson);

public sealed record IntegrationRecordIngestionResult(
    bool Success,
    Guid? SyncRunId,
    IntegrationFileDryRunResult? DryRun,
    string? ErrorCode,
    string? SafeMessage);

public interface IIntegrationRecordIngestionOrchestrator
{
    Task<IntegrationRecordIngestionResult> ExecuteAsync(
        IntegrationOperationContext context,
        IntegrationRecordIngestionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class IntegrationRecordIngestionOrchestrator(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IIntegrationMaterialProjectionWriter materialProjectionWriter) : IIntegrationRecordIngestionOrchestrator
{
    public Task<IntegrationRecordIngestionResult> ExecuteAsync(
        IntegrationOperationContext context,
        IntegrationRecordIngestionRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ExecuteInternalAsync(context, request, ct),
            cancellationToken);

    private async Task<IntegrationRecordIngestionResult> ExecuteInternalAsync(
        IntegrationOperationContext context,
        IntegrationRecordIngestionRequest request,
        CancellationToken cancellationToken)
    {
        var connector = await dbContext.IntegrationConnectors
            .SingleOrDefaultAsync(
                c => c.TenantId == context.TenantId
                    && c.FacilityId == context.FacilityId
                    && c.Id == request.ConnectorId,
                cancellationToken);
        if (connector is null)
        {
            return new IntegrationRecordIngestionResult(
                false,
                null,
                null,
                IntegrationFailureCodes.NotFound,
                "Connector not found.");
        }

        var mappingVersionId = request.MappingVersionId ?? connector.ActiveMappingVersionId;
        if (mappingVersionId is null)
        {
            return new IntegrationRecordIngestionResult(
                false,
                null,
                null,
                IntegrationFailureCodes.ValidationFailed,
                "An active mapping version is required.");
        }

        var mapping = await dbContext.IntegrationMappingVersions
            .SingleOrDefaultAsync(
                m => m.TenantId == context.TenantId
                    && m.FacilityId == context.FacilityId
                    && m.Id == mappingVersionId,
                cancellationToken);
        if (mapping is null)
        {
            return new IntegrationRecordIngestionResult(
                false,
                null,
                null,
                IntegrationFailureCodes.NotFound,
                "Mapping version not found.");
        }

        if (mapping.Status != IntegrationMappingVersionStatuses.Active
            && mapping.Status != IntegrationMappingVersionStatuses.Validated
            && mapping.Status != IntegrationMappingVersionStatuses.Draft)
        {
            return new IntegrationRecordIngestionResult(
                false,
                null,
                null,
                IntegrationFailureCodes.ValidationFailed,
                "Mapping version is not eligible for ingestion.");
        }

        var definition = IntegrationMappingDefinition.TryParse(mapping.MappingJson);
        if (definition is null)
        {
            return new IntegrationRecordIngestionResult(
                false,
                null,
                null,
                IntegrationFailureCodes.ValidationFailed,
                "Mapping JSON is invalid.");
        }

        if (request.Mutate && !string.IsNullOrWhiteSpace(request.ImportIdempotencyKey))
        {
            var existingRun = await dbContext.IntegrationSyncRuns.AsNoTracking()
                .SingleOrDefaultAsync(
                    r => r.ConnectorId == request.ConnectorId
                        && r.ImportIdempotencyKey == request.ImportIdempotencyKey,
                    cancellationToken);
            if (existingRun is not null)
            {
                return new IntegrationRecordIngestionResult(
                    false,
                    existingRun.Id,
                    null,
                    "integration.import-idempotency-conflict",
                    "An import with the same idempotency key already exists.");
            }
        }

        var existingIdentities = await dbContext.IntegrationSourceIdentities.AsNoTracking()
            .Where(i => i.TenantId == context.TenantId && i.ConnectorId == request.ConnectorId)
            .ToDictionaryAsync(i => i.SourceRecordId, cancellationToken);

        var dryRun = IntegrationFileDryRunEngine.Execute(
            request.Table,
            definition,
            mapping.SourceSchemaFingerprint,
            existingIdentities,
            string.Equals(request.SourceType, IntegrationConnectorKinds.ReadOnlyDatabase, StringComparison.Ordinal));

        if (!dryRun.Success)
        {
            return new IntegrationRecordIngestionResult(false, null, dryRun, dryRun.ErrorCode, dryRun.SafeMessage);
        }

        if (!request.Mutate)
        {
            return new IntegrationRecordIngestionResult(true, null, dryRun, null, null);
        }

        var syncRun = new IntegrationSyncRun
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ConnectorId = request.ConnectorId,
            MappingVersionId = mapping.Id,
            SourceType = request.SourceType,
            SourceFingerprint = request.SourceFingerprint,
            ImportIdempotencyKey = request.ImportIdempotencyKey,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = IntegrationSyncRunStatuses.Running,
            CheckpointBeforeJson = request.CheckpointBeforeJson,
            CheckpointAfterJson = request.CheckpointAfterJson,
            TriggeredBySubjectId = context.SubjectId,
            CorrelationId = context.CorrelationId,
        };
        dbContext.IntegrationSyncRuns.Add(syncRun);

        var materialContext = new MaterialOperationContext(
            context.TenantId,
            context.FacilityId,
            context.MembershipId,
            context.IdentitySubjectId,
            context.PermissionKey,
            context.CorrelationId);

        var seenSourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (rowIndex, row) in request.Table.Rows.Select((row, index) => (index + 1, row)))
        {
            syncRun.RecordsRead++;
            var mapped = IntegrationMappingValidator.MapRow(rowIndex, row, definition);
            if (!mapped.IsValid || !seenSourceIds.Add(mapped.SourceRecordId))
            {
                syncRun.RecordsRejected++;
                syncRun.RecordsQuarantined++;
                AddQuarantine(context, syncRun, connector, definition, mapped);
                continue;
            }

            if (existingIdentities.TryGetValue(mapped.SourceRecordId, out var existing)
                && string.Equals(existing.ContentFingerprint, mapped.ContentFingerprint, StringComparison.Ordinal))
            {
                syncRun.RecordsDuplicate++;
                continue;
            }

            if (existingIdentities.TryGetValue(mapped.SourceRecordId, out var conflict)
                && !string.Equals(conflict.ContentFingerprint, mapped.ContentFingerprint, StringComparison.Ordinal))
            {
                syncRun.RecordsRejected++;
                syncRun.RecordsQuarantined++;
                AddQuarantine(
                    context,
                    syncRun,
                    connector,
                    definition,
                    mapped with
                    {
                        Issues =
                        [
                            new IntegrationMappingValidationIssue(
                                IntegrationQuarantineReasonCodes.IdempotencyConflict,
                                "Source record identity conflicts with a different payload.",
                                definition.SourceKeyField),
                        ],
                    });
                continue;
            }

            var idempotencyKey = BuildRowIdempotencyKey(request.ConnectorId, definition.SourceEntityType, mapped.SourceRecordId);
            var writeResult = await materialProjectionWriter.UpsertAsync(
                materialContext,
                mapped.MappedValues,
                connector.SourceSystemName,
                mapped.SourceRecordId,
                idempotencyKey,
                cancellationToken);

            if (!writeResult.Success)
            {
                syncRun.RecordsRejected++;
                syncRun.RecordsQuarantined++;
                AddQuarantine(
                    context,
                    syncRun,
                    connector,
                    definition,
                    mapped with
                    {
                        Issues =
                        [
                            new IntegrationMappingValidationIssue(
                                writeResult.ErrorCode ?? IntegrationQuarantineReasonCodes.UnsupportedData,
                                writeResult.SafeMessage ?? "Projection write failed.",
                                definition.SourceKeyField),
                        ],
                    });
                continue;
            }

            syncRun.RecordsAccepted++;
            if (writeResult.Created)
            {
                syncRun.RecordsUpdated++;
            }
            else if (writeResult.Duplicate || writeResult.Unchanged)
            {
                syncRun.RecordsDuplicate++;
            }
            else if (writeResult.Updated)
            {
                syncRun.RecordsUpdated++;
            }

            UpsertSourceIdentity(context, request.ConnectorId, definition, mapped, writeResult.ProjectionId);
        }

        syncRun.CompletedAtUtc = DateTimeOffset.UtcNow;
        syncRun.Status = syncRun.RecordsQuarantined > 0
            ? IntegrationSyncRunStatuses.CompletedWithErrors
            : IntegrationSyncRunStatuses.Completed;
        connector.LastSuccessfulSyncAtUtc = syncRun.CompletedAtUtc;
        connector.LastAttemptedSyncAtUtc = syncRun.CompletedAtUtc;
        connector.CheckpointJson = request.CheckpointAfterJson ?? connector.CheckpointJson;
        connector.FreshnessState = IntegrationFreshnessStates.Fresh;
        connector.QualityState = syncRun.RecordsQuarantined > 0
            ? IntegrationQualityStates.Partial
            : IntegrationQualityStates.Good;
        connector.HealthStatus = IntegrationHealthStatuses.Healthy;

        IntegrationRepositorySupport.AddAudit(
            dbContext,
            context,
            "integration.sync",
            "integration.sync.completed",
            "integration_sync_run",
            syncRun.Id,
            syncRun.CompletedAtUtc.Value,
            new
            {
                request.SourceType,
                syncRun.RecordsRead,
                syncRun.RecordsAccepted,
                syncRun.RecordsUpdated,
                syncRun.RecordsDuplicate,
                syncRun.RecordsRejected,
                syncRun.RecordsQuarantined,
                syncRun.Status,
            });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new IntegrationRecordIngestionResult(true, syncRun.Id, dryRun, null, null);
    }

    private void UpsertSourceIdentity(
        IntegrationOperationContext context,
        Guid connectorId,
        IntegrationMappingDefinition definition,
        IntegrationMappedRowResult mapped,
        Guid? projectionId)
    {
        var existing = dbContext.IntegrationSourceIdentities
            .SingleOrDefault(i => i.TenantId == context.TenantId
                && i.ConnectorId == connectorId
                && i.SourceEntityType == definition.SourceEntityType
                && i.SourceRecordId == mapped.SourceRecordId);

        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            dbContext.IntegrationSourceIdentities.Add(new IntegrationSourceIdentity
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                ConnectorId = connectorId,
                SourceSystem = definition.SourceEntityType,
                SourceEntityType = definition.SourceEntityType,
                SourceRecordId = mapped.SourceRecordId,
                ContentFingerprint = mapped.ContentFingerprint,
                InternalProjectionType = definition.TargetCanonicalEntityType,
                InternalProjectionId = projectionId,
                EvidenceClass = IntegrationEvidenceClasses.Imported,
                FirstSeenAtUtc = now,
                LastSeenAtUtc = now,
            });
            return;
        }

        existing.ContentFingerprint = mapped.ContentFingerprint;
        existing.InternalProjectionId = projectionId ?? existing.InternalProjectionId;
        existing.LastSeenAtUtc = now;
    }

    private void AddQuarantine(
        IntegrationOperationContext context,
        IntegrationSyncRun syncRun,
        IntegrationConnector connector,
        IntegrationMappingDefinition definition,
        IntegrationMappedRowResult mapped)
    {
        var issue = mapped.Issues.Count > 0 ? mapped.Issues[0] : null;
        dbContext.IntegrationIngestionQuarantine.Add(new IntegrationIngestionQuarantine
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ConnectorId = connector.Id,
            SyncRunId = syncRun.Id,
            SourceSystem = connector.SourceSystemName,
            SourceEntityType = definition.SourceEntityType,
            SourceRecordId = mapped.SourceRecordId,
            ReasonCode = issue?.Code ?? IntegrationQuarantineReasonCodes.UnsupportedData,
            SafeReason = issue?.Message ?? "Row rejected during ingestion.",
            ValidationIssue = issue?.Message,
            Status = IntegrationQuarantineStatuses.Open,
            EvidenceClass = IntegrationEvidenceClasses.Imported,
            OccurredAtUtc = DateTimeOffset.UtcNow,
        });
    }

    private static string BuildRowIdempotencyKey(Guid connectorId, string sourceEntityType, string sourceRecordId) =>
        $"integration:{connectorId:N}:{sourceEntityType}:{sourceRecordId}";
}
