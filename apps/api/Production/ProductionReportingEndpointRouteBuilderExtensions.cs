using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Production;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Production;

public static class ProductionReportingEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapProductionReportingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/production/reporting").RequireAuthorization();
        group.MapGet("/overview", GetOverviewAsync);
        group.MapGet("/orders", ListOrderPerformanceAsync);
        group.MapGet("/downtime", GetDowntimeAnalyticsAsync);
        group.MapGet("/scrap", GetScrapAnalyticsAsync);
        group.MapGet("/setup", GetSetupAnalyticsAsync);
        group.MapGet("/cycles", GetCycleAnalyticsAsync);
        group.MapGet("/shifts", ListShiftAnalyticsAsync);
        group.MapGet("/exceptions", GetExceptionAnalyticsAsync);
        return endpoints;
    }

    private static async Task<IResult> GetOverviewAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] string? status,
        [FromQuery] Guid? productionOrderId,
        [FromQuery] Guid? shiftId,
        [FromQuery] string? resourceReference,
        [FromQuery] string? operatorSubjectId,
        [FromQuery] string? riskState,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionReportingRepository reportingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveReadAccessAsync(facilityId, facilityAccessResolver, cancellationToken);
        if (access.Context is null)
        {
            return access.Failure!;
        }

        var filter = BuildFilter(fromUtc, toUtc, status, productionOrderId, shiftId, resourceReference, operatorSubjectId, riskState);
        var result = await reportingRepository.GetOverviewAsync(access.Context, filter, cancellationToken);
        return MapReadResult(result, ProductionReportingApiMapper.MapOverview);
    }

    private static async Task<IResult> ListOrderPerformanceAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] string? status,
        [FromQuery] Guid? productionOrderId,
        [FromQuery] Guid? shiftId,
        [FromQuery] string? resourceReference,
        [FromQuery] string? operatorSubjectId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionReportingRepository reportingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveReadAccessAsync(facilityId, facilityAccessResolver, cancellationToken);
        if (access.Context is null)
        {
            return access.Failure!;
        }

        var filter = BuildFilter(fromUtc, toUtc, status, productionOrderId, shiftId, resourceReference, operatorSubjectId, null);
        var result = await reportingRepository.ListOrderPerformanceAsync(access.Context, filter, cancellationToken);
        return MapReadResult(result, items => items.Select(ProductionReportingApiMapper.MapOrderPerformance).ToList());
    }

    private static async Task<IResult> GetDowntimeAnalyticsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] Guid? productionOrderId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionReportingRepository reportingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveReadAccessAsync(facilityId, facilityAccessResolver, cancellationToken);
        if (access.Context is null)
        {
            return access.Failure!;
        }

        var filter = BuildFilter(fromUtc, toUtc, null, productionOrderId, null, null, null, null);
        var result = await reportingRepository.GetDowntimeAnalyticsAsync(access.Context, filter, cancellationToken);
        return MapReadResult(result, ProductionReportingApiMapper.MapDowntime);
    }

    private static async Task<IResult> GetScrapAnalyticsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] string? status,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionReportingRepository reportingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveReadAccessAsync(facilityId, facilityAccessResolver, cancellationToken);
        if (access.Context is null)
        {
            return access.Failure!;
        }

        var filter = BuildFilter(fromUtc, toUtc, status, null, null, null, null, null);
        var result = await reportingRepository.GetScrapAnalyticsAsync(access.Context, filter, cancellationToken);
        return MapReadResult(result, ProductionReportingApiMapper.MapScrap);
    }

    private static async Task<IResult> GetSetupAnalyticsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionReportingRepository reportingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveReadAccessAsync(facilityId, facilityAccessResolver, cancellationToken);
        if (access.Context is null)
        {
            return access.Failure!;
        }

        var filter = BuildFilter(fromUtc, toUtc, null, null, null, null, null, null);
        var result = await reportingRepository.GetSetupAnalyticsAsync(access.Context, filter, cancellationToken);
        return MapReadResult(result, ProductionReportingApiMapper.MapSetup);
    }

    private static async Task<IResult> GetCycleAnalyticsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionReportingRepository reportingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveReadAccessAsync(facilityId, facilityAccessResolver, cancellationToken);
        if (access.Context is null)
        {
            return access.Failure!;
        }

        var filter = BuildFilter(fromUtc, toUtc, null, null, null, null, null, null);
        var result = await reportingRepository.GetCycleAnalyticsAsync(access.Context, filter, cancellationToken);
        return MapReadResult(result, ProductionReportingApiMapper.MapCycles);
    }

    private static async Task<IResult> ListShiftAnalyticsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? shiftId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionReportingRepository reportingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveReadAccessAsync(facilityId, facilityAccessResolver, cancellationToken);
        if (access.Context is null)
        {
            return access.Failure!;
        }

        var filter = BuildFilter(null, null, null, null, shiftId, null, null, null);
        var result = await reportingRepository.ListShiftAnalyticsAsync(access.Context, filter, cancellationToken);
        return MapReadResult(result, items => items.Select(ProductionReportingApiMapper.MapShift).ToList());
    }

    private static async Task<IResult> GetExceptionAnalyticsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] Guid? productionOrderId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionReportingRepository reportingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var access = await ResolveReadAccessAsync(facilityId, facilityAccessResolver, cancellationToken);
        if (access.Context is null)
        {
            return access.Failure!;
        }

        var filter = BuildFilter(fromUtc, toUtc, null, productionOrderId, null, null, null, null);
        var result = await reportingRepository.GetExceptionAnalyticsAsync(access.Context, filter, cancellationToken);
        return MapReadResult(result, ProductionReportingApiMapper.MapExceptions);
    }

    private static ProductionReportingFilter BuildFilter(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string? status,
        Guid? productionOrderId,
        Guid? shiftId,
        string? resourceReference,
        string? operatorSubjectId,
        string? riskState) =>
        new(fromUtc, toUtc, status, productionOrderId, shiftId, resourceReference, operatorSubjectId, riskState);

    private static async Task<(ProductionOrderOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, ProductionExecutionEndpointHelpers.MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required."));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, ProductionExecutionEndpointHelpers.MapFacilityAccessFailure(accessResult));
        }

        return (ProductionExecutionEndpointHelpers.BuildOperationContext(accessResult.Access), null);
    }

    private static IResult MapReadResult<TModel, TDto>(
        ProductionReportingReadResult<TModel> result,
        Func<TModel, TDto> mapper)
    {
        if (!result.Succeeded || result.Value is null)
        {
            return result.FailureCode switch
            {
                ProductionOrderFailureCodes.InvalidScope => TypedResults.Problem(
                    title: result.FailureReason ?? "Invalid scope.",
                    statusCode: StatusCodes.Status400BadRequest),
                _ => TypedResults.Problem(
                    title: result.FailureReason ?? "Production reporting is unavailable.",
                    statusCode: StatusCodes.Status503ServiceUnavailable),
            };
        }

        return TypedResults.Ok(mapper(result.Value));
    }
}

