using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Quality;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.Quality;

public static class QualityReportingEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapQualityReportingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/quality").RequireAuthorization();

        group.MapGet("/reporting/overview", GetOverviewAsync);
        group.MapGet("/reporting/inspections", GetInspectionReportsAsync);
        group.MapGet("/reporting/ncrs", GetNcrAnalyticsAsync);
        group.MapGet("/reporting/holds", GetHoldAnalyticsAsync);
        group.MapGet("/reporting/corrective-actions", GetCorrectiveActionAnalyticsAsync);
        group.MapGet("/reporting/trends", GetTrendsAsync);
        group.MapGet("/reporting/failures", GetFailureBreakdownAsync);

        group.MapGet("/traceability/production-orders/{productionOrderId:guid}", GetProductionOrderTraceabilityAsync);
        group.MapGet("/traceability/products/{productReference}", GetProductHistoryAsync);
        group.MapGet("/inspections/{inspectionId:guid}/summary", GetInspectionSummaryAsync);
        group.MapGet("/inspections/{inspectionId:guid}/release-intelligence", GetReleaseIntelligenceAsync);
        group.MapGet("/production-orders/{productionOrderId:guid}/export-readiness", GetExportReadinessAsync);

        return endpoints;
    }

    private static QualityReportingFilters ParseFilters(HttpRequest request) =>
        new(
            DateFrom: ParseDate(request.Query["dateFrom"].FirstOrDefault()),
            DateTo: ParseDate(request.Query["dateTo"].FirstOrDefault()),
            ProductReference: request.Query["productReference"].FirstOrDefault(),
            ProductionOrderId: ParseGuid(request.Query["productionOrderId"].FirstOrDefault()),
            ControlPlanId: ParseGuid(request.Query["controlPlanId"].FirstOrDefault()),
            OverallResult: request.Query["overallResult"].FirstOrDefault(),
            ReleaseStatus: request.Query["releaseStatus"].FirstOrDefault(),
            NcrSeverity: request.Query["ncrSeverity"].FirstOrDefault(),
            NcrStatus: request.Query["ncrStatus"].FirstOrDefault(),
            Limit: int.TryParse(request.Query["limit"], out var limit) ? limit : 100,
            Offset: int.TryParse(request.Query["offset"], out var offset) ? offset : 0);

    private static DateTimeOffset? ParseDate(string? value) =>
        DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : null;

    private static async Task<IResult> GetOverviewAsync(
        HttpRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityReportingRepository reportingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var overview = await reportingRepository.GetOverviewAsync(context, ParseFilters(request), cancellationToken);
        return TypedResults.Ok(QualityReportingApiMapper.MapOverview(overview));
    }

    private static async Task<IResult> GetInspectionReportsAsync(
        HttpRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityReportingRepository reportingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var reports = await reportingRepository.GetInspectionReportsAsync(context, ParseFilters(request), cancellationToken);
        return TypedResults.Ok(reports.Select(QualityReportingApiMapper.MapInspectionSummary).ToList());
    }

    private static async Task<IResult> GetNcrAnalyticsAsync(
        HttpRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityReportingRepository reportingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var analytics = await reportingRepository.GetNcrAnalyticsAsync(context, ParseFilters(request), cancellationToken);
        return TypedResults.Ok(QualityReportingApiMapper.MapNcrAnalytics(analytics));
    }

    private static async Task<IResult> GetHoldAnalyticsAsync(
        HttpRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityReportingRepository reportingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var analytics = await reportingRepository.GetHoldAnalyticsAsync(context, ParseFilters(request), cancellationToken);
        return TypedResults.Ok(QualityReportingApiMapper.MapHoldAnalytics(analytics));
    }

    private static async Task<IResult> GetCorrectiveActionAnalyticsAsync(
        HttpRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityReportingRepository reportingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var analytics = await reportingRepository.GetCorrectiveActionAnalyticsAsync(
            context, ParseFilters(request), cancellationToken);
        return TypedResults.Ok(QualityReportingApiMapper.MapCorrectiveActionAnalytics(analytics));
    }

    private static async Task<IResult> GetTrendsAsync(
        HttpRequest request,
        [FromQuery] Guid? facilityId,
        [FromQuery] string? period,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityReportingRepository reportingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var trends = await reportingRepository.GetTrendsAsync(
            context, ParseFilters(request), period ?? "day", cancellationToken);
        return TypedResults.Ok(trends.Select(QualityReportingApiMapper.MapTrend).ToList());
    }

    private static async Task<IResult> GetFailureBreakdownAsync(
        HttpRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityReportingRepository reportingRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var breakdown = await reportingRepository.GetFailureBreakdownAsync(
            context, ParseFilters(request), cancellationToken);
        return TypedResults.Ok(breakdown.Select(QualityReportingApiMapper.MapFailure).ToList());
    }

    private static async Task<IResult> GetProductionOrderTraceabilityAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityTraceabilityRepository traceabilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var trace = await traceabilityRepository.GetProductionOrderTraceabilityAsync(
            context, productionOrderId, cancellationToken);
        if (trace is null)
        {
            return TypedResults.Problem(
                title: "Production order traceability not found.",
                statusCode: StatusCodes.Status404NotFound,
                detail: "Production order not found in facility scope.");
        }

        return TypedResults.Ok(QualityReportingApiMapper.MapProductionTraceability(trace));
    }

    private static async Task<IResult> GetProductHistoryAsync(
        string productReference,
        HttpRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityTraceabilityRepository traceabilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var history = await traceabilityRepository.GetProductHistoryAsync(
            context, Uri.UnescapeDataString(productReference), ParseFilters(request), cancellationToken);
        if (history is null)
        {
            return TypedResults.Problem(
                title: "Product quality history not found.",
                statusCode: StatusCodes.Status404NotFound,
                detail: "No quality history for product reference.");
        }

        return TypedResults.Ok(QualityReportingApiMapper.MapProductHistory(history));
    }

    private static async Task<IResult> GetInspectionSummaryAsync(
        Guid inspectionId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityTraceabilityRepository traceabilityRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var summary = await traceabilityRepository.GetInspectionSummaryAsync(context, inspectionId, cancellationToken);
        if (summary is null)
        {
            return TypedResults.Problem(
                title: "Quality inspection summary not found.",
                statusCode: StatusCodes.Status404NotFound,
                detail: "Inspection not found in facility scope.");
        }

        return TypedResults.Ok(QualityReportingApiMapper.MapInspectionSummary(summary));
    }

    private static async Task<IResult> GetReleaseIntelligenceAsync(
        Guid inspectionId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityReleaseIntelligenceReader releaseIntelligenceReader,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var intel = await releaseIntelligenceReader.GetForInspectionAsync(context, inspectionId, cancellationToken);
        return TypedResults.Ok(QualityReportingApiMapper.MapReleaseIntelligence(intel));
    }

    private static async Task<IResult> GetExportReadinessAsync(
        Guid productionOrderId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityReleaseIntelligenceReader releaseIntelligenceReader,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null)
        {
            return failResult!;
        }

        var projection = await releaseIntelligenceReader.GetExportReadinessAsync(
            context, productionOrderId, cancellationToken);
        return TypedResults.Ok(QualityReportingApiMapper.MapExportReadiness(projection));
    }
}

