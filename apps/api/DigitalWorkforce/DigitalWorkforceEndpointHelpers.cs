using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.DigitalWorkforce;

namespace NevaxisZentra.Api.DigitalWorkforce;

internal static class DigitalWorkforceEndpointHelpers
{
    internal static async Task<(FacilityAccessContext? Access, IResult? Failure)> ResolveAccessAsync(
        Guid? facilityId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, TypedResults.BadRequest(new { code = "facility.missing", message = "facilityId is required." }));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, permissionKey),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (accessResult.Access, null);
    }

    internal static IResult MapException(DigitalWorkforceException ex) =>
        TypedResults.Json(new { code = ex.FailureCode, message = ex.Message }, statusCode: MapStatusCode(ex.FailureCode));

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt("digital-workforce.unauthenticated")),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Permission denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("digital-workforce.permission-denied")),
            _ => TypedResults.Problem(
                title: accessResult.InternalReason,
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(accessResult.FailureCode ?? "digital-workforce.access-denied")),
        };

    private static Dictionary<string, object?> FailureExt(string code) =>
        new() { ["errorCode"] = code };

    internal static object ToHandoffResponse(DigitalWorkHandoff handoff) => new
    {
        handoff.Id,
        handoff.CaseId,
        handoff.FromWorkerId,
        handoff.ToWorkerId,
        handoff.Reason,
        handoff.Dependency,
        handoff.Status,
        handoff.CreatedAtUtc,
        handoff.CompletedAtUtc,
    };

    internal static object ToCaseResponse(DigitalWorkCase workCase) => new
    {
        workCase.Id,
        workCase.Title,
        workCase.Description,
        workCase.Status,
        workCase.CurrentStage,
        workCase.VerificationState,
        workCase.PrimaryWorkerId,
        workCase.RequiresApproval,
        workCase.IsHumanControlled,
        workCase.SourceClassification,
        workCase.StartedAtUtc,
        workCase.ClosedAtUtc,
        workCase.VerifiedAtUtc,
        workCase.Outcome,
        workCase.ConcurrencyToken,
    };

    private static int MapStatusCode(string failureCode) => failureCode switch
    {
        DigitalWorkforceFailureCodes.NotFound => StatusCodes.Status404NotFound,
        DigitalWorkforceFailureCodes.Forbidden or
        DigitalWorkforceFailureCodes.PolicyDenied or
        DigitalWorkforceFailureCodes.AutonomyDenied or
        DigitalWorkforceFailureCodes.KillSwitchActive or
        DigitalWorkforceFailureCodes.ScopeViolation => StatusCodes.Status403Forbidden,
        DigitalWorkforceFailureCodes.ConcurrencyConflict => StatusCodes.Status409Conflict,
        DigitalWorkforceFailureCodes.ValidationFailed => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status422UnprocessableEntity,
    };
}
