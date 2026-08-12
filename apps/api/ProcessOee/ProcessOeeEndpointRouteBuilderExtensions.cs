using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.ProcessOee;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.ProcessOee;

public static class ProcessOeeEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapProcessOeeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/process-oee").RequireAuthorization();

        group.MapGet("/workspace-summary", GetWorkspaceSummaryAsync);
        group.MapGet("/evaluation-windows", ListWindowsAsync);
        group.MapPost("/evaluation-windows", CreateWindowAsync);
        group.MapGet("/evaluation-windows/{windowId:guid}", GetWindowAsync);
        group.MapPut("/evaluation-windows/{windowId:guid}", UpdateWindowAsync);
        group.MapPost("/evaluation-windows/{windowId:guid}/evaluations", EvaluateWindowAsync);
        group.MapPost("/evaluations/bounded", EvaluateBoundedAsync);
        group.MapGet("/assessments", ListAssessmentsAsync);
        group.MapGet("/assessments/{assessmentId:guid}", GetAssessmentAsync);
        group.MapGet("/evaluation-windows/{windowId:guid}/current-assessment", GetCurrentAssessmentAsync);
        group.MapGet("/loss-overview", GetLossOverviewAsync);
        group.MapGet("/assessments/{assessmentId:guid}/losses", GetAssessmentLossesAsync);
        group.MapGet("/evaluation-windows/{windowId:guid}/losses", GetWindowLossesAsync);
        group.MapGet("/stability-overview", GetStabilityOverviewAsync);
        group.MapGet("/assessments/{assessmentId:guid}/stability", GetAssessmentStabilityAsync);
        group.MapGet("/evaluation-windows/{windowId:guid}/stability", GetWindowStabilityAsync);
        group.MapGet("/assessments/{assessmentId:guid}/downtime-pareto", GetDowntimeParetoAsync);
        group.MapGet("/shift-comparison", GetShiftComparisonAsync);
        group.MapPost("/shift-comparison/evaluations", EvaluateShiftComparisonAsync);

        return endpoints;
    }

    private static async Task<IResult> GetWorkspaceSummaryAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var summary = await repository.GetWorkspaceSummaryAsync(context, cancellationToken);
        var canManage = await ProcessOeeEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            totalWindowCount = summary.TotalWindowCount,
            assessmentCount = summary.AssessmentCount,
            calculatedCount = summary.CalculatedCount,
            insufficientDataCount = summary.InsufficientDataCount,
            reviewRequiredCount = summary.ReviewRequiredCount,
            staleDataCount = summary.StaleDataCount,
            latestOeePercent = summary.LatestOeePercent,
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> ListWindowsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? machineAssetId,
        [FromQuery] string? productionOrderReference,
        [FromQuery] string? moldReference,
        [FromQuery] DateTimeOffset? windowStartFromUtc,
        [FromQuery] DateTimeOffset? windowEndToUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IProcessOeeRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var windows = await repository.ListWindowsAsync(
            context,
            new ProcessOeeWindowListFilters(
                machineAssetId, productionOrderReference, moldReference,
                windowStartFromUtc, windowEndToUtc, page, pageSize),
            cancellationToken);

        return TypedResults.Ok(windows.Select(MapWindowSummary).ToList());
    }

    private static async Task<IResult> GetWindowAsync(
        Guid windowId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var window = await repository.GetWindowByIdAsync(context, windowId, cancellationToken);
        return window is null
            ? ProcessOeeEndpointHelpers.MapFailure(ProcessOeeFailureCodes.NotFound, "Evaluation window not found.")
            : TypedResults.Ok(MapWindowDetail(window));
    }

    private static async Task<IResult> CreateWindowAsync(
        [FromBody] CreateProcessOeeWindowRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateWindowAsync(
            context,
            new CreateProcessOeeWindowCommand(
                request.MachineAssetId,
                request.ProductionOrderReference,
                request.MoldReference,
                request.WindowStartUtc,
                request.WindowEndUtc,
                request.PlannedProductionSeconds,
                request.IdealCycleTimeSeconds,
                request.EffectiveCavityCount,
                request.SourceType ?? ProcessOeeSourceTypes.Manual,
                request.SourceReference,
                request.DataRecordedAtUtc,
                request.IdempotencyKey),
            cancellationToken);

        return ProcessOeeEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateWindowAsync(
        Guid windowId,
        [FromBody] UpdateProcessOeeWindowRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdateWindowAsync(
            context,
            new UpdateProcessOeeWindowCommand(
                windowId,
                request.ExpectedVersion,
                request.ProductionOrderReference,
                request.MoldReference,
                request.WindowStartUtc,
                request.WindowEndUtc,
                request.PlannedProductionSeconds,
                request.IdealCycleTimeSeconds,
                request.EffectiveCavityCount,
                request.SourceType ?? ProcessOeeSourceTypes.Manual,
                request.SourceReference,
                request.DataRecordedAtUtc),
            cancellationToken);

        return ProcessOeeEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> EvaluateWindowAsync(
        Guid windowId,
        [FromBody] EvaluateProcessOeeRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EvaluateWindowAsync(
            context,
            new EvaluateProcessOeeWindowCommand(windowId, request.IdempotencyKey),
            cancellationToken);

        return ProcessOeeEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> EvaluateBoundedAsync(
        [FromBody] EvaluateBoundedProcessOeeRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EvaluateBoundedAsync(
            context,
            new EvaluateProcessOeeBoundedCommand(
                request.MachineAssetId,
                request.ProductionOrderReference,
                request.MoldReference,
                request.WindowStartUtc,
                request.WindowEndUtc,
                request.PlannedProductionSeconds,
                request.IdealCycleTimeSeconds,
                request.EffectiveCavityCount,
                request.IdempotencyKey),
            cancellationToken);

        return ProcessOeeEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> ListAssessmentsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? machineAssetId,
        [FromQuery] string? productionOrderReference,
        [FromQuery] string? moldReference,
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? evaluatedFromUtc,
        [FromQuery] DateTimeOffset? evaluatedToUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IProcessOeeRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var assessments = await repository.ListAssessmentsAsync(
            context,
            new ProcessOeeAssessmentListFilters(
                machineAssetId, productionOrderReference, moldReference,
                status, evaluatedFromUtc, evaluatedToUtc, page, pageSize),
            cancellationToken);

        return TypedResults.Ok(assessments.Select(MapAssessmentSummary).ToList());
    }

    private static async Task<IResult> GetAssessmentAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var assessment = await repository.GetAssessmentByIdAsync(context, assessmentId, cancellationToken);
        return assessment is null
            ? ProcessOeeEndpointHelpers.MapFailure(ProcessOeeFailureCodes.NotFound, "Assessment not found.")
            : TypedResults.Ok(MapAssessmentDetail(assessment));
    }

    private static async Task<IResult> GetCurrentAssessmentAsync(
        Guid windowId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var assessment = await repository.GetCurrentAssessmentForWindowAsync(context, windowId, cancellationToken);
        return assessment is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(MapAssessmentDetail(assessment));
    }

    private static async Task<IResult> GetLossOverviewAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var overview = await repository.GetLossOverviewAsync(context, cancellationToken);
        return TypedResults.Ok(new
        {
            assessmentCountWithLossDrivers = overview.AssessmentCountWithLossDrivers,
            availabilityLossCount = overview.AvailabilityLossCount,
            performanceLossCount = overview.PerformanceLossCount,
            qualityLossCount = overview.QualityLossCount,
            insufficientDataCount = overview.InsufficientDataCount,
            sourceConflictCount = overview.SourceConflictCount,
            ruleSetVersion = overview.RuleSetVersion,
            leadingLossMethod = overview.LeadingLossMethod,
        });
    }

    private static async Task<IResult> GetAssessmentLossesAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var detail = await repository.GetLossDetailForAssessmentAsync(context, assessmentId, cancellationToken);
        return detail is null
            ? ProcessOeeEndpointHelpers.MapFailure(ProcessOeeFailureCodes.NotFound, "Loss detail not found.")
            : TypedResults.Ok(MapLossDetail(detail));
    }

    private static async Task<IResult> GetWindowLossesAsync(
        Guid windowId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var detail = await repository.GetLossDetailForWindowAsync(context, windowId, cancellationToken);
        return detail is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(MapLossDetail(detail));
    }

    private static async Task<IResult> GetStabilityOverviewAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var overview = await repository.GetStabilityOverviewAsync(context, cancellationToken);
        return TypedResults.Ok(new
        {
            assessmentCountWithStability = overview.AssessmentCountWithStability,
            stableCount = overview.StableCount,
            variableCount = overview.VariableCount,
            driftingCount = overview.DriftingCount,
            reviewRequiredCount = overview.ReviewRequiredCount,
            insufficientDataCount = overview.InsufficientDataCount,
            ruleSetVersion = overview.RuleSetVersion,
        });
    }

    private static async Task<IResult> GetAssessmentStabilityAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var detail = await repository.GetStabilityDetailForAssessmentAsync(context, assessmentId, cancellationToken);
        return detail is null
            ? ProcessOeeEndpointHelpers.MapFailure(ProcessOeeFailureCodes.NotFound, "Stability detail not found.")
            : TypedResults.Ok(MapStabilityDetail(detail));
    }

    private static async Task<IResult> GetWindowStabilityAsync(
        Guid windowId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var detail = await repository.GetStabilityDetailForWindowAsync(context, windowId, cancellationToken);
        return detail is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(MapStabilityDetail(detail));
    }

    private static async Task<IResult> GetDowntimeParetoAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var pareto = await repository.GetDowntimeParetoForAssessmentAsync(context, assessmentId, cancellationToken);
        return pareto is null
            ? ProcessOeeEndpointHelpers.MapFailure(ProcessOeeFailureCodes.NotFound, "Downtime Pareto not found.")
            : TypedResults.Ok(MapDowntimePareto(pareto));
    }

    private static async Task<IResult> GetShiftComparisonAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? machineAssetId,
        [FromQuery] DateTimeOffset periodStartUtc,
        [FromQuery] DateTimeOffset periodEndUtc,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var comparison = await repository.GetShiftComparisonAsync(
            context, machineAssetId, periodStartUtc, periodEndUtc, cancellationToken);
        return TypedResults.Ok(MapShiftComparison(comparison));
    }

    private static async Task<IResult> EvaluateShiftComparisonAsync(
        [FromBody] EvaluateShiftComparisonRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcessOeeRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProcessOeeEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EvaluateShiftComparisonAsync(
            context,
            new EvaluateProcessOeeShiftComparisonCommand(
                request.MachineAssetId,
                request.PeriodStartUtc,
                request.PeriodEndUtc,
                request.IdempotencyKey),
            cancellationToken);

        return ProcessOeeEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static object MapWindowSummary(ProcessOeeWindowSummaryReadModel window) => new
    {
        id = window.Id,
        machineAssetId = window.MachineAssetId,
        machineCode = window.MachineCode,
        machineDisplayName = window.MachineDisplayName,
        productionOrderReference = window.ProductionOrderReference,
        moldReference = window.MoldReference,
        windowStartUtc = window.WindowStartUtc,
        windowEndUtc = window.WindowEndUtc,
        plannedProductionSeconds = window.PlannedProductionSeconds,
        sourceType = window.SourceType,
        sourceTypeLabel = ProcessOeeValueConversions.ToDisplaySourceType(window.SourceType),
        sourceReference = window.SourceReference,
        updatedAtUtc = window.UpdatedAtUtc,
        version = window.Version,
    };

    private static object MapWindowDetail(ProcessOeeWindowDetailReadModel window) => new
    {
        id = window.Id,
        machineAssetId = window.MachineAssetId,
        machineCode = window.MachineCode,
        machineDisplayName = window.MachineDisplayName,
        productionOrderReference = window.ProductionOrderReference,
        moldReference = window.MoldReference,
        windowStartUtc = window.WindowStartUtc,
        windowEndUtc = window.WindowEndUtc,
        plannedProductionSeconds = window.PlannedProductionSeconds,
        idealCycleTimeSeconds = window.IdealCycleTimeSeconds,
        effectiveCavityCount = window.EffectiveCavityCount,
        sourceType = window.SourceType,
        sourceTypeLabel = ProcessOeeValueConversions.ToDisplaySourceType(window.SourceType),
        sourceReference = window.SourceReference,
        dataRecordedAtUtc = window.DataRecordedAtUtc,
        createdAtUtc = window.CreatedAtUtc,
        updatedAtUtc = window.UpdatedAtUtc,
        version = window.Version,
    };

    private static object MapAssessmentSummary(ProcessOeeAssessmentSummaryReadModel assessment) => new
    {
        id = assessment.Id,
        machineAssetId = assessment.MachineAssetId,
        machineCode = assessment.MachineCode,
        machineDisplayName = assessment.MachineDisplayName,
        productionOrderReference = assessment.ProductionOrderReference,
        moldReference = assessment.MoldReference,
        windowStartUtc = assessment.WindowStartUtc,
        windowEndUtc = assessment.WindowEndUtc,
        status = assessment.Status,
        statusLabel = ProcessOeeValueConversions.ToDisplayStatus(assessment.Status),
        availabilityPercent = assessment.AvailabilityPercent,
        performancePercent = assessment.PerformancePercent,
        qualityPercent = assessment.QualityPercent,
        oeePercent = assessment.OeePercent,
        evaluatedAtUtc = assessment.EvaluatedAtUtc,
    };

    private static object MapAssessmentDetail(ProcessOeeAssessmentDetailReadModel assessment) => new
    {
        id = assessment.Id,
        evaluationWindowId = assessment.EvaluationWindowId,
        machineAssetId = assessment.MachineAssetId,
        machineCode = assessment.MachineCode,
        machineDisplayName = assessment.MachineDisplayName,
        productionOrderReference = assessment.ProductionOrderReference,
        moldReference = assessment.MoldReference,
        windowStartUtc = assessment.WindowStartUtc,
        windowEndUtc = assessment.WindowEndUtc,
        status = assessment.Status,
        statusLabel = ProcessOeeValueConversions.ToDisplayStatus(assessment.Status),
        ruleSetVersion = assessment.RuleSetVersion,
        plannedProductionSeconds = assessment.PlannedProductionSeconds,
        operatingSeconds = assessment.OperatingSeconds,
        downtimeSeconds = assessment.DowntimeSeconds,
        unknownSeconds = assessment.UnknownSeconds,
        observedCycleCount = assessment.ObservedCycleCount,
        derivedShotCount = assessment.DerivedShotCount,
        totalPieceCount = assessment.TotalPieceCount,
        goodPieceCount = assessment.GoodPieceCount,
        scrapPieceCount = assessment.ScrapPieceCount,
        availabilityPercent = assessment.AvailabilityPercent,
        performanceRawPercent = assessment.PerformanceRawPercent,
        performancePercent = assessment.PerformancePercent,
        qualityPercent = assessment.QualityPercent,
        oeePercent = assessment.OeePercent,
        drivers = assessment.Drivers,
        evidence = assessment.Evidence,
        missingFields = assessment.MissingFields,
        conflicts = assessment.Conflicts,
        sourceSystem = assessment.SourceSystem,
        sourceFreshnessUtc = assessment.SourceFreshnessUtc,
        isSimulatorDerived = assessment.IsSimulatorDerived,
        calculationHonestyDisclaimer = assessment.CalculationHonestyDisclaimer,
        simulatorHonestyDisclaimer = assessment.SimulatorHonestyDisclaimer,
        evaluatedAtUtc = assessment.EvaluatedAtUtc,
    };

    private static object MapLossDetail(ProcessOeeLossDetailReadModel detail) => new
    {
        assessmentId = detail.AssessmentId,
        evaluationWindowId = detail.EvaluationWindowId,
        assessmentStatus = detail.AssessmentStatus,
        assessmentStatusLabel = ProcessOeeValueConversions.ToDisplayStatus(detail.AssessmentStatus),
        ruleSetVersion = detail.RuleSetVersion,
        leadingLossMethod = detail.LeadingLossMethod,
        leadingLoss = detail.LeadingLoss is null ? null : MapLossDriver(detail.LeadingLoss),
        drivers = detail.Drivers.Select(MapLossDriver),
        evidence = detail.Evidence,
        missingFields = detail.MissingFields,
        conflicts = detail.Conflicts,
        sourceSystem = detail.SourceSystem,
        sourceFreshnessUtc = detail.SourceFreshnessUtc,
        isSimulatorDerived = detail.IsSimulatorDerived,
        calculationHonestyDisclaimer = detail.CalculationHonestyDisclaimer,
        simulatorHonestyDisclaimer = detail.SimulatorHonestyDisclaimer,
    };

    private static object MapLossDriver(ProcessOeeLossDriverReadModel driver) => new
    {
        id = driver.Id,
        reasonCode = driver.ReasonCode,
        category = driver.Category,
        severity = driver.Severity,
        operationalPriority = driver.OperationalPriority,
        observedValue = driver.ObservedValue,
        expectedValue = driver.ExpectedValue,
        absoluteLossValue = driver.AbsoluteLossValue,
        affectedDurationSeconds = driver.AffectedDurationSeconds,
        affectedQuantity = driver.AffectedQuantity,
        sourceReferences = driver.SourceReferences,
        evidenceTimestampUtc = driver.EvidenceTimestampUtc,
        freshnessState = driver.FreshnessState,
        explanationCode = driver.ExplanationCode,
        dataLimitations = driver.DataLimitations,
        ruleSetVersion = driver.RuleSetVersion,
        isLeadingLoss = driver.IsLeadingLoss,
        normalizedImpactPoints = driver.NormalizedImpactPoints,
    };

    private static object MapStabilityDetail(ProcessOeeStabilityDetailReadModel detail) => new
    {
        assessmentId = detail.AssessmentId,
        evaluationWindowId = detail.EvaluationWindowId,
        assessmentStatus = detail.AssessmentStatus,
        assessmentStatusLabel = ProcessOeeValueConversions.ToDisplayStatus(detail.AssessmentStatus),
        resultState = detail.ResultState,
        ruleSetVersion = detail.RuleSetVersion,
        medianCycleMs = detail.MedianCycleMs,
        madCycleMs = detail.MadCycleMs,
        sampleCount = detail.SampleCount,
        driftDirection = detail.DriftDirection,
        microStopCount = detail.MicroStopCount,
        microStopTotalSeconds = detail.MicroStopTotalSeconds,
        microStopMedianSeconds = detail.MicroStopMedianSeconds,
        classifiedDowntimeSeconds = detail.ClassifiedDowntimeSeconds,
        unclassifiedDowntimeSeconds = detail.UnclassifiedDowntimeSeconds,
        drivers = detail.Drivers.Select(MapStabilityDriver),
        downtimeBreakdown = detail.DowntimeBreakdown.Select(MapDowntimeBreakdownRow),
        evidence = detail.Evidence,
        missingFields = detail.MissingFields,
        conflicts = detail.Conflicts,
        sourceSystem = detail.SourceSystem,
        sourceFreshnessUtc = detail.SourceFreshnessUtc,
        isSimulatorDerived = detail.IsSimulatorDerived,
        calculationHonestyDisclaimer = detail.CalculationHonestyDisclaimer,
        simulatorHonestyDisclaimer = detail.SimulatorHonestyDisclaimer,
    };

    private static object MapStabilityDriver(ProcessOeeStabilityDriverReadModel driver) => new
    {
        id = driver.Id,
        reasonCode = driver.ReasonCode,
        category = driver.Category,
        resultState = driver.ResultState,
        severity = driver.Severity,
        operationalPriority = driver.OperationalPriority,
        observedValue = driver.ObservedValue,
        referenceValue = driver.ReferenceValue,
        unit = driver.Unit,
        sampleCount = driver.SampleCount,
        timeRangeStartUtc = driver.TimeRangeStartUtc,
        timeRangeEndUtc = driver.TimeRangeEndUtc,
        sourceReferences = driver.SourceReferences,
        freshnessState = driver.FreshnessState,
        explanationCode = driver.ExplanationCode,
        dataLimitations = driver.DataLimitations,
        ruleSetVersion = driver.RuleSetVersion,
    };

    private static object MapDowntimeBreakdownRow(ProcessOeeDowntimeBreakdownReadModel row) => new
    {
        reasonCode = row.ReasonCode,
        durationSeconds = row.DurationSeconds,
        occurrenceCount = row.OccurrenceCount,
        sortRank = row.SortRank,
        isClassified = row.IsClassified,
        durationPercent = row.DurationPercent,
    };

    private static object MapDowntimePareto(ProcessOeeDowntimeParetoReadModel pareto) => new
    {
        assessmentId = pareto.AssessmentId,
        windowStartUtc = pareto.WindowStartUtc,
        windowEndUtc = pareto.WindowEndUtc,
        totalDowntimeSeconds = pareto.TotalDowntimeSeconds,
        classifiedDurationSeconds = pareto.ClassifiedDurationSeconds,
        unclassifiedDurationSeconds = pareto.UnclassifiedDurationSeconds,
        ruleSetVersion = pareto.RuleSetVersion,
        rows = pareto.Rows.Select(MapDowntimeBreakdownRow),
    };

    private static object MapShiftComparison(ProcessOeeShiftComparisonReadModel comparison) => new
    {
        periodStartUtc = comparison.PeriodStartUtc,
        periodEndUtc = comparison.PeriodEndUtc,
        isComparable = comparison.IsComparable,
        comparabilityWarningCode = comparison.ComparabilityWarningCode,
        ruleSetVersion = comparison.RuleSetVersion,
        summary = comparison.Summary is null ? null : new
        {
            highestOeeShiftLabel = comparison.Summary.HighestOeeShiftLabel,
            lowestAvailabilityShiftLabel = comparison.Summary.LowestAvailabilityShiftLabel,
            highestDowntimeShiftLabel = comparison.Summary.HighestDowntimeShiftLabel,
            highestVariabilityShiftLabel = comparison.Summary.HighestVariabilityShiftLabel,
        },
        rows = comparison.Rows.Select(row => new
        {
            assessmentId = row.AssessmentId,
            shiftCode = row.ShiftCode,
            shiftLabel = row.ShiftLabel,
            windowStartUtc = row.WindowStartUtc,
            windowEndUtc = row.WindowEndUtc,
            plannedProductionSeconds = row.PlannedProductionSeconds,
            operatingSeconds = row.OperatingSeconds,
            availabilityPercent = row.AvailabilityPercent,
            performancePercent = row.PerformancePercent,
            qualityPercent = row.QualityPercent,
            oeePercent = row.OeePercent,
            stabilityResultState = row.StabilityResultState,
            downtimeSeconds = row.DowntimeSeconds,
            microStopCount = row.MicroStopCount,
            dataSufficiency = row.DataSufficiency,
            freshnessState = row.FreshnessState,
            isComparable = row.IsComparable,
        }),
    };
}

