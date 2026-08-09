using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Erp;
using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Api.Erp;

public static class ErpEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapErpEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/erp").RequireAuthorization();

        group.MapGet("/connections", ListConnectionsAsync);
        group.MapPost("/connections", CreateConnectionAsync);
        group.MapGet("/connections/{connectionId:guid}", GetConnectionAsync);
        group.MapPut("/connections/{connectionId:guid}", UpdateConnectionAsync);
        group.MapPost("/connections/{connectionId:guid}/health-check", HealthCheckAsync);
        group.MapPost("/connections/{connectionId:guid}/sync/customers", SyncCustomersAsync);
        group.MapPost("/connections/{connectionId:guid}/sync/products", SyncProductsAsync);

        group.MapGet("/sync-runs", ListSyncRunsAsync);
        group.MapGet("/sync-runs/{syncRunId:guid}", GetSyncRunAsync);
        group.MapPost("/sync-runs/{syncRunId:guid}/retry", RetrySyncRunAsync);

        group.MapGet("/customers", ListCustomersAsync);
        group.MapGet("/products", ListProductsAsync);
        group.MapGet("/workspace-summary", GetWorkspaceSummaryAsync);

        return endpoints;
    }

    public static IServiceCollection AddCrevaFactOSErp(this IServiceCollection services)
    {
        services.AddSingleton<IErpAdapter, SimulatorErpAdapter>();
        services.AddSingleton<IErpAdapter, LogoErpAdapterPlaceholder>();
        services.AddSingleton<IErpAdapter, MikroErpAdapterPlaceholder>();
        services.AddSingleton<IErpAdapter, NetsisErpAdapterPlaceholder>();
        services.AddSingleton<IErpAdapterResolver, ErpAdapterResolver>();

        services.AddScoped<IErpConnectionRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableErpConnectionRepository()
                : new ErpConnectionRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IErpAdapterResolver>());
        });

        services.AddScoped<IErpSyncRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableErpSyncRepository()
                : new ErpSyncRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IErpAdapterResolver>());
        });

        return services;
    }

    private static async Task<IResult> ListConnectionsAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpConnectionRepository connectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await connectionRepository.ListAsync(context, cancellationToken);
        return TypedResults.Ok(rows.Select(MapConnection).ToList());
    }

    private static async Task<IResult> GetConnectionAsync(
        Guid connectionId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpConnectionRepository connectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var row = await connectionRepository.GetByIdAsync(context, connectionId, cancellationToken);
        if (row is null)
        {
            return TypedResults.Problem(
                title: "Connection profile not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(MapConnection(row));
    }

    private static async Task<IResult> CreateConnectionAsync(
        CreateErpConnectionRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IErpConnectionRepository connectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            ErpPermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await connectionRepository.CreateAsync(
            context,
            new CreateErpConnectionCommand(
                request.Provider,
                request.Name,
                request.ExternalCompanyId,
                request.ExternalCompanyCode,
                request.BaseEndpoint,
                request.ConnectionMode,
                request.CredentialReference,
                request.Status ?? ErpConnectionStatuses.Configured),
            cancellationToken);

        return ErpEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateConnectionAsync(
        Guid connectionId,
        UpdateErpConnectionRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IErpConnectionRepository connectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            request.FacilityId,
            ErpPermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await connectionRepository.UpdateAsync(
            context,
            new UpdateErpConnectionCommand(
                connectionId,
                request.ExpectedVersion,
                request.Name,
                request.ExternalCompanyId,
                request.ExternalCompanyCode,
                request.BaseEndpoint,
                request.ConnectionMode,
                request.CredentialReference,
                request.Status),
            cancellationToken);

        return ErpEndpointHelpers.MapWriteResult(result, isCreate: false);
    }

    private static async Task<IResult> HealthCheckAsync(
        Guid connectionId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpConnectionRepository connectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Sync,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var existing = await connectionRepository.GetByIdAsync(context, connectionId, cancellationToken);
        if (existing is null)
        {
            return TypedResults.Problem(
                title: "Connection profile not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.NotFound));
        }

        var health = await connectionRepository.CheckHealthAsync(context, connectionId, cancellationToken);
        if (health is null)
        {
            return TypedResults.Problem(
                title: "Health check failed.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.SourceUnavailable));
        }

        return TypedResults.Ok(health);
    }

    private static async Task<IResult> SyncCustomersAsync(
        Guid connectionId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Sync,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await syncRepository.SyncCustomersAsync(context, connectionId, null, cancellationToken);
        return ErpEndpointHelpers.MapSyncResult(result);
    }

    private static async Task<IResult> SyncProductsAsync(
        Guid connectionId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Sync,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await syncRepository.SyncProductsAsync(context, connectionId, null, cancellationToken);
        return ErpEndpointHelpers.MapSyncResult(result);
    }

    private static async Task<IResult> ListSyncRunsAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await syncRepository.ListSyncRunsAsync(context, connectionProfileId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetSyncRunAsync(
        Guid syncRunId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var row = await syncRepository.GetSyncRunAsync(context, syncRunId, cancellationToken);
        if (row is null)
        {
            return TypedResults.Problem(
                title: "Sync run not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: ErpEndpointHelpers.FailureExt(ErpApiFailureCodes.NotFound));
        }

        return TypedResults.Ok(row);
    }

    private static async Task<IResult> RetrySyncRunAsync(
        Guid syncRunId,
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Sync,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var result = await syncRepository.RetrySyncRunAsync(context, syncRunId, cancellationToken);
        return ErpEndpointHelpers.MapSyncResult(result);
    }

    private static async Task<IResult> ListCustomersAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await syncRepository.ListCustomersAsync(context, connectionProfileId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> ListProductsAsync(
        Guid? facilityId,
        Guid? connectionProfileId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpSyncRepository syncRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var rows = await syncRepository.ListProductsAsync(context, connectionProfileId, cancellationToken);
        return TypedResults.Ok(rows);
    }

    private static async Task<IResult> GetWorkspaceSummaryAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IErpConnectionRepository connectionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var summary = await connectionRepository.GetWorkspaceSummaryAsync(context, cancellationToken);
        return TypedResults.Ok(summary);
    }

    private static object MapConnection(ErpConnectionSummaryReadModel c) => new
    {
        c.Id,
        c.FacilityId,
        c.Provider,
        c.Name,
        c.ExternalCompanyId,
        c.ExternalCompanyCode,
        c.ConnectionMode,
        c.Status,
        c.LastSuccessfulConnectionAtUtc,
        c.LastFailedConnectionAtUtc,
        c.LastHealthCheckAtUtc,
        c.LastHealthLatencyMs,
        c.LastErrorCode,
        c.LastErrorMessage,
        c.LastSuccessfulSyncAtUtc,
        c.CreatedAtUtc,
        c.UpdatedAtUtc,
        c.Version,
        hasCredentialReference = !string.IsNullOrWhiteSpace(c.CredentialReference),
        providerDisplayName = c.Provider == ErpProviderKeys.Simulator
            ? SimulatorErpAdapter.DisplayName
            : c.Provider,
    };
}

internal sealed record CreateErpConnectionRequest(
    Guid FacilityId,
    string Provider,
    string Name,
    string? ExternalCompanyId,
    string? ExternalCompanyCode,
    string? BaseEndpoint,
    string ConnectionMode,
    string? CredentialReference,
    string? Status);

internal sealed record UpdateErpConnectionRequest(
    Guid FacilityId,
    uint ExpectedVersion,
    string Name,
    string? ExternalCompanyId,
    string? ExternalCompanyCode,
    string? BaseEndpoint,
    string ConnectionMode,
    string? CredentialReference,
    string Status);
