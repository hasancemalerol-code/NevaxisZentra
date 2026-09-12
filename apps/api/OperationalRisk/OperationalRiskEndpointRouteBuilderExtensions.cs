using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.OperationalRisk;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisFactOS.Api.OperationalRisk;

public static class OperationalRiskEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapOperationalRiskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/operational-risks").RequireAuthorization();

        group.MapGet("/cases", ListCasesAsync);
        group.MapGet("/cases/{caseId:guid}", GetCaseAsync);
        group.MapPost("/cases", CreateCaseAsync);
        group.MapPost("/cases/{caseId:guid}/evidence", AppendEvidenceAsync);
        group.MapPost("/cases/{caseId:guid}/assessment", UpdateAssessmentAsync);
        group.MapPost("/cases/{caseId:guid}/transitions", TransitionCaseAsync);

        group.MapGet("/detection/profiles", ListDetectionProfilesAsync);
        group.MapPost("/detection/profiles", CreateDetectionProfileAsync);
        group.MapPut("/detection/profiles/{profileId:guid}", UpdateDetectionProfileAsync);
        group.MapGet("/detection/runs", ListDetectionRunsAsync);
        group.MapGet("/detection/runs/{runId:guid}", GetDetectionRunAsync);
        group.MapPost("/detection/runs", ExecuteDetectionRunAsync);

        return endpoints;
    }

    private static async Task<IResult> ListCasesAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? category,
        [FromQuery] string? scopeType,
        [FromQuery] string? lifecycleStatus,
        [FromQuery] string? severityBand,
        [FromQuery] string? assessmentOutcome,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IOperationalRiskRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await OperationalRiskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListCasesAsync(
            context,
            new OperationalRiskCaseListFilters(
                category,
                scopeType,
                lifecycleStatus,
                severityBand,
                assessmentOutcome,
                page,
                pageSize),
            cancellationToken);

        var canManage = await OperationalRiskEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapSummary),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
            ruleSetVersion = OperationalRiskConstants.RuleSetVersion,
            provenanceDisclaimer = OperationalRiskConstants.DevelopmentPilotProvenance,
            capabilities = new { canRead = true, canManage },
        });
    }

    private static async Task<IResult> GetCaseAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await OperationalRiskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var riskCase = await repository.GetCaseByIdAsync(context, caseId, cancellationToken);
        return riskCase is null
            ? OperationalRiskEndpointHelpers.MapFailure(OperationalRiskFailureCodes.NotFound, "Risk case not found.")
            : TypedResults.Ok(MapDetail(riskCase));
    }

    private static async Task<IResult> CreateCaseAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateOperationalRiskCaseRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await OperationalRiskEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateCaseAsync(
            context,
            new CreateOperationalRiskCaseCommand(
                body.Category,
                body.ScopeType,
                body.ScopeReference,
                body.Title,
                body.Description,
                body.EvidenceState,
                body.LikelihoodLevel,
                body.ImpactLevel,
                body.SourceModule,
                body.SourceReference,
                body.SourceProvenance,
                body.FirstDetectedAtUtc,
                body.LastObservedAtUtc,
                body.IdempotencyKey),
            cancellationToken);

        return OperationalRiskEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> AppendEvidenceAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        [FromBody] AppendOperationalRiskEvidenceRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await OperationalRiskEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.AppendEvidenceAsync(
            context,
            new AppendOperationalRiskEvidenceCommand(
                caseId,
                body.EvidenceTypeCode,
                body.EvidenceState,
                body.ObservedAtUtc,
                body.SourceModule,
                body.SourceReference,
                body.Summary,
                body.NumericValue,
                body.Unit,
                body.ComparisonReference,
                body.SourceProvenance,
                body.IdempotencyKey),
            cancellationToken);

        return OperationalRiskEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateAssessmentAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateOperationalRiskAssessmentRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await OperationalRiskEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdateAssessmentAsync(
            context,
            new UpdateOperationalRiskAssessmentCommand(
                caseId,
                body.ExpectedVersion,
                body.EvidenceState,
                body.LikelihoodLevel,
                body.ImpactLevel,
                body.LastObservedAtUtc),
            cancellationToken);

        return OperationalRiskEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> TransitionCaseAsync(
        Guid caseId,
        [FromQuery] Guid? facilityId,
        [FromBody] TransitionOperationalRiskCaseRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await OperationalRiskEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.TransitionCaseAsync(
            context,
            new TransitionOperationalRiskCaseCommand(
                caseId,
                body.ExpectedVersion,
                body.TargetStatus,
                body.Note,
                body.SourceProvenance),
            cancellationToken);

        return OperationalRiskEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> ListDetectionProfilesAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalRiskDetectionRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await OperationalRiskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var profiles = await repository.ListProfilesAsync(context, cancellationToken);
        return TypedResults.Ok(new
        {
            items = profiles.Select(MapDetectionProfile),
            ruleSetVersion = OperationalRiskDetectionConstants.RuleSetVersion,
            provenanceDisclaimer = OperationalRiskDetectionConstants.DevelopmentPilotProvenance,
        });
    }

    private static async Task<IResult> CreateDetectionProfileAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateOperationalRiskDetectionProfileRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalRiskDetectionRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await OperationalRiskEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateProfileAsync(
            context,
            new CreateOperationalRiskDetectionProfileCommand(
                body.ProfileCode,
                body.DisplayName,
                body.SourceProvenance,
                body.IdempotencyKey,
                body.Rules.Select(r => new CreateOperationalRiskDetectionRuleCommand(
                    r.RuleCode,
                    r.SourceModule,
                    r.SourceOutcome,
                    r.RiskCategory,
                    r.DefaultLikelihood,
                    r.DefaultImpact,
                    r.ThresholdVarianceBand,
                    r.DriverReasonCode,
                    r.PriorityOrder,
                    r.IsActive)).ToList()),
            cancellationToken);

        return OperationalRiskEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateDetectionProfileAsync(
        Guid profileId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateOperationalRiskDetectionProfileRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalRiskDetectionRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await OperationalRiskEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdateProfileAsync(
            context,
            new UpdateOperationalRiskDetectionProfileCommand(
                profileId,
                body.ExpectedVersion,
                body.DisplayName,
                body.Status,
                body.SourceProvenance),
            cancellationToken);

        return OperationalRiskEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> ListDetectionRunsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IOperationalRiskDetectionRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await OperationalRiskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var runs = await repository.ListRunsAsync(context, page, pageSize, cancellationToken);
        return TypedResults.Ok(new
        {
            items = runs.Select(MapDetectionRunSummary),
            page,
            pageSize,
            ruleSetVersion = OperationalRiskDetectionConstants.RuleSetVersion,
        });
    }

    private static async Task<IResult> GetDetectionRunAsync(
        Guid runId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalRiskDetectionRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await OperationalRiskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var run = await repository.GetRunByIdAsync(context, runId, cancellationToken);
        return run is null
            ? OperationalRiskEndpointHelpers.MapFailure(OperationalRiskFailureCodes.NotFound, "Detection run not found.")
            : TypedResults.Ok(MapDetectionRunDetail(run));
    }

    private static async Task<IResult> ExecuteDetectionRunAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] ExecuteOperationalRiskDetectionRunRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IOperationalRiskDetectionRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await OperationalRiskEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ExecuteRunAsync(
            context,
            new ExecuteOperationalRiskDetectionRunCommand(
                body.ProfileId,
                body.WindowStartUtc,
                body.WindowEndUtc,
                body.SourceProvenance,
                body.IdempotencyKey),
            cancellationToken);

        return OperationalRiskEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static object MapDetectionProfile(OperationalRiskDetectionProfileReadModel profile) => new
    {
        id = profile.Id,
        profileCode = profile.ProfileCode,
        displayName = profile.DisplayName,
        status = profile.Status,
        ruleSetVersion = profile.RuleSetVersion,
        sourceProvenance = profile.SourceProvenance,
        version = profile.Version,
        rules = profile.Rules.Select(r => new
        {
            id = r.Id,
            ruleCode = r.RuleCode,
            sourceModule = r.SourceModule,
            sourceOutcome = r.SourceOutcome,
            riskCategory = r.RiskCategory,
            defaultLikelihood = r.DefaultLikelihood,
            defaultImpact = r.DefaultImpact,
            thresholdVarianceBand = r.ThresholdVarianceBand,
            driverReasonCode = r.DriverReasonCode,
            priorityOrder = r.PriorityOrder,
            isActive = r.IsActive,
        }),
        isPilotData = profile.IsPilotData,
    };

    private static object MapDetectionRunSummary(OperationalRiskDetectionRunSummaryReadModel run) => new
    {
        id = run.Id,
        runReference = run.RunReference,
        profileId = run.ProfileId,
        profileCode = run.ProfileCode,
        windowStartUtc = run.WindowStartUtc,
        windowEndUtc = run.WindowEndUtc,
        status = run.Status,
        sourceCount = run.SourceCount,
        resultCount = run.ResultCount,
        ruleSetVersion = run.RuleSetVersion,
        createdAtUtc = run.CreatedAtUtc,
        completedAtUtc = run.CompletedAtUtc,
        isPilotData = run.IsPilotData,
    };

    private static object MapDetectionRunDetail(OperationalRiskDetectionRunDetailReadModel run) => new
    {
        id = run.Id,
        runReference = run.RunReference,
        profileId = run.ProfileId,
        profileCode = run.ProfileCode,
        windowStartUtc = run.WindowStartUtc,
        windowEndUtc = run.WindowEndUtc,
        status = run.Status,
        sourceCount = run.SourceCount,
        resultCount = run.ResultCount,
        ruleSetVersion = run.RuleSetVersion,
        sourceProvenance = run.SourceProvenance,
        createdAtUtc = run.CreatedAtUtc,
        completedAtUtc = run.CompletedAtUtc,
        results = run.Results.Select(r => new
        {
            id = r.Id,
            sourceModule = r.SourceModule,
            sourceReference = r.SourceReference,
            riskCategory = r.RiskCategory,
            scopeType = r.ScopeType,
            scopeReference = r.ScopeReference,
            evidenceState = r.EvidenceState,
            sourceOutcome = r.SourceOutcome,
            detectionOutcome = r.DetectionOutcome,
            actualValue = r.ActualValue,
            thresholdValue = r.ThresholdValue,
            unit = r.Unit,
            normalizedVariance = r.NormalizedVariance,
            likelihoodLevel = r.LikelihoodLevel,
            impactLevel = r.ImpactLevel,
            riskScore = r.RiskScore,
            severityBand = r.SeverityBand,
            leadingDriverCode = r.LeadingDriverCode,
            linkedCaseId = r.LinkedCaseId,
            detectionKey = r.DetectionKey,
            sourceAssessmentReference = r.SourceAssessmentReference,
            evaluatedAtUtc = r.EvaluatedAtUtc,
            drivers = r.Drivers.Select(d => new
            {
                id = d.Id,
                reasonCode = d.ReasonCode,
                severity = d.Severity,
                actualValue = d.ActualValue,
                thresholdValue = d.ThresholdValue,
                unit = d.Unit,
                normalizedVariance = d.NormalizedVariance,
                supportingText = d.SupportingText,
                sortOrder = d.SortOrder,
            }),
        }),
        isPilotData = run.IsPilotData,
    };

    private static object MapSummary(OperationalRiskCaseSummaryReadModel item) => new
    {
        id = item.Id,
        caseReference = item.CaseReference,
        category = item.Category,
        scopeType = item.ScopeType,
        scopeReference = item.ScopeReference,
        title = item.Title,
        evidenceState = item.EvidenceState,
        likelihoodLevel = item.LikelihoodLevel,
        impactLevel = item.ImpactLevel,
        riskScore = item.RiskScore,
        severityBand = item.SeverityBand,
        assessmentOutcome = item.AssessmentOutcome,
        lifecycleStatus = item.LifecycleStatus,
        ruleSetVersion = item.RuleSetVersion,
        firstDetectedAtUtc = item.FirstDetectedAtUtc,
        lastObservedAtUtc = item.LastObservedAtUtc,
        version = item.Version,
        isPilotData = item.IsPilotData,
    };

    private static object MapDetail(OperationalRiskCaseDetailReadModel item) => new
    {
        id = item.Id,
        caseReference = item.CaseReference,
        category = item.Category,
        scopeType = item.ScopeType,
        scopeReference = item.ScopeReference,
        title = item.Title,
        description = item.Description,
        evidenceState = item.EvidenceState,
        likelihoodLevel = item.LikelihoodLevel,
        impactLevel = item.ImpactLevel,
        riskScore = item.RiskScore,
        severityBand = item.SeverityBand,
        assessmentOutcome = item.AssessmentOutcome,
        lifecycleStatus = item.LifecycleStatus,
        ruleSetVersion = item.RuleSetVersion,
        sourceModule = item.SourceModule,
        sourceReference = item.SourceReference,
        sourceProvenance = item.SourceProvenance,
        firstDetectedAtUtc = item.FirstDetectedAtUtc,
        lastObservedAtUtc = item.LastObservedAtUtc,
        version = item.Version,
        evidence = item.Evidence.Select(e => new
        {
            id = e.Id,
            evidenceTypeCode = e.EvidenceTypeCode,
            evidenceState = e.EvidenceState,
            observedAtUtc = e.ObservedAtUtc,
            sourceModule = e.SourceModule,
            sourceReference = e.SourceReference,
            summary = e.Summary,
            numericValue = e.NumericValue,
            unit = e.Unit,
            comparisonReference = e.ComparisonReference,
            sourceProvenance = e.SourceProvenance,
            createdAtUtc = e.CreatedAtUtc,
            isPilotData = e.IsPilotData,
        }),
        statusHistory = item.StatusHistory.Select(h => new
        {
            id = h.Id,
            previousStatus = h.PreviousStatus,
            newStatus = h.NewStatus,
            reasonNote = h.ReasonNote,
            actorSubjectId = h.ActorSubjectId,
            occurredAtUtc = h.OccurredAtUtc,
            expectedCaseVersion = h.ExpectedCaseVersion,
            sourceProvenance = h.SourceProvenance,
        }),
        isPilotData = item.IsPilotData,
    };
}

