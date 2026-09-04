using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.AiTasks;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Teams;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.AiTasks;

public static class AiTaskEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAiTaskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/ai-tasks").RequireAuthorization();

        group.MapGet("/", ListTasksAsync);
        group.MapGet("/{taskId:guid}", GetTaskAsync);
        group.MapGet("/by-idempotency/{idempotencyKey}", GetTaskByIdempotencyKeyAsync);
        group.MapGet("/by-source", GetTaskBySourceAsync);
        group.MapPost("/", CreateTaskAsync);
        group.MapPost("/source-linked", CreateSourceLinkedTaskAsync);
        group.MapPost("/{taskId:guid}/transitions", TransitionTaskAsync);

        return endpoints;
    }

    private static async Task<IResult> ListTasksAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? status,
        [FromQuery] string? category,
        [FromQuery] string? priority,
        [FromQuery] string? sourceModule,
        [FromQuery] string? closureState,
        [FromQuery] string? closureQueue,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        IAiTaskRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ListTasksAsync(
            context,
            new AiTaskListFilters(status, category, priority, sourceModule, closureState, closureQueue, page, pageSize),
            cancellationToken);

        var canCreate = await AiTaskEndpointHelpers.HasCreateAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var canReview = await AiTaskEndpointHelpers.HasReviewAccessAsync(
            facilityId.Value, facilityAccessResolver, cancellationToken);

        return TypedResults.Ok(new
        {
            items = result.Items.Select(MapSummary),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
            ruleSetVersion = AiTaskConstants.RuleSetVersion,
            provenanceDisclaimer = AiTaskConstants.DevelopmentPilotProvenance,
            capabilities = new { canRead = true, canCreate, canReview },
        });
    }

    private static async Task<IResult> GetTaskAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var task = await repository.GetTaskByIdAsync(context, taskId, cancellationToken);
        return task is null
            ? AiTaskEndpointHelpers.MapFailure(AiTaskFailureCodes.NotFound, "AI task not found.")
            : TypedResults.Ok(MapDetail(task));
    }

    private static async Task<IResult> GetTaskByIdempotencyKeyAsync(
        string idempotencyKey,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var task = await repository.GetTaskByIdempotencyKeyAsync(context, idempotencyKey, cancellationToken);
        return task is null
            ? AiTaskEndpointHelpers.MapFailure(AiTaskFailureCodes.NotFound, "AI task not found.")
            : TypedResults.Ok(MapDetail(task));
    }

    private static async Task<IResult> GetTaskBySourceAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? sourceModule,
        [FromQuery] string? sourceEntityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var task = await repository.GetTaskBySourceAsync(context, sourceModule ?? string.Empty, sourceEntityId ?? string.Empty, cancellationToken);
        return task is null
            ? AiTaskEndpointHelpers.MapFailure(AiTaskFailureCodes.NotFound, "AI task not found.")
            : TypedResults.Ok(MapDetail(task));
    }

    private static async Task<IResult> CreateTaskAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateAiTaskRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolveCreateAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateTaskAsync(
            context,
            new CreateAiTaskCommand(
                body.Title,
                body.Summary,
                body.Category,
                body.Priority,
                body.RiskLevel,
                body.DetectedIssue,
                body.Reasoning,
                MapRecommendation(body.Recommendation),
                MapConfidence(body.Confidence),
                MapImpact(body.Impact),
                body.Sources.Select(MapSource).ToList(),
                body.SiteId,
                body.ProductionLineId,
                body.MachineId,
                body.MoldId,
                body.ProductionOrderId,
                body.SalesOrderId,
                body.MaterialId,
                body.SupplierId,
                body.CustomerId,
                body.DueAtUtc),
            cancellationToken);

        return AiTaskEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> CreateSourceLinkedTaskAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateSourceLinkedAiTaskRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolveCreateAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateSourceLinkedTaskAsync(
            context,
            new CreateSourceLinkedAiTaskCommand(
                body.Title,
                body.Summary,
                body.Category,
                body.Priority,
                body.RiskLevel,
                body.DetectedIssue,
                body.Reasoning,
                MapRecommendation(body.Recommendation),
                MapConfidence(body.Confidence),
                MapImpact(body.Impact),
                body.Sources.Select(MapSource).ToList(),
                body.SourceModule,
                body.SourceEntityId,
                body.IdempotencyKey,
                body.SiteId,
                body.ProductionLineId,
                body.MachineId,
                body.MoldId,
                body.ProductionOrderId,
                body.SalesOrderId,
                body.MaterialId,
                body.SupplierId,
                body.CustomerId,
                body.DueAtUtc),
            cancellationToken);

        return AiTaskEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> TransitionTaskAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] TransitionAiTaskRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolveReviewAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.TransitionTaskAsync(
            context,
            new TransitionAiTaskCommand(taskId, body.ExpectedVersion, body.TargetStatus, body.Note),
            cancellationToken);

        return AiTaskEndpointHelpers.MapWriteResult(result);
    }

    private static object MapSummary(AiTaskSummaryReadModel item) => new
    {
        id = item.Id,
        taskReference = item.TaskReference,
        title = item.Title,
        summary = item.Summary,
        category = item.Category,
        priority = item.Priority,
        riskLevel = item.RiskLevel,
        status = item.Status,
        approval = new { state = item.ApprovalState },
        sourceModule = item.SourceModule,
        sourceEntityId = item.SourceEntityId,
        listOriginKind = item.ListOriginKind,
        dueAt = item.DueAtUtc,
        assignee = item.Assignee is null
            ? null
            : new { userId = item.Assignee.UserId, displayName = item.Assignee.DisplayName, role = item.Assignee.Role },
        assignedMembershipId = item.AssignedMembershipId,
        assignedTeamChannelId = item.AssignedTeamChannelId,
        acknowledgedAtUtc = item.AcknowledgedAtUtc,
        escalationLevel = item.EscalationLevel,
        escalationState = item.EscalationState,
        slaState = item.SlaState,
        closureState = item.ClosureState,
        evidenceCount = item.EvidenceCount,
        closureApprovalRequired = item.ClosureApprovalRequired,
        createdAt = item.CreatedAtUtc,
        updatedAt = item.UpdatedAtUtc,
        version = item.Version,
    };

    private static object MapDetail(AiTaskDetailReadModel item) => new
    {
        id = item.Id,
        taskReference = item.TaskReference,
        title = item.Title,
        summary = item.Summary,
        category = item.Category,
        priority = item.Priority,
        riskLevel = item.RiskLevel,
        status = item.Status,
        detectedIssue = item.DetectedIssue,
        reasoning = item.Reasoning,
        recommendation = new
        {
            title = item.Recommendation.Title,
            description = item.Recommendation.Description,
            actionType = item.Recommendation.ActionType,
            requiresApproval = item.Recommendation.RequiresApproval,
            reversible = item.Recommendation.Reversible,
            targetSystem = item.Recommendation.TargetSystem,
        },
        confidence = new
        {
            score = item.Confidence.Score,
            explanation = item.Confidence.Explanation,
            modelVersion = item.Confidence.ModelVersion,
        },
        impact = new
        {
            financialAmount = item.Impact.FinancialAmount,
            currency = item.Impact.Currency,
            productionQuantity = item.Impact.ProductionQuantity,
            productionUnit = item.Impact.ProductionUnit,
            estimatedDelayMinutes = item.Impact.EstimatedDelayMinutes,
            estimatedDowntimeMinutes = item.Impact.EstimatedDowntimeMinutes,
            estimatedSavingsAmount = item.Impact.EstimatedSavingsAmount,
            summary = item.Impact.Summary,
        },
        sources = item.Sources.Select(s => new
        {
            sourceType = s.SourceType,
            sourceId = s.SourceId,
            sourceLabel = s.SourceLabel,
            observedAt = s.ObservedAt,
            valueSummary = s.ValueSummary,
        }),
        assignee = item.Assignee is null
            ? null
            : new
            {
                userId = item.Assignee.UserId,
                displayName = item.Assignee.DisplayName,
                role = item.Assignee.Role,
            },
        approval = new
        {
            state = item.Approval.State,
            requestedAt = item.Approval.RequestedAt,
            requestedBy = item.Approval.RequestedBy,
            decidedAt = item.Approval.DecidedAt,
            decidedBy = item.Approval.DecidedBy,
            decisionNote = item.Approval.DecisionNote,
        },
        execution = item.Execution is null
            ? null
            : new
            {
                startedAt = item.Execution.StartedAt,
                completedAt = item.Execution.CompletedAt,
                targetSystem = item.Execution.TargetSystem,
                externalReferenceId = item.Execution.ExternalReferenceId,
                previousValue = item.Execution.PreviousValue,
                appliedValue = item.Execution.AppliedValue,
                errorCode = item.Execution.ErrorCode,
                errorMessage = item.Execution.ErrorMessage,
            },
        siteId = item.SiteId,
        productionLineId = item.ProductionLineId,
        machineId = item.MachineId,
        moldId = item.MoldId,
        productionOrderId = item.ProductionOrderId,
        salesOrderId = item.SalesOrderId,
        materialId = item.MaterialId,
        supplierId = item.SupplierId,
        customerId = item.CustomerId,
        sourceModule = item.SourceModule,
        sourceEntityId = item.SourceEntityId,
        ruleSetVersion = item.RuleSetVersion,
        dueAt = item.DueAtUtc,
        completedAt = item.CompletedAtUtc,
        acknowledgedAtUtc = item.AcknowledgedAtUtc,
        startedAtUtc = item.StartedAtUtc,
        resolvedAtUtc = item.ResolvedAtUtc,
        escalationLevel = item.EscalationLevel,
        escalationState = item.EscalationState,
        lastEscalatedAtUtc = item.LastEscalatedAtUtc,
        assignmentReason = item.AssignmentReason,
        assignmentSource = item.AssignmentSource,
        assignedMembershipId = item.AssignedMembershipId,
        assignedTeamChannelId = item.AssignedTeamChannelId,
        slaPolicyId = item.SlaPolicyId,
        sla = new
        {
            state = item.Sla.State,
            acknowledgmentDueAtUtc = item.Sla.AcknowledgmentDueAtUtc,
            responseDueAtUtc = item.Sla.ResponseDueAtUtc,
            resolutionDueAtUtc = item.Sla.ResolutionDueAtUtc,
            isOverdue = item.Sla.IsOverdue,
            breachType = item.Sla.BreachType,
        },
        createdAt = item.CreatedAtUtc,
        updatedAt = item.UpdatedAtUtc,
        version = item.Version,
        closure = new
        {
            state = item.Closure.ClosureState,
            resolutionSummary = item.Closure.ResolutionSummary,
            resolutionCode = item.Closure.ResolutionCode,
            closureReason = item.Closure.ClosureReason,
            approvalRequired = item.Closure.ClosureApprovalRequired,
            requestedAtUtc = item.Closure.ClosureRequestedAtUtc,
            requestedByMembershipId = item.Closure.ClosureRequestedByMembershipId,
            approvedAtUtc = item.Closure.ClosureApprovedAtUtc,
            approvedByMembershipId = item.Closure.ClosureApprovedByMembershipId,
            rejectedAtUtc = item.Closure.ClosureRejectedAtUtc,
            rejectedByMembershipId = item.Closure.ClosureRejectedByMembershipId,
            rejectionReason = item.Closure.ClosureRejectionReason,
            reopenedAtUtc = item.Closure.ReopenedAtUtc,
            reopenedByMembershipId = item.Closure.ReopenedByMembershipId,
            reopenReason = item.Closure.ReopenReason,
            finalClosedAtUtc = item.Closure.FinalClosedAtUtc,
        },
        closurePolicy = new
        {
            policyKey = item.ClosurePolicy.PolicyKey,
            name = item.ClosurePolicy.Name,
            minimumEvidenceCount = item.ClosurePolicy.MinimumEvidenceCount,
            requiredEvidenceTypes = item.ClosurePolicy.RequiredEvidenceTypes,
            approvalRequired = item.ClosurePolicy.ApprovalRequired,
            fourEyesRequired = item.ClosurePolicy.FourEyesRequired,
            resolutionSummaryRequired = item.ClosurePolicy.ResolutionSummaryRequired,
        },
        closureValidation = new
        {
            isValid = item.ClosureValidation.IsValid,
            issues = item.ClosureValidation.Issues.Select(issue => new { code = issue.Code, message = issue.Message }),
        },
        evidence = item.Evidence.Select(e => new
        {
            id = e.Id,
            taskId = e.TaskId,
            evidenceType = e.EvidenceType,
            title = e.Title,
            description = e.Description,
            referenceMetadataJson = e.ReferenceMetadataJson,
            sourceModule = e.SourceModule,
            sourceEntityId = e.SourceEntityId,
            submittedByMembershipId = e.SubmittedByMembershipId,
            submittedByLabel = e.SubmittedByLabel,
            submittedAtUtc = e.SubmittedAtUtc,
        }),
        auditTrail = item.AuditTrail.Select(e => new
        {
            id = e.Id,
            taskId = e.TaskId,
            eventType = e.EventType,
            occurredAt = e.OccurredAt,
            actorType = e.ActorType,
            actorId = e.ActorId,
            actorLabel = e.ActorLabel,
            note = e.Note,
            previousStatus = e.PreviousStatus,
            nextStatus = e.NextStatus,
            metadataJson = e.MetadataJson,
        }),
        timeline = item.AuditTrail.Select(e => new
        {
            id = e.Id,
            eventType = e.EventType,
            type = e.EventType,
            occurredAt = e.OccurredAt,
            actorLabel = e.ActorLabel,
            note = e.Note,
        }),
        automationRuleId = item.AutomationRuleId,
        automationRuleVersion = item.AutomationRuleVersion,
        minimumEvidenceRequired = item.MinimumEvidenceRequired,
        fourEyesRequired = item.FourEyesRequired,
        automationMetadataJson = item.AutomationMetadataJson,
        automationOverrideJson = item.AutomationOverrideJson,
        closureApprovalRequired = item.Closure.ClosureApprovalRequired,
    };

    private static AiTaskRecommendationModel MapRecommendation(AiTaskRecommendationRequest recommendation) =>
        new(
            recommendation.Title,
            recommendation.Description,
            recommendation.ActionType,
            recommendation.RequiresApproval,
            recommendation.Reversible,
            recommendation.TargetSystem);

    private static AiTaskConfidenceModel MapConfidence(AiTaskConfidenceRequest confidence) =>
        new(confidence.Score, confidence.Explanation, confidence.ModelVersion);

    private static AiTaskImpactModel MapImpact(AiTaskImpactRequest impact) =>
        new(
            impact.FinancialAmount,
            impact.Currency,
            impact.ProductionQuantity,
            impact.ProductionUnit,
            impact.EstimatedDelayMinutes,
            impact.EstimatedDowntimeMinutes,
            impact.EstimatedSavingsAmount,
            impact.Summary);

    private static AiTaskSourceReferenceModel MapSource(AiTaskSourceReferenceRequest source) =>
        new(
            source.SourceType,
            source.SourceId,
            source.SourceLabel,
            source.ObservedAt,
            source.ValueSummary);
}

