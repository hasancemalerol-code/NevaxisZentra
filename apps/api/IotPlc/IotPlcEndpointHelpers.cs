using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.IotPlc;

namespace NevaxisZentra.Api.IotPlc;

internal static class IotPlcEndpointHelpers
{
    internal static async Task<(IotPlcOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, TypedResults.Problem(
                title: "A facility identifier is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt("iot-plc.invalid-scope")));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, IotPlcPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<(IotPlcOperationContext? Context, IResult? Failure)> ResolveManageAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, TypedResults.Problem(
                title: "A facility identifier is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt("iot-plc.invalid-scope")));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, IotPlcPermissionKeys.Manage),
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
            new FacilityAccessRequest(facilityId, IotPlcPermissionKeys.Manage),
            cancellationToken);

        return accessResult.Succeeded && accessResult.Access is not null;
    }

    internal static IotPlcOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    internal static IResult MapWriteResult(IotPlcWriteResult result, bool isCreate = false)
    {
        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        var dto = new IotPlcWriteSuccessDto(result.EntityId!.Value, result.Version, result.IdempotentReplay);
        if (result.IdempotentReplay)
        {
            return TypedResults.Ok(dto);
        }

        return isCreate ? TypedResults.Created((string?)null, dto) : TypedResults.Ok(dto);
    }

    internal static IResult MapFailure(string failureCode, string? reason) =>
        failureCode switch
        {
            IotPlcFailureCodes.EndpointNotFound or IotPlcFailureCodes.SignalNotFound or IotPlcFailureCodes.NotFound =>
                TypedResults.Problem(
                    title: reason ?? "Record not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: FailureExt(failureCode)),
            IotPlcFailureCodes.ConcurrencyConflict
                or IotPlcFailureCodes.IdempotencyConflict
                or IotPlcFailureCodes.IdempotencyLegacyUnverifiable
                or IotPlcFailureCodes.SourceReferenceConflict
                or IotPlcFailureCodes.DuplicateObservationTimestamp =>
                TypedResults.Problem(
                    title: reason ?? "Request conflict.",
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: FailureExt(failureCode)),
            IotPlcFailureCodes.ValidationFailed =>
                TypedResults.Problem(
                    title: reason ?? "Validation failed.",
                    statusCode: StatusCodes.Status400BadRequest,
                    extensions: FailureExt(failureCode)),
            IotPlcFailureCodes.MachineScopeDenied or IotPlcFailureCodes.FacilityScopeDenied =>
                TypedResults.Problem(
                    title: reason ?? "Scope denied.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: FailureExt(failureCode)),
            IotPlcIntelligenceFailureCodes.ProfileNotFound
                or IotPlcIntelligenceFailureCodes.AssessmentNotFound
                or IotPlcIntelligenceFailureCodes.TargetNotFound => TypedResults.Problem(
                title: reason ?? "Record not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(failureCode)),
            IotPlcIntelligenceFailureCodes.ConcurrencyConflict
                or IotPlcIntelligenceFailureCodes.IdempotencyConflict
                or IotPlcIntelligenceFailureCodes.IdempotencyLegacyUnverifiable => TypedResults.Problem(
                title: reason ?? "Request conflict.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(failureCode)),
            IotPlcIntelligenceFailureCodes.ValidationFailed => TypedResults.Problem(
                title: reason ?? "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(failureCode)),
            _ => TypedResults.Problem(
                title: reason ?? "Request failed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(failureCode)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(
        FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication is required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt("iot-plc.unauthenticated")),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "You do not have permission for this operation.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("iot-plc.permission-denied")),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "You do not have access to this facility.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("iot-plc.facility-access-denied")),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "A facility identifier is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt("iot-plc.invalid-scope")),
            _ => TypedResults.Problem(
                title: "Access denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("iot-plc.facility-access-denied")),
        };

    private static Dictionary<string, object?> FailureExt(string code) =>
        new() { ["failureCode"] = code };
}

internal sealed record IotPlcWriteSuccessDto(Guid Id, uint? Version, bool IdempotentReplay);
