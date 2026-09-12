using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Erp;

namespace NevaxisFactOS.Api.Erp;

internal static class ErpEndpointHelpers
{
    internal static async Task<(ErpOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
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
            new FacilityAccessRequest(facilityId.Value, ErpPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<(ErpOperationContext? Context, IResult? Failure)> ResolveWriteAccessAsync(
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

    internal static ErpOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    internal static IResult MapWriteResult(ErpWriteResult result, bool isCreate)
    {
        if (!result.Succeeded)
        {
            return MapErpFailure(result.FailureCode!, result.FailureReason);
        }

        var dto = new ErpWriteSuccessDto(result.EntityId!.Value, result.Version!.Value);
        return isCreate
            ? TypedResults.Created((string?)null, dto)
            : TypedResults.Ok(dto);
    }

    internal static IResult MapSyncResult(ErpSyncResult result)
    {
        if (!result.Succeeded)
        {
            return MapErpFailure(result.FailureCode!, result.FailureReason);
        }

        return TypedResults.Ok(new ErpSyncSuccessDto(
            result.SyncRunId!.Value,
            result.Status!,
            result.RecordsRead,
            result.RecordsCreated,
            result.RecordsUpdated,
            result.RecordsSkipped,
            result.RecordsFailed));
    }

    internal static Dictionary<string, object?> FailureExt(string failureCode) =>
        new() { ["failureCode"] = failureCode };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapErpFailure(string failureCode, string? reason) =>
        failureCode switch
        {
            ErpFailureCodes.InvalidScope => TypedResults.Problem(
                title: reason ?? "Invalid scope.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ErpApiFailureCodes.InvalidScope)),
            ErpFailureCodes.NotFound => TypedResults.Problem(
                title: reason ?? "Resource not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(ErpApiFailureCodes.NotFound)),
            ErpFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: reason ?? "The record was modified by another user.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ErpApiFailureCodes.ConcurrencyConflict)),
            ErpFailureCodes.SyncAlreadyRunning => TypedResults.Problem(
                title: reason ?? "A sync is already running.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(ErpApiFailureCodes.ConcurrencyConflict)),
            ErpFailureCodes.ValidationFailed => TypedResults.Problem(
                title: reason ?? "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ErpApiFailureCodes.ValidationFailed)),
            ErpFailureCodes.CapabilityUnsupported => TypedResults.Problem(
                title: reason ?? "Capability not supported.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ErpApiFailureCodes.CapabilityUnsupported)),
            ErpFailureCodes.ProviderUnsupported => TypedResults.Problem(
                title: reason ?? "Provider not supported.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ErpApiFailureCodes.ProviderUnsupported)),
            ErpFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: reason ?? "ERP service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(ErpApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: reason ?? "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExt(ErpApiFailureCodes.UnexpectedError)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(
        FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt(ErpApiFailureCodes.Unauthenticated)),
            FacilityAccessFailureCodes.TenantContextMissing
                or FacilityAccessFailureCodes.MembershipMissing => TypedResults.Problem(
                    title: "Authorization denied.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: FailureExt(ErpApiFailureCodes.MembershipMissing)),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ErpApiFailureCodes.InvalidRequest)),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(ErpApiFailureCodes.FacilityAccessDenied)),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(ErpApiFailureCodes.PermissionDenied)),
            FacilityAccessFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Service unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(ErpApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExt(ErpApiFailureCodes.UnexpectedError)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapAccessDenied(
        string facilityFailureCode,
        string message) =>
        facilityFailureCode == FacilityAccessFailureCodes.FacilityMissing
            ? TypedResults.Problem(
                title: message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(ErpApiFailureCodes.InvalidRequest))
            : TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(ErpApiFailureCodes.FacilityAccessDenied));
}

internal static class ErpApiFailureCodes
{
    public const string InvalidRequest = "erp.invalid-request";
    public const string InvalidScope = "erp.invalid-scope";
    public const string NotFound = "erp.not-found";
    public const string ConcurrencyConflict = "erp.concurrency-conflict";
    public const string ValidationFailed = "erp.validation-failed";
    public const string CapabilityUnsupported = "erp.capability-unsupported";
    public const string ProviderUnsupported = "erp.provider-unsupported";
    public const string SourceUnavailable = "erp.source-unavailable";
    public const string Unauthenticated = "erp.unauthenticated";
    public const string MembershipMissing = "erp.membership-missing";
    public const string FacilityAccessDenied = "erp.facility-access-denied";
    public const string PermissionDenied = "erp.permission-denied";
    public const string UnexpectedError = "erp.unexpected-error";
}

internal sealed record ErpWriteSuccessDto(Guid Id, uint Version);

internal sealed record ErpSyncSuccessDto(
    Guid SyncRunId,
    string Status,
    int RecordsRead,
    int RecordsCreated,
    int RecordsUpdated,
    int RecordsSkipped,
    int RecordsFailed);
