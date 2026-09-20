using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.ContinuousOptimization;

namespace NevaxisZentra.Api.ContinuousOptimization;

public static class ContinuousOptimizationEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapContinuousOptimizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/continuous-optimization").RequireAuthorization();

        group.MapPost("/triggers/evaluate", EvaluateTriggerAsync);
        group.MapGet("/control-center", GetControlCenterAsync);
        group.MapGet("/opportunities", GetOpportunitiesAsync);
        group.MapGet("/runs/{runId:guid}", GetRunAsync);
        group.MapGet("/runs/{runId:guid}/explainability", GetExplainabilityAsync);
        group.MapGet("/history/{entityType}/{entityId}", GetHistoryAsync);
        group.MapPut("/policies", UpsertPolicyAsync);
        group.MapPost("/dev/golden-continuous", RunGoldenContinuousAsync);
        group.MapPost("/dev/golden-no-op", RunGoldenNoOpAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraContinuousOptimizationApi(this IServiceCollection services)
    {
        services.AddNevaxisZentraContinuousOptimization();
        return services;
    }

    private static async Task<IResult> EvaluateTriggerAsync(
        [FromBody] ContinuousOptimizationTriggerRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IContinuousOptimizationService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContinuousOptimizationPermissionKeys.Run, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        try
        {
            var result = await service.EvaluateTriggerAsync(
                access.FacilityId,
                access.TenantId,
                access.IdentitySubjectId.ToString(),
                request,
                cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (ContinuousOptimizationException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> GetControlCenterAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IContinuousOptimizationService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContinuousOptimizationPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var center = await service.GetControlCenterAsync(access.FacilityId, cancellationToken);
        return TypedResults.Ok(center);
    }

    private static async Task<IResult> GetOpportunitiesAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? lineId,
        IFacilityAccessResolver facilityAccessResolver,
        IContinuousOptimizationService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContinuousOptimizationPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var opportunities = await service.GetActiveOpportunitiesAsync(access.FacilityId, lineId, cancellationToken);
        return TypedResults.Ok(opportunities);
    }

    private static async Task<IResult> GetRunAsync(
        Guid runId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IContinuousOptimizationService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContinuousOptimizationPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var run = await service.GetRunAsync(access.FacilityId, runId, cancellationToken);
        return run is null ? TypedResults.NotFound() : TypedResults.Ok(run);
    }

    private static async Task<IResult> GetExplainabilityAsync(
        Guid runId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IContinuousOptimizationService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContinuousOptimizationPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var explainability = await service.GetExplainabilityAsync(access.FacilityId, runId, cancellationToken);
        return explainability is null ? TypedResults.NotFound() : TypedResults.Ok(explainability);
    }

    private static async Task<IResult> GetHistoryAsync(
        string entityType,
        string entityId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IContinuousOptimizationService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContinuousOptimizationPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var history = await service.GetHistoryAsync(access.FacilityId, entityType, entityId, cancellationToken);
        return TypedResults.Ok(history);
    }

    private static async Task<IResult> UpsertPolicyAsync(
        [FromBody] UpsertContinuousOptimizationPolicyRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IContinuousOptimizationService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContinuousOptimizationPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var policy = await service.UpsertPolicyAsync(
            access.FacilityId, access.TenantId, request, cancellationToken);
        return TypedResults.Ok(policy);
    }

    private static async Task<IResult> RunGoldenContinuousAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IContinuousOptimizationService service,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return TypedResults.NotFound();
        }

        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContinuousOptimizationPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        try
        {
            var result = await service.RunGoldenContinuousDemoAsync(
                access.FacilityId, access.TenantId, access.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (ContinuousOptimizationException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> RunGoldenNoOpAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IContinuousOptimizationService service,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return TypedResults.NotFound();
        }

        var (access, failure) = await ResolveAccessAsync(
            facilityId, ContinuousOptimizationPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await service.RunGoldenNoOpDemoAsync(
            access.FacilityId, access.TenantId, access.IdentitySubjectId.ToString(), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<(FacilityAccessContext? Access, IResult? Failure)> ResolveAccessAsync(
        Guid? facilityId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, TypedResults.BadRequest(new { code = "facility-missing", message = "Tesis tanımlayıcısı zorunludur." }));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, permissionKey),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, TypedResults.Forbid());
        }

        return (accessResult.Access, null);
    }

    private static IResult MapException(ContinuousOptimizationException ex) =>
        ex.FailureCode switch
        {
            ContinuousOptimizationFailureCodes.NotFound =>
                TypedResults.NotFound(new { failureCode = ex.FailureCode, message = ex.Message }),
            ContinuousOptimizationFailureCodes.InsufficientContext =>
                TypedResults.Json(
                    new { failureCode = ex.FailureCode, message = ex.Message },
                    statusCode: StatusCodes.Status422UnprocessableEntity),
            _ => TypedResults.BadRequest(new { failureCode = ex.FailureCode, message = ex.Message }),
        };
}
