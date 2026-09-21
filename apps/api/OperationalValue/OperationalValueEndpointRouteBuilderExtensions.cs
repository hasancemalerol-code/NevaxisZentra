using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.OperationalValue;

namespace NevaxisZentra.Api.OperationalValue;

public static class OperationalValueEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapOperationalValueEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/operational-value").RequireAuthorization();

        group.MapGet("/control-center", GetControlCenterAsync);
        group.MapGet("/cases/{valueRecordId:guid}", GetCaseDetailAsync);
        group.MapGet("/economic-inputs", GetEconomicInputsAsync);
        group.MapPut("/economic-inputs", UpsertEconomicInputsAsync);
        group.MapGet("/report-preview", GetReportPreviewAsync);
        group.MapPost("/cases/{valueRecordId:guid}/invalidate", InvalidateAsync);
        group.MapPost("/dev/golden-value", RunGoldenValueAsync);
        group.MapPost("/dev/golden-double-count", RunGoldenDoubleCountAsync);
        group.MapPost("/dev/golden-unverified", RunGoldenUnverifiedAsync);
        group.MapPost("/dev/golden-invalidation", RunGoldenInvalidationAsync);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisZentraOperationalValueApi(this IServiceCollection services)
    {
        services.AddNevaxisZentraOperationalValue();
        return services;
    }

    private static async Task<IResult> GetControlCenterAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalValueService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, OperationalValuePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var center = await service.GetControlCenterAsync(access.FacilityId, cancellationToken);
        return TypedResults.Ok(center);
    }

    private static async Task<IResult> GetCaseDetailAsync(
        Guid valueRecordId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalValueService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, OperationalValuePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var detail = await service.GetCaseDetailAsync(access.FacilityId, valueRecordId, cancellationToken);
        return detail is null ? TypedResults.NotFound() : TypedResults.Ok(detail);
    }

    private static async Task<IResult> GetEconomicInputsAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalValueService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, OperationalValuePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var inputs = await service.GetEconomicInputsAsync(access.FacilityId, cancellationToken);
        return TypedResults.Ok(inputs);
    }

    private static async Task<IResult> UpsertEconomicInputsAsync(
        [FromBody] UpsertTenantEconomicInputRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalValueService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, OperationalValuePermissionKeys.ManageEconomicInputs, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var inputs = await service.UpsertEconomicInputsAsync(access.FacilityId, access.TenantId, request, cancellationToken);
        return TypedResults.Ok(inputs);
    }

    private static async Task<IResult> GetReportPreviewAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalValueService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, OperationalValuePermissionKeys.Export, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var report = await service.GetCustomerReportPreviewAsync(access.FacilityId, cancellationToken);
        return TypedResults.Ok(report);
    }

    private static async Task<IResult> InvalidateAsync(
        Guid valueRecordId,
        [FromQuery] Guid? facilityId,
        [FromQuery] string? reason,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalValueService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, OperationalValuePermissionKeys.ManageEconomicInputs, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await service.InvalidateValueAsync(
            access.FacilityId,
            valueRecordId,
            reason ?? "manual-invalidation",
            cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> RunGoldenValueAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalValueService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, OperationalValuePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await service.RunGoldenValueDemoAsync(access.FacilityId, access.TenantId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> RunGoldenDoubleCountAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalValueService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, OperationalValuePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await service.RunGoldenDoubleCountDemoAsync(access.FacilityId, access.TenantId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> RunGoldenUnverifiedAsync(
        IOperationalValueService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RunGoldenUnverifiedDemoAsync(cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> RunGoldenInvalidationAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalValueService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, OperationalValuePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        var result = await service.RunGoldenInvalidationDemoAsync(access.FacilityId, access.TenantId, cancellationToken);
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
