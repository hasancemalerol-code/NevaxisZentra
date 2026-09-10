using CrevaFactOS.Persistence.Integration.Database;
using CrevaFactOS.Persistence.Integration.Mapping;
using CrevaFactOS.Persistence.Integration.Erp;
using CrevaFactOS.Persistence.Integration.FileImport;
using CrevaFactOS.Persistence.Integration.FolderWatch;
using CrevaFactOS.Persistence.Integration.Rest;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Integration.Sync;

public sealed record IntegrationConnectorSyncRequest(
    Guid ConnectorId,
    Guid? MappingVersionId,
    string? ImportIdempotencyKey,
    bool DryRun);

public sealed record IntegrationConnectorSyncResult(
    bool Success,
    Guid? SyncRunId,
    IntegrationFileDryRunResult? DryRun,
    string? ErrorCode,
    string? SafeMessage);

public interface IIntegrationConnectorSyncService
{
    Task<IntegrationConnectorSyncResult> SyncAsync(
        IntegrationOperationContext context,
        IntegrationConnectorSyncRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class IntegrationConnectorSyncService(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IIntegrationRestRecordFetcher restRecordFetcher,
    IIntegrationReadOnlyDatabaseFetcher databaseFetcher,
    IIntegrationFolderWatchScanner folderWatchScanner,
    IIntegrationErpRecordBridge erpRecordBridge,
    IIntegrationRecordIngestionOrchestrator ingestionOrchestrator) : IIntegrationConnectorSyncService
{
    public Task<IntegrationConnectorSyncResult> SyncAsync(
        IntegrationOperationContext context,
        IntegrationConnectorSyncRequest request,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => SyncInternalAsync(context, request, ct),
            cancellationToken);

    private async Task<IntegrationConnectorSyncResult> SyncInternalAsync(
        IntegrationOperationContext context,
        IntegrationConnectorSyncRequest request,
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
            return new IntegrationConnectorSyncResult(
                false,
                null,
                null,
                IntegrationFailureCodes.NotFound,
                "Connector not found.");
        }

        IntegrationFileTable? table = null;
        string? checkpointAfter = connector.CheckpointJson;
        string? sourceFingerprint = null;
        string? processedFilePath = null;
        var sourceType = connector.ConnectorKind;

        switch (connector.ConnectorKind)
        {
            case IntegrationConnectorKinds.RestApi:
            {
                var fetch = await restRecordFetcher.FetchAsync(
                    connector,
                    connector.CheckpointJson,
                    bearerToken: null,
                    cancellationToken);
                if (!fetch.Success || fetch.Table is null)
                {
                    connector.HealthStatus = IntegrationHealthStatuses.SyncFailed;
                    connector.LastFailureAtUtc = DateTimeOffset.UtcNow;
                    connector.FailureStatus = fetch.FailureCategory;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    return new IntegrationConnectorSyncResult(
                        false,
                        null,
                        null,
                        IntegrationFailureCodes.ValidationFailed,
                        fetch.SafeMessage ?? "REST fetch failed.");
                }

                table = fetch.Table;
                checkpointAfter = fetch.CheckpointJson;
                sourceFingerprint = IntegrationContentFingerprint.ComputeTableFingerprint(table);
                break;
            }

            case IntegrationConnectorKinds.ReadOnlyDatabase:
            {
                var fetch = await databaseFetcher.FetchAsync(connector, connector.CheckpointJson, cancellationToken);
                if (!fetch.Success || fetch.Table is null)
                {
                    connector.HealthStatus = IntegrationHealthStatuses.SyncFailed;
                    connector.LastFailureAtUtc = DateTimeOffset.UtcNow;
                    connector.FailureStatus = fetch.FailureCategory;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    return new IntegrationConnectorSyncResult(
                        false,
                        null,
                        null,
                        IntegrationFailureCodes.ValidationFailed,
                        fetch.SafeMessage ?? "Database fetch failed.");
                }

                table = fetch.Table;
                checkpointAfter = fetch.CheckpointJson;
                sourceFingerprint = IntegrationContentFingerprint.ComputeTableFingerprint(table);
                break;
            }

            case IntegrationConnectorKinds.FolderWatch:
            {
                var scan = folderWatchScanner.ScanStableFile(connector);
                if (!scan.Success || scan.Table is null)
                {
                    return new IntegrationConnectorSyncResult(
                        false,
                        null,
                        null,
                        IntegrationFailureCodes.ValidationFailed,
                        scan.SafeMessage ?? "Folder watch scan failed.");
                }

                table = scan.Table;
                sourceFingerprint = scan.SourceFingerprint;
                checkpointAfter = scan.CheckpointJson;
                processedFilePath = scan.ProcessedFilePath;
                break;
            }

            case IntegrationConnectorKinds.ErpAdapter:
            {
                var fetch = await erpRecordBridge.FetchAsync(connector, cancellationToken);
                if (!fetch.Success || fetch.Table is null)
                {
                    return new IntegrationConnectorSyncResult(
                        false,
                        null,
                        null,
                        IntegrationFailureCodes.ValidationFailed,
                        fetch.SafeMessage ?? "ERP bridge fetch failed.");
                }

                table = fetch.Table;
                sourceFingerprint = IntegrationContentFingerprint.ComputeTableFingerprint(table);
                break;
            }

            default:
                return new IntegrationConnectorSyncResult(
                    false,
                    null,
                    null,
                    IntegrationFailureCodes.ValidationFailed,
                    "Connector kind does not support sync.");
        }

        var ingestion = await ingestionOrchestrator.ExecuteAsync(
            context,
            new IntegrationRecordIngestionRequest(
                request.ConnectorId,
                request.MappingVersionId,
                table,
                sourceType,
                sourceFingerprint,
                request.ImportIdempotencyKey,
                Mutate: !request.DryRun,
                connector.CheckpointJson,
                checkpointAfter),
            cancellationToken);

        if (!ingestion.Success)
        {
            return new IntegrationConnectorSyncResult(
                false,
                ingestion.SyncRunId,
                ingestion.DryRun,
                ingestion.ErrorCode,
                ingestion.SafeMessage);
        }

        if (!request.DryRun && connector.ConnectorKind == IntegrationConnectorKinds.FolderWatch)
        {
            folderWatchScanner.ArchiveProcessedFile(connector, processedFilePath);
        }

        return new IntegrationConnectorSyncResult(
            true,
            ingestion.SyncRunId,
            ingestion.DryRun,
            null,
            null);
    }
}
