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
    IOptions<FactoryEventSpineOptions> options) : BackgroundService
{
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

                var discoverer = scope.ServiceProvider.GetRequiredService<IAiTaskDeliveryWorkTenantDiscoverer>();
                var tenantAccessor = scope.ServiceProvider.GetRequiredService<TenantContextAccessor>();
                var spineService = scope.ServiceProvider.GetRequiredService<IFactoryEventSpineService>();
                var contexts = await discoverer.DiscoverWorkerContextsAsync(stoppingToken);

                foreach (var tenantContext in contexts)
                {
                    tenantAccessor.Set(tenantContext);
                    var facilityIds = await db.IntegrationInboundFactoryEvents
                        .Where(e => e.ProcessingStatus == FactoryInboundEventStatuses.Received ||
                                    e.ProcessingStatus == FactoryInboundEventStatuses.Failed)
                        .Select(e => e.FacilityId)
                        .Distinct()
                        .Take(5)
                        .ToListAsync(stoppingToken);

                    var dispatchFacilityIds = await db.IntegrationCanonicalFactoryEvents
                        .Where(e => e.ProcessingState == FactoryCanonicalEventStates.DispatchPending ||
                                    e.ProcessingState == FactoryCanonicalEventStates.Failed)
                        .Select(e => e.FacilityId)
                        .Distinct()
                        .Take(5)
                        .ToListAsync(stoppingToken);

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
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Worker failures are surfaced via connector health and event records.
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
}
