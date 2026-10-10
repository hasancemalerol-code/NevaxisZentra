using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.ScenarioRecovery;

namespace NevaxisZentra.Api.ScenarioRecovery;

public static class ScenarioRecoveryEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapScenarioRecoveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/scenario-recovery").RequireAuthorization();

        group.MapPost("/runs", CreateRunAsync);
        group.MapGet("/runs/{runId:guid}/workspace", GetWorkspaceAsync);
        group.MapPost("/runs/{runId:guid}/generate-options", GenerateOptionsAsync);
        group.MapPost("/runs/{runId:guid}/select-option", SelectOptionAsync);
        group.MapPost("/runs/{runId:guid}/advance", AdvanceAsync);
        group.MapGet("/history", ListHistoryAsync);
        group.MapPost("/lines/{lineId:guid}/simulate-action", SimulateActionAsync);
        group.MapPost("/dev/golden-recovery-demo", RunGoldenRecoveryDemoAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraScenarioRecovery(this IServiceCollection services)
    {
        services.AddScoped<IScenarioRecoveryService, ScenarioRecoveryService>();
        return services;
    }

    private static async Task<IResult> CreateRunAsync(
        [FromBody] CreateScenarioRecoveryRunRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IScenarioRecoveryService service,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ScenarioRecoveryPermissionKeys.Simulate, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.CreateRunAsync(
                access.FacilityId, access.TenantId, request, access.IdentitySubjectId.ToString(),
                new ScenarioRecoveryRunAudit(
                    access.IdentitySubjectId,
                    access.MembershipId,
                    ScenarioRecoveryPermissionKeys.Simulate,
                    correlationContext.IsAvailable ? correlationContext.CorrelationId : Guid.NewGuid(),
                    correlationContext.RequestId),
                cancellationToken);
            return TypedResults.Ok(workspace);
        }
        catch (ScenarioRecoveryException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> GetWorkspaceAsync(
        Guid runId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IScenarioRecoveryService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ScenarioRecoveryPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.GetWorkspaceAsync(access.FacilityId, runId, cancellationToken);
            return TypedResults.Ok(workspace);
        }
        catch (ScenarioRecoveryException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> GenerateOptionsAsync(
        Guid runId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IScenarioRecoveryService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ScenarioRecoveryPermissionKeys.Simulate, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.GenerateOptionsAsync(
                access.FacilityId, access.TenantId, runId, access.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.Ok(workspace);
        }
        catch (ScenarioRecoveryException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> SelectOptionAsync(
        Guid runId,
        [FromBody] SelectScenarioRecoveryOptionRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IScenarioRecoveryService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ScenarioRecoveryPermissionKeys.Execute, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.SelectOptionAsync(
                access.FacilityId, access.TenantId, runId, request, access.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.Ok(workspace);
        }
        catch (ScenarioRecoveryException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> AdvanceAsync(
        Guid runId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IScenarioRecoveryService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ScenarioRecoveryPermissionKeys.Execute, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.AdvanceAsync(
                access.FacilityId, access.TenantId, runId, access.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.Ok(workspace);
        }
        catch (ScenarioRecoveryException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> ListHistoryAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? lineId,
        IFacilityAccessResolver facilityAccessResolver,
        IScenarioRecoveryService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ScenarioRecoveryPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var history = await service.ListHistoryAsync(access.FacilityId, lineId, cancellationToken);
        return TypedResults.Ok(history);
    }

    private static async Task<IResult> SimulateActionAsync(
        Guid lineId,
        [FromBody] SimulateScenarioRecoveryActionRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IScenarioRecoveryService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ScenarioRecoveryPermissionKeys.Simulate, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var snapshot = await service.SimulateActionAsync(
                access.FacilityId, access.TenantId, lineId, request, access.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.Ok(snapshot);
        }
        catch (ScenarioRecoveryException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> RunGoldenRecoveryDemoAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IScenarioRecoveryService service,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return TypedResults.NotFound();
        }

        var (access, failure) = await ResolveAccessAsync(
            facilityId, ScenarioRecoveryPermissionKeys.Execute, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var result = await service.RunGoldenRecoveryDemoAsync(
                access.FacilityId, access.TenantId, access.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (ScenarioRecoveryException ex)
        {
            return MapException(ex);
        }
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

    private static IResult MapException(ScenarioRecoveryException ex) =>
        ex.Code switch
        {
            ScenarioRecoveryFailureCodes.NotFound => TypedResults.NotFound(new { code = ex.Code, message = ex.Message }),
            ScenarioRecoveryFailureCodes.UnauthorizedExecute => TypedResults.Json(
                new { code = ex.Code, message = ex.Message }, statusCode: StatusCodes.Status403Forbidden),
            _ => TypedResults.BadRequest(new { code = ex.Code, message = ex.Message }),
        };
}