internal static class QualityReportingApiMapper
{
    public static object MapOverview(QualityReportingOverviewReadModel m) => new
    {
        totalInspectionCount = m.TotalInspectionCount,
        openInspectionCount = m.OpenInspectionCount,
        completedInspectionCount = m.CompletedInspectionCount,
        passedInspectionCount = m.PassedInspectionCount,
        failedInspectionCount = m.FailedInspectionCount,
        releasedInspectionCount = m.ReleasedInspectionCount,
        rejectedInspectionCount = m.RejectedInspectionCount,
        openNcrCount = m.OpenNcrCount,
        criticalNcrCount = m.CriticalNcrCount,
        activeHoldCount = m.ActiveHoldCount,
        blockingProductionOrderCount = m.BlockingProductionOrderCount,
        openCorrectiveActionCount = m.OpenCorrectiveActionCount,
        overdueCorrectiveActionCount = m.OverdueCorrectiveActionCount,
        firstPassYieldPercent = m.FirstPassYieldPercent,
        passRatePercent = m.PassRatePercent,
        releaseRatePercent = m.ReleaseRatePercent,
    };

    public static object MapNcrAnalytics(QualityNcrAnalyticsReadModel m) => new
    {
        openCount = m.OpenCount,
        resolvedCount = m.ResolvedCount,
        closedCount = m.ClosedCount,
        criticalCount = m.CriticalCount,
        majorCount = m.MajorCount,
        minorCount = m.MinorCount,
        averageAgeHours = m.AverageAgeHours,
        averageResolutionHours = m.AverageResolutionHours,
        overdueCorrectiveActionCount = m.OverdueCorrectiveActionCount,
    };