public sealed record CreateProcessOeeWindowRequest(
    Guid MachineAssetId,
    string? ProductionOrderReference,
    string? MoldReference,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    int? PlannedProductionSeconds,
    decimal? IdealCycleTimeSeconds,
    int? EffectiveCavityCount,
    string? SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    string IdempotencyKey);

public sealed record UpdateProcessOeeWindowRequest(
    uint ExpectedVersion,
    string? ProductionOrderReference,
    string? MoldReference,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    int? PlannedProductionSeconds,
    decimal? IdealCycleTimeSeconds,
    int? EffectiveCavityCount,
    string? SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc);

public sealed record EvaluateProcessOeeRequest(string IdempotencyKey);

public sealed record EvaluateBoundedProcessOeeRequest(
    Guid MachineAssetId,
    string? ProductionOrderReference,
    string? MoldReference,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    int? PlannedProductionSeconds,
    decimal? IdealCycleTimeSeconds,
    int? EffectiveCavityCount,
    string IdempotencyKey);

public sealed record EvaluateShiftComparisonRequest(
    Guid? MachineAssetId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    string IdempotencyKey);

public static class ProcessOeeServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSProcessOee(this IServiceCollection services)
    {
        services.AddScoped<ProcessOeeEvidenceCollector>();
        services.AddScoped<ProcessOeeStabilityDataCollector>();
        services.AddScoped<IProcessOeeRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableProcessOeeRepository()
                : new ProcessOeeRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<ProcessOeeEvidenceCollector>(),
                    serviceProvider.GetRequiredService<ProcessOeeStabilityDataCollector>());
        });

        return services;
    }
}
