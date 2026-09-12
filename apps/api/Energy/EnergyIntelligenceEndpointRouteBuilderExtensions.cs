using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Energy;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisFactOS.Api.Energy;

public static class EnergyIntelligenceEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapEnergyIntelligenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/energy").RequireAuthorization();

        group.MapGet("/intelligence/workspace", GetIntelligenceWorkspaceAsync);
        group.MapGet("/baselines", ListBaselinesAsync);
        group.MapPost("/baselines/evaluate", EvaluateBaselineAsync);
        group.MapGet("/targets", ListTargetsAsync);
        group.MapPost("/targets", CreateTargetAsync);
        group.MapPut("/targets/{reference}", UpdateTargetAsync);
        group.MapGet("/assessments", ListAssessmentsAsync);
        group.MapGet("/assessments/{reference}", GetAssessmentAsync);
        group.MapPost("/assessments/evaluate", EvaluateAssessmentAsync);

        return endpoints;
    }

    private static async Task<IResult> GetIntelligenceWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEnergyIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EnergyEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var workspace = await repository.GetWorkspaceAsync(context, cancellationToken);
        var canManage = await EnergyEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            baselineCount = workspace.BaselineCount,
            activeTargetCount = workspace.ActiveTargetCount,
            recentAssessmentCount = workspace.RecentAssessmentCount,
            ruleSetVersion = workspace.RuleSetVersion,
            provenanceDisclaimer = workspace.ProvenanceDisclaimer,
            hasDevelopmentPilotData = workspace.HasDevelopmentPilotData,
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> ListBaselinesAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? comparisonDimension,
        [FromQuery] string? measurementPointReference,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IEnergyIntelligenceRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await EnergyEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListBaselinesAsync(
            context,
            new EnergyBaselineListFilters(comparisonDimension, measurementPointReference, page, pageSize),
            cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapBaseline),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
        });
    }

    private static async Task<IResult> EvaluateBaselineAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] EvaluateEnergyBaselineRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IEnergyIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EnergyEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var command = new EvaluateEnergyBaselineCommand(
            body.ComparisonDimension,
            body.MeasurementPointReference,
            body.ReportingWindowDurationSeconds,
            body.ShiftReference,
            body.ProductionContextReference,
            body.RelativeTolerance,
            body.IdempotencyKey);

        var result = await repository.EvaluateBaselineAsync(context, command, cancellationToken);
        return EnergyEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> ListTargetsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? targetScope,
        [FromQuery] string? targetMetric,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IEnergyIntelligenceRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await EnergyEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListTargetsAsync(
            context,
            new EnergyTargetListFilters(targetScope, targetMetric, status, page, pageSize),
            cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapTarget),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
        });
    }

    private static async Task<IResult> CreateTargetAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateEnergyTargetRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IEnergyIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EnergyEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var command = new CreateEnergyTargetCommand(
            body.TargetReference,
            body.TargetScope,
            body.ScopeReference,
            body.TargetMetric,
            body.TargetValue,
            body.Unit,
            body.Tolerance,
            body.EffectiveFromUtc,
            body.EffectiveToUtc,
            body.SourceProvenance,
            body.IdempotencyKey);

        var result = await repository.CreateTargetAsync(context, command, cancellationToken);
        return EnergyEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateTargetAsync(
        string reference,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateEnergyTargetRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IEnergyIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EnergyEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var command = new UpdateEnergyTargetCommand(
            reference,
            body.TargetValue,
            body.Tolerance,
            body.EffectiveFromUtc,
            body.EffectiveToUtc,
            body.SourceProvenance,
            body.Status,
            body.ExpectedVersion);

        var result = await repository.UpdateTargetAsync(context, command, cancellationToken);
        return EnergyEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> ListAssessmentsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? comparisonDimension,
        [FromQuery] string? outcome,
        [FromQuery] string? measurementPointReference,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IEnergyIntelligenceRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await EnergyEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListAssessmentsAsync(
            context,
            new EnergyIntelligenceAssessmentListFilters(
                comparisonDimension,
                outcome,
                measurementPointReference,
                page,
                pageSize),
            cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapAssessmentSummary),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
        });
    }

    private static async Task<IResult> GetAssessmentAsync(
        string reference,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IEnergyIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EnergyEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var assessment = await repository.GetAssessmentByReferenceAsync(context, reference, cancellationToken);
        if (assessment is null)
        {
            return EnergyEndpointHelpers.MapFailure(
                EnergyIntelligenceFailureCodes.AssessmentNotFound,
                "Assessment not found.");
        }

        return TypedResults.Ok(new
        {
            assessment = MapAssessmentDetail(assessment),
        });
    }

    private static async Task<IResult> EvaluateAssessmentAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] EvaluateEnergyIntelligenceAssessmentRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IEnergyIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await EnergyEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var command = new EvaluateEnergyIntelligenceAssessmentCommand(
            body.AssessmentReference,
            body.ComparisonDimension,
            body.MeasurementPointReference,
            body.EvaluationWindowStartUtc,
            body.EvaluationWindowEndUtc,
            body.BaselineReference,
            body.ConsumptionTargetReference,
            body.DemandTargetReference,
            body.IdleTargetReference,
            body.IntensityTargetReference,
            body.ProductionContextReference,
            body.IdempotencyKey);

        var result = await repository.EvaluateAssessmentAsync(context, command, cancellationToken);
        return EnergyEndpointHelpers.MapWriteResult(result);
    }

    private static object MapBaseline(EnergyBaselineProfileReadModel baseline) => new
    {
        id = baseline.Id,
        baselineReference = baseline.BaselineReference,
        comparisonDimension = baseline.ComparisonDimension,
        measurementPointCode = baseline.MeasurementPointCode,
        machineAssetId = baseline.MachineAssetId,
        shiftReference = baseline.ShiftReference,
        productionContextReference = baseline.ProductionContextReference,
        reportingWindowDurationSeconds = baseline.ReportingWindowDurationSeconds,
        sampleCount = baseline.SampleCount,
        baselineConsumedKwh = baseline.BaselineConsumedKwh,
        baselineMaximumDemandKw = baseline.BaselineMaximumDemandKw,
        medianCenter = baseline.MedianCenter,
        madDispersion = baseline.MadDispersion,
        normalizedDispersion = baseline.NormalizedDispersion,
        relativeTolerance = baseline.RelativeTolerance,
        upperThresholdConsumedKwh = baseline.UpperThresholdConsumedKwh,
        upperThresholdDemandKw = baseline.UpperThresholdDemandKw,
        calculationMethod = baseline.CalculationMethod,
        evidenceStatus = baseline.EvidenceStatus,
        ruleSetVersion = baseline.RuleSetVersion,
        provenance = baseline.Provenance,
        version = baseline.Version,
        isPilotData = baseline.IsPilotData,
    };

    private static object MapTarget(EnergyTargetReadModel target) => new
    {
        id = target.Id,
        targetReference = target.TargetReference,
        targetScope = target.TargetScope,
        scopeReference = target.ScopeReference,
        targetMetric = target.TargetMetric,
        targetValue = target.TargetValue,
        unit = target.Unit,
        tolerance = target.Tolerance,
        effectiveFromUtc = target.EffectiveFromUtc,
        effectiveToUtc = target.EffectiveToUtc,
        sourceProvenance = target.SourceProvenance,
        status = target.Status,
        version = target.Version,
        isPilotData = target.IsPilotData,
    };

    private static object MapAssessmentSummary(EnergyIntelligenceAssessmentSummaryReadModel assessment) => new
    {
        id = assessment.Id,
        assessmentReference = assessment.AssessmentReference,
        comparisonDimension = assessment.ComparisonDimension,
        measurementPointCode = assessment.MeasurementPointCode,
        evaluationWindowStartUtc = assessment.EvaluationWindowStartUtc,
        evaluationWindowEndUtc = assessment.EvaluationWindowEndUtc,
        actualConsumedKwh = assessment.ActualConsumedKwh,
        actualMaximumDemandKw = assessment.ActualMaximumDemandKw,
        varianceConsumedKwh = assessment.VarianceConsumedKwh,
        variancePercent = assessment.VariancePercent,
        outcome = assessment.Outcome,
        leadingDriverCode = assessment.LeadingDriverCode,
        potentialReducibleKwh = assessment.PotentialReducibleKwh,
        estimatedCostImpact = assessment.EstimatedCostImpact,
        costCurrencyCode = assessment.CostCurrencyCode,
        opportunityLabel = assessment.OpportunityLabel,
        evidenceStatus = assessment.EvidenceStatus,
        evaluatedAtUtc = assessment.EvaluatedAtUtc,
        isPilotData = assessment.IsPilotData,
    };

    private static object MapAssessmentDetail(EnergyIntelligenceAssessmentDetailReadModel assessment) => new
    {
        summary = MapAssessmentSummary(assessment.Summary),
        baselineReference = assessment.BaselineReference,
        targetReference = assessment.TargetReference,
        drivers = assessment.Drivers.Select(d => new
        {
            id = d.Id,
            reasonCode = d.ReasonCode,
            labelTr = d.LabelTr,
            severity = d.Severity,
            category = d.Category,
            actualValue = d.ActualValue,
            actualUnit = d.ActualUnit,
            comparisonValue = d.ComparisonValue,
            comparisonUnit = d.ComparisonUnit,
            normalizedVariance = d.NormalizedVariance,
            explanation = d.Explanation,
            evidenceSource = d.EvidenceSource,
            freshnessState = d.FreshnessState,
            isLeadingDriver = d.IsLeadingDriver,
        }),
    };

    public static IServiceCollection AddNevaxisFactOSEnergyIntelligence(this IServiceCollection services)
    {
        services.AddScoped<IEnergyIntelligenceRepository>(sp =>
        {
            var dbContext = sp.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableEnergyIntelligenceRepository()
                : new EnergyIntelligenceRepository(dbContext, sp.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}

internal sealed record EvaluateEnergyBaselineRequest(
    string ComparisonDimension,
    string MeasurementPointReference,
    int ReportingWindowDurationSeconds,
    string? ShiftReference,
    string? ProductionContextReference,
    decimal? RelativeTolerance,
    string? IdempotencyKey);

internal sealed record CreateEnergyTargetRequest(
    string TargetReference,
    string TargetScope,
    string ScopeReference,
    string TargetMetric,
    decimal TargetValue,
    string Unit,
    decimal Tolerance,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset EffectiveToUtc,
    string SourceProvenance,
    string? IdempotencyKey);

internal sealed record UpdateEnergyTargetRequest(
    decimal TargetValue,
    decimal Tolerance,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset EffectiveToUtc,
    string SourceProvenance,
    string Status,
    uint ExpectedVersion);

internal sealed record EvaluateEnergyIntelligenceAssessmentRequest(
    string AssessmentReference,
    string ComparisonDimension,
    string MeasurementPointReference,
    DateTimeOffset EvaluationWindowStartUtc,
    DateTimeOffset EvaluationWindowEndUtc,
    string? BaselineReference,
    string? ConsumptionTargetReference,
    string? DemandTargetReference,
    string? IdleTargetReference,
    string? IntensityTargetReference,
    string? ProductionContextReference,
    string? IdempotencyKey);
