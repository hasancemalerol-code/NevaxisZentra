using NevaxisZentra.Api.AiTasks;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence;
using NevaxisZentra.Persistence.AiTasks;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.CommercialIntelligence;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace NevaxisZentra.Api.CommercialIntelligence;

internal static class CommercialProfitabilityAlertEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCommercialProfitabilityAlertEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/commercial-intelligence/profitability/alerts").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapGet("/", ListAlertsAsync);
        group.MapGet("/{alertId:guid}", GetAlertAsync);
        group.MapPost("/materialize", MaterializeAlertsAsync);
        group.MapPost("/{alertId:guid}/transitions", TransitionAlertAsync);
        group.MapPost("/{alertId:guid}/task-draft", CreateTaskDraftAsync);

        return endpoints;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialProfitabilityAlertRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var canManage = await CommercialIntelligenceEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var canCreateTask = await AiTaskEndpointHelpers.HasCreateAccessAsync(
            facilityId.Value, facilityAccessResolver, cancellationToken);

        var workspace = await repository.GetWorkspaceAsync(context, canManage, canCreateTask, cancellationToken);
        return TypedResults.Ok(new
        {
            ruleSetVersion = workspace.RuleSetVersion,
            provenanceDisclaimer = workspace.ProvenanceDisclaimer,
            kpis = new
            {
                activeCount = workspace.ActiveCount,
                highOrCriticalCount = workspace.HighOrCriticalCount,
                acknowledgedCount = workspace.AcknowledgedCount,
                taskCreatedCount = workspace.TaskCreatedCount,
                evidenceReviewCount = workspace.EvidenceReviewCount,
            },
            capabilities = new
            {
                canRead = workspace.CanRead,
                canManage = workspace.CanManage,
                canCreateTask = workspace.CanCreateTask,
            },
            automationDisclaimer = "Otomatik ticari işlem yapılmaz.",
        });
    }

    private static async Task<IResult> ListAlertsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? category,
        [FromQuery] string? severity,
        [FromQuery] string? lifecycleStatus,
        [FromQuery] string? evidenceStatus,
        [FromQuery] string? comparisonType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        ICommercialProfitabilityAlertRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var alerts = await repository.ListAlertsAsync(
            context,
            new CommercialProfitabilityAlertListFilters(page, pageSize, category, severity, lifecycleStatus, evidenceStatus, comparisonType),
            cancellationToken);

        return TypedResults.Ok(new
        {
            items = alerts.Select(MapSummary),
            ruleSetVersion = CommercialProfitabilityAlertConstants.RuleSetVersion,
            automationDisclaimer = "Otomatik ticari işlem yapılmaz.",
        });
    }

    private static async Task<IResult> GetAlertAsync(
        Guid alertId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialProfitabilityAlertRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var alert = await repository.GetAlertByIdAsync(context, alertId, cancellationToken);
        return alert is null
            ? CommercialIntelligenceEndpointHelpers.MapFailure(CommercialIntelligenceFailureCodes.NotFound, "Kârlılık uyarısı bulunamadı.")
            : TypedResults.Ok(MapDetail(alert));
    }

    private static async Task<IResult> MaterializeAlertsAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] MaterializeCommercialProfitabilityAlertsRequest? body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialProfitabilityAlertRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.MaterializeAlertsAsync(
            context,
            new MaterializeCommercialProfitabilityAlertsCommand(body?.IdempotencyKey),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> TransitionAlertAsync(
        Guid alertId,
        [FromQuery] Guid? facilityId,
        [FromBody] TransitionCommercialProfitabilityAlertRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialProfitabilityAlertRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.TransitionAlertAsync(
            context,
            new TransitionCommercialProfitabilityAlertCommand(
                alertId,
                body.ExpectedVersion,
                body.TargetLifecycleStatus,
                body.Note),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> CreateTaskDraftAsync(
        Guid alertId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialProfitabilityAlertRepository alertRepository,
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

        var alert = await alertRepository.GetAlertByIdAsync(readContext, alertId, cancellationToken);
        if (alert is null)
        {
            return CommercialIntelligenceEndpointHelpers.MapFailure(
                CommercialIntelligenceFailureCodes.NotFound,
                "Kârlılık uyarısı bulunamadı.");
        }

        if (alert.LinkedAiTaskId.HasValue)
        {
            return TypedResults.Ok(new
            {
                taskId = alert.LinkedAiTaskId.Value,
                alertId = alert.Id,
                idempotentReplay = true,
                alert = MapDetail(alert),
            });
        }

        var idempotencyKey = CommercialProfitabilityAlertPresentationBuilder.BuildTaskDraftIdempotencyKey(alert.Id);
        var priority = alert.Severity switch
        {
            CommercialProfitabilityAlertSeverities.Critical => "critical",
            CommercialProfitabilityAlertSeverities.High => "high",
            CommercialProfitabilityAlertSeverities.Low => "low",
            _ => "medium",
        };

        var taskResult = await aiTaskRepository.CreateSourceLinkedTaskAsync(
            createContext,
            new CreateSourceLinkedAiTaskCommand(
                Title: alert.Title,
                Summary: alert.EvidenceSummary,
                Category: "commercial-intelligence",
                Priority: priority,
                RiskLevel: priority,
                DetectedIssue: alert.EvidenceSummary,
                Reasoning: $"Kârlılık uyarısı {alert.AlertReference} için insan incelemeli görev taslağı.",
                Recommendation: new AiTaskRecommendationModel(
                    "Kârlılık uyarısını incele",
                    "Kaynak karşılaştırma, sürücüler ve kanıt özetini doğrulayın. Otomatik ticari işlem yapılmaz.",
                    "review",
                    RequiresApproval: true,
                    Reversible: true,
                    TargetSystem: "crevafactos"),
                Confidence: new AiTaskConfidenceModel(0.75m, "Kârlılık karşılaştırma motoru kanıtına dayalı uyarı.", null),
                Impact: new AiTaskImpactModel(
                    null, null, null, null, null, null, null,
                    alert.EvidenceSummary),
                Sources:
                [
                    new AiTaskSourceReferenceModel(
                        "commercial-intelligence",
                        alert.SourceComparisonId.ToString(),
                        $"Karşılaştırma {alert.SourceComparison.ComparisonReference}",
                        alert.SourceComparison.EvaluatedAtUtc,
                        alert.EvidenceSummary),
                    new AiTaskSourceReferenceModel(
                        "commercial-intelligence",
                        alert.Id.ToString(),
                        $"Uyarı {alert.AlertReference}",
                        alert.MaterializedAtUtc,
                        alert.EvidenceSummary),
                ],
                SourceModule: CommercialProfitabilityAlertConstants.SourceModule,
                SourceEntityId: alert.Id.ToString(),
                IdempotencyKey: idempotencyKey,
                SiteId: null,
                ProductionLineId: null,
                MachineId: null,
                MoldId: null,
                ProductionOrderId: null,
                SalesOrderId: null,
                MaterialId: null,
                SupplierId: null,
                CustomerId: null,
                DueAtUtc: null),
            cancellationToken);

        if (!taskResult.Succeeded)
        {
            return AiTaskEndpointHelpers.MapFailure(taskResult.FailureCode!, taskResult.FailureReason!);
        }

        var taskId = taskResult.EntityId!.Value;

        var linkResult = await alertRepository.LinkTaskAsync(
            readContext,
            new LinkCommercialProfitabilityAlertTaskCommand(alertId, alert.Version, taskId),
            cancellationToken);

        if (!linkResult.Succeeded)
        {
            return MapWriteResult(linkResult);
        }

        return TypedResults.Ok(new
        {
            taskId,
            alertId,
            idempotentReplay = taskResult.IdempotentReplay,
            alert = linkResult.Alert is null ? MapDetail(alert) : MapDetail(linkResult.Alert),
        });
    }

    private static IResult MapWriteResult(CommercialProfitabilityAlertWriteResult result)
    {
        if (result.Succeeded)
        {
            return TypedResults.Ok(new
            {
                entityId = result.EntityId,
                version = result.Version,
                idempotentReplay = result.IdempotentReplay,
                alert = result.Alert is null ? null : MapDetail(result.Alert),
                materialization = result.Materialization is null
                    ? null
                    : new
                    {
                        createdCount = result.Materialization.CreatedCount,
                        refreshedCount = result.Materialization.RefreshedCount,
                        skippedCount = result.Materialization.SkippedCount,
                        alertIds = result.Materialization.AlertIds,
                    },
            });
        }

        return CommercialIntelligenceEndpointHelpers.MapFailure(result.FailureCode!, result.FailureReason!);
    }

    private static object MapSummary(CommercialProfitabilityAlertSummaryReadModel alert) => new
    {
        id = alert.Id,
        alertReference = alert.AlertReference,
        alertCategory = alert.AlertCategory,
        severity = alert.Severity,
        lifecycleStatus = alert.LifecycleStatus,
        sourceComparisonId = alert.SourceComparisonId,
        sourceComparisonType = alert.SourceComparisonType,
        title = alert.Title,
        evidenceSummary = alert.EvidenceSummary,
        evidenceStatus = alert.EvidenceStatus,
        comparisonOutcome = alert.ComparisonOutcome,
        leadingDriverCode = alert.LeadingDriverCode,
        linkedAiTaskId = alert.LinkedAiTaskId,
        materializedAtUtc = alert.MaterializedAtUtc,
        updatedAtUtc = alert.UpdatedAtUtc,
        version = alert.Version,
    };

    private static object MapDetail(CommercialProfitabilityAlertDetailReadModel alert) => new
    {
        id = alert.Id,
        alertReference = alert.AlertReference,
        alertCategory = alert.AlertCategory,
        severity = alert.Severity,
        lifecycleStatus = alert.LifecycleStatus,
        sourceComparisonId = alert.SourceComparisonId,
        sourceComparisonType = alert.SourceComparisonType,
        title = alert.Title,
        evidenceSummary = alert.EvidenceSummary,
        evidenceStatus = alert.EvidenceStatus,
        comparisonOutcome = alert.ComparisonOutcome,
        leadingDriverCode = alert.LeadingDriverCode,
        sourceSnapshotJson = alert.SourceSnapshotJson,
        linkedAiTaskId = alert.LinkedAiTaskId,
        ruleSetVersion = alert.RuleSetVersion,
        sourceProvenance = alert.SourceProvenance,
        materializedAtUtc = alert.MaterializedAtUtc,
        updatedAtUtc = alert.UpdatedAtUtc,
        version = alert.Version,
        sourceComparison = alert.SourceComparison,
        auditEvents = alert.AuditEvents.Select(entry => new
        {
            id = entry.Id,
            eventType = entry.EventType,
            payloadJson = entry.PayloadJson,
            actorSubjectId = entry.ActorSubjectId,
            occurredAtUtc = entry.OccurredAtUtc,
        }),
        automationDisclaimer = "Otomatik ticari işlem yapılmaz.",
    };
}

public sealed record MaterializeCommercialProfitabilityAlertsRequest(string? IdempotencyKey);

public sealed record TransitionCommercialProfitabilityAlertRequest(
    uint ExpectedVersion,
    string TargetLifecycleStatus,
    string? Note);

public static class CommercialProfitabilityAlertServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraCommercialProfitabilityAlerts(this IServiceCollection services)
    {
        services.AddScoped<ICommercialProfitabilityAlertRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableCommercialProfitabilityAlertRepository()
                : new CommercialProfitabilityAlertRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}
