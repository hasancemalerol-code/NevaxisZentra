using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.Authorization;

namespace NevaxisFactOS.Api.PresentationMode;

internal static class PresentationModeEndpointHelpers
{
    internal static async Task<IResult?> ResolvePresentationAccessFailureAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return null;
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, PresentationModePermissionKeys.Access),
            cancellationToken);

        if (accessResult.Succeeded)
        {
            return null;
        }

        return MapFacilityAccessFailure(accessResult);
    }

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
