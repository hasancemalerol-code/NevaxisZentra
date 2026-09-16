using Microsoft.AspNetCore.Mvc;
using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.MixedProduction;

namespace NevaxisFactOS.Api.MixedProduction;

public static class MixedProductionEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMixedProductionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/mixed-production").RequireAuthorization();

        group.MapGet("/lines", ListLinesAsync);
        group.MapGet("/lines/{lineId:guid}", GetLineDetailAsync);
        group.MapGet("/lines/{lineId:guid}/timeline", GetTimelineAsync);
        group.MapGet("/lines/{lineId:guid}/playback", GetPlaybackAsync);
        group.MapPost("/events", RecordEventAsync);
        group.MapPost("/operator/actions", OperatorActionAsync);
        group.MapPost("/dev/seed-mix-line-01", SeedMixLine01Async);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisFactOSMixedProduction(this IServiceCollection services)
    {
        services.AddScoped<IMixedProductionService, MixedProductionService>();
        return services;
    }

    private static async Task<IResult> ListLinesAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMixedProductionService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, MixedProductionPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var lines = await service.ListLinesAsync(access.FacilityId, cancellationToken);
        return TypedResults.Ok(lines);
    }

    private static async Task<IResult> GetLineDetailAsync(
        Guid lineId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMixedProductionService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, MixedProductionPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var detail = await service.GetLineDetailAsync(access.FacilityId, lineId, cancellationToken);
            return TypedResults.Ok(detail);
        }
        catch (MixedProductionException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> GetTimelineAsync(
        Guid lineId,
        [FromQuery] Guid? facilityId,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        IFacilityAccessResolver facilityAccessResolver,
        IMixedProductionService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, MixedProductionPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var timeline = await service.GetTimelineAsync(access.FacilityId, lineId, fromUtc, toUtc, cancellationToken);
        return TypedResults.Ok(timeline);
    }

    private static async Task<IResult> GetPlaybackAsync(
        Guid lineId,
        [FromQuery] Guid? facilityId,
        [FromQuery] DateTimeOffset fromUtc,
        [FromQuery] DateTimeOffset toUtc,
        IFacilityAccessResolver facilityAccessResolver,
        IMixedProductionService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, MixedProductionPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var playback = await service.GetPlaybackAsync(access.FacilityId, lineId, fromUtc, toUtc, cancellationToken);
        return TypedResults.Ok(playback);
    }

    private static async Task<IResult> RecordEventAsync(
        RecordProductionEventRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMixedProductionService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId ?? request.FacilityId,
            MixedProductionPermissionKeys.Operate,
            facilityAccessResolver,
            cancellationToken);
        if (access is null) return failure!;

        try
        {
            var eventRow = await service.RecordEventAsync(
                request with { FacilityId = access.FacilityId },
                access.TenantId,
                cancellationToken);
            return TypedResults.Ok(eventRow);
        }
        catch (MixedProductionException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> OperatorActionAsync(
        OperatorActionRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMixedProductionService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId ?? request.FacilityId,
            MixedProductionPermissionKeys.Operate,
            facilityAccessResolver,
            cancellationToken);
        if (access is null) return failure!;

        try
        {
            var session = await service.PerformOperatorActionAsync(
                request with { FacilityId = access.FacilityId },
                access.TenantId,
                cancellationToken);
            return TypedResults.Ok(session);
        }
        catch (MixedProductionException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> SeedMixLine01Async(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMixedProductionService service,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return TypedResults.NotFound();
        }

        var (access, failure) = await ResolveAccessAsync(
            facilityId, MixedProductionPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var result = await service.SeedMixLine01Async(access.FacilityId, access.TenantId, cancellationToken);
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

    private static IResult MapException(MixedProductionException ex) =>
        ex.FailureCode switch
        {
            MixedProductionFailureCodes.NotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            MixedProductionFailureCodes.DuplicateEvent => TypedResults.Conflict(new { code = ex.FailureCode, message = ex.Message }),
            _ => TypedResults.BadRequest(new { code = ex.FailureCode, message = ex.Message }),
        };
}
