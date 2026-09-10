using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Integration;

namespace CrevaFactOS.Api.Integration;

internal static class IntegrationEndpointHelpers
{
    internal static async Task<(IntegrationOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required."));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, IntegrationPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access), null);
    }

    internal static async Task<(IntegrationOperationContext? Context, IResult? Failure)> ResolveWriteAccessAsync(
        Guid? facilityId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
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

        return (BuildContext(accessResult.Access), null);
    }

    internal static IntegrationOperationContext BuildContext(
        FacilityAccessContext access,
        Guid? correlationId = null) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationId);

    internal static IntegrationOperationContext WithCorrelation(
        IntegrationOperationContext context,
        CorrelationContext correlationContext) =>
        context with
        {
            CorrelationId = correlationContext.IsAvailable
                ? correlationContext.CorrelationId
                : context.CorrelationId,
        };

    internal static IResult MapWriteResult(IntegrationWriteResult result, bool isCreate)
    {
        if (!result.Success)
        {
            return MapFailure(result.ErrorCode!, result.ErrorMessage!);
        }

        return isCreate
            ? TypedResults.Created($"/api/integration/connectors/{result.ConnectorId}", new { id = result.ConnectorId })
            : TypedResults.Ok(new { id = result.ConnectorId });
    }

    internal static IResult MapFailure(string errorCode, string message) =>
        errorCode switch
        {
            IntegrationFailureCodes.NotFound => TypedResults.Problem(
                title: message,
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt("integration.not-found")),
            IntegrationFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: message,
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt("integration.concurrency-conflict")),
            IntegrationFailureCodes.ValidationFailed => TypedResults.Problem(
                title: message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt("integration.validation-failed")),
            _ => TypedResults.Problem(
                title: message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(errorCode)),
        };

    internal static Dictionary<string, object?> FailureExt(string code) =>
        new() { ["errorCode"] = code };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapAccessDenied(string code, string message) =>
        TypedResults.Problem(
            title: message,
            statusCode: StatusCodes.Status403Forbidden,
            extensions: FailureExt(code));

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(
        FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt("integration.unauthenticated")),
            FacilityAccessFailureCodes.TenantContextMissing
                or FacilityAccessFailureCodes.MembershipMissing => TypedResults.Problem(
                    title: "Authorization denied.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: FailureExt("integration.membership-missing")),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt("integration.invalid-request")),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("integration.facility-access-denied")),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("integration.permission-denied")),
            FacilityAccessFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Service unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt("integration.source-unavailable")),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExt("integration.unexpected-error")),
        };
}
