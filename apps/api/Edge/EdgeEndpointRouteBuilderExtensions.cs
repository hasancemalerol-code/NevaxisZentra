using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Edge;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Hosting;

namespace NevaxisFactOS.Api.Edge;

public static class EdgeServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSEdge(this IServiceCollection services)
    {
        services.AddScoped<IEdgeQueryRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableEdgeQueryRepository()
                : new EdgeQueryRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IEdgeIngestionService>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableEdgeIngestionService()
                : new EdgeIngestionService(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IEdgeTelemetryIngestionService>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableEdgeTelemetryIngestionService()
                : new EdgeTelemetryIngestionService(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IEdgeCycleIngestionService>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableEdgeCycleIngestionService()
                : new EdgeCycleIngestionService(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IEdgeCounterIngestionService>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableEdgeCounterIngestionService()
                : new EdgeCounterIngestionService(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IEdgeDowntimeIngestionService>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableEdgeDowntimeIngestionService()
                : new EdgeDowntimeIngestionService(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IEdgeAlarmIngestionService>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableEdgeAlarmIngestionService()
                : new EdgeAlarmIngestionService(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IEdgeSetupIngestionService>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableEdgeSetupIngestionService()
                : new EdgeSetupIngestionService(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IEdgeScrapIngestionService>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableEdgeScrapIngestionService()
                : new EdgeScrapIngestionService(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddSingleton<IEdgeGatewayAdapter, EdgeSimulatorAdapter>();
        services.AddSingleton<IEdgeGatewayAdapterResolver, EdgeGatewayAdapterResolver>();

        services.AddHttpClient(EdgeSimulatorClientNames.Simulator, client =>
        {
            var simulatorUrl = Environment.GetEnvironmentVariable("EDGE_SIMULATOR_URL")
                ?? "http://localhost:5100";
            client.BaseAddress = new Uri(simulatorUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }
}

public static class EdgeEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapEdgeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/edge").RequireAuthorization();

        group.MapGet("/gateways", ListGatewaysAsync);
        group.MapGet("/gateways/{gatewayId:guid}", GetGatewayAsync);
        group.MapGet("/machines", ListMachinesAsync);
        group.MapGet("/machines/{machineId:guid}", GetMachineAsync);
        group.MapGet("/machines/{machineId:guid}/events", ListMachineEventsAsync);
        group.MapGet("/machines/{machineId:guid}/runtime", GetMachineRuntimeAsync);
        group.MapPost("/events/ingest", IngestEventAsync);

        // EDGE-2 telemetry, cycle, counter, signals, state history
        group.MapGet("/machines/{machineId:guid}/telemetry/latest", GetLatestTelemetryAsync);
        group.MapGet("/machines/{machineId:guid}/telemetry", ListTelemetryHistoryAsync);
        group.MapGet("/machines/{machineId:guid}/cycles", ListCycleHistoryAsync);
        group.MapGet("/machines/{machineId:guid}/signals", GetSignalProjectionAsync);
        group.MapGet("/machines/{machineId:guid}/state-transitions", ListStateTransitionsAsync);

        // EDGE-3 operational observation reads
        group.MapGet("/machines/{machineId:guid}/downtime", ListDowntimeAsync);
        group.MapGet("/machines/{machineId:guid}/alarms", ListAlarmHistoryAsync);
        group.MapGet("/machines/{machineId:guid}/alarms/active", ListActiveAlarmsAsync);
        group.MapGet("/machines/{machineId:guid}/setups", ListSetupsAsync);
        group.MapGet("/machines/{machineId:guid}/scrap-observations", ListScrapObservationsAsync);
        group.MapGet("/machines/{machineId:guid}/operational-summary", GetOperationalSummaryAsync);

        // Development-only ingestion endpoints (simulator integration)
        var devGroup = endpoints.MapGroup("/api/edge/dev").RequireAuthorization();
        devGroup.MapPost("/telemetry", DevIngestTelemetryAsync);
        devGroup.MapPost("/cycle", DevIngestCycleAsync);
        devGroup.MapPost("/counter", DevIngestCounterAsync);
        // EDGE-3 development-only operational ingestion
        devGroup.MapPost("/downtime/start", DevIngestDowntimeStartAsync);
        devGroup.MapPost("/downtime/end", DevIngestDowntimeEndAsync);
        devGroup.MapPost("/alarm/raise", DevIngestAlarmRaiseAsync);
        devGroup.MapPost("/alarm/clear", DevIngestAlarmClearAsync);
        devGroup.MapPost("/setup/start", DevIngestSetupStartAsync);
        devGroup.MapPost("/setup/complete", DevIngestSetupCompleteAsync);
        devGroup.MapPost("/scrap", DevIngestScrapAsync);

        return endpoints;
    }

    private static async Task<IResult> ListGatewaysAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var rows = await edgeQueryRepository.ListGatewaysAsync(context, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetGatewayAsync(
        Guid gatewayId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var gateway = await edgeQueryRepository.GetGatewayAsync(context, gatewayId, cancellationToken);
        return gateway is null
            ? TypedResults.Problem(
                title: "Gateway not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: EdgeEndpointHelpers.FailureExt(EdgeApiFailureCodes.NotFound))
            : TypedResults.Ok(gateway);
    }

    private static async Task<IResult> ListMachinesAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var rows = await edgeQueryRepository.ListMachinesAsync(context, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetMachineAsync(
        Guid machineId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var machine = await edgeQueryRepository.GetMachineAsync(context, machineId, cancellationToken);
        return machine is null
            ? TypedResults.Problem(
                title: "Machine not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: EdgeEndpointHelpers.FailureExt(EdgeApiFailureCodes.NotFound))
            : TypedResults.Ok(machine);
    }

    private static async Task<IResult> ListMachineEventsAsync(
        Guid machineId,
        Guid? facilityId,
        int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var rows = await edgeQueryRepository.ListMachineEventsAsync(
            context, machineId, limit ?? 50, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetMachineRuntimeAsync(
        Guid machineId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var runtime = await edgeQueryRepository.GetMachineRuntimeAsync(context, machineId, cancellationToken);
        return runtime is null
            ? TypedResults.Problem(
                title: "Machine runtime not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: EdgeEndpointHelpers.FailureExt(EdgeApiFailureCodes.NotFound))
            : TypedResults.Ok(runtime);
    }

    private static async Task<IResult> IngestEventAsync(
        EdgeIngestRequest request,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeIngestionService ingestionService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveIngestAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await ingestionService.IngestAsync(context, request, cancellationToken);

        if (result.IsDuplicate)
        {
            return TypedResults.Ok(new EdgeIngestResponseDto(
                Accepted: false,
                IsDuplicate: true,
                IsOutOfOrder: false,
                EventId: result.EventId));
        }

        if (!result.Accepted)
        {
            return TypedResults.Problem(
                title: result.FailureReason ?? "Ingestion failed.",
                statusCode: StatusCodes.Status422UnprocessableEntity,
                extensions: EdgeEndpointHelpers.FailureExt(result.FailureCode ?? EdgeApiFailureCodes.UnexpectedError));
        }

        return TypedResults.Created(
            (string?)null,
            new EdgeIngestResponseDto(
                Accepted: true,
                IsDuplicate: false,
                IsOutOfOrder: result.IsOutOfOrder,
                EventId: result.EventId));
    }

    // ── EDGE-2 read handlers ──────────────────────────────────────────────

    private static async Task<IResult> GetLatestTelemetryAsync(
        Guid machineId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var rows = await edgeQueryRepository.ListLatestTelemetryAsync(context, machineId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> ListTelemetryHistoryAsync(
        Guid machineId,
        Guid? facilityId,
        string? metricKey,
        DateTimeOffset? from,
        DateTimeOffset? toUtc,
        int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var rows = await edgeQueryRepository.ListTelemetryHistoryAsync(
            context, machineId, metricKey, from, toUtc, limit ?? 100, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> ListCycleHistoryAsync(
        Guid machineId,
        Guid? facilityId,
        int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var rows = await edgeQueryRepository.ListCycleHistoryAsync(
            context, machineId, limit ?? 50, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetSignalProjectionAsync(
        Guid machineId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var signal = await edgeQueryRepository.GetSignalProjectionAsync(context, machineId, cancellationToken);
        return signal is null
            ? TypedResults.Problem(
                title: "Signal projection not yet available for this machine.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: EdgeEndpointHelpers.FailureExt(EdgeApiFailureCodes.NotFound))
            : TypedResults.Ok(signal);
    }

    private static async Task<IResult> ListStateTransitionsAsync(
        Guid machineId,
        Guid? facilityId,
        int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var rows = await edgeQueryRepository.ListStateTransitionsAsync(
            context, machineId, limit ?? 50, cancellationToken);
        return TypedResults.Ok(rows);
    }

    // ── EDGE-2 dev-only ingestion handlers ────────────────────────────────

    private static async Task<IResult> DevIngestTelemetryAsync(
        TelemetryIngestRequest request,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeTelemetryIngestionService telemetryService,
        CorrelationContext correlationContext,
        IWebHostEnvironment env,
        CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment())
            return TypedResults.Problem(
                title: "Dev ingestion endpoint is not available outside Development environment.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: EdgeEndpointHelpers.FailureExt(EdgeApiFailureCodes.UnexpectedError));

        var (context, failResult) = await EdgeEndpointHelpers.ResolveIngestAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await telemetryService.IngestTelemetryAsync(context, request, cancellationToken);

        if (result.IsDuplicate)
            return TypedResults.Ok(new TelemetryIngestResponseDto(false, true, false, result.SampleId, null, null));

        if (!result.Accepted)
            return TypedResults.Problem(
                title: result.FailureReason ?? "Telemetry ingestion failed.",
                statusCode: StatusCodes.Status422UnprocessableEntity,
                extensions: EdgeEndpointHelpers.FailureExt(result.FailureCode ?? EdgeApiFailureCodes.UnexpectedError));

        return TypedResults.Created(
            (string?)null,
            new TelemetryIngestResponseDto(true, false, result.IsOutOfOrder, result.SampleId, null, null));
    }

    private static async Task<IResult> DevIngestCycleAsync(
        CycleIngestRequest request,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeCycleIngestionService cycleService,
        CorrelationContext correlationContext,
        IWebHostEnvironment env,
        CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment())
            return TypedResults.Problem(
                title: "Dev ingestion endpoint is not available outside Development environment.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: EdgeEndpointHelpers.FailureExt(EdgeApiFailureCodes.UnexpectedError));

        var (context, failResult) = await EdgeEndpointHelpers.ResolveIngestAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await cycleService.IngestCycleAsync(context, request, cancellationToken);

        if (result.IsDuplicate)
            return TypedResults.Ok(new TelemetryIngestResponseDto(false, true, false, result.CycleId, null, null));

        if (!result.Accepted)
            return TypedResults.Problem(
                title: result.FailureReason ?? "Cycle ingestion failed.",
                statusCode: StatusCodes.Status422UnprocessableEntity,
                extensions: EdgeEndpointHelpers.FailureExt(result.FailureCode ?? EdgeApiFailureCodes.UnexpectedError));

        return TypedResults.Created(
            (string?)null,
            new TelemetryIngestResponseDto(true, false, result.IsOutOfOrder, result.CycleId, null, null));
    }

    private static async Task<IResult> DevIngestCounterAsync(
        CounterIngestRequest request,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeCounterIngestionService counterService,
        CorrelationContext correlationContext,
        IWebHostEnvironment env,
        CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment())
            return TypedResults.Problem(
                title: "Dev ingestion endpoint is not available outside Development environment.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: EdgeEndpointHelpers.FailureExt(EdgeApiFailureCodes.UnexpectedError));

        var (context, failResult) = await EdgeEndpointHelpers.ResolveIngestAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await counterService.IngestCounterAsync(context, request, cancellationToken);

        if (!result.Accepted)
            return TypedResults.Problem(
                title: result.FailureReason ?? "Counter ingestion failed.",
                statusCode: StatusCodes.Status422UnprocessableEntity,
                extensions: EdgeEndpointHelpers.FailureExt(result.FailureCode ?? EdgeApiFailureCodes.UnexpectedError));

        return TypedResults.Created(
            (string?)null,
            new TelemetryIngestResponseDto(true, false, false, result.ObservationId,
                result.CounterResetSuspected ? EdgeFailureCodes.CounterResetSuspected : null, null));
    }

    // ── EDGE-3 read handlers ──────────────────────────────────────────────

    private static async Task<IResult> ListDowntimeAsync(
        Guid machineId,
        Guid? facilityId,
        int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var rows = await edgeQueryRepository.ListDowntimeEpisodesAsync(
            context, machineId, limit ?? 50, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> ListAlarmHistoryAsync(
        Guid machineId,
        Guid? facilityId,
        int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var rows = await edgeQueryRepository.ListAlarmHistoryAsync(
            context, machineId, limit ?? 50, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> ListActiveAlarmsAsync(
        Guid machineId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var rows = await edgeQueryRepository.ListActiveAlarmsAsync(context, machineId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> ListSetupsAsync(
        Guid machineId,
        Guid? facilityId,
        int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var rows = await edgeQueryRepository.ListSetupEpisodesAsync(
            context, machineId, limit ?? 50, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> ListScrapObservationsAsync(
        Guid machineId,
        Guid? facilityId,
        int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var rows = await edgeQueryRepository.ListScrapObservationsAsync(
            context, machineId, limit ?? 50, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetOperationalSummaryAsync(
        Guid machineId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeQueryRepository edgeQueryRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EdgeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var summary = await edgeQueryRepository.GetOperationalSummaryAsync(context, machineId, cancellationToken);
        return TypedResults.Ok(summary);
    }

    // ── EDGE-3 development-only ingestion handlers ────────────────────────

    private static async Task<IResult> DevIngestDowntimeStartAsync(
        DowntimeStartIngestRequest request,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeDowntimeIngestionService downtimeService,
        CorrelationContext correlationContext,
        IWebHostEnvironment env,
        CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment())
            return TypedResults.Problem(
                title: "Dev ingestion endpoint is not available outside Development environment.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: EdgeEndpointHelpers.FailureExt(EdgeApiFailureCodes.UnexpectedError));

        var (context, failResult) = await EdgeEndpointHelpers.ResolveIngestAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await downtimeService.IngestDowntimeStartAsync(context, request, cancellationToken);
        return MapOperationalResult(result, "Downtime start ingestion failed.");
    }

    private static async Task<IResult> DevIngestDowntimeEndAsync(
        DowntimeEndIngestRequest request,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeDowntimeIngestionService downtimeService,
        CorrelationContext correlationContext,
        IWebHostEnvironment env,
        CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment())
            return TypedResults.Problem(
                title: "Dev ingestion endpoint is not available outside Development environment.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: EdgeEndpointHelpers.FailureExt(EdgeApiFailureCodes.UnexpectedError));

        var (context, failResult) = await EdgeEndpointHelpers.ResolveIngestAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await downtimeService.IngestDowntimeEndAsync(context, request, cancellationToken);
        return MapOperationalResult(result, "Downtime end ingestion failed.");
    }

    private static async Task<IResult> DevIngestAlarmRaiseAsync(
        AlarmRaiseIngestRequest request,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeAlarmIngestionService alarmService,
        CorrelationContext correlationContext,
        IWebHostEnvironment env,
        CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment())
            return TypedResults.Problem(
                title: "Dev ingestion endpoint is not available outside Development environment.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: EdgeEndpointHelpers.FailureExt(EdgeApiFailureCodes.UnexpectedError));

        var (context, failResult) = await EdgeEndpointHelpers.ResolveIngestAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await alarmService.IngestAlarmRaisedAsync(context, request, cancellationToken);
        return MapOperationalResult(result, "Alarm raise ingestion failed.");
    }

    private static async Task<IResult> DevIngestAlarmClearAsync(
        AlarmClearIngestRequest request,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeAlarmIngestionService alarmService,
        CorrelationContext correlationContext,
        IWebHostEnvironment env,
        CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment())
            return TypedResults.Problem(
                title: "Dev ingestion endpoint is not available outside Development environment.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: EdgeEndpointHelpers.FailureExt(EdgeApiFailureCodes.UnexpectedError));

        var (context, failResult) = await EdgeEndpointHelpers.ResolveIngestAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await alarmService.IngestAlarmClearedAsync(context, request, cancellationToken);
        return MapOperationalResult(result, "Alarm clear ingestion failed.");
    }

    private static async Task<IResult> DevIngestSetupStartAsync(
        SetupStartIngestRequest request,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeSetupIngestionService setupService,
        CorrelationContext correlationContext,
        IWebHostEnvironment env,
        CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment())
            return TypedResults.Problem(
                title: "Dev ingestion endpoint is not available outside Development environment.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: EdgeEndpointHelpers.FailureExt(EdgeApiFailureCodes.UnexpectedError));

        var (context, failResult) = await EdgeEndpointHelpers.ResolveIngestAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await setupService.IngestSetupStartedAsync(context, request, cancellationToken);
        return MapOperationalResult(result, "Setup start ingestion failed.");
    }

    private static async Task<IResult> DevIngestSetupCompleteAsync(
        SetupCompleteIngestRequest request,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeSetupIngestionService setupService,
        CorrelationContext correlationContext,
        IWebHostEnvironment env,
        CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment())
            return TypedResults.Problem(
                title: "Dev ingestion endpoint is not available outside Development environment.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: EdgeEndpointHelpers.FailureExt(EdgeApiFailureCodes.UnexpectedError));

        var (context, failResult) = await EdgeEndpointHelpers.ResolveIngestAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await setupService.IngestSetupCompletedAsync(context, request, cancellationToken);
        return MapOperationalResult(result, "Setup complete ingestion failed.");
    }

    private static async Task<IResult> DevIngestScrapAsync(
        ScrapIngestRequest request,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEdgeScrapIngestionService scrapService,
        CorrelationContext correlationContext,
        IWebHostEnvironment env,
        CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment())
            return TypedResults.Problem(
                title: "Dev ingestion endpoint is not available outside Development environment.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: EdgeEndpointHelpers.FailureExt(EdgeApiFailureCodes.UnexpectedError));

        var (context, failResult) = await EdgeEndpointHelpers.ResolveIngestAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await scrapService.IngestScrapObservedAsync(context, request, cancellationToken);
        return MapOperationalResult(result, "Scrap observation ingestion failed.");
    }

    private static IResult MapOperationalResult(OperationalIngestResult result, string defaultTitle)
    {
        if (result.IsDuplicate)
            return TypedResults.Ok(new OperationalIngestResponseDto(
                false, true, false, false, result.ObservationId, result.EpisodeId,
                result.IsMicroStop, result.DurationMs, null, null));

        if (result.IsUnmatched)
            return TypedResults.Ok(new OperationalIngestResponseDto(
                false, false, result.IsOutOfOrder, true, result.ObservationId, result.EpisodeId,
                result.IsMicroStop, result.DurationMs, result.FailureCode, result.FailureReason));

        if (!result.Accepted)
            return TypedResults.Problem(
                title: result.FailureReason ?? defaultTitle,
                statusCode: StatusCodes.Status422UnprocessableEntity,
                extensions: EdgeEndpointHelpers.FailureExt(result.FailureCode ?? EdgeApiFailureCodes.UnexpectedError));

        return TypedResults.Created(
            (string?)null,
            new OperationalIngestResponseDto(
                true, false, result.IsOutOfOrder, false, result.ObservationId, result.EpisodeId,
                result.IsMicroStop, result.DurationMs, null, null));
    }
}

internal sealed record EdgeIngestResponseDto(
    bool Accepted,
    bool IsDuplicate,
    bool IsOutOfOrder,
    Guid? EventId);

internal sealed record TelemetryIngestResponseDto(
    bool Accepted,
    bool IsDuplicate,
    bool IsOutOfOrder,
    Guid? Id,
    string? WarningCode,
    string? WarningReason);

internal sealed record OperationalIngestResponseDto(
    bool Accepted,
    bool IsDuplicate,
    bool IsOutOfOrder,
    bool IsUnmatched,
    Guid? ObservationId,
    Guid? EpisodeId,
    bool? IsMicroStop,
    long? DurationMs,
    string? FailureCode,
    string? FailureReason);
