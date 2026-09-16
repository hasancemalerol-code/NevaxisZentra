using Microsoft.AspNetCore.Mvc;
using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.FactoryIntelligence;

namespace NevaxisFactOS.Api.FactoryIntelligence;

public static class FactoryIntelligenceEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapFactoryIntelligenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/factory-intelligence").RequireAuthorization();

        group.MapPost("/evaluate", EvaluateAsync);
        group.MapGet("/overview", GetOverviewAsync);
        group.MapGet("/risks", ListRisksAsync);
        group.MapGet("/risks/{riskId:guid}", GetRiskDetailAsync);
        group.MapGet("/history", GetHistoryAsync);
        group.MapGet("/calibration", GetCalibrationAsync);
        group.MapPost("/risks/{riskId:guid}/evaluate-outcome", EvaluateOutcomeAsync);
        group.MapPost("/dev/simulate-inj006", SimulateInj006Async);

        return endpoints;
    }

    public static IServiceCollection AddNevaxisFactOSFactoryIntelligence(this IServiceCollection services)
    {
        services.AddScoped<IFactoryIntelligenceService, FactoryIntelligenceService>();
        return services;
    }

    private static async Task<IResult> EvaluateAsync(
        FactoryIntelligenceEvaluationRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryIntelligenceService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId ?? request.FacilityId,
            FactoryIntelligencePermissionKeys.Manage,
            facilityAccessResolver,
            cancellationToken);
        if (access is null) return failure!;

        try
        {
            var result = await service.EvaluateAsync(
                request with { FacilityId = access.FacilityId },
                httpContext.User.Identity?.Name ?? "system",
                cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (FactoryIntelligenceException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> GetOverviewAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryIntelligenceService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, FactoryIntelligencePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var overview = await service.GetOverviewAsync(access.FacilityId, cancellationToken);
        return TypedResults.Ok(overview);
    }

    private static async Task<IResult> ListRisksAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryIntelligenceService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, FactoryIntelligencePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var cards = await service.GetEarlyWarningCardsAsync(access.FacilityId, cancellationToken);
        return TypedResults.Ok(cards);
    }

    private static async Task<IResult> GetRiskDetailAsync(
        Guid riskId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryIntelligenceService service,
        CancellationToken cancellationToken)
    {
        _ = await ResolveAccessAsync(null, FactoryIntelligencePermissionKeys.Read, facilityAccessResolver, cancellationToken);

        try
        {
            var detail = await service.GetRiskDetailAsync(riskId, cancellationToken);
            return TypedResults.Ok(detail);
        }
        catch (FactoryIntelligenceException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> GetHistoryAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? riskType,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryIntelligenceService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, FactoryIntelligencePermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var history = await service.GetRiskHistoryAsync(access.FacilityId, riskType, cancellationToken);
        return TypedResults.Ok(history);
    }

    private static async Task<IResult> GetCalibrationAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryIntelligenceService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, FactoryIntelligencePermissionKeys.Calibration, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var calibration = await service.GetCalibrationAsync(access.FacilityId, cancellationToken);
        return TypedResults.Ok(calibration);
    }

    private static async Task<IResult> EvaluateOutcomeAsync(
        Guid riskId,
        FactoryIntelligenceOutcomeEvaluationRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryIntelligenceService service,
        CancellationToken cancellationToken)
    {
        var (_, failure) = await ResolveAccessAsync(
            null, FactoryIntelligencePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (failure is not null) return failure;

        try
        {
            var evaluation = await service.EvaluateOutcomeAsync(
                riskId, request.Outcome, request.InterventionOccurred, request.Notes, cancellationToken);
            return TypedResults.Ok(evaluation);
        }
        catch (FactoryIntelligenceException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> SimulateInj006Async(
        [FromQuery] Guid? facilityId,
        [FromQuery] int step,
        [FromQuery] string? scenario,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryIntelligenceService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, FactoryIntelligencePermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var result = await service.SimulateInj006StepAsync(
            step,
            scenario ?? "gradual-degradation",
            access.FacilityId,
            httpContext.User.Identity?.Name ?? "dev-simulator",
            cancellationToken);
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

    private static IResult MapException(FactoryIntelligenceException ex) =>
        ex.FailureCode switch
        {
            FactoryIntelligenceFailureCodes.NotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            _ => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
        };
}

public sealed record FactoryIntelligenceOutcomeEvaluationRequest(
    string Outcome,
    bool InterventionOccurred,
    string? Notes);
