using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Api.PresentationMode;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.GoldenDemoP2;

namespace NevaxisZentra.Api.GoldenDemoP2;

internal static class GoldenDemoP2EndpointHelpers
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

        var presentationFailure = await PresentationModeEndpointHelpers.ResolvePresentationAccessFailureAsync(
            facilityId, facilityAccessResolver, cancellationToken);
        if (presentationFailure is not null)
        {
            return (null, presentationFailure);
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

    internal static IResult MapException(GoldenDemoP2Exception ex) =>
        ex.FailureCode switch
        {
            GoldenDemoP2FailureCodes.EventNotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            GoldenDemoP2FailureCodes.Unauthorized => TypedResults.Forbid(),
            _ => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
        };

    internal static object MapPresentationStatus(GoldenDemoPresentationStatus status) => new
    {
        productionEventId = status.ProductionEventId,
        machineCode = status.MachineCode,
        eventStatus = status.EventStatus,
        presentationStepCode = status.PresentationStepCode,
        presentationStepLabel = status.PresentationStepLabel,
        stepNumber = status.StepNumber,
        totalSteps = status.TotalSteps,
        isComplete = status.IsComplete,
        canReset = status.CanReset,
        productionRecoveryGoodParts = status.ProductionRecoveryGoodParts,
    };

    internal static object MapResetResult(GoldenDemoResetResult result) => new
    {
        productionEventId = result.ProductionEventId,
        machineCode = result.MachineCode,
        previousStatus = result.PreviousStatus,
        resetStatus = result.ResetStatus,
        resetAtUtc = result.ResetAtUtc,
    };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(
        FacilityAccessResolutionResult accessResult) =>
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
