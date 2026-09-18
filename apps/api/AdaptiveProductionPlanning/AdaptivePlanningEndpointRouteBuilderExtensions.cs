using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.AdaptiveProductionPlanning;
using NevaxisZentra.Persistence.Authorization;

namespace NevaxisZentra.Api.AdaptiveProductionPlanning;

public static class AdaptivePlanningEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAdaptivePlanningEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/adaptive-planning").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapPost("/build", BuildPlanAsync);
        group.MapPost("/simulate", SimulateAsync);
        group.MapPost("/evaluate-replan", EvaluateReplanAsync);
        group.MapPost("/publish", PublishAsync);
        group.MapPost("/approve", ApproveReplanAsync);
        group.MapPost("/verify-outcome", VerifyOutcomeAsync);
        group.MapPut("/policy", UpdatePolicyAsync);
        group.MapGet("/diff", GetDiffAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraAdaptivePlanning(this IServiceCollection services)
    {
        services.AddScoped<IAdaptivePlanningRepository, AdaptivePlanningRepository>();
        services.AddScoped<IAdaptivePlanningService, AdaptivePlanningService>();
        return services;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAdaptivePlanningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await AdaptivePlanningEndpointHelpers.ResolveAccessAsync(
            facilityId, AdaptivePlanningPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.GetWorkspaceAsync(access.FacilityId, cancellationToken);
            return TypedResults.Ok(AdaptivePlanningEndpointHelpers.MapWorkspace(workspace));
        }
        catch (AdaptivePlanningException ex)
        {
            return AdaptivePlanningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> BuildPlanAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] BuildPlanRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAdaptivePlanningService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await AdaptivePlanningEndpointHelpers.ResolveAccessAsync(
            facilityId, AdaptivePlanningPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var subject = httpContext.User.Identity?.Name ?? "system";
            var result = await service.BuildPlanAsync(access.FacilityId, request, subject, cancellationToken);
            return TypedResults.Ok(new
            {
                version = AdaptivePlanningEndpointHelpers.MapVersion(result.Version),
                operations = result.Operations.Select(AdaptivePlanningEndpointHelpers.MapOperation),
                feasibility = result.Feasibility,
                score = result.Score,
                comparisonLabel = result.ComparisonLabel,
            });
        }
        catch (AdaptivePlanningException ex)
        {
            return AdaptivePlanningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> SimulateAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] SimulatePlanRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAdaptivePlanningService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await AdaptivePlanningEndpointHelpers.ResolveAccessAsync(
            facilityId, AdaptivePlanningPermissionKeys.Simulate, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var subject = httpContext.User.Identity?.Name ?? "system";
            var results = await service.SimulateAsync(access.FacilityId, request, subject, cancellationToken);
            return TypedResults.Ok(results.Select(r => new
            {
                scenario = AdaptivePlanningEndpointHelpers.MapScenario(r.Scenario),
                metrics = r.Metrics,
                comparisonJson = r.ComparisonJson,
            }));
        }
        catch (AdaptivePlanningException ex)
        {
            return AdaptivePlanningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> EvaluateReplanAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] EvaluateReplanRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAdaptivePlanningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await AdaptivePlanningEndpointHelpers.ResolveAccessAsync(
            facilityId, AdaptivePlanningPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var result = await service.EvaluateReplanAsync(access.FacilityId, request, cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (AdaptivePlanningException ex)
        {
            return AdaptivePlanningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> PublishAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] PublishPlanRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAdaptivePlanningService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await AdaptivePlanningEndpointHelpers.ResolveAccessAsync(
            facilityId, AdaptivePlanningPermissionKeys.Publish, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var subject = httpContext.User.Identity?.Name ?? "system";
            var result = await service.PublishAsync(access.FacilityId, request, subject, cancellationToken);
            if (result.ApprovalRequired)
            {
                return TypedResults.UnprocessableEntity(new
                {
                    code = AdaptivePlanningFailureCodes.ApprovalRequired,
                    message = "Yüksek etkili replan onay gerektiriyor.",
                    version = AdaptivePlanningEndpointHelpers.MapVersion(result.Version),
                });
            }

            return TypedResults.Ok(new
            {
                version = AdaptivePlanningEndpointHelpers.MapVersion(result.Version),
                planId = result.Plan.Id,
                autoPublished = result.AutoPublished,
                change = AdaptivePlanningEndpointHelpers.MapChange(result.Change),
            });
        }
        catch (AdaptivePlanningException ex)
        {
            return AdaptivePlanningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ApproveReplanAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] ApproveReplanRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAdaptivePlanningService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await AdaptivePlanningEndpointHelpers.ResolveAccessAsync(
            facilityId, AdaptivePlanningPermissionKeys.Approve, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var subject = httpContext.User.Identity?.Name ?? "system";
            var result = await service.ApproveReplanAsync(access.FacilityId, request, subject, cancellationToken);
            return TypedResults.Ok(new
            {
                version = AdaptivePlanningEndpointHelpers.MapVersion(result.Version),
                planId = result.Plan.Id,
                change = AdaptivePlanningEndpointHelpers.MapChange(result.Change),
            });
        }
        catch (AdaptivePlanningException ex)
        {
            return AdaptivePlanningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> VerifyOutcomeAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid planVersionId,
        IFacilityAccessResolver facilityAccessResolver,
        IAdaptivePlanningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await AdaptivePlanningEndpointHelpers.ResolveAccessAsync(
            facilityId, AdaptivePlanningPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var result = await service.VerifyOutcomeAsync(planVersionId, cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (AdaptivePlanningException ex)
        {
            return AdaptivePlanningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> UpdatePolicyAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateAdaptivePlanningPolicyRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAdaptivePlanningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await AdaptivePlanningEndpointHelpers.ResolveAccessAsync(
            facilityId, AdaptivePlanningPermissionKeys.PolicyManage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var policy = await service.UpdatePolicyAsync(access.FacilityId, request, cancellationToken);
            return TypedResults.Ok(AdaptivePlanningEndpointHelpers.MapPolicy(policy));
        }
        catch (AdaptivePlanningException ex)
        {
            return AdaptivePlanningEndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> GetDiffAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid fromVersionId,
        [FromQuery] Guid toVersionId,
        IFacilityAccessResolver facilityAccessResolver,
        IAdaptivePlanningService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await AdaptivePlanningEndpointHelpers.ResolveAccessAsync(
            facilityId, AdaptivePlanningPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var diff = await service.GetPlanDiffAsync(fromVersionId, toVersionId, cancellationToken);
            return TypedResults.Ok(diff);
        }
        catch (AdaptivePlanningException ex)
        {
            return AdaptivePlanningEndpointHelpers.MapException(ex);
        }
    }
}