public sealed record CreateOperationalRiskCaseRequest(
    string Category,
    string ScopeType,
    string? ScopeReference,
    string Title,
    string Description,
    string EvidenceState,
    int? LikelihoodLevel,
    int? ImpactLevel,
    string? SourceModule,
    string? SourceReference,
    string SourceProvenance,
    DateTimeOffset FirstDetectedAtUtc,
    DateTimeOffset LastObservedAtUtc,
    string IdempotencyKey);

public sealed record AppendOperationalRiskEvidenceRequest(
    string EvidenceTypeCode,
    string EvidenceState,
    DateTimeOffset ObservedAtUtc,
    string? SourceModule,
    string? SourceReference,
    string Summary,
    decimal? NumericValue,
    string? Unit,
    string? ComparisonReference,
    string SourceProvenance,
    string IdempotencyKey);

public sealed record UpdateOperationalRiskAssessmentRequest(
    uint ExpectedVersion,
    string EvidenceState,
    int? LikelihoodLevel,
    int? ImpactLevel,
    DateTimeOffset? LastObservedAtUtc);

public sealed record TransitionOperationalRiskCaseRequest(
    uint ExpectedVersion,
    string TargetStatus,
    string? Note,
    string SourceProvenance);

public sealed record CreateOperationalRiskDetectionProfileRequest(
    string ProfileCode,
    string DisplayName,
    string SourceProvenance,
    string IdempotencyKey,
    IReadOnlyList<CreateOperationalRiskDetectionRuleRequest> Rules);