internal static class ProductionExecutionEndpointHelpers
{
    public static ProductionOrderOperationContext BuildOperationContext(FacilityAccessContext access) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            null);

    public static IResult MapAccessDenied(string facilityFailureCode, string message) =>
        facilityFailureCode == FacilityAccessFailureCodes.FacilityMissing
            ? TypedResults.Problem(title: message, statusCode: StatusCodes.Status400BadRequest)
            : TypedResults.Problem(title: "Authorization denied.", statusCode: StatusCodes.Status403Forbidden);

    public static IResult MapFacilityAccessFailure(FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication required.",
                statusCode: StatusCodes.Status401Unauthorized),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden),
            _ => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden),
        };
}

internal static class ProductionReportingApiMapper
{
    public static object MapOverview(ProductionOperationalOverviewReadModel model) => new
    {
        totalOrderCount = model.TotalOrderCount,
        activeOrderCount = model.ActiveOrderCount,
        completedOrderCount = model.CompletedOrderCount,
        pausedOrderCount = model.PausedOrderCount,
        readyOrderCount = model.ReadyOrderCount,
        plannedQuantity = model.PlannedQuantity,
        goodQuantity = model.GoodQuantity,
        scrapQuantity = model.ScrapQuantity,
        remainingQuantity = model.RemainingQuantity,
        completionRatePercent = model.CompletionRatePercent,
        yieldPercent = model.YieldPercent,
        activeDowntimeCount = model.ActiveDowntimeCount,
        totalDowntimeMinutes = model.TotalDowntimeMinutes,
        totalSetupMinutes = model.TotalSetupMinutes,
        manualCycleCount = model.ManualCycleCount,
        openExceptionCount = model.OpenExceptionCount,
        unresolvedExceptionCount = model.UnresolvedExceptionCount,
        shiftCoverageCount = model.ShiftCoverageCount,
        operatorCoverageCount = model.OperatorCoverageCount,
        riskSignals = model.RiskSignals,
        logisticsRisk = MapLogistics(model.LogisticsRisk),
    };

    public static object MapOrderPerformance(ProductionOrderPerformanceReadModel model) => new
    {
        productionOrderId = model.ProductionOrderId,
        referenceCode = model.ReferenceCode,
        title = model.Title,
        status = model.Status,
        productReference = model.ProductReference,
        machineReference = model.MachineReference,
        sourceProductionPlanId = model.SourceProductionPlanId,
        planActual = model.PlanActual,
        hasActiveException = model.HasActiveException,
        hasActiveDowntime = model.HasActiveDowntime,
        operationalRisk = model.OperationalRisk,
        logisticsRisk = model.LogisticsRisk is null ? null : MapLogistics(model.LogisticsRisk),
    };

    public static object MapDowntime(ProductionDowntimeAnalyticsReadModel model) => model;
    public static object MapScrap(ProductionScrapAnalyticsReadModel model) => model;
    public static object MapSetup(ProductionSetupAnalyticsReadModel model) => model;
    public static object MapCycles(ProductionCycleAnalyticsReadModel model) => model;
    public static object MapShift(ProductionShiftReportingReadModel model) => model;
    public static object MapExceptions(ProductionExceptionAnalyticsReadModel model) => model;

    private static object MapLogistics(ProductionLogisticsRiskSignal signal) => new
    {
        riskLevel = signal.RiskLevel,
        slackMinutes = signal.SlackMinutes,
        blocking = signal.Blocking,
        reason = signal.Reason,
        latestProductionCompletionUtc = signal.LatestProductionCompletionUtc,
        available = signal.Available,
    };
}
