using Microsoft.AspNetCore.Mvc;
using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.GoldenDemoP2;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Api.GoldenDemoP2;

public static class GoldenDemoP2EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapGoldenDemoP2Endpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/golden-demo-p2").RequireAuthorization();

        group.MapGet("/presentation-status", GetPresentationStatusAsync);
        group.MapPost("/events/{eventId:guid}/reset", ResetPilotEventAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisFactOSGoldenDemoP2Api(this IServiceCollection services)
    {
        services.AddNevaxisFactOSGoldenDemoP2();
        return services;
    }

    private static async Task<IResult> GetPresentationStatusAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? machineAssetId,
        IFacilityAccessResolver facilityAccessResolver,
        ITenantContextAccessor tenantContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoP2Service service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoP2EndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoP2PermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        if (!tenantContext.IsAvailable)
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var status = await tenantDatabaseSession.ExecuteReadAsync(
                ct => service.GetPresentationStatusAsync(
                    tenantContext.Current!.TenantId,
                    access.FacilityId,
                    machineAssetId,
                    ct),
                cancellationToken);
            return TypedResults.Ok(GoldenDemoP2EndpointHelpers.MapPresentationStatus(status));
        }
        catch (GoldenDemoP2Exception ex)
        {
            return GoldenDemoP2EndpointHelpers.MapException(ex);
        }
    }

    private static async Task<IResult> ResetPilotEventAsync(
        Guid eventId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAuthorizationContext authorizationContext,
        ITenantContextAccessor tenantContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoP2Service service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoP2EndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoP2PermissionKeys.Reset, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        if (!tenantContext.IsAvailable)
        {
            return TypedResults.Unauthorized();
        }

        var subjectId = authorizationContext.Current?.MembershipId.ToString() ?? "system";
        try
        {
            var result = await tenantDatabaseSession.ExecuteWriteAsync(
                ct => service.ResetPilotEventAsync(
                    tenantContext.Current!.TenantId,
                    access.FacilityId,
                    eventId,
                    subjectId,
                    ct),
                cancellationToken);
            return TypedResults.Ok(GoldenDemoP2EndpointHelpers.MapResetResult(result));
        }
        catch (GoldenDemoP2Exception ex)
        {
            return GoldenDemoP2EndpointHelpers.MapException(ex);
        }
    }
}
