using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Edge;
using CrevaFactOS.Persistence.Maintenance;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Maintenance;

public static class MaintenanceConditionEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapMaintenanceConditionEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/condition/overview", GetConditionOverviewAsync);
        group.MapGet("/assets/{assetReference}/condition-intelligence", GetAssetConditionIntelligenceAsync);
        group.MapGet("/assets/{assetReference}/anomalies", ListAssetAnomaliesAsync);
        group.MapGet("/assets/{assetReference}/condition-signals", ListAssetConditionSignalsAsync);
        group.MapPost("/condition/evaluate", EvaluateConditionAsync);
        group.MapGet("/condition/risk/overview", GetPredictiveRiskOverviewAsync);
        group.MapGet("/assets/{assetReference}/predictive-risk", GetAssetPredictiveRiskAsync);
        group.MapGet("/assets/{assetReference}/predictive-risk/history", ListAssetPredictiveRiskHistoryAsync);
        group.MapPost("/condition/risk/evaluate", EvaluatePredictiveRiskAsync);

        return group;
    }

    private static async Task<IResult> GetConditionOverviewAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceConditionRepository conditionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var overview = await conditionRepository.GetOverviewAsync(context, cancellationToken);
        var rows = await conditionRepository.ListAssetRowsAsync(context, cancellationToken);

        return overview is null
            ? TypedResults.Problem(title: "Maintenance condition intelligence is unavailable.", statusCode: 503)
            : TypedResults.Ok(new
            {
                overview = MaintenanceApiMapper.MapConditionOverview(overview),
                assets = rows.Select(MaintenanceApiMapper.MapConditionAssetRow).ToList(),
            });
    }

    private static async Task<IResult> GetAssetConditionIntelligenceAsync(
        string assetReference,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceConditionRepository conditionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var profile = await conditionRepository.GetAssetConditionAsync(context, assetReference, cancellationToken);
        return profile is null
            ? TypedResults.NotFound(new { code = MaintenanceFailureCodes.NotFound, message = "Asset condition intelligence not found." })
            : TypedResults.Ok(MaintenanceApiMapper.MapConditionProfile(profile));
    }

    private static async Task<IResult> ListAssetAnomaliesAsync(
        string assetReference,
        [FromQuery] Guid? facilityId,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceConditionRepository conditionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var anomalies = await conditionRepository.ListAnomaliesAsync(
            context,
            assetReference,
            limit ?? 50,
            cancellationToken);

        return TypedResults.Ok(anomalies.Select(MaintenanceApiMapper.MapConditionAnomaly).ToList());
    }

    private static async Task<IResult> ListAssetConditionSignalsAsync(
        string assetReference,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceConditionRepository conditionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var signals = await conditionRepository.ListConditionSignalsAsync(context, assetReference, cancellationToken);
        return TypedResults.Ok(signals.Select(MaintenanceApiMapper.MapConditionIntelligenceSignal).ToList());
    }

    private static async Task<IResult> EvaluateConditionAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenanceConditionRepository conditionRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var evaluated = await conditionRepository.EvaluateFacilityAsync(context, cancellationToken);
        return evaluated
            ? TypedResults.Ok(new { evaluated = true })
            : TypedResults.Problem(title: "Maintenance condition evaluation failed.", statusCode: 503);
    }

    private static async Task<IResult> GetPredictiveRiskOverviewAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenancePredictiveRiskRepository predictiveRiskRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var overview = await predictiveRiskRepository.GetOverviewAsync(context, cancellationToken);
        var rows = await predictiveRiskRepository.ListAssetRowsAsync(context, cancellationToken);

        return overview is null
            ? TypedResults.Problem(title: "Predictive maintenance risk is unavailable.", statusCode: 503)
            : TypedResults.Ok(new
            {
                overview = MaintenanceApiMapper.MapPredictiveRiskOverview(overview),
                assets = rows.Select(MaintenanceApiMapper.MapPredictiveRiskAssetRow).ToList(),
            });
    }

    private static async Task<IResult> GetAssetPredictiveRiskAsync(
        string assetReference,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenancePredictiveRiskRepository predictiveRiskRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var assessment = await predictiveRiskRepository.GetCurrentAssessmentAsync(context, assetReference, cancellationToken);
        return assessment is null
            ? TypedResults.NotFound(new { code = MaintenanceFailureCodes.NotFound, message = "Predictive risk assessment not found." })
            : TypedResults.Ok(MaintenanceApiMapper.MapPredictiveRiskAssessment(assessment));
    }

    private static async Task<IResult> ListAssetPredictiveRiskHistoryAsync(
        string assetReference,
        [FromQuery] Guid? facilityId,
        [FromQuery] int? limit,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenancePredictiveRiskRepository predictiveRiskRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var history = await predictiveRiskRepository.ListAssessmentHistoryAsync(
            context,
            assetReference,
            limit ?? 20,
            cancellationToken);

        return TypedResults.Ok(history.Select(MaintenanceApiMapper.MapPredictiveRiskAssessmentSummary).ToList());
    }

    private static async Task<IResult> EvaluatePredictiveRiskAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenancePredictiveRiskRepository predictiveRiskRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            MaintenancePermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

        if (context is null)
        {
            return failResult!;
        }

        var evaluated = await predictiveRiskRepository.EvaluateFacilityRiskAsync(context, cancellationToken);
        return evaluated
            ? TypedResults.Ok(new { evaluated = true })
            : TypedResults.Ok(new { evaluated = false });
    }

    public static IServiceCollection AddCrevaFactOSMaintenanceCondition(this IServiceCollection services)
    {
        services.AddScoped<IConditionEvaluationService, ConditionEvaluationService>();
        services.AddScoped<IPredictiveRiskEvaluationService, PredictiveRiskEvaluationService>();
        services.AddScoped<IMaintenanceConditionRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            if (dbContext is null)
            {
                return new UnavailableMaintenanceConditionRepository();
            }

            return new MaintenanceConditionRepository(
                dbContext,
                serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                serviceProvider.GetRequiredService<IConditionEvaluationService>());
        });

        services.AddScoped<IMaintenancePredictiveRiskRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            if (dbContext is null)
            {
                return new UnavailableMaintenancePredictiveRiskRepository();
            }

            return new MaintenancePredictiveRiskRepository(
                dbContext,
                serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                serviceProvider.GetRequiredService<IPredictiveRiskEvaluationService>());
        });

        return services;
    }
}
