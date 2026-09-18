using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.ProductionPlanning;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.ProductionPlanning;

public static class PlanningLogisticsEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapProductionPlanningLogisticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/production-planning/plans")
            .RequireAuthorization();

        group.MapGet("/{productionPlanId:guid}/logistics-constraints", GetLogisticsConstraintsAsync);
        group.MapPost("/{productionPlanId:guid}/logistics-constraints/evaluate", EvaluateLogisticsConstraintsAsync);

        return endpoints;
    }

    private static async Task<IResult> GetLogisticsConstraintsAsync(
        Guid productionPlanId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IPlanningLogisticsService logisticsService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required.",
                    DateTimeOffset.UtcNow,
                    permissionKey: ProductionPlanningPermissionKeys.Read));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionPlanningPermissionKeys.Read),
            cancellationToken);
        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(accessResult);
        }

        var operationContext = ProductionPlanApiResultMapper.CreateOperationContext(
            accessResult.Access,
            "production-planning.logistics.read",
            accessResult.EvaluatedAtUtc,
            productionPlanId,
            ReadCorrelationId(correlationContext));

        var result = await logisticsService.GetLatestAsync(
            operationContext,
            productionPlanId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return MapLogisticsFailure(result.FailureCode!, result.FailureReason!);
        }

        return TypedResults.Ok(PlanningLogisticsApiMapper.Map(result.Evaluation!));
    }

    private static async Task<IResult> EvaluateLogisticsConstraintsAsync(
        Guid productionPlanId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IPlanningLogisticsService logisticsService,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(
                FacilityAccessResolutionResult.Deny(
                    FacilityAccessFailureCodes.FacilityMissing,
                    "A facility identifier is required.",
                    DateTimeOffset.UtcNow,
                    permissionKey: ProductionPlanningPermissionKeys.Write));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionPlanningPermissionKeys.Write),
            cancellationToken);
        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return ProductionPlanApiResultMapper.MapAccessFailure(accessResult);
        }

        var operationContext = new ProductionPlanWriteOperationContext(
            accessResult.Access.TenantId,
            accessResult.Access.FacilityId,
            accessResult.Access.MembershipId,
            accessResult.Access.IdentitySubjectId,
            accessResult.Access.PermissionKey,
            ReadCorrelationId(correlationContext));

        var result = await logisticsService.EvaluateAsync(
            operationContext,
            productionPlanId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return MapLogisticsFailure(result.FailureCode!, result.FailureReason!);
        }

        return TypedResults.Ok(PlanningLogisticsApiMapper.Map(result.Evaluation!));
    }

    private static IResult MapLogisticsFailure(string failureCode, string reason) =>
        failureCode switch
        {
            PlanningLogisticsFailureCodes.NotFound => TypedResults.NotFound(new
            {
                failureCode,
                message = reason,
            }),
            PlanningLogisticsFailureCodes.InvalidScope => TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["failureCode"] = failureCode }),
            PlanningLogisticsFailureCodes.SourceUnavailable => TypedResults.Problem(
                title: "Planning logistics service is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?> { ["failureCode"] = failureCode }),
            _ => TypedResults.Problem(
                title: reason,
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: new Dictionary<string, object?> { ["failureCode"] = failureCode }),
        };

    private static Guid? ReadCorrelationId(CorrelationContext correlationContext) =>
        correlationContext.IsAvailable ? correlationContext.CorrelationId : null;
}
