using CrevaFactOS.Api.AiTasks;
using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.AiTasks;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.CommercialIntelligence;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CrevaFactOS.Api.CommercialIntelligence;

internal static class CommercialPricingScenarioEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCommercialPricingScenarioEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/commercial-intelligence/pricing-scenarios").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapGet("/", ListScenariosAsync);
        group.MapGet("/{scenarioId:guid}", GetScenarioAsync);
        group.MapPost("/", CreateScenarioAsync);
        group.MapPut("/{scenarioId:guid}", UpdateScenarioAsync);
        group.MapPost("/{scenarioId:guid}/evaluate", EvaluateScenarioAsync);
        group.MapPost("/{scenarioId:guid}/transitions", TransitionScenarioAsync);
        group.MapPost("/{scenarioId:guid}/task-draft", CreateTaskDraftAsync);

        return endpoints;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialPricingScenarioRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var canManage = await CommercialIntelligenceEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var canReview = await AiTaskEndpointHelpers.HasReviewAccessAsync(
            facilityId.Value, facilityAccessResolver, cancellationToken);
        var canCreateTask = await AiTaskEndpointHelpers.HasCreateAccessAsync(
            facilityId.Value, facilityAccessResolver, cancellationToken);

        var workspace = await repository.GetWorkspaceAsync(context, canManage, canReview, canCreateTask, cancellationToken);
        return TypedResults.Ok(new
        {
            ruleSetVersion = workspace.RuleSetVersion,
            provenanceDisclaimer = workspace.ProvenanceDisclaimer,
            kpis = new
            {
                draftCount = workspace.DraftCount,
                needsReviewCount = workspace.NeedsReviewCount,
                eligibleCount = workspace.EligibleCount,
                blockedCount = workspace.BlockedCount,
                approvedCount = workspace.ApprovedCount,
            },
            capabilities = new
            {
                canRead = workspace.CanRead,
                canManage = workspace.CanManage,
                canReview = workspace.CanReview,
                canCreateTask = workspace.CanCreateTask,
            },
            automationDisclaimer = CommercialPricingScenarioConstants.AutomationDisclaimer,
        });
    }

    private static async Task<IResult> ListScenariosAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? lifecycleStatus,
        [FromQuery] string? guardrailResult,
        [FromQuery] Guid? profileId,
        [FromQuery] Guid? materialId,
        [FromQuery] string? customerReference,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        ICommercialPricingScenarioRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var scenarios = await repository.ListScenariosAsync(
            context,
            new CommercialPricingScenarioListFilters(page, pageSize, lifecycleStatus, guardrailResult, profileId, materialId, customerReference),
            cancellationToken);

        return TypedResults.Ok(new
        {
            items = scenarios.Select(MapSummary),
            ruleSetVersion = CommercialPricingScenarioConstants.RuleSetVersion,
            automationDisclaimer = CommercialPricingScenarioConstants.AutomationDisclaimer,
        });
    }

    private static async Task<IResult> GetScenarioAsync(
        Guid scenarioId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialPricingScenarioRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var scenario = await repository.GetScenarioByIdAsync(context, scenarioId, cancellationToken);
        return scenario is null
            ? CommercialIntelligenceEndpointHelpers.MapFailure(CommercialIntelligenceFailureCodes.NotFound, "Fiyat senaryosu bulunamadı.")
            : TypedResults.Ok(MapDetail(scenario));
    }

    private static async Task<IResult> CreateScenarioAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateCommercialPricingScenarioRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialPricingScenarioRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateScenarioAsync(
            context,
            new CreateCommercialPricingScenarioCommand(
                body.ProfileId,
                body.ExpectedProfileVersion,
                body.SalesEvidenceObservationId,
                body.ProposedUnitPrice,
                body.ProposedDiscountRatio,
                body.Quantity,
                body.ValidityWindowStartUtc,
                body.ValidityWindowEndUtc,
                body.CommercialRationale,
                body.SourceProvenance),
            cancellationToken);

        return MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateScenarioAsync(
        Guid scenarioId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateCommercialPricingScenarioRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialPricingScenarioRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdateScenarioAsync(
            context,
            new UpdateCommercialPricingScenarioCommand(
                scenarioId,
                body.ExpectedVersion,
                body.ProfileId,
                body.ExpectedProfileVersion,
                body.SalesEvidenceObservationId,
                body.ProposedUnitPrice,
                body.ProposedDiscountRatio,
                body.Quantity,
                body.ValidityWindowStartUtc,
                body.ValidityWindowEndUtc,
                body.CommercialRationale),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> EvaluateScenarioAsync(
        Guid scenarioId,
        [FromQuery] Guid? facilityId,
        [FromBody] EvaluateCommercialPricingScenarioRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialPricingScenarioRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EvaluateScenarioAsync(
            context,
            new EvaluateCommercialPricingScenarioCommand(scenarioId, body.ExpectedVersion),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> TransitionScenarioAsync(
        Guid scenarioId,
        [FromQuery] Guid? facilityId,
        [FromBody] TransitionCommercialPricingScenarioRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialPricingScenarioRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var targetStatus = body.TargetLifecycleStatus.Trim();
        var requiresReview = targetStatus is CommercialPricingScenarioLifecycleStatuses.Approved
            or CommercialPricingScenarioLifecycleStatuses.Rejected;

        var (context, failResult) = requiresReview
            ? await ResolveReviewAccessAsync(facilityId, facilityAccessResolver, correlationContext, cancellationToken)
            : await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
                facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        if (targetStatus == CommercialPricingScenarioLifecycleStatuses.SubmittedForApproval)
        {
            var readContext = (await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
                facilityId, facilityAccessResolver, correlationContext, cancellationToken)).Context;
            if (readContext is null)
            {
                return failResult!;
            }

            var scenario = await repository.GetScenarioByIdAsync(readContext, scenarioId, cancellationToken);
            if (scenario is null)
            {
                return CommercialIntelligenceEndpointHelpers.MapFailure(
                    CommercialIntelligenceFailureCodes.NotFound,
                    "Fiyat senaryosu bulunamadı.");
            }

            if (scenario.LifecycleStatus is not (
                CommercialPricingScenarioLifecycleStatuses.NeedsReview
                or CommercialPricingScenarioLifecycleStatuses.EligibleForApproval))
            {
                return CommercialIntelligenceEndpointHelpers.MapFailure(
                    CommercialIntelligenceFailureCodes.ValidationFailed,
                    "Senaryo onaya gönderilemez durumda.");
            }
        }

        var result = await repository.TransitionScenarioAsync(
            context,
            new TransitionCommercialPricingScenarioCommand(
                scenarioId,
                body.ExpectedVersion,
                targetStatus,
                body.Note),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> CreateTaskDraftAsync(
        Guid scenarioId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialPricingScenarioRepository scenarioRepository,
        IAiTaskRepository aiTaskRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (readContext, readFail) = await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (readContext is null) return readFail!;

        var (createContext, createFail) = await AiTaskEndpointHelpers.ResolveCreateAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (createContext is null) return createFail!;

        var scenario = await scenarioRepository.GetScenarioByIdAsync(readContext, scenarioId, cancellationToken);
        if (scenario is null)
        {
            return CommercialIntelligenceEndpointHelpers.MapFailure(
                CommercialIntelligenceFailureCodes.NotFound,
                "Fiyat senaryosu bulunamadı.");
        }

        if (scenario.LinkedAiTaskId.HasValue)
        {
            return TypedResults.Ok(new
            {
                taskId = scenario.LinkedAiTaskId.Value,
                scenarioId = scenario.Id,
                idempotentReplay = true,
                scenario = MapDetail(scenario),
            });
        }

        var evaluation = scenario.LatestEvaluation;
        var guardrail = scenario.GuardrailResult ?? "unknown";
        var idempotencyKey = CommercialPricingScenarioPresentationBuilder.BuildTaskDraftIdempotencyKey(scenario.Id);
        var priority = guardrail switch
        {
            CommercialPricingScenarioGuardrailResults.Blocked => "high",
            CommercialPricingScenarioGuardrailResults.NeedsReview => "medium",
            _ => "low",
        };

        var driverSummary = evaluation?.Drivers.Count > 0
            ? evaluation.Drivers[0].SupportingText
            : scenario.CommercialRationale;

        var taskResult = await aiTaskRepository.CreateSourceLinkedTaskAsync(
            createContext,
            new CreateSourceLinkedAiTaskCommand(
                Title: CommercialPricingScenarioPresentationBuilder.BuildScenarioTitle(scenario),
                Summary: CommercialPricingScenarioPresentationBuilder.BuildScenarioSummary(scenario),
                Category: "commercial-intelligence",
                Priority: priority,
                RiskLevel: priority,
                DetectedIssue: $"Fiyat koruma sonucu: {guardrail}",
                Reasoning: $"Senaryo {scenario.ScenarioReference} için önerilen fiyat {scenario.ProposedUnitPrice:0.00} TRY. {CommercialPricingScenarioConstants.AutomationDisclaimer}",
                Recommendation: new AiTaskRecommendationModel(
                    "Fiyat senaryosunu incele",
                    "Koruma sonucu, marj eşiği, kanıt kapsamı ve sürücüleri doğrulayın. Bu onay fiyatı otomatik olarak uygulamaz.",
                    "review",
                    RequiresApproval: true,
                    Reversible: true,
                    TargetSystem: "crevafactos"),
                Confidence: new AiTaskConfidenceModel(0.8m, "Sunucu tarafı kârlılık koruma değerlendirmesine dayalı.", null),
                Impact: new AiTaskImpactModel(
                    null, null, null, null, null, null, null,
                    driverSummary),
                Sources:
                [
                    new AiTaskSourceReferenceModel(
                        CommercialPricingScenarioConstants.SourceModule,
                        scenario.Id.ToString(),
                        $"Senaryo {scenario.ScenarioReference}",
                        scenario.UpdatedAtUtc,
                        CommercialPricingScenarioPresentationBuilder.BuildScenarioSummary(scenario)),
                ],
                SourceModule: CommercialPricingScenarioConstants.SourceModule,
                SourceEntityId: scenario.Id.ToString(),
                IdempotencyKey: idempotencyKey,
                SiteId: null,
                ProductionLineId: null,
                MachineId: null,
                MoldId: null,
                ProductionOrderId: null,
                SalesOrderId: null,
                MaterialId: scenario.MaterialId?.ToString(),
                SupplierId: null,
                CustomerId: scenario.CustomerReference,
                DueAtUtc: null),
            cancellationToken);

        if (!taskResult.Succeeded)
        {
            return AiTaskEndpointHelpers.MapFailure(taskResult.FailureCode!, taskResult.FailureReason!);
        }

        var taskId = taskResult.EntityId!.Value;

        var linkResult = await scenarioRepository.LinkTaskAsync(
            readContext,
            new LinkCommercialPricingScenarioTaskCommand(scenarioId, scenario.Version, taskId),
            cancellationToken);

        if (!linkResult.Succeeded)
        {
            return MapWriteResult(linkResult);
        }

        return TypedResults.Ok(new
        {
            taskId,
            scenarioId,
            idempotentReplay = taskResult.IdempotentReplay,
            scenario = linkResult.Scenario is null ? MapDetail(scenario) : MapDetail(linkResult.Scenario),
        });
    }

    private static async Task<(CommercialIntelligenceOperationContext? Context, IResult? Failure)> ResolveReviewAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, CommercialIntelligenceEndpointHelpers.MapFailure(
                CommercialIntelligenceFailureCodes.ValidationFailed,
                "A facility identifier is required."));
        }

        var hasReview = await AiTaskEndpointHelpers.HasReviewAccessAsync(
            facilityId.Value, facilityAccessResolver, cancellationToken);
        if (!hasReview)
        {
            return (null, CommercialIntelligenceEndpointHelpers.MapFailure(
                CommercialIntelligenceFailureCodes.ValidationFailed,
                "Onay/red işlemi için inceleme yetkisi gereklidir."));
        }

        return await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
    }

    private static IResult MapWriteResult(CommercialPricingScenarioWriteResult result, bool isCreate = false)
    {
        if (result.Succeeded)
        {
            var payload = new
            {
                entityId = result.EntityId,
                version = result.Version,
                idempotentReplay = result.IdempotentReplay,
                scenario = result.Scenario is null ? null : MapDetail(result.Scenario),
            };

            return isCreate && !result.IdempotentReplay
                ? TypedResults.Created((string?)null, payload)
                : TypedResults.Ok(payload);
        }

        return CommercialIntelligenceEndpointHelpers.MapFailure(result.FailureCode!, result.FailureReason!);
    }

    private static object MapSummary(CommercialPricingScenarioSummaryReadModel scenario) => new
    {
        id = scenario.Id,
        scenarioReference = scenario.ScenarioReference,
        lifecycleStatus = scenario.LifecycleStatus,
        guardrailResult = scenario.GuardrailResult,
        profileId = scenario.ProfileId,
        materialId = scenario.MaterialId,
        customerReference = scenario.CustomerReference,
        baselineUnitPrice = scenario.BaselineUnitPrice,
        proposedUnitPrice = scenario.ProposedUnitPrice,
        proposedDiscountRatio = scenario.ProposedDiscountRatio,
        validityWindowStartUtc = scenario.ValidityWindowStartUtc,
        validityWindowEndUtc = scenario.ValidityWindowEndUtc,
        linkedAiTaskId = scenario.LinkedAiTaskId,
        evaluationCount = scenario.EvaluationCount,
        createdAtUtc = scenario.CreatedAtUtc,
        updatedAtUtc = scenario.UpdatedAtUtc,
        version = scenario.Version,
    };

    private static object MapDetail(CommercialPricingScenarioDetailReadModel scenario) => new
    {
        id = scenario.Id,
        scenarioReference = scenario.ScenarioReference,
        lifecycleStatus = scenario.LifecycleStatus,
        guardrailResult = scenario.GuardrailResult,
        profileId = scenario.ProfileId,
        profileVersion = scenario.ProfileVersion,
        materialId = scenario.MaterialId,
        customerReference = scenario.CustomerReference,
        salesEvidenceObservationId = scenario.SalesEvidenceObservationId,
        quantity = scenario.Quantity,
        baselineUnitPrice = scenario.BaselineUnitPrice,
        proposedUnitPrice = scenario.ProposedUnitPrice,
        proposedDiscountRatio = scenario.ProposedDiscountRatio,
        validityWindowStartUtc = scenario.ValidityWindowStartUtc,
        validityWindowEndUtc = scenario.ValidityWindowEndUtc,
        commercialRationale = scenario.CommercialRationale,
        latestEvaluationSnapshotId = scenario.LatestEvaluationSnapshotId,
        linkedAiTaskId = scenario.LinkedAiTaskId,
        ruleSetVersion = scenario.RuleSetVersion,
        sourceProvenance = scenario.SourceProvenance,
        evaluationCount = scenario.EvaluationCount,
        createdAtUtc = scenario.CreatedAtUtc,
        updatedAtUtc = scenario.UpdatedAtUtc,
        version = scenario.Version,
        latestEvaluation = scenario.LatestEvaluation is null ? null : MapSnapshot(scenario.LatestEvaluation),
        evaluationHistory = scenario.EvaluationHistory.Select(MapSnapshot),
        auditEvents = scenario.AuditEvents.Select(entry => new
        {
            id = entry.Id,
            eventType = entry.EventType,
            payloadJson = entry.PayloadJson,
            actorSubjectId = entry.ActorSubjectId,
            occurredAtUtc = entry.OccurredAtUtc,
        }),
        deepLink = CommercialPricingScenarioPresentationBuilder.BuildScenarioDeepLink(scenario.Id),
        automationDisclaimer = CommercialPricingScenarioConstants.AutomationDisclaimer,
    };

    private static object MapSnapshot(CommercialPricingScenarioEvaluationSnapshotReadModel snapshot) => new
    {
        id = snapshot.Id,
        scenarioVersionAtEvaluation = snapshot.ScenarioVersionAtEvaluation,
        evaluationSequence = snapshot.EvaluationSequence,
        guardrailResult = snapshot.GuardrailResult,
        assessmentOutcome = snapshot.AssessmentOutcome,
        evidenceStatus = snapshot.EvidenceStatus,
        costCoverageStatus = snapshot.CostCoverageStatus,
        leadingDriverCode = snapshot.LeadingDriverCode,
        insufficientDataReason = snapshot.InsufficientDataReason,
        baselineUnitPrice = snapshot.BaselineUnitPrice,
        proposedUnitPrice = snapshot.ProposedUnitPrice,
        totalIncludedUnitCost = snapshot.TotalIncludedUnitCost,
        expectedUnitContribution = snapshot.ExpectedUnitContribution,
        expectedContributionMarginRatio = snapshot.ExpectedContributionMarginRatio,
        baselineContributionMarginRatio = snapshot.BaselineContributionMarginRatio,
        contributionMarginDelta = snapshot.ContributionMarginDelta,
        profileMinimumContributionMarginRatio = snapshot.ProfileMinimumContributionMarginRatio,
        profileId = snapshot.ProfileId,
        profileVersion = snapshot.ProfileVersion,
        inputSnapshotJson = snapshot.InputSnapshotJson,
        outputSnapshotJson = snapshot.OutputSnapshotJson,
        ruleSetVersion = snapshot.RuleSetVersion,
        evaluatedAtUtc = snapshot.EvaluatedAtUtc,
        evaluatedBy = snapshot.EvaluatedBy,
        drivers = snapshot.Drivers.Select(driver => new
        {
            reasonCode = driver.ReasonCode,
            severity = driver.Severity,
            actualValue = driver.ActualValue,
            thresholdValue = driver.ThresholdValue,
            unit = driver.Unit,
            normalizedVariance = driver.NormalizedVariance,
            affectedComponentCount = driver.AffectedComponentCount,
            supportingText = driver.SupportingText,
        }),
    };
}

public sealed record CreateCommercialPricingScenarioRequest(
    Guid ProfileId,
    uint ExpectedProfileVersion,
    Guid SalesEvidenceObservationId,
    decimal ProposedUnitPrice,
    decimal? ProposedDiscountRatio,
    decimal? Quantity,
    DateTimeOffset? ValidityWindowStartUtc,
    DateTimeOffset? ValidityWindowEndUtc,
    string CommercialRationale,
    string SourceProvenance);

public sealed record UpdateCommercialPricingScenarioRequest(
    uint ExpectedVersion,
    Guid ProfileId,
    uint ExpectedProfileVersion,
    Guid SalesEvidenceObservationId,
    decimal ProposedUnitPrice,
    decimal? ProposedDiscountRatio,
    decimal? Quantity,
    DateTimeOffset? ValidityWindowStartUtc,
    DateTimeOffset? ValidityWindowEndUtc,
    string CommercialRationale);

public sealed record EvaluateCommercialPricingScenarioRequest(uint ExpectedVersion);

public sealed record TransitionCommercialPricingScenarioRequest(
    uint ExpectedVersion,
    string TargetLifecycleStatus,
    string? Note);

public static class CommercialPricingScenarioServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSCommercialPricingScenarios(this IServiceCollection services)
    {
        services.AddScoped<ICommercialPricingScenarioRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableCommercialPricingScenarioRepository()
                : new CommercialPricingScenarioRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}
