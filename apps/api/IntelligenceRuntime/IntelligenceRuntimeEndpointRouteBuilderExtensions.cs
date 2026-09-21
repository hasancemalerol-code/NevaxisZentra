using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.IntelligenceRuntime;

namespace NevaxisZentra.Api.IntelligenceRuntime;

public static class IntelligenceRuntimeEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapIntelligenceRuntimeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/intelligence-runtime").RequireAuthorization();

        group.MapPost("/route", RouteIntelligenceAsync);
        group.MapGet("/control-center", GetControlCenterAsync);
        group.MapPost("/dataset/build", BuildDatasetAsync);
        group.MapPost("/models/register", RegisterModelAsync);
        group.MapPost("/models/evaluate", EvaluateModelAsync);
        group.MapPost("/models/shadow", RunShadowAsync);
        group.MapPost("/models/promote", PromoteModelAsync);
        group.MapPut("/learning-policy", UpsertLearningPolicyAsync);
        group.MapGet("/openai/validate", ValidateOpenAiAsync);
        group.MapPost("/dev/golden-routing", RunGoldenRoutingAsync);
        group.MapPost("/dev/golden-dataset", RunGoldenDatasetAsync);
        group.MapPost("/dev/golden-promotion", RunGoldenPromotionAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraIntelligenceRuntimeApi(this IServiceCollection services)
    {
        services.AddNevaxisZentraIntelligenceRuntime();
        return services;
    }

    private static async Task<IResult> RouteIntelligenceAsync(
        [FromBody] IntelligenceRouteRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIntelligenceRuntimeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, IntelligenceRuntimePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        try
        {
            var result = await service.RouteIntelligenceAsync(
                access.FacilityId, access.TenantId, request, cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (IntelligenceRuntimeException ex)
        {
            return TypedResults.BadRequest(new { failureState = ex.FailureState, message = ex.Message });
        }
    }

    private static async Task<IResult> GetControlCenterAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIntelligenceRuntimeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, IntelligenceRuntimePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var center = await service.GetControlCenterAsync(access.FacilityId, cancellationToken);
        return TypedResults.Ok(center);
    }

    private static async Task<IResult> BuildDatasetAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIntelligenceRuntimeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, IntelligenceRuntimePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await service.BuildDatasetVersionAsync(access.FacilityId, access.TenantId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> RegisterModelAsync(
        [FromBody] RegisterNevaxisModelRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIntelligenceRuntimeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, IntelligenceRuntimePermissionKeys.Evaluate, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await service.RegisterModelAsync(access.FacilityId, access.TenantId, request, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> EvaluateModelAsync(
        [FromBody] StartModelEvaluationRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIntelligenceRuntimeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, IntelligenceRuntimePermissionKeys.Evaluate, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await service.EvaluateModelAsync(access.FacilityId, access.TenantId, request, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> RunShadowAsync(
        [FromBody] StartModelShadowRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIntelligenceRuntimeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, IntelligenceRuntimePermissionKeys.Evaluate, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await service.RunShadowAsync(access.FacilityId, access.TenantId, request, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> PromoteModelAsync(
        [FromBody] PromoteModelRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIntelligenceRuntimeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, IntelligenceRuntimePermissionKeys.Evaluate, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await service.PromoteModelAsync(access.FacilityId, access.TenantId, request, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> UpsertLearningPolicyAsync(
        [FromBody] UpsertTenantIntelligenceLearningPolicyRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIntelligenceRuntimeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, IntelligenceRuntimePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await service.UpsertLearningPolicyAsync(access.FacilityId, access.TenantId, request, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> ValidateOpenAiAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIntelligenceRuntimeService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, IntelligenceRuntimePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await service.ValidateOpenAiLiveAsync(cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> RunGoldenRoutingAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIntelligenceRuntimeService service,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return TypedResults.NotFound();
        }

        var (access, failure) = await ResolveAccessAsync(
            facilityId, IntelligenceRuntimePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await service.RunGoldenRoutingDemoAsync(access.FacilityId, access.TenantId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> RunGoldenDatasetAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIntelligenceRuntimeService service,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return TypedResults.NotFound();
        }

        var (access, failure) = await ResolveAccessAsync(
            facilityId, IntelligenceRuntimePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await service.RunGoldenDatasetDemoAsync(access.FacilityId, access.TenantId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> RunGoldenPromotionAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IIntelligenceRuntimeService service,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return TypedResults.NotFound();
        }

        var (access, failure) = await ResolveAccessAsync(
            facilityId, IntelligenceRuntimePermissionKeys.Evaluate, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await service.RunGoldenPromotionSimulationAsync(access.FacilityId, access.TenantId, cancellationToken);
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
}
