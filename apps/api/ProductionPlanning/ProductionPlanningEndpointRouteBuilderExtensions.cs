using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.ProductionPlanning;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.ProductionPlanning;

public static class ProductionPlanningEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapProductionPlanningReadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/production-planning/plans")
            .RequireAuthorization();

        group.MapGet("/", ListProductionPlansAsync);
        group.MapGet("/{productionPlanId:guid}", GetProductionPlanDetailAsync);

        return endpoints;
    }

    private static async Task<IResult> ListProductionPlansAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionPlanReadService readService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required.",
                    DateTimeOffset.UtcNow,
                    permissionKey: ProductionPlanningPermissionKeys.Read));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionPlanningPermissionKeys.Read),
            cancellationToken);
        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(accessResult);
        }

        var operationContext = ProductionPlanApiResultMapper.CreateOperationContext(
            accessResult.Access,
            "production-planning.list",
            accessResult.EvaluatedAtUtc,
            productionPlanId: null,
            ReadCorrelationId(correlationContext));

        var serviceResult = await readService.ListAsync(
            operationContext,
            new ProductionPlanListRequest(facilityId.Value, status, search, limit),
            cancellationToken);

        return serviceResult.Succeeded
            ? TypedResults.Ok(serviceResult.Value)
            : ProductionPlanApiResultMapper.MapServiceFailure(serviceResult.FailureCode!);
    }

    private static async Task<IResult> GetProductionPlanDetailAsync(
        Guid productionPlanId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionPlanReadService readService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required.",
                    DateTimeOffset.UtcNow,
                    permissionKey: ProductionPlanningPermissionKeys.Read));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionPlanningPermissionKeys.Read),
            cancellationToken);
        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(accessResult);
        }

        var operationContext = ProductionPlanApiResultMapper.CreateOperationContext(
            accessResult.Access,
            "production-planning.detail",
            accessResult.EvaluatedAtUtc,
            productionPlanId,
            ReadCorrelationId(correlationContext));

        var serviceResult = await readService.GetDetailAsync(
            operationContext,
            new ProductionPlanDetailRequest(facilityId.Value, productionPlanId),
            cancellationToken);

        return serviceResult.Succeeded
            ? TypedResults.Ok(serviceResult.Value)
            : ProductionPlanApiResultMapper.MapServiceFailure(serviceResult.FailureCode!);
    }

    private static Guid? ReadCorrelationId(CorrelationContext correlationContext) =>
        correlationContext.IsAvailable ? correlationContext.CorrelationId : null;
}

public static class ProductionPlanningServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSProductionPlanning(this IServiceCollection services)
    {
        services.AddScoped<IProductionPlanReadRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableProductionPlanReadRepository()
                : new ProductionPlanReadRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });
        services.AddScoped<IProductionPlanReadService, ProductionPlanReadService>();
        return services;
    }
}
