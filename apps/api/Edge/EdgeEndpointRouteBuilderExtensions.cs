using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Edge;
using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Api.Edge;

public static class EdgeServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSEdge(this IServiceCollection services)
    {
        services.AddScoped<IEdgeQueryRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableEdgeQueryRepository()
                : new EdgeQueryRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<IEdgeIngestionService>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableEdgeIngestionService()
                : new EdgeIngestionService(
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
}

internal sealed record EdgeIngestResponseDto(
    bool Accepted,
    bool IsDuplicate,
    bool IsOutOfOrder,
    Guid? EventId);
