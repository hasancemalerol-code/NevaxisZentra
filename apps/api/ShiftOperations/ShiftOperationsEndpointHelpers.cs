using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.ShiftOperations;

namespace NevaxisZentra.Api.ShiftOperations;

internal static class ShiftOperationsEndpointHelpers
{
    internal static async Task<(FacilityAccessContext? Access, IResult? Failure)> ResolveAccessAsync(
        Guid? facilityId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, TypedResults.BadRequest(new { code = "facility-missing", message = "Tesis tanımlayıcısı zorunludur." }));
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

    internal static IResult MapException(ShiftOperationsException ex) =>
        ex.FailureCode switch
        {
            ShiftOperationsFailureCodes.NotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            ShiftOperationsFailureCodes.ConcurrencyConflict => TypedResults.Conflict(new { code = ex.FailureCode, message = ex.Message }),
            ShiftOperationsFailureCodes.SilentDropBlocked => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
            ShiftOperationsFailureCodes.Forbidden => TypedResults.Forbid(),
            _ => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
        };

    internal static object MapShift(OperationalShiftInstance shift) => new
    {
        id = shift.Id,
        facilityId = shift.FacilityId,
        shiftCode = shift.ShiftCode,
        name = shift.Name,
        shiftDate = shift.ShiftDate,
        plannedStartUtc = shift.PlannedStartUtc,
        plannedEndUtc = shift.PlannedEndUtc,
        actualStartUtc = shift.ActualStartUtc,
        actualEndUtc = shift.ActualEndUtc,
        status = shift.Status,
        handoverState = shift.HandoverState,
        supervisorLabel = shift.SupervisorLabel,
        teamLabel = shift.TeamLabel,
        handoverReadinessScore = shift.HandoverReadinessScore,
        concurrencyToken = shift.ConcurrencyToken,
    };

    internal static object MapOpenItem(ShiftOpenItem item) => new
    {
        id = item.Id,
        shiftInstanceId = item.ShiftInstanceId,
        sourceType = item.SourceType,
        sourceId = item.SourceId,
        category = item.Category,
        title = item.Title,
        description = item.Description,
        severity = item.Severity,
        status = item.Status,
        handoverPriority = item.HandoverPriority,
        requiresHandover = item.RequiresHandover,
        verificationRequired = item.VerificationRequired,
        digitalWorkCaseId = item.DigitalWorkCaseId,
        linkedAiTaskId = item.LinkedAiTaskId,
        carryoverCount = item.CarryoverCount,
        carriedFromShiftInstanceId = item.CarriedFromShiftInstanceId,
        createdAtUtc = item.CreatedAtUtc,
        concurrencyToken = item.ConcurrencyToken,
    };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Kimlik doğrulama gerekli.",
                statusCode: StatusCodes.Status401Unauthorized),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Yetki reddedildi.",
                statusCode: StatusCodes.Status403Forbidden),
            _ => TypedResults.Problem(
                title: accessResult.InternalReason,
                statusCode: StatusCodes.Status403Forbidden),
        };
}