public sealed record CreateAiTaskRequest(
    string Title,
    string Summary,
    string Category,
    string Priority,
    string RiskLevel,
    string DetectedIssue,
    string Reasoning,
    AiTaskRecommendationRequest Recommendation,
    AiTaskConfidenceRequest Confidence,
    AiTaskImpactRequest Impact,
    IReadOnlyList<AiTaskSourceReferenceRequest> Sources,
    string? SiteId,
    string? ProductionLineId,
    string? MachineId,
    string? MoldId,
    string? ProductionOrderId,
    string? SalesOrderId,
    string? MaterialId,
    string? SupplierId,
    string? CustomerId,
    DateTimeOffset? DueAtUtc);

public sealed record CreateSourceLinkedAiTaskRequest(
    string Title,
    string Summary,
    string Category,
    string Priority,
    string RiskLevel,
    string DetectedIssue,
    string Reasoning,
    AiTaskRecommendationRequest Recommendation,
    AiTaskConfidenceRequest Confidence,
    AiTaskImpactRequest Impact,
    IReadOnlyList<AiTaskSourceReferenceRequest> Sources,
    string SourceModule,
    string SourceEntityId,
    string IdempotencyKey,
    string? SiteId,
    string? ProductionLineId,
    string? MachineId,
    string? MoldId,
    string? ProductionOrderId,
    string? SalesOrderId,
    string? MaterialId,
    string? SupplierId,
    string? CustomerId,
    DateTimeOffset? DueAtUtc);