    public static object MapHoldAnalytics(QualityHoldAnalyticsReadModel m) => new
    {
        activeCount = m.ActiveCount,
        releasedCount = m.ReleasedCount,
        affectedProductionOrderCount = m.AffectedProductionOrderCount,
        averageHoldDurationHours = m.AverageHoldDurationHours,
    };

    public static object MapCorrectiveActionAnalytics(QualityCorrectiveActionAnalyticsReadModel m) => new
    {
        openCount = m.OpenCount,
        inProgressCount = m.InProgressCount,
        completedCount = m.CompletedCount,
        overdueCount = m.OverdueCount,
        averageCompletionLeadTimeHours = m.AverageCompletionLeadTimeHours,
    };

    public static object MapTrend(QualityTrendPeriodReadModel m) => new
    {
        periodKey = m.PeriodKey,
        periodStartUtc = m.PeriodStartUtc,
        inspectionCount = m.InspectionCount,
        passCount = m.PassCount,
        failCount = m.FailCount,
        ncrCount = m.NcrCount,
        criticalNcrCount = m.CriticalNcrCount,
        holdCount = m.HoldCount,
    };

    public static object MapFailure(QualityFailureBreakdownItemReadModel m) => new
    {
        dimension = m.Dimension,
        key = m.Key,
        label = m.Label,
        count = m.Count,
    };

    public static object MapInspectionSummary(QualityInspectionControlSummaryReadModel m) => new
    {
        inspectionId = m.InspectionId,
        inspectionNumber = m.InspectionNumber,
        facilityReference = m.FacilityReference,
        productionOrderId = m.ProductionOrderId,
        productionOrderReference = m.ProductionOrderReference,
        productReference = m.ProductReference,
        controlPlanId = m.ControlPlanId,
        controlPlanCode = m.ControlPlanCode,
        controlPlanName = m.ControlPlanName,
        controlPlanRevision = m.ControlPlanRevision,
        inspectionDateUtc = m.InspectionDateUtc,
        inspectorSubjectId = m.InspectorSubjectId,
        overallResult = m.OverallResult,
        releaseStatus = m.ReleaseStatus,
        measurements = m.Measurements.Select(x => new
        {
            definitionCode = x.DefinitionCode,
            definitionName = x.DefinitionName,
            unit = x.Unit,
            nominalValue = x.NominalValue,
            lowerLimit = x.LowerLimit,
            upperLimit = x.UpperLimit,
            required = x.Required,
            critical = x.Critical,
            measuredNumericValue = x.MeasuredNumericValue,
            measuredTextValue = x.MeasuredTextValue,
            booleanResult = x.BooleanResult,
            result = x.Result,
        }),
        hasBlockingHold = m.HasBlockingHold,
        openNcrCount = m.OpenNcrCount,
        blockingSummary = m.BlockingSummary,
    };

    public static object MapReleaseIntelligence(QualityReleaseIntelligenceReadModel m) => new
    {
        available = m.Available,
        inspectionStatus = m.InspectionStatus,
        overallResult = m.OverallResult,
        releaseStatus = m.ReleaseStatus,
        blocking = m.Blocking,
        activeHoldCount = m.ActiveHoldCount,
        openNcrCount = m.OpenNcrCount,
        criticalNcrCount = m.CriticalNcrCount,
        incompleteCorrectiveActionCount = m.IncompleteCorrectiveActionCount,
        readyForRelease = m.ReadyForRelease,
        reasonCodes = m.ReasonCodes,
        reasonSummary = m.ReasonSummary,
    };

    public static object MapExportReadiness(QualityExportReadinessProjectionReadModel m) => new
    {
        qualityReleaseStatus = m.QualityReleaseStatus,
        qualityBlocking = m.QualityBlocking,
        qualityReady = m.QualityReady,
        reason = m.Reason,
    };

    public static object MapProductionTraceability(ProductionOrderTraceabilityReadModel m) => new
    {
        productionOrderId = m.ProductionOrderId,
        productionOrderReference = m.ProductionOrderReference,
        productReference = m.ProductReference,
        inspections = m.Inspections,
        nonconformances = m.Nonconformances,
        holds = m.Holds,
        correctiveActions = m.CorrectiveActions,
    };

    public static object MapProductHistory(ProductQualityHistoryReadModel m) => new
    {
        productReference = m.ProductReference,
        recentInspections = m.RecentInspections.Select(MapInspectionSummary),
        nonconformances = m.Nonconformances,
        holds = m.Holds,
    };
}
