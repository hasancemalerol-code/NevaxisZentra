using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.FactoryCopilot;

namespace NevaxisZentra.Api.FactoryCopilot;

public static class FactoryCopilotEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapFactoryCopilotEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/factory-copilot").RequireAuthorization();

        group.MapPost("/query", QueryAsync);
        group.MapPost("/suggested-questions", GetSuggestedQuestionsAsync);

        return endpoints;
    }

    private static async Task<IResult> QueryAsync(
        [FromBody] FactoryCopilotQueryRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryCopilotService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, FactoryCopilotPermissionKeys.Query, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        try
        {
            var result = await service.QueryAsync(
                access.FacilityId,
                access.TenantId,
                access.IdentitySubjectId.ToString(),
                request,
                cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (FactoryCopilotException ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> GetSuggestedQuestionsAsync(
        [FromBody] FactoryCopilotSuggestedQuestionsRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IFactoryCopilotService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, FactoryCopilotPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null)
        {
            return failure!;
        }

        try
        {
            var result = await service.GetSuggestedQuestionsAsync(
                access.FacilityId,
                request.Context,
                cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (FactoryCopilotException ex)
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

    private static IResult MapException(FactoryCopilotException ex) =>
        ex.FailureCode switch
        {
            FactoryCopilotFailureCodes.ValidationFailed =>
                TypedResults.BadRequest(new { failureCode = ex.FailureCode, message = ex.Message }),
            FactoryCopilotFailureCodes.InsufficientContext =>
                TypedResults.Json(
                    new { failureCode = ex.FailureCode, message = ex.Message },
                    statusCode: StatusCodes.Status422UnprocessableEntity),
            FactoryCopilotFailureCodes.ToolUnauthorized =>
                TypedResults.Json(
                    new { failureCode = ex.FailureCode, message = ex.Message },
                    statusCode: StatusCodes.Status403Forbidden),
            FactoryCopilotFailureCodes.UnsupportedIntent =>
                TypedResults.BadRequest(new { failureCode = ex.FailureCode, message = ex.Message }),
            _ => TypedResults.Conflict(new { failureCode = ex.FailureCode, message = ex.Message }),
        };
}
