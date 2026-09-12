using NevaxisFactOS.Api.Facilities;
using Microsoft.AspNetCore.Mvc;
using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Integration;
using NevaxisFactOS.Persistence.Integration.FileImport;
using NevaxisFactOS.Persistence.Integration.Mapping;
using NevaxisFactOS.Persistence.Integration.Connectors;
using NevaxisFactOS.Persistence.Integration.Database;
using NevaxisFactOS.Persistence.Integration.Erp;
using NevaxisFactOS.Persistence.Integration.FolderWatch;
using NevaxisFactOS.Persistence.Integration.Projections;
using NevaxisFactOS.Persistence.Integration.Rest;
using NevaxisFactOS.Persistence.Integration.Sync;
using NevaxisFactOS.Persistence.Integration.Webhook;
using NevaxisFactOS.Persistence.Material;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Api.Integration;

public static class IntegrationEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapIntegrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/integration").RequireAuthorization();

        group.MapGet("/connectors", ListConnectorsAsync);
        group.MapPost("/connectors", CreateConnectorAsync);
        group.MapGet("/connectors/{connectorId:guid}", GetConnectorAsync);
        group.MapPut("/connectors/{connectorId:guid}", UpdateConnectorAsync);
        group.MapPost("/connectors/{connectorId:guid}/test-connection", TestConnectionAsync);
        group.MapPost("/connectors/{connectorId:guid}/file-imports/preview", PreviewFileImportAsync)
            .DisableAntiforgery();
        group.MapPost("/connectors/{connectorId:guid}/file-imports/dry-run", DryRunFileImportAsync)
            .DisableAntiforgery();
        group.MapPost("/connectors/{connectorId:guid}/file-imports/import", ImportFileAsync)
            .DisableAntiforgery();
        group.MapGet("/connectors/{connectorId:guid}/mappings", ListMappingsAsync);
        group.MapPost("/connectors/{connectorId:guid}/mappings", CreateMappingAsync);
        group.MapPost("/connectors/{connectorId:guid}/mappings/{mappingVersionId:guid}/validate", ValidateMappingAsync);
        group.MapPost("/connectors/{connectorId:guid}/mappings/{mappingVersionId:guid}/activate", ActivateMappingAsync);
        group.MapGet("/connectors/{connectorId:guid}/sync-runs", ListSyncRunsAsync);
        group.MapGet("/connectors/{connectorId:guid}/quarantine", ListQuarantineAsync);
        group.MapPost("/connectors/{connectorId:guid}/sync", SyncConnectorAsync);
        group.MapPost("/connectors/{connectorId:guid}/sync/dry-run", DryRunSyncConnectorAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisFactOSIntegration(this IServiceCollection services)
    {
        services.AddHttpClient("integration-rest");
        services.AddSingleton<IIntegrationConnectorImplementation, SimulatedIntegrationConnectorImplementation>();
        services.AddSingleton<IIntegrationConnectorImplementation, RestApiIntegrationConnectorImplementation>();
        services.AddSingleton<IIntegrationConnectorImplementation, ReadOnlyDatabaseIntegrationConnectorImplementation>();
        services.AddSingleton<IIntegrationConnectorImplementation, FolderWatchIntegrationConnectorImplementation>();
        services.AddSingleton<IIntegrationConnectorImplementation, WebhookIntegrationConnectorImplementation>();
        services.AddSingleton<IIntegrationConnectorImplementation, ErpAdapterIntegrationConnectorImplementation>();
        services.AddSingleton<IIntegrationConnectorImplementationResolver, IntegrationConnectorImplementationResolver>();
        services.AddSingleton<IIntegrationFileImportService, IntegrationFileImportService>();
        services.AddSingleton<IIntegrationRestRecordFetcher, IntegrationRestRecordFetcher>();
        services.AddSingleton<IIntegrationReadOnlyDatabaseFetcher, IntegrationReadOnlyDatabaseFetcher>();
        services.AddSingleton<IIntegrationFolderWatchScanner, IntegrationFolderWatchScanner>();
        services.AddScoped<IIntegrationErpRecordBridge, IntegrationErpRecordBridge>();
        services.AddScoped<IIntegrationRecordIngestionOrchestrator, IntegrationRecordIngestionOrchestrator>();
        services.AddScoped<IIntegrationConnectorSyncService, IntegrationConnectorSyncService>();
        services.AddScoped<IIntegrationWebhookIngestionService, IntegrationWebhookIngestionService>();
        services.AddScoped<IIntegrationMaterialProjectionWriter, IntegrationMaterialProjectionWriter>();
        services.AddScoped<IIntegrationFileImportOrchestrator, IntegrationFileImportOrchestrator>();

        services.AddScoped<IIntegrationMappingRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableIntegrationMappingRepository()
                : new IntegrationMappingRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IIntegrationConnectorRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableIntegrationConnectorRepository()
                : new IntegrationConnectorRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }

    private static async Task<IResult> ListConnectorsAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIntegrationConnectorRepository connectorRepository,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await connectorRepository.ListAsync(context, cancellationToken);
        return TypedResults.Ok(rows.Select(MapConnectorSummary));
    }

    private static async Task<IResult> GetConnectorAsync(
        Guid connectorId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIntegrationConnectorRepository connectorRepository,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var entity = await connectorRepository.GetEntityByIdAsync(context, connectorId, cancellationToken);
        if (entity is null)
        {
            return IntegrationEndpointHelpers.MapFailure(
                IntegrationFailureCodes.NotFound,
                "Connector not found.");
        }

        return TypedResults.Ok(MapConnectorDetail(entity));
    }

    private static async Task<IResult> CreateConnectorAsync(
        CreateIntegrationConnectorRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IIntegrationConnectorRepository connectorRepository,
        CancellationToken cancellationToken)
    {
        var permission = string.IsNullOrWhiteSpace(request.SecretReference)
            ? IntegrationPermissionKeys.Manage
            : IntegrationPermissionKeys.SecretConfigure;

        var (context, failResult) = await IntegrationEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            permission,
            facilityAccessResolver,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await connectorRepository.CreateAsync(
            context,
            new CreateIntegrationConnectorCommand(
                request.Name,
                request.Description,
                request.ConnectorKind,
                request.SourceSystemType,
                request.SourceSystemName,
                request.Environment ?? IntegrationEnvironments.Development,
                request.AuthenticationMode ?? IntegrationAuthenticationModes.None,
                request.SecretReference,
                request.EndpointOrLocation,
                request.SynchronizationMode ?? IntegrationSynchronizationModes.Manual,
                request.ScheduleExpression,
                request.DomainBindingKind ?? IntegrationDomainBindingKinds.Standalone,
                request.DomainBindingId,
                request.ConfigurationJson ?? "{}"),
            cancellationToken);

        return IntegrationEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateConnectorAsync(
        Guid connectorId,
        UpdateIntegrationConnectorRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IIntegrationConnectorRepository connectorRepository,
        CancellationToken cancellationToken)
    {
        var permission = string.IsNullOrWhiteSpace(request.SecretReference)
            ? IntegrationPermissionKeys.Manage
            : IntegrationPermissionKeys.SecretConfigure;

        var (context, failResult) = await IntegrationEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            permission,
            facilityAccessResolver,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await connectorRepository.UpdateAsync(
            context,
            new UpdateIntegrationConnectorCommand(
                connectorId,
                request.ExpectedVersion,
                request.Name,
                request.Description,
                request.Environment,
                request.Enabled,
                request.AuthenticationMode,
                request.SecretReference,
                request.EndpointOrLocation,
                request.SynchronizationMode,
                request.ScheduleExpression,
                request.ConfigurationJson ?? "{}"),
            cancellationToken);

        return IntegrationEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> TestConnectionAsync(
        Guid connectorId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIntegrationConnectorRepository connectorRepository,
        IIntegrationConnectorImplementationResolver implementationResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            IntegrationPermissionKeys.Run,
            facilityAccessResolver,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var connector = await connectorRepository.GetEntityByIdAsync(context, connectorId, cancellationToken);
        if (connector is null)
        {
            return IntegrationEndpointHelpers.MapFailure(
                IntegrationFailureCodes.NotFound,
                "Connector not found.");
        }

        var implementation = implementationResolver.Resolve(connector.ConnectorKind);
        if (implementation is null)
        {
            return TypedResults.Ok(new
            {
                success = false,
                healthStatus = IntegrationHealthStatuses.Misconfigured,
                safeMessage = "No connector implementation is registered for this connector kind.",
            });
        }

        var testResult = await implementation.TestConnectionAsync(
            new IntegrationConnectorContext(
                connector,
                context.SubjectId,
                correlationContext.IsAvailable ? correlationContext.CorrelationId : null),
            cancellationToken);

        return TypedResults.Ok(new
        {
            success = testResult.Success,
            healthStatus = testResult.HealthStatus,
            safeMessage = testResult.SafeMessage,
            latencyMs = testResult.LatencyMs,
        });
    }

    private static async Task<IResult> PreviewFileImportAsync(
        Guid connectorId,
        Guid? facilityId,
        IFormFile file,
        string? delimiter,
        bool? hasHeaderRow,
        string? encodingName,
        IFacilityAccessResolver facilityAccessResolver,
        IIntegrationConnectorRepository connectorRepository,
        IIntegrationFileImportService fileImportService,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            IntegrationPermissionKeys.Run,
            facilityAccessResolver,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var connector = await connectorRepository.GetEntityByIdAsync(context, connectorId, cancellationToken);
        if (connector is null)
        {
            return IntegrationEndpointHelpers.MapFailure(
                IntegrationFailureCodes.NotFound,
                "Connector not found.");
        }

        if (connector.ConnectorKind != IntegrationConnectorKinds.FileImport
            && connector.ConnectorKind != IntegrationConnectorKinds.FolderWatch)
        {
            return IntegrationEndpointHelpers.MapFailure(
                IntegrationFailureCodes.ValidationFailed,
                "Connector kind does not support file import preview.");
        }

        await using var uploadStream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await uploadStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        IReadOnlyList<string>? sheetNames = null;
        if (Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            sheetNames = IntegrationXlsxParser.ListSheets(buffer).Sheets.Select(s => s.Name).ToList();
            buffer.Position = 0;
        }

        var preview = fileImportService.Preview(new IntegrationFileImportPreviewRequest(
            file.FileName,
            file.Length,
            buffer,
            delimiter,
            hasHeaderRow,
            encodingName));

        if (!preview.Success)
        {
            return IntegrationEndpointHelpers.MapFailure(
                preview.ErrorCode ?? IntegrationFailureCodes.ValidationFailed,
                preview.SafeMessage ?? "File preview failed.");
        }

        return TypedResults.Ok(new
        {
            preview.FileKind,
            preview.DetectedHeaders,
            preview.PreviewRows,
            preview.TotalPreviewRowCount,
            preview.EvidenceClass,
            schemaFingerprint = IntegrationSchemaFingerprint.Compute(preview.DetectedHeaders),
            sheetNames,
            connectorId,
        });
    }

    private static async Task<IResult> DryRunFileImportAsync(
        Guid connectorId,
        Guid? facilityId,
        Guid? mappingVersionId,
        IFormFile file,
        string? delimiter,
        bool? hasHeaderRow,
        string? encodingName,
        string? sheetName,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        IIntegrationFileImportOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            IntegrationPermissionKeys.Run,
            facilityAccessResolver,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var enrichedContext = IntegrationEndpointHelpers.WithCorrelation(context, correlationContext);

        await using var stream = file.OpenReadStream();
        var result = await orchestrator.ExecuteAsync(
            enrichedContext,
            new IntegrationFileImportRequest(
                file.FileName,
                file.Length,
                stream,
                delimiter,
                hasHeaderRow,
                encodingName,
                sheetName,
                connectorId,
                mappingVersionId,
                IntegrationImportModes.DryRun,
                null),
            cancellationToken);

        if (!result.Success)
        {
            return IntegrationEndpointHelpers.MapFailure(
                result.ErrorCode ?? IntegrationFailureCodes.ValidationFailed,
                result.SafeMessage ?? "Dry run failed.");
        }

        return TypedResults.Ok(result.DryRun);
    }

    private static async Task<IResult> ImportFileAsync(
        Guid connectorId,
        Guid? facilityId,
        Guid? mappingVersionId,
        string? importIdempotencyKey,
        IFormFile file,
        string? delimiter,
        bool? hasHeaderRow,
        string? encodingName,
        string? sheetName,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        IIntegrationFileImportOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            IntegrationPermissionKeys.Run,
            facilityAccessResolver,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var enrichedContext = IntegrationEndpointHelpers.WithCorrelation(context, correlationContext);

        await using var stream = file.OpenReadStream();
        var result = await orchestrator.ExecuteAsync(
            enrichedContext,
            new IntegrationFileImportRequest(
                file.FileName,
                file.Length,
                stream,
                delimiter,
                hasHeaderRow,
                encodingName,
                sheetName,
                connectorId,
                mappingVersionId,
                IntegrationImportModes.Import,
                importIdempotencyKey),
            cancellationToken);

        if (!result.Success)
        {
            return IntegrationEndpointHelpers.MapFailure(
                result.ErrorCode ?? IntegrationFailureCodes.ValidationFailed,
                result.SafeMessage ?? "Import failed.");
        }

        return TypedResults.Ok(new { syncRunId = result.SyncRunId, summary = result.DryRun });
    }

    private static async Task<IResult> ListMappingsAsync(
        Guid connectorId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIntegrationMappingRepository mappingRepository,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await mappingRepository.ListByConnectorAsync(context, connectorId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> CreateMappingAsync(
        Guid connectorId,
        CreateIntegrationMappingRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IIntegrationMappingRepository mappingRepository,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            IntegrationPermissionKeys.MappingManage,
            facilityAccessResolver,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var definition = IntegrationMappingDefinition.TryParse(request.MappingJson);
        if (definition is null)
        {
            return IntegrationEndpointHelpers.MapFailure(
                IntegrationFailureCodes.ValidationFailed,
                "Mapping JSON is invalid.");
        }

        var fingerprint = IntegrationSchemaFingerprint.Compute(request.SourceHeaders);
        var result = await mappingRepository.CreateDraftAsync(
            context,
            new CreateIntegrationMappingDraftCommand(
                connectorId,
                request.TargetCanonicalEntityType ?? IntegrationCanonicalProjectionTypes.Material,
                fingerprint,
                request.SourceHeaders,
                definition),
            cancellationToken);

        return IntegrationEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> ValidateMappingAsync(
        Guid connectorId,
        Guid mappingVersionId,
        ValidateIntegrationMappingRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IIntegrationMappingRepository mappingRepository,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            IntegrationPermissionKeys.MappingManage,
            facilityAccessResolver,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var validation = await mappingRepository.ValidateDraftAsync(
            context,
            mappingVersionId,
            request.SourceHeaders,
            cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.Ok(validation);
        }

        var result = await mappingRepository.MarkValidatedAsync(
            context,
            mappingVersionId,
            request.ExpectedVersion,
            cancellationToken);
        if (!result.Success)
        {
            return IntegrationEndpointHelpers.MapWriteResult(result, isCreate: false);
        }

        return TypedResults.Ok(validation);
    }

    private static async Task<IResult> ActivateMappingAsync(
        Guid connectorId,
        Guid mappingVersionId,
        ActivateIntegrationMappingRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IIntegrationMappingRepository mappingRepository,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            IntegrationPermissionKeys.MappingManage,
            facilityAccessResolver,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await mappingRepository.ActivateAsync(
            context,
            new ActivateIntegrationMappingCommand(mappingVersionId, request.ExpectedVersion),
            cancellationToken);
        return IntegrationEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> ListSyncRunsAsync(
        Guid connectorId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var tenantDatabaseSession = services.GetRequiredService<TenantDatabaseSession>();
        var dbContext = services.GetRequiredService<NevaxisFactOSDbContext>();
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await tenantDatabaseSession.ExecuteReadAsync(async ct =>
            await dbContext.IntegrationSyncRuns.AsNoTracking()
                .Where(r => r.TenantId == context.TenantId
                    && r.FacilityId == context.FacilityId
                    && r.ConnectorId == connectorId)
                .OrderByDescending(r => r.StartedAtUtc)
                .Take(50)
                .Select(r => new
                {
                    r.Id,
                    r.Status,
                    r.SourceType,
                    r.SourceFingerprint,
                    r.MappingVersionId,
                    r.StartedAtUtc,
                    r.CompletedAtUtc,
                    r.RecordsRead,
                    r.RecordsAccepted,
                    r.RecordsUpdated,
                    r.RecordsDuplicate,
                    r.RecordsRejected,
                    r.RecordsQuarantined,
                })
                .ToListAsync(ct), cancellationToken);

        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> ListQuarantineAsync(
        Guid connectorId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var tenantDatabaseSession = services.GetRequiredService<TenantDatabaseSession>();
        var dbContext = services.GetRequiredService<NevaxisFactOSDbContext>();
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await tenantDatabaseSession.ExecuteReadAsync(async ct =>
            await dbContext.IntegrationIngestionQuarantine.AsNoTracking()
                .Where(q => q.TenantId == context.TenantId
                    && q.FacilityId == context.FacilityId
                    && q.ConnectorId == connectorId)
                .OrderByDescending(q => q.OccurredAtUtc)
                .Take(100)
                .Select(q => new
                {
                    q.Id,
                    q.SourceEntityType,
                    q.SourceRecordId,
                    q.ReasonCode,
                    q.SafeReason,
                    q.ValidationIssue,
                    q.Status,
                    q.OccurredAtUtc,
                })
                .ToListAsync(ct), cancellationToken);

        return TypedResults.Ok(rows);
    }

    private static object MapConnectorSummary(IntegrationConnectorSummaryReadModel c) => new
    {
        c.Id,
        c.FacilityId,
        c.Name,
        c.ConnectorKind,
        c.SourceSystemType,
        c.SourceSystemName,
        c.Environment,
        c.Status,
        c.Enabled,
        c.HealthStatus,
        c.FreshnessState,
        c.QualityState,
        c.LastSuccessfulSyncAtUtc,
        c.LastAttemptedSyncAtUtc,
        c.DomainBindingKind,
        c.DomainBindingId,
        c.Version,
    };

    private static object MapConnectorDetail(IntegrationConnector connector) => new
    {
        connector.Id,
        connector.FacilityId,
        connector.Name,
        connector.Description,
        connector.ConnectorKind,
        connector.SourceSystemType,
        connector.SourceSystemName,
        connector.Environment,
        connector.Direction,
        connector.Status,
        connector.Enabled,
        connector.AuthenticationMode,
        hasSecretReference = !string.IsNullOrWhiteSpace(connector.SecretReference),
        endpointOrLocation = connector.EndpointOrLocation,
        connector.SynchronizationMode,
        connector.ScheduleExpression,
        connector.HealthStatus,
        connector.FreshnessState,
        connector.QualityState,
        connector.LastSuccessfulSyncAtUtc,
        connector.LastAttemptedSyncAtUtc,
        connector.DomainBindingKind,
        connector.DomainBindingId,
        connector.ConfigurationJson,
        connector.CreatedAtUtc,
        connector.UpdatedAtUtc,
        connector.Version,
    };

    private static async Task<IResult> SyncConnectorAsync(
        Guid connectorId,
        Guid? facilityId,
        Guid? mappingVersionId,
        string? importIdempotencyKey,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        IIntegrationConnectorSyncService syncService,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            IntegrationPermissionKeys.Run,
            facilityAccessResolver,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var enrichedContext = IntegrationEndpointHelpers.WithCorrelation(context, correlationContext);
        var result = await syncService.SyncAsync(
            enrichedContext,
            new IntegrationConnectorSyncRequest(
                connectorId,
                mappingVersionId,
                importIdempotencyKey,
                DryRun: false),
            cancellationToken);

        if (!result.Success)
        {
            return IntegrationEndpointHelpers.MapFailure(
                result.ErrorCode ?? IntegrationFailureCodes.ValidationFailed,
                result.SafeMessage ?? "Sync failed.");
        }

        return TypedResults.Ok(new
        {
            syncRunId = result.SyncRunId,
            summary = result.DryRun,
        });
    }

    private static async Task<IResult> DryRunSyncConnectorAsync(
        Guid connectorId,
        Guid? facilityId,
        Guid? mappingVersionId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        IIntegrationConnectorSyncService syncService,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            IntegrationPermissionKeys.Run,
            facilityAccessResolver,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var enrichedContext = IntegrationEndpointHelpers.WithCorrelation(context, correlationContext);
        var result = await syncService.SyncAsync(
            enrichedContext,
            new IntegrationConnectorSyncRequest(
                connectorId,
                mappingVersionId,
                null,
                DryRun: true),
            cancellationToken);

        if (!result.Success)
        {
            return IntegrationEndpointHelpers.MapFailure(
                result.ErrorCode ?? IntegrationFailureCodes.ValidationFailed,
                result.SafeMessage ?? "Sync dry-run failed.");
        }

        return TypedResults.Ok(result.DryRun);
    }
}

internal sealed record CreateIntegrationConnectorRequest(
    Guid FacilityId,
    string Name,
    string? Description,
    string ConnectorKind,
    string SourceSystemType,
    string SourceSystemName,
    string? Environment,
    string? AuthenticationMode,
    string? SecretReference,
    string? EndpointOrLocation,
    string? SynchronizationMode,
    string? ScheduleExpression,
    string? DomainBindingKind,
    Guid? DomainBindingId,
    string? ConfigurationJson);

internal sealed record CreateIntegrationMappingRequest(
    Guid FacilityId,
    IReadOnlyList<string> SourceHeaders,
    string MappingJson,
    string? TargetCanonicalEntityType);

internal sealed record ValidateIntegrationMappingRequest(
    Guid FacilityId,
    uint ExpectedVersion,
    IReadOnlyList<string>? SourceHeaders);

internal sealed record ActivateIntegrationMappingRequest(
    Guid FacilityId,
    uint ExpectedVersion);

internal sealed record UpdateIntegrationConnectorRequest(
    Guid FacilityId,
    uint ExpectedVersion,
    string Name,
    string? Description,
    string Environment,
    bool Enabled,
    string AuthenticationMode,
    string? SecretReference,
    string? EndpointOrLocation,
    string SynchronizationMode,
    string? ScheduleExpression,
    string ConfigurationJson);
