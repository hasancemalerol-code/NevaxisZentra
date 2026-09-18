using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Edge;

namespace NevaxisZentra.Api.Edge;

internal static class EdgeEndpointHelpers
{
    internal static async Task<(EdgeOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        await ResolveAccessAsync(facilityId, EdgePermissionKeys.Read, facilityAccessResolver, correlationContext, cancellationToken);

    internal static async Task<(EdgeOperationContext? Context, IResult? Failure)> ResolveIngestAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        await ResolveAccessAsync(facilityId, EdgePermissionKeys.Ingest, facilityAccessResolver, correlationContext, cancellationToken);

    private static async Task<(EdgeOperationContext? Context, IResult? Failure)> ResolveAccessAsync(
        Guid? facilityId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, TypedResults.Problem(
                title: "A facility identifier is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(EdgeApiFailureCodes.InvalidRequest)));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, permissionKey),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        var access = accessResult.Access;
        return (new EdgeOperationContext(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null), null);
    }

    internal static Dictionary<string, object?> FailureExt(string failureCode) =>
        new() { ["failureCode"] = failureCode };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(
        FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt(EdgeApiFailureCodes.Unauthenticated)),
            FacilityAccessFailureCodes.TenantContextMissing
                or FacilityAccessFailureCodes.MembershipMissing => TypedResults.Problem(
                    title: "Authorization denied.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: FailureExt(EdgeApiFailureCodes.MembershipMissing)),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(EdgeApiFailureCodes.InvalidRequest)),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(EdgeApiFailureCodes.FacilityAccessDenied)),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(EdgeApiFailureCodes.PermissionDenied)),
            FacilityAccessFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Service unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(EdgeApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExt(EdgeApiFailureCodes.UnexpectedError)),
        };
}

internal static class EdgeApiFailureCodes
{
    public const string InvalidRequest = "edge.invalid-request";
    public const string NotFound = "edge.not-found";
    public const string Unauthenticated = "edge.unauthenticated";
    public const string MembershipMissing = "edge.membership-missing";
    public const string FacilityAccessDenied = "edge.facility-access-denied";
    public const string PermissionDenied = "edge.permission-denied";
    public const string SourceUnavailable = "edge.source-unavailable";
    public const string UnexpectedError = "edge.unexpected-error";
}
