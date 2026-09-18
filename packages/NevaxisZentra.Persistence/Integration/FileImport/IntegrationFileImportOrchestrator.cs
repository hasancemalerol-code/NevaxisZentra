using NevaxisZentra.Persistence.Integration.Mapping;
using NevaxisZentra.Persistence.Integration.Projections;
using NevaxisZentra.Persistence.Material;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Integration.FileImport;

public sealed record IntegrationFileImportRequest(
    string FileName,
    long FileSizeBytes,
    Stream Content,
    string? Delimiter,
    bool? HasHeaderRow,
    string? EncodingName,
    string? SheetName,
    Guid ConnectorId,
    Guid? MappingVersionId,
    string Mode,
    string? ImportIdempotencyKey);

public sealed record IntegrationFileImportRunResult(
    bool Success,
    Guid? SyncRunId,
    IntegrationFileDryRunResult? DryRun,
    string? ErrorCode,
    string? SafeMessage);

public interface IIntegrationFileImportOrchestrator
{
    Task<IntegrationFileImportRunResult> ExecuteAsync(
        IntegrationOperationContext context,
        IntegrationFileImportRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class IntegrationFileImportOrchestrator(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IIntegrationMaterialProjectionWriter materialProjectionWriter) : IIntegrationFileImportOrchestrator
{
    public Task<IntegrationFileImportRunResult> ExecuteAsync(
        IntegrationOperationContext context,
        IntegrationFileImportRequest request,
        CancellationToken cancellationToken = default)
    {
        var securityValidation = ValidateSecurity(request);
        if (securityValidation is not null)
        {
            return Task.FromResult(new IntegrationFileImportRunResult(
                false,
                null,
                null,
                "integration.file.validation-failed",
                securityValidation));
        }

        return tenantDatabaseSession.ExecuteWriteAsync(
            ct => ExecuteInternalAsync(
                context,
                request,
                mutate: request.Mode == IntegrationImportModes.Import,
                ct),
            cancellationToken);
    }

    private async Task<IntegrationFileImportRunResult> ExecuteInternalAsync(
        IntegrationOperationContext context,
        IntegrationFileImportRequest request,
        bool mutate,
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
            return new IntegrationFileImportRunResult(
                false,
                null,
                null,
                IntegrationFailureCodes.NotFound,
                "Connector not found.");
        }

        var mappingVersionId = request.MappingVersionId ?? connector.ActiveMappingVersionId;
        if (mappingVersionId is null)
        {
            return new IntegrationFileImportRunResult(
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
            return new IntegrationFileImportRunResult(
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
            return new IntegrationFileImportRunResult(
                false,
                null,
                null,
                IntegrationFailureCodes.ValidationFailed,
                "Mapping version is not eligible for import.");
        }

        var definition = IntegrationMappingDefinition.TryParse(mapping.MappingJson);
        if (definition is null)
        {
            return new IntegrationFileImportRunResult(
                false,
                null,
                null,
                IntegrationFailureCodes.ValidationFailed,
                "Mapping JSON is invalid.");
        }

        request.Content.Position = 0;
        var fileFingerprint = IntegrationContentFingerprint.ComputeFileFingerprint(request.Content);
        request.Content.Position = 0;

        if (mutate && !string.IsNullOrWhiteSpace(request.ImportIdempotencyKey))
        {
            var existingRun = await dbContext.IntegrationSyncRuns.AsNoTracking()
                .SingleOrDefaultAsync(
                    r => r.ConnectorId == request.ConnectorId
                        && r.ImportIdempotencyKey == request.ImportIdempotencyKey,
                    cancellationToken);
            if (existingRun is not null)
            {
                return new IntegrationFileImportRunResult(
                    false,
                    existingRun.Id,
                    null,
                    "integration.import-idempotency-conflict",
                    "An import with the same idempotency key already exists.");
            }
        }

        var table = IntegrationFileTableReader.Read(
            request.FileName,
            request.Content,
            request.Delimiter,
            request.HasHeaderRow,
            request.EncodingName,
            request.SheetName);
        if (table is null)
        {
            return new IntegrationFileImportRunResult(
                false,
                null,
                null,
                "integration.file.parse-failed",
                "File could not be parsed.");
        }

        var existingIdentities = await dbContext.IntegrationSourceIdentities.AsNoTracking()
            .Where(i => i.TenantId == context.TenantId && i.ConnectorId == request.ConnectorId)
            .ToDictionaryAsync(i => i.SourceRecordId, cancellationToken);

        var dryRun = IntegrationFileDryRunEngine.Execute(
            table,
            definition,
            mapping.SourceSchemaFingerprint,
            existingIdentities);

        if (!dryRun.Success)
        {
            return new IntegrationFileImportRunResult(false, null, dryRun, dryRun.ErrorCode, dryRun.SafeMessage);
        }

        if (!mutate)
        {
            var now = DateTimeOffset.UtcNow;
            IntegrationRepositorySupport.AddAudit(
                dbContext,
                context,
                "integration.file.dry-run",
                "integration.file.dry-run.completed",
                "integration_connector",
                request.ConnectorId,
                now,
                new
                {
                    request.FileName,
                    dryRun.TotalRows,
                    dryRun.ValidRows,
                    dryRun.InvalidRows,
                    dryRun.DuplicateRows,
                    dryRun.NewProjections,
                    dryRun.Updates,
                    dryRun.Unchanged,
                });
            if (dbContext.ChangeTracker.HasChanges())
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return new IntegrationFileImportRunResult(true, null, dryRun, null, null);
        }

        var syncRun = new IntegrationSyncRun
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ConnectorId = request.ConnectorId,
            MappingVersionId = mapping.Id,
            SourceType = IntegrationConnectorKinds.FileImport,
            SourceFingerprint = fileFingerprint,
            ImportIdempotencyKey = request.ImportIdempotencyKey,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = IntegrationSyncRunStatuses.Running,
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
        foreach (var (rowIndex, row) in table.Rows.Select((row, index) => (index + 1, row)))
        {
            syncRun.RecordsRead++;
            var mapped = IntegrationMappingValidator.MapRow(rowIndex, row, definition);
            if (!mapped.IsValid || !seenSourceIds.Add(mapped.SourceRecordId))
            {
                syncRun.RecordsRejected++;
                syncRun.RecordsQuarantined++;
                AddQuarantine(context, syncRun, connector, definition, mapped, cancellationToken);
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
                    },
                    cancellationToken);
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
                    },
                    cancellationToken);
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
        connector.FreshnessState = IntegrationFreshnessStates.Fresh;
        connector.QualityState = syncRun.RecordsQuarantined > 0
            ? IntegrationQualityStates.Partial
            : IntegrationQualityStates.Good;

        IntegrationRepositorySupport.AddAudit(
            dbContext,
            context,
            "integration.file.import",
            "integration.file.import.completed",
            "integration_sync_run",
            syncRun.Id,
            syncRun.CompletedAtUtc.Value,
            new
            {
                request.FileName,
                syncRun.RecordsRead,
                syncRun.RecordsAccepted,
                syncRun.RecordsUpdated,
                syncRun.RecordsDuplicate,
                syncRun.RecordsRejected,
                syncRun.RecordsQuarantined,
                syncRun.Status,
            });

        await dbContext.SaveChangesAsync(cancellationToken);

        var resultDryRun = IntegrationFileDryRunEngine.Execute(
            table,
            definition,
            mapping.SourceSchemaFingerprint,
            existingIdentities);

        return new IntegrationFileImportRunResult(true, syncRun.Id, resultDryRun, null, null);
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
        IntegrationMappedRowResult mapped,
        CancellationToken cancellationToken)
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
            SafeReason = issue?.Message ?? "Row rejected during import.",
            ValidationIssue = issue?.Message,
            Status = IntegrationQuarantineStatuses.Open,
            EvidenceClass = IntegrationEvidenceClasses.Imported,
            OccurredAtUtc = DateTimeOffset.UtcNow,
        });
    }

    private static string BuildRowIdempotencyKey(Guid connectorId, string sourceEntityType, string sourceRecordId) =>
        $"integration:{connectorId:N}:{sourceEntityType}:{sourceRecordId}";

    private static string? ValidateSecurity(IntegrationFileImportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return "File name is required.";
        }

        if (request.FileSizeBytes <= 0)
        {
            return "File is empty.";
        }

        if (request.FileSizeBytes > IntegrationFileIngestionLimits.MaxUploadBytes)
        {
            return "File exceeds the maximum allowed upload size.";
        }

        var extension = Path.GetExtension(request.FileName);
        if (!IntegrationFileIngestionLimits.AllowedExtensions.Contains(extension))
        {
            return "File extension is not allowlisted.";
        }

        var normalizedName = Path.GetFileName(request.FileName);
        if (!string.Equals(normalizedName, request.FileName, StringComparison.Ordinal)
            || normalizedName.Contains("..", StringComparison.Ordinal))
        {
            return "Invalid file name.";
        }

        return null;
    }
}
