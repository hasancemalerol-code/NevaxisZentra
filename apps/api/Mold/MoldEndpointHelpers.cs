using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Mold;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Api.Mold;

internal static class MoldEndpointHelpers
{
    internal static async Task<(MoldOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
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
            new FacilityAccessRequest(facilityId.Value, MoldPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<(MoldOperationContext? Context, IResult? Failure)> ResolveWriteAccessAsync(
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
            new FacilityAccessRequest(facilityId.Value, MoldPermissionKeys.Manage),
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
            new FacilityAccessRequest(facilityId, MoldPermissionKeys.Manage),
            cancellationToken);

        return accessResult.Succeeded && accessResult.Access is not null;
    }

    internal static MoldOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    internal static IResult MapWriteResult(MoldWriteResult result, bool isCreate)
    {
        if (!result.Succeeded)
        {
            return MapMoldFailure(result.FailureCode!, result.FailureReason);
        }

        var dto = new MoldWriteSuccessDto(result.EntityId!.Value, result.Version!.Value, result.IdempotentReplay);

        return isCreate
            ? TypedResults.Created((string?)null, dto)
            : TypedResults.Ok(dto);
    }

    internal static IResult MapMoldFailure(string failureCode, string? reason) =>
        failureCode switch
        {
            MoldFailureCodes.InvalidScope => TypedResults.Problem(
                title: reason ?? "Invalid scope.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MoldApiFailureCodes.InvalidScope)),
            MoldFailureCodes.NotFound => TypedResults.Problem(
                title: reason ?? "Mold not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(MoldApiFailureCodes.NotFound)),
            MoldFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: reason ?? "The record was modified by another user.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MoldApiFailureCodes.ConcurrencyConflict)),
            MoldFailureCodes.InvalidTransition => TypedResults.Problem(
                title: reason ?? "This transition is not allowed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MoldApiFailureCodes.InvalidTransition)),
            MoldFailureCodes.DuplicateCode => TypedResults.Problem(
                title: reason ?? "A mold with this code already exists.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MoldApiFailureCodes.DuplicateCode)),
            MoldFailureCodes.IdempotencyConflict => TypedResults.Problem(
                title: reason ?? "The idempotency key was already used with a different command payload.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MoldApiFailureCodes.IdempotencyConflict)),
            MoldFailureCodes.IdempotencyLegacyUnverifiable => TypedResults.Problem(
                title: reason ?? "The idempotency key belongs to a legacy create that cannot be verified safely.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MoldApiFailureCodes.IdempotencyLegacyUnverifiable)),
            MoldHistoryFailureCodes.HistoryIdempotencyConflict => TypedResults.Problem(
                title: reason ?? "The idempotency key was already used with a different history payload.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MoldApiFailureCodes.HistoryIdempotencyConflict)),
            MoldHistoryFailureCodes.DuplicateRevisionNumber => TypedResults.Problem(
                title: reason ?? "A revision with this number already exists for the mold.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MoldApiFailureCodes.DuplicateRevisionNumber)),
            MoldHistoryFailureCodes.DuplicateTrialNumber => TypedResults.Problem(
                title: reason ?? "A trial with this number already exists for the mold.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MoldApiFailureCodes.DuplicateTrialNumber)),
            MoldHistoryFailureCodes.InvalidMaintenanceState => TypedResults.Problem(
                title: reason ?? "The maintenance record is not in a valid state for this operation.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MoldApiFailureCodes.InvalidMaintenanceState)),
            MoldFailureCodes.ValidationFailed => TypedResults.Problem(
                title: reason ?? "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MoldApiFailureCodes.ValidationFailed)),
            MoldFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: reason ?? "Mold service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(MoldApiFailureCodes.SourceUnavailable)),
            MoldCompatibilityFailureCodes.MachineNotVisible => TypedResults.Problem(
                title: reason ?? "Machine is not visible in this facility.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(MoldApiFailureCodes.MachineNotVisible)),
            _ => TypedResults.Problem(
                title: reason ?? "Request failed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MoldApiFailureCodes.ValidationFailed)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication is required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt(MoldApiFailureCodes.Unauthenticated)),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "You do not have permission for this operation.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(MoldApiFailureCodes.PermissionDenied)),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "You do not have access to this facility.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(MoldApiFailureCodes.FacilityAccessDenied)),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "A facility identifier is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MoldApiFailureCodes.InvalidScope)),
            _ => TypedResults.Problem(
                title: "Access denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(MoldApiFailureCodes.FacilityAccessDenied)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapAccessDenied(string failureCode, string reason) =>
        failureCode == FacilityAccessFailureCodes.FacilityMissing
            ? TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MoldApiFailureCodes.InvalidScope))
            : TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(MoldApiFailureCodes.FacilityAccessDenied));

    internal static Dictionary<string, object?> FailureExt(string failureCode) =>
        new() { ["failureCode"] = failureCode };
}

internal static class MoldApiFailureCodes
{
    public const string InvalidScope = "mold.invalid-scope";
    public const string NotFound = "mold.not-found";
    public const string InvalidTransition = "mold.invalid-transition";
    public const string ConcurrencyConflict = "mold.concurrency-conflict";
    public const string ValidationFailed = "mold.validation-failed";
    public const string DuplicateCode = "mold.duplicate-code";
    public const string IdempotencyConflict = "mold.idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "mold.idempotency-legacy-unverifiable";
    public const string HistoryIdempotencyConflict = "mold.history-idempotency-conflict";
    public const string DuplicateRevisionNumber = "mold.duplicate-revision-number";
    public const string DuplicateTrialNumber = "mold.duplicate-trial-number";
    public const string InvalidMaintenanceState = "mold.invalid-maintenance-state";
    public const string SourceUnavailable = "mold.source-unavailable";
    public const string MachineNotVisible = "mold.machine-not-visible";
    public const string Unauthenticated = "mold.unauthenticated";
    public const string PermissionDenied = "mold.permission-denied";
    public const string FacilityAccessDenied = "mold.facility-access-denied";
}

internal sealed record MoldWriteSuccessDto(Guid Id, uint Version, bool IdempotentReplay);
