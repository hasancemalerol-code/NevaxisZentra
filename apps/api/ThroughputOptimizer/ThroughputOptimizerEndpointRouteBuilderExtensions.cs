using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.ThroughputOptimizer;

namespace NevaxisZentra.Api.ThroughputOptimizer;

public static class ThroughputOptimizerEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapThroughputOptimizerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/throughput-optimizer").RequireAuthorization();

        group.MapPost("/lines/{lineId:guid}/observe", ObserveAsync);
        group.MapGet("/executive-summary", GetExecutiveSummaryAsync);
        group.MapGet("/cases/{caseId:guid}/workspace", GetWorkspaceAsync);
        group.MapPost("/cases/{caseId:guid}/advance", AdvanceAsync);
        group.MapPost("/dev/golden-demo", RunGoldenDemoAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraThroughputOptimizer(this IServiceCollection services)
    {
        services.AddScoped<IThroughputOptimizerService, ThroughputOptimizerService>();
        return services;
    }

    private static async Task<IResult> ObserveAsync(
        Guid lineId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IThroughputOptimizerService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ThroughputOptimizerPermissionKeys.Run, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.ObserveLineAsync(
                access.FacilityId, access.TenantId, lineId, access.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.Ok(workspace);
        }
        catch (ThroughputOptimizerException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> GetExecutiveSummaryAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IThroughputOptimizerService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ThroughputOptimizerPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var summary = await service.GetExecutiveSummaryAsync(access.FacilityId, cancellationToken);
        return TypedResults.Ok(summary);
    }

    private static async Task<IResult> GetWorkspaceAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IThroughputOptimizerService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ThroughputOptimizerPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.GetWorkspaceAsync(access.FacilityId, caseId, cancellationToken);
            return TypedResults.Ok(workspace);
        }
        catch (ThroughputOptimizerException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> AdvanceAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IThroughputOptimizerService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ThroughputOptimizerPermissionKeys.Run, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.RunLifecycleStepAsync(
                access.FacilityId, access.TenantId, caseId, access.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.Ok(workspace);
        }
        catch (ThroughputOptimizerException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> RunGoldenDemoAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IThroughputOptimizerService service,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return TypedResults.NotFound();
        }

        var (access, failure) = await ResolveAccessAsync(
            facilityId, ThroughputOptimizerPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var result = await service.RunGoldenDemoAsync(
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

    private static IResult MapException(ThroughputOptimizerException ex) =>
        ex.FailureCode switch
        {
            ThroughputOptimizerFailureCodes.NotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            _ => TypedResults.BadRequest(new { code = ex.FailureCode, message = ex.Message }),
        };
}
