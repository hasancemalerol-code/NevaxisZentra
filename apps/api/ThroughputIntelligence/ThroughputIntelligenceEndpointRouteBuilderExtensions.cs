using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.ThroughputIntelligence;

namespace NevaxisZentra.Api.ThroughputIntelligence;

public static class ThroughputIntelligenceEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapThroughputIntelligenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/throughput-intelligence").RequireAuthorization();

        group.MapGet("/lines/{lineId:guid}/overview", GetOverviewAsync);
        group.MapPost("/lines/{lineId:guid}/scenarios", RunScenarioAsync);
        group.MapGet("/lines/{lineId:guid}/scenarios/compare", CompareScenariosAsync);
        group.MapPost("/dev/seed-mix-line-01-mp2", SeedMp2ExtensionAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraThroughputIntelligence(this IServiceCollection services)
    {
        services.AddScoped<IThroughputIntelligenceService, ThroughputIntelligenceService>();
        return services;
    }

    private static async Task<IResult> GetOverviewAsync(
        Guid lineId,
        [FromQuery] Guid? facilityId,
        [FromQuery] string? window,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        IFacilityAccessResolver facilityAccessResolver,
        IThroughputIntelligenceService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ThroughputIntelligencePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var overview = await service.GetLineOverviewAsync(
                access.FacilityId, lineId, window, fromUtc, toUtc, cancellationToken);
            return TypedResults.Ok(overview);
        }
        catch (ThroughputIntelligenceException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> RunScenarioAsync(
        Guid lineId,
        RunThroughputScenarioRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IThroughputIntelligenceService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ThroughputIntelligencePermissionKeys.Simulate, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var result = await service.RunScenarioAsync(
                access.FacilityId,
                lineId,
                request,
                access.TenantId,
                access.IdentitySubjectId.ToString(),
                cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (ThroughputIntelligenceException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> CompareScenariosAsync(
        Guid lineId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IThroughputIntelligenceService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ThroughputIntelligencePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var comparison = await service.CompareScenariosAsync(access.FacilityId, lineId, cancellationToken);
        return TypedResults.Ok(comparison);
    }

    private static async Task<IResult> SeedMp2ExtensionAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IThroughputIntelligenceService service,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return TypedResults.NotFound();
        }

        var (access, failure) = await ResolveAccessAsync(
            facilityId, ThroughputIntelligencePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var result = await service.SeedMixLine01Mp2ExtensionAsync(
            access.FacilityId, access.TenantId, cancellationToken);
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

    private static IResult MapException(ThroughputIntelligenceException ex) =>
        ex.FailureCode switch
        {
            ThroughputIntelligenceFailureCodes.NotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            _ => TypedResults.BadRequest(new { code = ex.FailureCode, message = ex.Message }),
        };
}
