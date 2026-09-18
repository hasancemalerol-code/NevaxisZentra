using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.ShiftWorkforce;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Api.ShiftWorkforce;

internal static class ShiftWorkforceEndpointHelpers
{
    internal static async Task<(ShiftWorkforceOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
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
            new FacilityAccessRequest(facilityId.Value, ShiftWorkforcePermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<(ShiftWorkforceOperationContext? Context, IResult? Failure)> ResolveWriteAccessAsync(
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
            new FacilityAccessRequest(facilityId.Value, ShiftWorkforcePermissionKeys.Manage),
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
            new FacilityAccessRequest(facilityId, ShiftWorkforcePermissionKeys.Manage),
            cancellationToken);

        return accessResult.Succeeded && accessResult.Access is not null;
    }

    internal static ShiftWorkforceOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    internal static IResult MapWriteResult(ShiftWorkforceWriteResult result, bool isCreate = false)
    {
        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        var dto = new ShiftWorkforceWriteSuccessDto(result.EntityId!.Value, result.Version!.Value, result.IdempotentReplay);
        return isCreate ? TypedResults.Created((string?)null, dto) : TypedResults.Ok(dto);
    }

    internal static IResult MapFailure(string failureCode, string? reason) =>
        failureCode switch
        {
            ShiftWorkforceFailureCodes.NotFound => TypedResults.Problem(
                title: reason ?? "Record not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(failureCode)),
            ShiftWorkforceFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: reason ?? "The record was modified by another user.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(failureCode)),
            ShiftWorkforceFailureCodes.IdempotencyConflict => TypedResults.Problem(
                title: reason ?? "The idempotency key was already used with a different command payload.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(failureCode)),
            ShiftWorkforceFailureCodes.IdempotencyLegacyUnverifiable => TypedResults.Problem(
                title: reason ?? "The idempotency key belongs to a legacy create that cannot be verified safely.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(failureCode)),
            ShiftWorkforceFailureCodes.PersonInactive => TypedResults.Problem(
                title: reason ?? "The workforce person is inactive.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(failureCode)),
            ShiftWorkforceFailureCodes.CompetencyDefinitionNotFound => TypedResults.Problem(
                title: reason ?? "Competency definition not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(failureCode)),
            ShiftWorkforceFailureCodes.ValidationFailed => TypedResults.Problem(
                title: reason ?? "Validation failed.",
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
                extensions: FailureExt("shift-workforce.unauthenticated")),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "You do not have permission for this operation.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("shift-workforce.permission-denied")),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "You do not have access to this facility.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("shift-workforce.facility-access-denied")),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "A facility identifier is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt("shift-workforce.invalid-scope")),
            _ => TypedResults.Problem(
                title: "Access denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("shift-workforce.facility-access-denied")),
        };

    private static Dictionary<string, object?> FailureExt(string code) =>
        new() { ["failureCode"] = code };
}

internal sealed record ShiftWorkforceWriteSuccessDto(Guid Id, uint Version, bool IdempotentReplay);
