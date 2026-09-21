using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.RealFactoryPilot;

namespace NevaxisZentra.Api.RealFactoryPilot;

public static class RealFactoryPilotEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapRealFactoryPilotEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/real-factory-pilot").RequireAuthorization();

        group.MapPost("/initialize", InitializeAsync);
        group.MapGet("/runs/{pilotRunId:guid}/workspace", GetWorkspaceAsync);
        group.MapPost("/dev/golden-pilot", RunGoldenPilotAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraRealFactoryPilot(this IServiceCollection services)
    {
        services.AddScoped<IRealFactoryPilotService, RealFactoryPilotService>();
        return services;
    }

    private static async Task<IResult> InitializeAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IRealFactoryPilotService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, RealFactoryPilotPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.InitializePilotAsync(
                access.FacilityId, access.TenantId, access.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.Ok(workspace);
        }
        catch (RealFactoryPilotException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> GetWorkspaceAsync(
        Guid pilotRunId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IRealFactoryPilotService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, RealFactoryPilotPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var workspace = await service.GetWorkspaceAsync(access.FacilityId, pilotRunId, cancellationToken);
            return TypedResults.Ok(workspace);
        }
        catch (RealFactoryPilotException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> RunGoldenPilotAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IRealFactoryPilotService service,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return TypedResults.NotFound();
        }

        var (access, failure) = await ResolveAccessAsync(
            facilityId, RealFactoryPilotPermissionKeys.Run, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var result = await service.RunGoldenPilotAsync(
                access.FacilityId, access.TenantId, access.IdentitySubjectId.ToString(), cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (RealFactoryPilotException ex)
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

    private static IResult MapException(RealFactoryPilotException ex) =>
        ex.FailureCode switch
        {
            RealFactoryPilotFailureCodes.NotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            _ => TypedResults.BadRequest(new { code = ex.FailureCode, message = ex.Message }),
        };
}