public sealed record TransitionAiTaskRequest(
    uint ExpectedVersion,
    string TargetStatus,
    string? Note);

public sealed record AiTaskRecommendationRequest(
    string Title,
    string Description,
    string ActionType,
    bool RequiresApproval,
    bool Reversible,
    string? TargetSystem);

public sealed record AiTaskConfidenceRequest(
    decimal Score,
    string Explanation,
    string? ModelVersion);

public sealed record AiTaskImpactRequest(
    decimal? FinancialAmount,
    string? Currency,
    decimal? ProductionQuantity,
    string? ProductionUnit,
    int? EstimatedDelayMinutes,
    int? EstimatedDowntimeMinutes,
    decimal? EstimatedSavingsAmount,
    string Summary);

public sealed record AiTaskSourceReferenceRequest(
    string SourceType,
    string? SourceId,
    string SourceLabel,
    DateTimeOffset? ObservedAt,
    string? ValueSummary);

public static class AiTaskServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSAiTasks(this IServiceCollection services)
    {
        services.AddScoped<IAiTaskClosureEngine, AiTaskClosureEngine>();
        services.AddScoped<IAiTaskClosureRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            if (dbContext is null)
            {
                return new UnavailableAiTaskClosureRepository();
            }

            return new AiTaskClosureRepository(
                dbContext,
                serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                serviceProvider.GetRequiredService<IAiTaskClosureEngine>(),
                serviceProvider.GetRequiredService<ITeamsNotificationRepository>(),
                serviceProvider.GetRequiredService<IAiTaskPlaybookOrchestrator>());
        });

        services.AddScoped<IAiTaskPlaybookEngine, AiTaskPlaybookEngine>();
        services.AddScoped<AiTaskPlaybookRepository>();
        services.AddScoped<IAiTaskPlaybookRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            if (dbContext is null)
            {
                return new UnavailableAiTaskPlaybookRepository();
            }

            return serviceProvider.GetRequiredService<AiTaskPlaybookRepository>();
        });
        services.AddScoped<IAiTaskPlaybookOrchestrator>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            if (dbContext is null)
            {
                return new UnavailableAiTaskPlaybookRepository();
            }

            return serviceProvider.GetRequiredService<AiTaskPlaybookRepository>();
        });

        services.AddScoped<IAiTaskAutomationEngine, AiTaskAutomationEngine>();
        services.AddScoped<IAiTaskAutomationRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            if (dbContext is null)
            {
                return new UnavailableAiTaskAutomationRepository();
            }

            return new AiTaskAutomationRepository(
                dbContext,
                serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                serviceProvider.GetRequiredService<IAiTaskAutomationEngine>(),
                serviceProvider.GetRequiredService<ITeamsNotificationRepository>(),
                serviceProvider);
        });

        services.AddScoped<IAiTaskRoutingService, AiTaskRoutingService>();
        services.AddScoped<IAiTaskOperationsRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            if (dbContext is null)
            {
                return new UnavailableAiTaskOperationsRepository();
            }

            return new AiTaskOperationsRepository(
                dbContext,
                serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                serviceProvider.GetRequiredService<IAiTaskRoutingService>(),
                serviceProvider.GetRequiredService<IAiTaskAutomationRepository>(),
                serviceProvider.GetRequiredService<ITeamsNotificationRepository>());
        });

        services.AddScoped<IAiTaskDecisionProvider, DeterministicAiTaskDecisionProvider>();
        services.AddScoped<IAiTaskDecisionIntelligenceRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            if (dbContext is null)
            {
                return new UnavailableAiTaskDecisionIntelligenceRepository();
            }

            return new AiTaskDecisionIntelligenceRepository(
                dbContext,
                serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                serviceProvider.GetRequiredService<IAiTaskDecisionProvider>(),
                serviceProvider.GetRequiredService<IAiTaskOperationsRepository>(),
                serviceProvider);
        });

        services.AddScoped<IAiTaskRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableAiTaskRepository()
                : new AiTaskRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<IAiTaskOperationsRepository>(),
                    serviceProvider.GetRequiredService<IAiTaskClosureEngine>(),
                    serviceProvider.GetRequiredService<IAiTaskPlaybookOrchestrator>());
        });

        return services;
    }
}
