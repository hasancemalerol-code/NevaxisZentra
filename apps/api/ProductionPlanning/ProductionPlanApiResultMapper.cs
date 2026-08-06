using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Authorization;

namespace CrevaFactOS.Api.ProductionPlanning;

public static class ProductionPlanApiResultMapper
{
    public static IResult MapAccessFailure(FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExtensions(ProductionPlanApiFailureCodes.Unauthenticated)),
            FacilityAccessFailureCodes.TenantContextMissing
                or FacilityAccessFailureCodes.MembershipMissing => TypedResults.Problem(
                    title: "Authorization denied.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: FailureExtensions(ProductionPlanApiFailureCodes.MembershipMissing)),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExtensions(ProductionPlanApiFailureCodes.InvalidRequest)),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExtensions(ProductionPlanApiFailureCodes.FacilityAccessDenied)),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExtensions(ProductionPlanApiFailureCodes.PermissionDenied)),
            FacilityAccessFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Service unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExtensions(ProductionPlanApiFailureCodes.SourceUnavailable)),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExtensions(ProductionPlanApiFailureCodes.UnexpectedError)),
        };

    public static IResult MapServiceFailure(string failureCode) =>
        failureCode switch
        {
            ProductionPlanApiFailureCodes.InvalidRequest => TypedResults.Problem(
                title: "Invalid request.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExtensions(failureCode)),
            ProductionPlanApiFailureCodes.NotFound => TypedResults.Problem(
                title: "Resource not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExtensions(failureCode)),
            ProductionPlanApiFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Service unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: FailureExtensions(failureCode)),
            ProductionPlanApiFailureCodes.InvalidRow
                or ProductionPlanApiFailureCodes.DataIntegrity => TypedResults.Problem(
                    title: "Production plan data is invalid.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    extensions: FailureExtensions(failureCode)),
            _ => TypedResults.Problem(
                title: "Unexpected error.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: FailureExtensions(ProductionPlanApiFailureCodes.UnexpectedError)),
        };

    public static ProductionPlanReadOperationContext CreateOperationContext(
        FacilityAccessContext access,
        string endpointAction,
        DateTimeOffset evaluatedAtUtc,
        Guid? productionPlanId,
        Guid? correlationId) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            "granted",
            endpointAction,
            evaluatedAtUtc,
            productionPlanId,
            correlationId);

    private static Dictionary<string, object?> FailureExtensions(string failureCode) =>
        new()
        {
            ["failureCode"] = failureCode,
        };
}
