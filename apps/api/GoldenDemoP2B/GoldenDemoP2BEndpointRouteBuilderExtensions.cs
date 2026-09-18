using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.GoldenDemoP1;
using NevaxisZentra.Persistence.GoldenDemoP2B;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Api.GoldenDemoP2B;

public static class GoldenDemoP2BEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapGoldenDemoP2BEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/golden-demo-p2b").RequireAuthorization();
        group.MapGet("/digital-factory-team", GetDigitalFactoryTeamAsync);
        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraGoldenDemoP2BApi(this IServiceCollection services)
    {
        services.AddNevaxisZentraGoldenDemoP2B();
        return services;
    }

    private static async Task<IResult> GetDigitalFactoryTeamAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? machineAssetId,
        IFacilityAccessResolver facilityAccessResolver,
        ITenantContextAccessor tenantContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoP2BService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoP2BEndpointHelpers.ResolveAccessAsync(
            facilityId, GoldenDemoP1PermissionKeys.Read, facilityAccessResolver, cancellationToken);
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
            var snapshot = await tenantDatabaseSession.ExecuteReadAsync(
                ct => service.GetDigitalFactoryTeamSnapshotAsync(
                    tenantContext.Current!.TenantId,
                    access.FacilityId,
                    machineAssetId,
                    ct),
                cancellationToken);
            return TypedResults.Ok(GoldenDemoP2BEndpointHelpers.MapSnapshot(snapshot));
        }
        catch (GoldenDemoP1Exception ex)
        {
            return GoldenDemoP2BEndpointHelpers.MapP1Exception(ex);
        }
    }
}
