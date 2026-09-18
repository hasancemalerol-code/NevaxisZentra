using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.GoldenDemoP1;
using NevaxisZentra.Persistence.GoldenDemoP3;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Api.GoldenDemoP3;

public static class GoldenDemoP3EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapGoldenDemoP3Endpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/golden-demo-p3").RequireAuthorization();
        group.MapGet("/outcome", GetOutcomeAsync);
        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraGoldenDemoP3Api(this IServiceCollection services)
    {
        services.AddNevaxisZentraGoldenDemoP3();
        return services;
    }

    private static async Task<IResult> GetOutcomeAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? machineAssetId,
        IFacilityAccessResolver facilityAccessResolver,
        ITenantContextAccessor tenantContext,
        [FromServices] TenantDatabaseSession tenantDatabaseSession,
        IGoldenDemoP3Service service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await GoldenDemoP3EndpointHelpers.ResolveAccessAsync(
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
                ct => service.GetOutcomeAsync(
                    tenantContext.Current!.TenantId,
                    access.FacilityId,
                    machineAssetId,
                    ct),
                cancellationToken);
            return TypedResults.Ok(GoldenDemoP3EndpointHelpers.MapOutcome(snapshot));
        }
        catch (GoldenDemoP1Exception ex)
        {
            return GoldenDemoP3EndpointHelpers.MapP1Exception(ex);
        }
    }
}
