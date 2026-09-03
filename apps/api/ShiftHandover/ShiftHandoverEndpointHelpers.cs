using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.AiTasks;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.ShiftHandover;
using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Api.ShiftHandover;

internal static class ShiftHandoverEndpointHelpers
{
    internal static async Task<(ShiftHandoverOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "Tesis tan─▒mlay─▒c─▒s─▒ zorunludur."));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ShiftHandoverPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<(ShiftHandoverOperationContext? Context, IResult? Failure)> ResolveManageAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        await ResolveWriteAccessAsync(
            facilityId,
            ShiftHandoverPermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

    internal static async Task<(ShiftHandoverOperationContext? Context, IResult? Failure)> ResolveAcknowledgeAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken) =>
        await ResolveWriteAccessAsync(
            facilityId,
            ShiftHandoverPermissionKeys.Acknowledge,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);

    internal static async Task<ShiftHandoverAccessCapabilities> ResolveCapabilitiesAsync(
        Guid facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        var canRead = await HasPermissionAsync(facilityId, ShiftHandoverPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        var canManage = await HasPermissionAsync(facilityId, ShiftHandoverPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        var canAcknowledge = await HasPermissionAsync(facilityId, ShiftHandoverPermissionKeys.Acknowledge, facilityAccessResolver, cancellationToken);
        var canCreateTask = await HasPermissionAsync(facilityId, AiTaskPermissionKeys.Create, facilityAccessResolver, cancellationToken);

        return new ShiftHandoverAccessCapabilities(canRead, canManage, canAcknowledge, canCreateTask);
    }

    private static async Task<(ShiftHandoverOperationContext? Context, IResult? Failure)> ResolveWriteAccessAsync(
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
                "Tesis tan─▒mlay─▒c─▒s─▒ zorunludur."));
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

    private static async Task<bool> HasPermissionAsync(
        Guid facilityId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        var result = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId, permissionKey),
            cancellationToken);
        return result.Succeeded && result.Access is not null;
    }

    internal static ShiftHandoverOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    internal static IResult MapFailure(string code, string message) =>
        TypedResults.Problem(
            title: message,
            statusCode: StatusCodes.Status400BadRequest,
            extensions: FailureExt(code));

    internal static Dictionary<string, object?> FailureExt(string code) =>
        new() { ["code"] = code };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapAccessDenied(string code, string message) =>
        TypedResults.Problem(
            title: message,
            statusCode: StatusCodes.Status403Forbidden,
            extensions: FailureExt(code));

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(
        FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Kimlik do─şrulama gerekli.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt(accessResult.FailureCode)),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "Ge├ğersiz istek.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(accessResult.FailureCode)),
            _ => TypedResults.Problem(
                title: "Yetkilendirme reddedildi.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt(accessResult.FailureCode ?? FacilityAccessFailureCodes.FacilityAccessDenied)),
        };
}
