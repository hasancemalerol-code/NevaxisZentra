using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence;
using NevaxisZentra.Persistence.AiTasks.Delivery;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Integration;
using NevaxisZentra.Persistence.Integration.FactoryEventSpine;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Api.Integration;

public static class FactoryEventSpineEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapFactoryEventSpineEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/integration/event-spine").RequireAuthorization();

        group.MapPost("/ingest", IngestAsync);
        group.MapPost("/process", ProcessPendingAsync);
        group.MapPost("/dispatch", DispatchPendingAsync);
        group.MapGet("/events", ListEventsAsync);
        group.MapGet("/connectors/{connectorId:guid}/operations", GetConnectorOperationsAsync);
        group.MapPost("/inbound/{inboundEventId:guid}/reprocess", ReprocessInboundAsync);
        group.MapPost("/canonical/{canonicalEventId:guid}/replay", ReplayCanonicalAsync);
        group.MapPost("/dev/simulate-inj004", SimulateInj004Async);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraFactoryEventSpine(this IServiceCollection services)
    {
        services.AddScoped<IFactoryEventSpineService, FactoryEventSpineService>();
        services.AddScoped<IFactoryEventSpineWorkTenantDiscoverer, PostgreSqlFactoryEventSpineWorkTenantDiscoverer>();
        services.Configure<FactoryEventSpineOptions>(options =>
        {
            options.WorkerEnabled = true;
            options.BatchSize = 50;
        });
        services.AddHostedService<FactoryEventSpineBackgroundService>();
        return services;
    }

    private static async Task<IResult> IngestAsync(
        FactoryEventIngestionRequest request,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryEventSpineService spineService,
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

        var result = await spineService.IngestAsync(context, request, cancellationToken);
        if (!result.Accepted)
        {
            return IntegrationEndpointHelpers.MapFailure(result.ErrorCode!, result.SafeMessage!);
        }

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> ProcessPendingAsync(
        Guid? facilityId,
        int? batchSize,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryEventSpineService spineService,
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

        var result = await spineService.ProcessPendingIngestionAsync(
            context,
            batchSize ?? 50,
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> DispatchPendingAsync(
        Guid? facilityId,
        int? batchSize,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryEventSpineService spineService,
        HttpContext httpContext,
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

        var subjectId = httpContext.User.Identity?.Name ?? context.SubjectId;
        var result = await spineService.DispatchPendingEventsAsync(
            context,
            batchSize ?? 50,
            subjectId,
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> ListEventsAsync(
        Guid? facilityId,
        int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryEventSpineService spineService,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveReadAccessAsync(
            facilityId,
            facilityAccessResolver,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await spineService.ListRecentEventsAsync(context, limit ?? 50, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetConnectorOperationsAsync(
        Guid connectorId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryEventSpineService spineService,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveReadAccessAsync(
            facilityId,
            facilityAccessResolver,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var snapshot = await spineService.GetConnectorOperationsAsync(context, connectorId, cancellationToken);
        if (snapshot is null)
        {
            return IntegrationEndpointHelpers.MapFailure(
                IntegrationFailureCodes.NotFound,
                "Connector not found.");
        }

        return TypedResults.Ok(snapshot);
    }

    private static async Task<IResult> ReprocessInboundAsync(
        Guid inboundEventId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryEventSpineService spineService,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            IntegrationPermissionKeys.Reprocess,
            facilityAccessResolver,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await spineService.ReprocessQuarantinedInboundAsync(context, inboundEventId, cancellationToken);
        if (!result.Accepted)
        {
            return IntegrationEndpointHelpers.MapFailure(result.ErrorCode!, result.SafeMessage!);
        }

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> ReplayCanonicalAsync(
        Guid canonicalEventId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryEventSpineService spineService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await IntegrationEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            IntegrationPermissionKeys.EventReplay,
            facilityAccessResolver,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var subjectId = httpContext.User.Identity?.Name ?? context.SubjectId;
        var result = await spineService.ReplayCanonicalEventAsync(
            context,
            canonicalEventId,
            subjectId,
            cancellationToken);
        if (!result.Accepted)
        {
            return IntegrationEndpointHelpers.MapFailure(result.ErrorCode!, result.SafeMessage!);
        }

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> SimulateInj004Async(
        Guid connectorId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryEventSpineService spineService,
        IHostEnvironment hostEnvironment,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return Results.NotFound();
        }

        var (context, failResult) = await IntegrationEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            IntegrationPermissionKeys.Run,
            facilityAccessResolver,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var subjectId = httpContext.User.Identity?.Name ?? context.SubjectId;
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var events = new[]
        {
            DevelopmentFactorySourceSimulator.CreateCycleDegradationEvent(connectorId, $"sim-cycle-{suffix}"),
            DevelopmentFactorySourceSimulator.CreateMicroStopEvent(connectorId, $"sim-stop-{suffix}"),
            DevelopmentFactorySourceSimulator.CreateMaterialCoverageRiskEvent(connectorId, $"sim-material-{suffix}"),
        };

        var ingested = new List<FactoryEventIngestionResult>();
        foreach (var evt in events)
        {
            ingested.Add(await spineService.IngestAsync(context, evt, cancellationToken));
        }

        await spineService.ProcessPendingIngestionAsync(context, 10, cancellationToken);
        await spineService.DispatchPendingEventsAsync(context, 10, subjectId, cancellationToken);

        return TypedResults.Ok(new
        {
            ingested,
            message = "INJ-004 simülasyon olayları gerçek ingestion pipeline üzerinden işlendi.",
        });
    }
}

public sealed class FactoryEventSpineOptions
{
    public bool WorkerEnabled { get; set; } = true;
    public int BatchSize { get; set; } = 50;
}

internal sealed class FactoryEventSpineBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<FactoryEventSpineOptions> options,
    ILogger<FactoryEventSpineBackgroundService> logger) : BackgroundService
{
    private static readonly Action<ILogger, string, Exception?> PassFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(4311, nameof(PassFailed)),
        "Factory event spine worker pass failed: {ExceptionType}");

    private static readonly Action<ILogger, string, Exception?> FacilityBatchFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(4312, nameof(FacilityBatchFailed)),
        "Factory event spine worker facility batch failed: {ExceptionType}");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.WorkerEnabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetService<NevaxisZentraDbContext>();
                if (db is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                    continue;
                }

                var discoverer = scope.ServiceProvider.GetRequiredService<IFactoryEventSpineWorkTenantDiscoverer>();
                var tenantAccessor = scope.ServiceProvider.GetRequiredService<TenantContextAccessor>();
                var spineService = scope.ServiceProvider.GetRequiredService<IFactoryEventSpineService>();
                var tenantSession = scope.ServiceProvider.GetRequiredService<TenantDatabaseSession>();
                var contexts = await discoverer.DiscoverWorkerContextsAsync(stoppingToken);

                foreach (var tenantContext in contexts)
                {
                    try
                    {
                        tenantAccessor.Set(tenantContext);
                        // Row level security only shows a tenant's rows inside its tenant session; outside one the
                        // runtime role sees no rows and the worker would never find any work.
                        var staleProcessingBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
                        var (facilityIds, dispatchFacilityIds) = await tenantSession.ExecuteReadAsync(
                            async ct =>
                            {
                                var inbound = await db.IntegrationInboundFactoryEvents
                                    .Where(e => e.ProcessingStatus == FactoryInboundEventStatuses.Received ||
                                                e.ProcessingStatus == FactoryInboundEventStatuses.Failed)
                                    .Select(e => e.FacilityId)
                                    .Distinct()
                                    .OrderBy(id => id)
                                    .Take(5)
                                    .ToListAsync(ct);

                                var canonical = await db.IntegrationCanonicalFactoryEvents
                                    .Where(e => e.ProcessingState == FactoryCanonicalEventStates.DispatchPending ||
                                                e.ProcessingState == FactoryCanonicalEventStates.Failed ||
                                                (e.ProcessingState == FactoryCanonicalEventStates.Processing &&
                                                 e.UpdatedAtUtc < staleProcessingBefore))
                                    .Select(e => e.FacilityId)
                                    .Distinct()
                                    .OrderBy(id => id)
                                    .Take(5)
                                    .ToListAsync(ct);
                                return (inbound, canonical);
                            },
                            stoppingToken);

                        foreach (var facilityId in facilityIds.Concat(dispatchFacilityIds).Distinct())
                        {
                            var operationContext = new IntegrationOperationContext(
                                tenantContext.TenantId,
                                facilityId,
                                tenantContext.MembershipId,
                                tenantContext.MembershipId,
                                IntegrationPermissionKeys.Run);

                            await spineService.ProcessPendingIngestionAsync(
                                operationContext,
                                options.Value.BatchSize,
                                stoppingToken);
                            await spineService.DispatchPendingEventsAsync(
                                operationContext,
                                options.Value.BatchSize,
                                "factory-event-spine-worker",
                                stoppingToken);
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // One tenant's failure must not starve the tenants after it. Never log the message (data).
                        FacilityBatchFailed(logger, ex.GetType().Name, null);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                PassFailed(logger, ex.GetType().Name, null);
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
}