public sealed record CreateOperationalRiskDetectionRuleRequest(
    string RuleCode,
    string SourceModule,
    string SourceOutcome,
    string RiskCategory,
    int DefaultLikelihood,
    int DefaultImpact,
    decimal? ThresholdVarianceBand,
    string DriverReasonCode,
    int PriorityOrder,
    bool IsActive);

public sealed record UpdateOperationalRiskDetectionProfileRequest(
    uint ExpectedVersion,
    string DisplayName,
    string Status,
    string SourceProvenance);

public sealed record ExecuteOperationalRiskDetectionRunRequest(
    Guid ProfileId,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    string SourceProvenance,
    string IdempotencyKey);

public static class OperationalRiskServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSOperationalRisk(this IServiceCollection services)
    {
        services.AddScoped<IOperationalRiskRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableOperationalRiskRepository()
                : new OperationalRiskRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        services.AddScoped<OperationalRiskEnergySourceAdapter>();
        services.AddScoped<OperationalRiskIotPlcSourceAdapter>();
        services.AddScoped<OperationalRiskMaintenanceSourceAdapter>();
        services.AddScoped<OperationalRiskDevelopmentSimulatorSource>();
        services.AddScoped<IEnumerable<IOperationalRiskEvidenceSource>>(serviceProvider =>
        [
            serviceProvider.GetRequiredService<OperationalRiskEnergySourceAdapter>(),
            serviceProvider.GetRequiredService<OperationalRiskIotPlcSourceAdapter>(),
            serviceProvider.GetRequiredService<OperationalRiskMaintenanceSourceAdapter>(),
            new OperationalRiskUnavailableSourceAdapter(OperationalRiskSourceModules.Material),
            new OperationalRiskUnavailableSourceAdapter(OperationalRiskSourceModules.Supplier),
            new OperationalRiskUnavailableSourceAdapter(OperationalRiskSourceModules.Order),
            new OperationalRiskUnavailableSourceAdapter(OperationalRiskSourceModules.Quality),
            new OperationalRiskUnavailableSourceAdapter(OperationalRiskSourceModules.Production),
            new OperationalRiskUnavailableSourceAdapter(OperationalRiskSourceModules.Cost),
            serviceProvider.GetRequiredService<OperationalRiskDevelopmentSimulatorSource>(),
        ]);

        services.AddScoped<IOperationalRiskDetectionRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisFactOSDbContext>();
            return dbContext is null
                ? new UnavailableOperationalRiskDetectionRepository()
                : new OperationalRiskDetectionRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IEnumerable<IOperationalRiskEvidenceSource>>());
        });

        return services;
    }
}
