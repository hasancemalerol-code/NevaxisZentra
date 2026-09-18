using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Quality;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Api.Quality;

internal static class QualityEndpointHelpers
{
    internal static async Task<(QualityOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required."));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, QualityPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<(QualityOperationContext? Context, IResult? Failure)> ResolveWriteAccessAsync(
        Guid? facilityId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required."));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, permissionKey),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static QualityOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    internal static IResult MapWriteResult(QualityWriteResult result, bool isCreate)
    {
        if (!result.Succeeded)
        {
            return MapQualityFailure(result.FailureCode!, result.FailureReason);
        }

        var dto = new QualityWriteSuccessDto(result.EntityId!.Value, result.Version!.Value);

        return isCreate
            ? TypedResults.Created((string?)null, dto)
            : TypedResults.Ok(dto);
    }

    internal static Dictionary<string, object?> FailureExt(string failureCode) =>
        new() { ["failureCode"] = failureCode };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapQualityFailure(string failureCode, string? reason) =>
        failureCode switch
        {
            QualityFailureCodes.InvalidScope => TypedResults.Problem(
                title: reason ?? "Invalid scope.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(QualityApiFailureCodes.InvalidScope)),
            QualityFailureCodes.NotFound => TypedResults.Problem(
                title: reason ?? "Resource not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(QualityApiFailureCodes.NotFound)),
            QualityFailureCodes.ProductionOrderNotFound => TypedResults.Problem(
                title: reason ?? "Production order not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(QualityApiFailureCodes.ProductionOrderNotFound)),
            QualityFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: reason ?? "The record was modified by another user.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(QualityApiFailureCodes.ConcurrencyConflict)),
            QualityFailureCodes.InvalidTransition => TypedResults.Problem(
                title: reason ?? "This transition is not allowed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(QualityApiFailureCodes.InvalidTransition)),
            QualityFailureCodes.PlanNotActive => TypedResults.Problem(
                title: reason ?? "Quality control plan is not active.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(QualityApiFailureCodes.PlanNotActive)),
            QualityFailureCodes.ReleaseNotAllowed => TypedResults.Problem(
                title: reason ?? "Release is not allowed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(QualityApiFailureCodes.ReleaseNotAllowed)),
            QualityFailureCodes.BlockingQualityHold => TypedResults.Problem(
                title: reason ?? "Quality hold blocks release.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(QualityApiFailureCodes.BlockingQualityHold)),
            QualityNcrFailureCodes.DuplicateNonconformance => TypedResults.Problem(
                title: reason ?? "Duplicate nonconformance.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(QualityApiFailureCodes.DuplicateNonconformance)),
            QualityNcrFailureCodes.CloseNotAllowed or QualityNcrFailureCodes.ResolveNotAllowed => TypedResults.Problem(
                title: reason ?? "Operation not allowed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(QualityApiFailureCodes.CloseNotAllowed)),
            QualityFailureCodes.ValidationFailed => TypedResults.Problem(
                title: reason ?? "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(QualityApiFailureCodes.ValidationFailed)),
            QualityFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: reason ?? "Quality service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(QualityApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: reason ?? "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExt(QualityApiFailureCodes.UnexpectedError)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt(QualityApiFailureCodes.Unauthenticated)),
            FacilityAccessFailureCodes.TenantContextMissing
                or FacilityAccessFailureCodes.MembershipMissing => TypedResults.Problem(
                    title: "Authorization denied.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: FailureExt(QualityApiFailureCodes.MembershipMissing)),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(QualityApiFailureCodes.InvalidRequest)),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(QualityApiFailureCodes.FacilityAccessDenied)),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(QualityApiFailureCodes.PermissionDenied)),
            FacilityAccessFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Service unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(QualityApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExt(QualityApiFailureCodes.UnexpectedError)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapAccessDenied(string facilityFailureCode, string message) =>
        facilityFailureCode == FacilityAccessFailureCodes.FacilityMissing
            ? TypedResults.Problem(
                title: message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(QualityApiFailureCodes.InvalidRequest))
            : TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(QualityApiFailureCodes.FacilityAccessDenied));
}
