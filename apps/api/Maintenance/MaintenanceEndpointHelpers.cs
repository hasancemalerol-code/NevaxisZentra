using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Maintenance;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Api.Maintenance;

internal static class MaintenanceEndpointHelpers
{
    internal static async Task<(MaintenanceOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
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
            new FacilityAccessRequest(facilityId.Value, MaintenancePermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<(MaintenanceOperationContext? Context, IResult? Failure)> ResolveWriteAccessAsync(
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

    internal static async Task<(MaintenanceOperationContext? Context, IResult? Failure)> ResolveExecuteAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        await ResolveWriteAccessAsync(
            facilityId,
            MaintenancePermissionKeys.Execute,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

    internal static MaintenanceOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    internal static IResult MapWriteResult(MaintenanceWriteResult result, bool isCreate)
    {
        if (!result.Succeeded)
        {
            return MapMaintenanceFailure(result.FailureCode!, result.FailureReason);
        }

        var dto = new MaintenanceWriteSuccessDto(result.EntityId!.Value, result.Version!.Value);

        return isCreate
            ? TypedResults.Created((string?)null, dto)
            : TypedResults.Ok(dto);
    }

    internal static Dictionary<string, object?> FailureExt(string failureCode) =>
        new() { ["failureCode"] = failureCode };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapMaintenanceFailure(string failureCode, string? reason) =>
        failureCode switch
        {
            MaintenanceFailureCodes.InvalidScope => TypedResults.Problem(
                title: reason ?? "Invalid scope.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaintenanceApiFailureCodes.InvalidScope)),
            MaintenanceFailureCodes.NotFound => TypedResults.Problem(
                title: reason ?? "Resource not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(MaintenanceApiFailureCodes.NotFound)),
            MaintenanceFailureCodes.ProductionOrderNotFound => TypedResults.Problem(
                title: reason ?? "Production order not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(MaintenanceApiFailureCodes.ProductionOrderNotFound)),
            MaintenanceFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: reason ?? "The record was modified by another user.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MaintenanceApiFailureCodes.ConcurrencyConflict)),
            MaintenanceFailureCodes.InvalidTransition => TypedResults.Problem(
                title: reason ?? "This transition is not allowed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaintenanceApiFailureCodes.InvalidTransition)),
            MaintenanceFailureCodes.ValidationFailed => TypedResults.Problem(
                title: reason ?? "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaintenanceApiFailureCodes.ValidationFailed)),
            MaintenanceFailureCodes.CompletionNoteRequired => TypedResults.Problem(
                title: reason ?? "Completion note is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaintenanceApiFailureCodes.CompletionNoteRequired)),
            MaintenanceFailureCodes.DuplicateDue => TypedResults.Problem(
                title: reason ?? "An open preventive due already exists.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(MaintenanceApiFailureCodes.ConcurrencyConflict)),
            MaintenanceFailureCodes.WorkOrderClosed => TypedResults.Problem(
                title: reason ?? "Work order is closed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaintenanceApiFailureCodes.InvalidTransition)),
            MaintenanceFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: reason ?? "Maintenance service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(MaintenanceApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: reason ?? "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExt(MaintenanceApiFailureCodes.UnexpectedError)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt(MaintenanceApiFailureCodes.Unauthenticated)),
            FacilityAccessFailureCodes.TenantContextMissing
                or FacilityAccessFailureCodes.MembershipMissing => TypedResults.Problem(
                    title: "Authorization denied.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: FailureExt(MaintenanceApiFailureCodes.MembershipMissing)),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaintenanceApiFailureCodes.InvalidRequest)),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(MaintenanceApiFailureCodes.FacilityAccessDenied)),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(MaintenanceApiFailureCodes.PermissionDenied)),
            FacilityAccessFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Service unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExt(MaintenanceApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExt(MaintenanceApiFailureCodes.UnexpectedError)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapAccessDenied(string facilityFailureCode, string message) =>
        facilityFailureCode == FacilityAccessFailureCodes.FacilityMissing
            ? TypedResults.Problem(
                title: message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(MaintenanceApiFailureCodes.InvalidRequest))
            : TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(MaintenanceApiFailureCodes.FacilityAccessDenied));
}

internal static class MaintenanceApiFailureCodes
{
    public const string InvalidRequest = "maintenance.invalid-request";
    public const string InvalidScope = "maintenance.invalid-scope";
    public const string NotFound = "maintenance.not-found";
    public const string ProductionOrderNotFound = "maintenance.production-order-not-found";
    public const string ConcurrencyConflict = "maintenance.concurrency-conflict";
    public const string InvalidTransition = "maintenance.invalid-transition";
    public const string ValidationFailed = "maintenance.validation-failed";
    public const string CompletionNoteRequired = "maintenance.completion-note-required";
    public const string SourceUnavailable = "maintenance.source-unavailable";
    public const string Unauthenticated = "maintenance.unauthenticated";
    public const string MembershipMissing = "maintenance.membership-missing";
    public const string FacilityAccessDenied = "maintenance.facility-access-denied";
    public const string PermissionDenied = "maintenance.permission-denied";
    public const string UnexpectedError = "maintenance.unexpected-error";
}

internal sealed record MaintenanceWriteSuccessDto(Guid Id, uint Version);
