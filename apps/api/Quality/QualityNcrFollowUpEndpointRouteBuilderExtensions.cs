using NevaxisZentra.Api.AiTasks;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.AiTasks;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Quality;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.Quality;

internal static class QualityNcrFollowUpEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapQualityNcrFollowUpEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/ncr-follow-up/workspace", GetWorkspaceAsync);
        group.MapGet("/ncr-follow-up/{ncrId:guid}", GetDetailAsync);
        group.MapPost("/ncr-follow-up/{ncrId:guid}/task-draft", CreateTaskDraftAsync);
        group.MapPost("/ncr-follow-up/{ncrId:guid}/follow-up", TransitionFollowUpAsync);

        return group;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? followUpStatus,
        [FromQuery] string? severity,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var canManage = await HasNcrManageAccessAsync(facilityId!.Value, facilityAccessResolver, cancellationToken);
        var canCreateTask = await AiTaskEndpointHelpers.HasCreateAccessAsync(
            facilityId.Value, facilityAccessResolver, cancellationToken);

        var workspace = await repository.GetFollowUpWorkspaceAsync(context, canManage, canCreateTask, cancellationToken);
        var rows = await repository.ListFollowUpRowsAsync(context, followUpStatus, severity, cancellationToken);

        return TypedResults.Ok(new
        {
            automationDisclaimer = workspace.AutomationDisclaimer,
            kpis = new
            {
                requiresReviewCount = workspace.RequiresReviewCount,
                acknowledgedCount = workspace.AcknowledgedCount,
                taskLinkedCount = workspace.TaskLinkedCount,
                highPriorityCount = workspace.HighPriorityCount,
                dataReviewCount = workspace.DataReviewCount,
            },
            capabilities = new
            {
                canRead = workspace.CanRead,
                canManage = workspace.CanManage,
                canCreateTask = workspace.CanCreateTask,
            },
            findings = rows.Select(QualityFollowUpApiMapper.MapRow).ToList(),
        });
    }

    private static async Task<IResult> GetDetailAsync(
        Guid ncrId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var detail = await repository.GetFollowUpDetailAsync(context, ncrId, cancellationToken);
        return detail is null
            ? TypedResults.NotFound(new { code = QualityApiFailureCodes.NotFound, message = "Uygunsuzluk kaydı bulunamadı." })
            : TypedResults.Ok(QualityFollowUpApiMapper.MapDetail(detail));
    }

    private static async Task<IResult> CreateTaskDraftAsync(
        Guid ncrId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository repository,
        IAiTaskRepository aiTaskRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (readContext, readFail) = await QualityEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (readContext is null) return readFail!;

        var (createContext, createFail) = await AiTaskEndpointHelpers.ResolveCreateAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (createContext is null) return createFail!;

        var followUp = await repository.GetFollowUpDetailAsync(readContext, ncrId, cancellationToken);
        if (followUp is null)
        {
            return MapFailure(QualityFailureCodes.NotFound, "Uygunsuzluk kaydı bulunamadı.");
        }

        if (followUp.LinkedAiTaskId.HasValue)
        {
            return TypedResults.Ok(new
            {
                taskId = followUp.LinkedAiTaskId.Value,
                ncrId,
                idempotentReplay = true,
                finding = QualityFollowUpApiMapper.MapDetail(followUp),
            });
        }

        if (!followUp.CanCreateTaskDraft)
        {
            return MapFailure(
                QualityFailureCodes.ValidationFailed,
                "Bu uygunsuzluk kaydı için görev taslağı oluşturulamaz.");
        }

        var taskPurpose = followUp.TaskDraftPurpose;
        var idempotencyKey = QualityFollowUpPresentationBuilder.BuildTaskDraftIdempotencyKey(ncrId, taskPurpose);
        var priority = followUp.Detail.Severity switch
        {
            "critical" => "critical",
            "major" => "high",
            "minor" => "medium",
            _ => "low",
        };
        var deepLink = QualityFollowUpPresentationBuilder.BuildQualityDeepLink(ncrId);
        var ncr = followUp.Detail;
        var isDataReview = taskPurpose == QualityFollowUpConstants.TaskPurposeDataReviewDraft;

        var taskResult = await aiTaskRepository.CreateSourceLinkedTaskAsync(
            createContext,
            new CreateSourceLinkedAiTaskCommand(
                Title: isDataReview
                    ? $"Kalite veri incelemesi — {ncr.NcrNumber}"
                    : $"Kalite uygunsuzluk incelemesi — {ncr.NcrNumber}",
                Summary: $"NCR: {ncr.NcrNumber}. Durum: {ncr.Status}. Şiddet: {ncr.Severity}. {ncr.Title}",
                Category: AiTaskCategories.Quality,
                Priority: priority,
                RiskLevel: priority,
                DetectedIssue: ncr.Description,
                Reasoning: $"Kalite uygunsuzluk kaydı {ncrId:D} için insan incelemeli görev taslağı.",
                Recommendation: new AiTaskRecommendationModel(
                    isDataReview ? "Veri incelemesi" : "Kalite incelemesi",
                    isDataReview
                        ? "Veri bütünlüğü ve kanıt yeterliliği doğrulanmalıdır."
                        : "Yetkili kalite ekibi uygunsuzluk kaydını incelemelidir. Otomatik üretim veya stok kararı uygulanmaz.",
                    AiTaskActionTypes.Review,
                    RequiresApproval: true,
                    Reversible: true,
                    TargetSystem: "crevafactos"),
                Confidence: new AiTaskConfidenceModel(
                    isDataReview ? 0.45m : 0.72m,
                    $"NCR şiddeti: {ncr.Severity}. Takip durumu: {followUp.FollowUpStatus}.",
                    null),
                Impact: new AiTaskImpactModel(
                    null, null, null, null, null, null, null,
                    followUp.EvidenceSummary),
                Sources:
                [
                    new AiTaskSourceReferenceModel(
                        QualityFollowUpConstants.SourceModule,
                        ncrId.ToString(),
                        $"Uygunsuzluk {ncr.NcrNumber}",
                        ncr.DetectedAtUtc,
                        followUp.EvidenceSummary),
                    new AiTaskSourceReferenceModel(
                        QualityFollowUpConstants.SourceModule,
                        ncr.NcrNumber,
                        ncr.Title,
                        ncr.DetectedAtUtc,
                        deepLink),
                ],
                SourceModule: QualityFollowUpConstants.SourceModule,
                SourceEntityId: ncrId.ToString(),
                IdempotencyKey: idempotencyKey,
                SiteId: null,
                ProductionLineId: null,
                MachineId: null,
                MoldId: null,
                ProductionOrderId: ncr.ProductionOrderId?.ToString(),
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
        var linkResult = await repository.LinkTaskAsync(
            readContext,
            new LinkQualityNcrFollowUpTaskCommand(ncrId, taskId),
            cancellationToken);

        if (!linkResult.Succeeded)
        {
            return MapFollowUpWriteResult(linkResult);
        }

        return TypedResults.Ok(new
        {
            taskId,
            ncrId,
            idempotentReplay = taskResult.IdempotentReplay,
            finding = linkResult.Detail is null
                ? QualityFollowUpApiMapper.MapDetail(followUp)
                : QualityFollowUpApiMapper.MapDetail(linkResult.Detail),
        });
    }

    private static async Task<IResult> TransitionFollowUpAsync(
        Guid ncrId,
        [FromBody] TransitionFollowUpRequest body,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IQualityNonconformanceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await QualityEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, QualityPermissionKeys.NcrManage, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.TransitionFollowUpAsync(
            context,
            new TransitionQualityNcrFollowUpCommand(
                ncrId,
                body.ExpectedVersion,
                body.TargetFollowUpStatus,
                body.Note),
            cancellationToken);

        return MapFollowUpWriteResult(result);
    }

    private static async Task<bool> HasNcrManageAccessAsync(
        Guid facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId, QualityPermissionKeys.NcrManage),
            cancellationToken);
        return accessResult.Succeeded && accessResult.Access is not null;
    }

    private static IResult MapFollowUpWriteResult(QualityNcrFollowUpWriteResult result)
    {
        if (result.Succeeded)
        {
            return TypedResults.Ok(new
            {
                entityId = result.EntityId,
                idempotentReplay = result.IdempotentReplay,
                finding = result.Detail is null ? null : QualityFollowUpApiMapper.MapDetail(result.Detail),
            });
        }

        return MapFailure(result.FailureCode!, result.FailureReason!);
    }

    private static IResult MapFailure(string code, string message) =>
        code switch
        {
            QualityFailureCodes.NotFound => TypedResults.NotFound(new { code, message }),
            QualityFailureCodes.ValidationFailed => TypedResults.Problem(
                title: message, statusCode: StatusCodes.Status400BadRequest,
                extensions: QualityEndpointHelpers.FailureExt(QualityApiFailureCodes.ValidationFailed)),
            QualityFailureCodes.InvalidTransition or QualityFailureCodes.ConcurrencyConflict =>
                TypedResults.Problem(
                    title: message, statusCode: StatusCodes.Status409Conflict,
                    extensions: QualityEndpointHelpers.FailureExt(QualityApiFailureCodes.InvalidTransition)),
            _ => TypedResults.Problem(
                title: message, statusCode: StatusCodes.Status400BadRequest,
                extensions: QualityEndpointHelpers.FailureExt(QualityApiFailureCodes.ValidationFailed)),
        };

    private sealed record TransitionFollowUpRequest(
        uint ExpectedVersion,
        string TargetFollowUpStatus,
        string? Note);
}

