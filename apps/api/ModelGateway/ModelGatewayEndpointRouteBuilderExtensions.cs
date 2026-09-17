using Microsoft.AspNetCore.Mvc;
using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.ModelGateway;

namespace NevaxisFactOS.Api.ModelGateway;

public static class ModelGatewayEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapModelGatewayEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/model-gateway").RequireAuthorization();

        group.MapPost("/invoke", InvokeAsync);
        group.MapGet("/audit", ListAuditAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisFactOSModelGateway(this IServiceCollection services)
    {
        services.AddScoped<IModelProvider, DeterministicEngineProvider>();
        services.AddScoped<IModelProvider, LocalModelProvider>();
        services.AddScoped<IModelProvider, FrontierModelProvider>();
        services.AddScoped<IModelGatewayService, ModelGatewayService>();
        return services;
    }

    private static async Task<IResult> InvokeAsync(
        ModelGatewayRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IModelGatewayService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ModelGatewayPermissionKeys.Route, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var response = await service.InvokeAsync(
                access.FacilityId, access.TenantId,
                request with { TenantId = access.TenantId, FacilityId = access.FacilityId },
                cancellationToken);
            return TypedResults.Ok(response);
        }
        catch (ModelGatewayException ex)
        {
            return TypedResults.BadRequest(new { code = ex.FailureCode, message = ex.Message });
        }
    }

    private static async Task<IResult> ListAuditAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? digitalWorkCaseId,
        [FromQuery] int take,
        IFacilityAccessResolver facilityAccessResolver,
        IModelGatewayService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, ModelGatewayPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var records = await service.ListAuditRecordsAsync(
            access.FacilityId, digitalWorkCaseId, take <= 0 ? 20 : Math.Min(take, 100), cancellationToken);
        return TypedResults.Ok(records);
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
