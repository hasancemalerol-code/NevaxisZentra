using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.OperationalRisk;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.OperationalRisk;

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

public static class OperationalRiskServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSOperationalRisk(this IServiceCollection services)
    {
        services.AddScoped<IOperationalRiskRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableOperationalRiskRepository()
                : new OperationalRiskRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}
