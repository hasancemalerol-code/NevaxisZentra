using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.ProductionPlanning;

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

    public static IResult MapWriteServiceResult(ProductionPlanWriteApiResult result, bool isCreate)
    {
        if (result.Succeeded)
        {
            var response = new ProductionPlanWriteSuccessResponse(
                result.ProductionPlanId!.Value,
                result.ReferenceCode!,
                result.Status!,
                result.Version!.Value,
                result.CreatedAtUtc!.Value,
                result.UpdatedAtUtc!.Value);

            return isCreate
                ? TypedResults.Created((string?)null, response)
                : TypedResults.Ok(response);
        }

        return MapWriteFailure(result);
    }

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapWriteFailure(ProductionPlanWriteApiResult result) =>
        result.FailureCode switch
        {
            ProductionPlanWriteFailureCodes.InvalidScope
                or ProductionPlanWriteFailureCodes.ValidationFailed => TypedResults.Problem(
                    title: result.FailureReason ?? "Validation failed.",
                    statusCode: StatusCodes.Status400BadRequest,
                    extensions: WriteFailureExtensions(
                        result.FailureCode == ProductionPlanWriteFailureCodes.ValidationFailed
                            ? ProductionPlanApiFailureCodes.ValidationFailed
                            : ProductionPlanApiFailureCodes.InvalidRequest,
                        result.FieldErrors)),
            ProductionPlanWriteFailureCodes.NotFound => TypedResults.Problem(
                title: "Production plan not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: WriteFailureExtensions(ProductionPlanApiFailureCodes.NotFound)),
            ProductionPlanWriteFailureCodes.PlanNotEditable => TypedResults.Problem(
                title: result.FailureReason ?? "This production plan cannot be edited.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: WriteFailureExtensions(ProductionPlanApiFailureCodes.PlanNotEditable)),
            ProductionPlanWriteFailureCodes.ReferenceCodeConflict => TypedResults.Problem(
                title: "A production plan with this reference code already exists.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: WriteFailureExtensions(ProductionPlanApiFailureCodes.ReferenceCodeConflict)),
            ProductionPlanWriteFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: result.FailureReason ?? "The production plan was modified by another user.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: WriteFailureExtensions(ProductionPlanApiFailureCodes.ConcurrencyConflict)),
            ProductionPlanWriteFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Production plan service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: WriteFailureExtensions(ProductionPlanApiFailureCodes.SourceUnavailable)),
            ProductionPlanWriteFailureCodes.DataIntegrity => TypedResults.Problem(
                title: "A data integrity error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: WriteFailureExtensions(ProductionPlanApiFailureCodes.DataIntegrity)),
            _ => TypedResults.Problem(
                title: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: WriteFailureExtensions(ProductionPlanApiFailureCodes.UnexpectedError)),
        };

    private static Dictionary<string, object?> WriteFailureExtensions(
        string failureCode,
        IReadOnlyList<ProductionPlanWriteFieldError>? fieldErrors = null)
    {
        var ext = new Dictionary<string, object?>
        {
            ["failureCode"] = failureCode,
        };

        if (fieldErrors is { Count: > 0 })
        {
            ext["fieldErrors"] = fieldErrors.Select(e => new
            {
                field = e.Field,
                code = e.Code,
                message = e.Message,
            }).ToArray();
        }

        return ext;
    }

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

    public static IResult MapWorkflowResult(ProductionPlanWorkflowApiResult result)
    {
        if (result.Succeeded)
        {
            var response = new ProductionPlanWorkflowSuccessResponse(
                result.ProductionPlanId!.Value,
                result.ReferenceCode!,
                result.PreviousStatus!,
                result.Status!,
                result.Version!.Value,
                result.UpdatedAtUtc!.Value,
                result.ApprovalStatus!);

            return TypedResults.Ok(response);
        }

        return MapWorkflowFailure(result);
    }

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapWorkflowFailure(
        ProductionPlanWorkflowApiResult result) =>
        result.FailureCode switch
        {
            ProductionPlanWorkflowFailureCodes.InvalidScope
                or ProductionPlanWorkflowFailureCodes.ValidationFailed => TypedResults.Problem(
                    title: result.FailureReason ?? "Validation failed.",
                    statusCode: StatusCodes.Status400BadRequest,
                    extensions: WriteFailureExtensions(
                        result.FailureCode == ProductionPlanWorkflowFailureCodes.ValidationFailed
                            ? ProductionPlanApiFailureCodes.ValidationFailed
                            : ProductionPlanApiFailureCodes.InvalidRequest,
                        result.FieldErrors?.Select(e =>
                            new ProductionPlanWriteFieldError(e.Field, e.Code, e.Message)).ToArray())),
            ProductionPlanWorkflowFailureCodes.NotFound => TypedResults.Problem(
                title: "Production plan not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: WriteFailureExtensions(ProductionPlanApiFailureCodes.NotFound)),
            ProductionPlanWorkflowFailureCodes.InvalidTransition => TypedResults.Problem(
                title: result.FailureReason ?? "This workflow transition is not allowed.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: WriteFailureExtensions(ProductionPlanApiFailureCodes.InvalidTransition)),
            ProductionPlanWorkflowFailureCodes.ConcurrencyConflict => TypedResults.Problem(
                title: result.FailureReason ?? "The production plan was modified by another user.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: WriteFailureExtensions(ProductionPlanApiFailureCodes.ConcurrencyConflict)),
            ProductionPlanWorkflowFailureCodes.AuditFailure => TypedResults.Problem(
                title: "The workflow transition could not be recorded. Please try again.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: WriteFailureExtensions(ProductionPlanApiFailureCodes.AuditFailure)),
            ProductionPlanWorkflowFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Production plan workflow service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: WriteFailureExtensions(ProductionPlanApiFailureCodes.SourceUnavailable)),
            ProductionPlanWorkflowFailureCodes.DataIntegrity => TypedResults.Problem(
                title: "A data integrity error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: WriteFailureExtensions(ProductionPlanApiFailureCodes.DataIntegrity)),
            _ => TypedResults.Problem(
                title: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: WriteFailureExtensions(ProductionPlanApiFailureCodes.UnexpectedError)),
        };

    private static Dictionary<string, object?> FailureExtensions(string failureCode) =>
        new()
        {
            ["failureCode"] = failureCode,
        };
}
