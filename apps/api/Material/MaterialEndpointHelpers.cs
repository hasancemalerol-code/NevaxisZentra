using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Material;
using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Api.Material;

internal static class MaterialEndpointHelpers
{
    internal static async Task<(MaterialOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
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
            new FacilityAccessRequest(facilityId.Value, MaterialPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<(MaterialOperationContext? Context, IResult? Failure)> ResolveWriteAccessAsync(
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
            new FacilityAccessRequest(facilityId.Value, MaterialPermissionKeys.Manage),
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
            new FacilityAccessRequest(facilityId, MaterialPermissionKeys.Manage),
            cancellationToken);

        return accessResult.Succeeded && accessResult.Access is not null;
    }

    internal static MaterialOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    internal static IResult MapWriteResult(MaterialWriteResult result, bool isCreate)
    {
        if (!result.Succeeded)
        {
            return MapMaterialFailure(result.FailureCode!, result.FailureReason);
        }

        var dto = new MaterialWriteSuccessDto(result.EntityId!.Value, result.Version!.Value, result.IdempotentReplay);

        return isCreate
            ? TypedResults.Created((string?)null, dto)
            : TypedResults.Ok(dto);
    }

    internal static IResult MapLotWriteResult(MaterialLotWriteResult result, bool isCreate)
    {
        if (!result.Succeeded)
        {
            return MapMaterialLotFailure(result.FailureCode!, result.FailureReason);
        }

        var dto = new MaterialWriteSuccessDto(result.EntityId!.Value, result.Version!.Value, result.IdempotentReplay);

        return isCreate
            ? TypedResults.Created((string?)null, dto)
            : TypedResults.Ok(dto);
    }

    internal static IResult MapMaterialLotFailure(string failureCode, string? reason) =>
        failureCode switch
        {
            MaterialLotFailureCodes.InvalidScope => TypedResults.Problem(
                title: reason ?? "Invalid scope.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaterialLotApiFailureCodes.InvalidScope)),
            MaterialLotFailureCodes.NotFound => TypedResults.Problem(
                title: reason ?? "Material lot not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(MaterialLotApiFailureCodes.NotFound)),
            MaterialLotFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: reason ?? "The record was modified by another user.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MaterialLotApiFailureCodes.ConcurrencyConflict)),
            MaterialLotFailureCodes.InvalidTransition => TypedResults.Problem(
                title: reason ?? "This lot status transition is not allowed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MaterialLotApiFailureCodes.InvalidTransition)),
            MaterialLotFailureCodes.LotNumberConflict => TypedResults.Problem(
                title: reason ?? "A lot with this number already exists for the material.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MaterialLotApiFailureCodes.LotNumberConflict)),
            MaterialLotFailureCodes.IdempotencyConflict => TypedResults.Problem(
                title: reason ?? "The idempotency key was already used with a different command payload.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MaterialLotApiFailureCodes.IdempotencyConflict)),
            MaterialLotFailureCodes.IdempotencyLegacyUnverifiable => TypedResults.Problem(
                title: reason ?? "The idempotency key belongs to a legacy create that cannot be verified safely.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MaterialLotApiFailureCodes.IdempotencyLegacyUnverifiable)),
            MaterialLotFailureCodes.InvalidLocation => TypedResults.Problem(
                title: reason ?? "Storage location is invalid.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaterialLotApiFailureCodes.InvalidLocation)),
            MaterialLotFailureCodes.InvalidQuantity => TypedResults.Problem(
                title: reason ?? "Quantity is invalid.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaterialLotApiFailureCodes.InvalidQuantity)),
            MaterialLotFailureCodes.ValidationFailed => TypedResults.Problem(
                title: reason ?? "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaterialLotApiFailureCodes.ValidationFailed)),
            MaterialLotFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: reason ?? "Material lot service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(MaterialLotApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: reason ?? "Request failed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaterialLotApiFailureCodes.ValidationFailed)),
        };

    internal static IResult MapMaterialFailure(string failureCode, string? reason) =>
        failureCode switch
        {
            MaterialFailureCodes.InvalidScope => TypedResults.Problem(
                title: reason ?? "Invalid scope.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaterialApiFailureCodes.InvalidScope)),
            MaterialFailureCodes.NotFound => TypedResults.Problem(
                title: reason ?? "Material not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(MaterialApiFailureCodes.NotFound)),
            MaterialFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: reason ?? "The record was modified by another user.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MaterialApiFailureCodes.ConcurrencyConflict)),
            MaterialFailureCodes.InvalidTransition => TypedResults.Problem(
                title: reason ?? "This transition is not allowed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MaterialApiFailureCodes.InvalidTransition)),
            MaterialFailureCodes.DuplicateCode => TypedResults.Problem(
                title: reason ?? "A material with this code already exists.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MaterialApiFailureCodes.CodeConflict)),
            MaterialFailureCodes.IdempotencyConflict => TypedResults.Problem(
                title: reason ?? "The idempotency key was already used with a different command payload.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MaterialApiFailureCodes.IdempotencyConflict)),
            MaterialFailureCodes.IdempotencyLegacyUnverifiable => TypedResults.Problem(
                title: reason ?? "The idempotency key belongs to a legacy create that cannot be verified safely.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MaterialApiFailureCodes.IdempotencyLegacyUnverifiable)),
            MaterialFailureCodes.ValidationFailed => TypedResults.Problem(
                title: reason ?? "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaterialApiFailureCodes.ValidationFailed)),
            MaterialFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: reason ?? "Material service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(MaterialApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: reason ?? "Request failed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaterialApiFailureCodes.ValidationFailed)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(
        FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication is required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt(MaterialApiFailureCodes.Unauthenticated)),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "You do not have permission for this operation.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(MaterialApiFailureCodes.PermissionDenied)),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "You do not have access to this facility.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(MaterialApiFailureCodes.FacilityAccessDenied)),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "A facility identifier is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaterialApiFailureCodes.InvalidScope)),
            _ => TypedResults.Problem(
                title: "Access denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(MaterialApiFailureCodes.FacilityAccessDenied)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapAccessDenied(string failureCode, string reason) =>
        failureCode == FacilityAccessFailureCodes.FacilityMissing
            ? TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaterialApiFailureCodes.InvalidScope))
            : TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(MaterialApiFailureCodes.FacilityAccessDenied));

    internal static Dictionary<string, object?> FailureExt(string failureCode) =>
        new() { ["failureCode"] = failureCode };
}

internal static class MaterialLotApiFailureCodes
{
    public const string InvalidScope = "material.lot.invalid-scope";
    public const string NotFound = "material.lot.not-found";
    public const string InvalidTransition = "material.lot.invalid-transition";
    public const string ConcurrencyConflict = "material.lot.concurrency-conflict";
    public const string ValidationFailed = "material.lot.validation-failed";
    public const string LotNumberConflict = "material.lot.lot-number-conflict";
    public const string IdempotencyConflict = "material.lot.idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "material.lot.idempotency-legacy-unverifiable";
    public const string InvalidLocation = "material.lot.invalid-location";
    public const string InvalidQuantity = "material.lot.invalid-quantity";
    public const string SourceUnavailable = "material.lot.source-unavailable";
}

internal static class MaterialApiFailureCodes
{
    public const string InvalidScope = "material.invalid-scope";
    public const string NotFound = "material.not-found";
    public const string InvalidTransition = "material.invalid-transition";
    public const string ConcurrencyConflict = "material.concurrency-conflict";
    public const string ValidationFailed = "material.validation-failed";
    public const string CodeConflict = "material.code-conflict";
    public const string IdempotencyConflict = "material.idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "material.idempotency-legacy-unverifiable";
    public const string SourceUnavailable = "material.source-unavailable";
    public const string Unauthenticated = "material.unauthenticated";
    public const string PermissionDenied = "material.permission-denied";
    public const string FacilityAccessDenied = "material.facility-access-denied";
}

internal sealed record MaterialWriteSuccessDto(Guid Id, uint Version, bool IdempotentReplay);
