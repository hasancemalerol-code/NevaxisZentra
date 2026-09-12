using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Maintenance;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisFactOS.Api.Maintenance;

public static class MaintenanceReportingEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapMaintenanceReportingEndpoints(this RouteGroupBuilder group)
    {
        var reporting = group.MapGroup("/reporting");

        reporting.MapGet("/overview", GetOverviewAsync);
        reporting.MapGet("/reliability", GetReliabilityAsync);
        reporting.MapGet("/failures", GetFailureBreakdownAsync);

        group.MapGet("/assets/{assetReference}/reliability", GetAssetReliabilityAsync);
        group.MapGet("/condition-signals", ListConditionSignalsAsync);
        group.MapGet("/assets/{assetReference}/condition", GetAssetConditionAsync);
        group.MapGet("/risk-projections", ListRiskProjectionsAsync);

        return group;
    }

    private static async Task<IResult> GetOverviewAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceReliabilityRepository reliabilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult, period) = await ResolveReportingAccessAsync(
            facilityId, dateFrom, dateTo, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var overview = await reliabilityRepository.GetOverviewAsync(
            context, period!.DateFromUtc, period.DateToUtc, cancellationToken);

        return overview is null
            ? TypedResults.Problem(title: "Maintenance reporting service is unavailable.", statusCode: 503)
            : TypedResults.Ok(MaintenanceApiMapper.MapReliabilityOverview(overview));
    }

    private static async Task<IResult> GetReliabilityAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceReliabilityRepository reliabilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult, period) = await ResolveReportingAccessAsync(
            facilityId, dateFrom, dateTo, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var report = await reliabilityRepository.GetReliabilityReportAsync(
            context, period!.DateFromUtc, period.DateToUtc, cancellationToken);

        return report is null
            ? TypedResults.Problem(title: "Maintenance reporting service is unavailable.", statusCode: 503)
            : TypedResults.Ok(MaintenanceApiMapper.MapReliabilityReport(report));
    }

    private static async Task<IResult> GetFailureBreakdownAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] string? groupBy,
        [FromQuery] string? assetType,
        [FromQuery] string? assetReference,
        [FromQuery] string? failureCategory,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceReliabilityRepository reliabilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult, period) = await ResolveReportingAccessAsync(
            facilityId, dateFrom, dateTo, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var normalizedGroupBy = string.IsNullOrWhiteSpace(groupBy) ? "failure-category" : groupBy.Trim().ToLowerInvariant();
        if (normalizedGroupBy is not ("failure-category" or "asset-type" or "asset-reference" or "priority" or "resolution-code"))
        {
            return TypedResults.BadRequest(new { code = "InvalidGroupBy", message = "Unsupported groupBy value." });
        }

        var rows = await reliabilityRepository.GetFailureBreakdownAsync(
            context,
            period!.DateFromUtc,
            period.DateToUtc,
            normalizedGroupBy,
            assetType,
            assetReference,
            failureCategory,
            cancellationToken);

        return TypedResults.Ok(rows.Select(MaintenanceApiMapper.MapFailureBreakdownRow).ToList());
    }

    private static async Task<IResult> GetAssetReliabilityAsync(
        string assetReference,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceReliabilityRepository reliabilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var reliability = await reliabilityRepository.GetAssetReliabilityAsync(
            context, assetReference, cancellationToken);

        return reliability is null
            ? TypedResults.NotFound(new { code = MaintenanceFailureCodes.NotFound, message = "Asset reliability not found." })
            : TypedResults.Ok(MaintenanceApiMapper.MapAssetReliability(reliability));
    }

    private static async Task<IResult> ListConditionSignalsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? assetReference,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceReliabilityRepository reliabilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var signals = await reliabilityRepository.ListConditionSignalsAsync(context, assetReference, cancellationToken);
        return TypedResults.Ok(signals.Select(MaintenanceApiMapper.MapConditionSignal).ToList());
    }

    private static async Task<IResult> GetAssetConditionAsync(
        string assetReference,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceReliabilityRepository reliabilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var condition = await reliabilityRepository.GetAssetConditionAsync(context, assetReference, cancellationToken);
        return condition is null
            ? TypedResults.NotFound(new { code = MaintenanceFailureCodes.NotFound, message = "Asset condition not found." })
            : TypedResults.Ok(MaintenanceApiMapper.MapAssetCondition(condition));
    }

    private static async Task<IResult> ListRiskProjectionsAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceReliabilityRepository reliabilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var projections = await reliabilityRepository.ListRiskProjectionsAsync(context, cancellationToken);
        return TypedResults.Ok(projections.Select(MaintenanceApiMapper.MapRiskProjection).ToList());
    }

    private static async Task<(MaintenanceOperationContext? Context, IResult? FailResult, MaintenanceReportingPeriod? Period)>
        ResolveReportingAccessAsync(
            Guid? facilityId,
            DateTimeOffset? dateFrom,
            DateTimeOffset? dateTo,
            IFacilityAccessResolver facilityAccessResolver,
            CorrelationContext correlationContext,
            CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return (null, failResult, null);
        }

        if (dateFrom is null || dateTo is null)
        {
            return (null, TypedResults.BadRequest(new { code = "DateRangeRequired", message = "dateFrom and dateTo are required." }), null);
        }

        if (dateTo < dateFrom)
        {
            return (null, TypedResults.BadRequest(new { code = "InvalidDateRange", message = "dateTo must be greater than or equal to dateFrom." }), null);
        }

        return (context, null, new MaintenanceReportingPeriod(dateFrom.Value, dateTo.Value));
    }

    public static IServiceCollection AddNevaxisFactOSMaintenanceReporting(this IServiceCollection services)
    {
        services.AddScoped<IMaintenanceOperatingTimeSource, UnavailableMaintenanceOperatingTimeSource>();
        services.AddScoped<IMaintenanceReliabilityRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableMaintenanceReliabilityRepository()
                : new MaintenanceReliabilityRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IMaintenanceOperatingTimeSource>());
        });

        return services;
    }
}