internal static class QualityFollowUpApiMapper
{
    public static object MapRow(QualityNcrFollowUpRowReadModel model) => new
    {
        id = model.Id,
        ncrNumber = model.NcrNumber,
        title = model.Title,
        severity = model.Severity,
        status = model.Status,
        followUpStatus = model.FollowUpStatus,
        holdStatus = model.HoldStatus,
        dispositionStatus = model.DispositionStatus,
        productReference = model.ProductReference,
        productionOrderId = model.ProductionOrderId,
        productionOrderReference = model.ProductionOrderReference,
        detectedAtUtc = model.DetectedAtUtc,
        linkedAiTaskId = model.LinkedAiTaskId,
        canCreateTaskDraft = model.CanCreateTaskDraft,
        taskDraftPurpose = model.TaskDraftPurpose,
        hasInsufficientEvidence = model.HasInsufficientEvidence,
    };

    public static object MapDetail(QualityNcrFollowUpDetailReadModel model)
    {
        var ncr = model.Detail;
        return new
        {
            id = ncr.Id,
            ncrNumber = ncr.NcrNumber,
            title = ncr.Title,
            description = ncr.Description,
            category = ncr.Category,
            severity = ncr.Severity,
            status = ncr.Status,
            holdStatus = ncr.HoldStatus,
            dispositionStatus = ncr.DispositionStatus,
            dispositionType = ncr.DispositionType,
            dispositionNote = ncr.DispositionNote,
            productionOrderId = ncr.ProductionOrderId,
            productionContext = ncr.ProductionContext is null ? null : new
            {
                productionOrderId = ncr.ProductionContext.ProductionOrderId,
                referenceCode = ncr.ProductionContext.ReferenceCode,
                productReference = ncr.ProductionContext.ProductReference,
                status = ncr.ProductionContext.Status,
            },
            qualityInspectionId = ncr.QualityInspectionId,
            qualityInspectionNumber = ncr.QualityInspectionNumber,
            qualityInspectionResultId = ncr.QualityInspectionResultId,
            productReference = ncr.ProductReference,
            operationReference = ncr.OperationReference,
            detectedAtUtc = ncr.DetectedAtUtc,
            version = ncr.Version,
            followUpStatus = model.FollowUpStatus,
            linkedAiTaskId = model.LinkedAiTaskId,
            canCreateTaskDraft = model.CanCreateTaskDraft,
            taskDraftPurpose = model.TaskDraftPurpose,
            hasInsufficientEvidence = model.HasInsufficientEvidence,
            evidenceSummary = model.EvidenceSummary,
            auditEvents = model.AuditEvents.Select(e => new
            {
                eventType = e.EventType,
                payloadJson = e.PayloadJson,
                actor = e.Actor,
                occurredAtUtc = e.OccurredAtUtc,
            }).ToList(),
            activeHold = ncr.ActiveHold is null ? null : QualityNcrApiMapper.MapHold(ncr.ActiveHold),
            correctiveActions = ncr.CorrectiveActions.Select(QualityNcrApiMapper.MapCorrectiveAction).ToList(),
        };
    }
}
