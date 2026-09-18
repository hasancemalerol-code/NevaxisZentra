using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.ShiftAssignment;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Api.ShiftAssignment;

internal static class ShiftAssignmentEndpointHelpers
{
    internal static async Task<(ShiftAssignmentOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
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
            new FacilityAccessRequest(facilityId.Value, ShiftAssignmentPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<(ShiftAssignmentOperationContext? Context, IResult? Failure)> ResolveWriteAccessAsync(
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
            new FacilityAccessRequest(facilityId.Value, ShiftAssignmentPermissionKeys.Manage),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<bool> HasManageAccessAsync(
        Guid facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId, ShiftAssignmentPermissionKeys.Manage),
            cancellationToken);

        return accessResult.Succeeded && accessResult.Access is not null;
    }

    internal static ShiftAssignmentOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    internal static IResult MapWriteResult(ShiftAssignmentWriteResult result, bool isCreate = false)
    {
        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        var dto = new ShiftAssignmentWriteSuccessDto(result.EntityId!.Value, result.Version!.Value, result.IdempotentReplay);
        return isCreate ? TypedResults.Created((string?)null, dto) : TypedResults.Ok(dto);
    }

    internal static IResult MapFailure(string failureCode, string? reason) =>
        failureCode switch
        {
            ShiftAssignmentFailureCodes.NotFound => TypedResults.Problem(
                title: reason ?? "Record not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(failureCode)),
            ShiftAssignmentFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: reason ?? "The record was modified by another user.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(failureCode)),
            ShiftAssignmentFailureCodes.IdempotencyConflict => TypedResults.Problem(
                title: reason ?? "The idempotency key was already used with a different command payload.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(failureCode)),
            ShiftAssignmentFailureCodes.IdempotencyLegacyUnverifiable => TypedResults.Problem(
                title: reason ?? "The idempotency key belongs to a legacy create that cannot be verified safely.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(failureCode)),
            ShiftAssignmentFailureCodes.EligibilityBlocked => TypedResults.Problem(
                title: reason ?? "Eligibility outcome blocks this operation.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(failureCode)),
            ShiftAssignmentFailureCodes.OverlapConflict => TypedResults.Problem(
                title: reason ?? "Approved assignment overlap detected.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(failureCode)),
            ShiftAssignmentFailureCodes.ValidationFailed => TypedResults.Problem(
                title: reason ?? "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(failureCode)),
            ShiftAssignmentFailureCodes.AssessmentMismatch => TypedResults.Problem(
                title: reason ?? "Eligibility assessment mismatch.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(failureCode)),
            ShiftAssignmentFailureCodes.PersonNotFound => TypedResults.Problem(
                title: reason ?? "Workforce person not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(failureCode)),
            ShiftAssignmentFailureCodes.InvalidStatus => TypedResults.Problem(
                title: reason ?? "Invalid assignment status for operation.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(failureCode)),
            _ => TypedResults.Problem(
                title: reason ?? "Request failed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(failureCode)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapAccessDenied(string code, string reason) =>
        TypedResults.Problem(
            title: reason,
            statusCode: StatusCodes.Status403Forbidden,
            extensions: FailureExt(code));

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication is required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt("shift-assign.unauthenticated")),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "You do not have permission for this operation.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("shift-assign.permission-denied")),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "You do not have access to this facility.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("shift-assign.facility-access-denied")),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "A facility identifier is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt("shift-assign.invalid-scope")),
            _ => TypedResults.Problem(
                title: "Access denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("shift-assign.facility-access-denied")),
        };

    private static Dictionary<string, object?> FailureExt(string code) =>
        new() { ["failureCode"] = code };
}

internal sealed record ShiftAssignmentWriteSuccessDto(Guid Id, uint Version, bool IdempotentReplay);
