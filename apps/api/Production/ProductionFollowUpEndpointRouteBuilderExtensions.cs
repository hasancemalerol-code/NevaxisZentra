using NevaxisFactOS.Api.AiTasks;
using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.AiTasks;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Production;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisFactOS.Api.Production;

internal static class ProductionFollowUpEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapProductionFollowUpEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/follow-up/workspace", GetWorkspaceAsync);
        group.MapGet("/follow-up/{exceptionId:guid}", GetDetailAsync);
        group.MapPost("/follow-up/{exceptionId:guid}/task-draft", CreateTaskDraftAsync);
        group.MapPost("/follow-up/{exceptionId:guid}/follow-up", TransitionFollowUpAsync);

        return group;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? followUpStatus,
        [FromQuery] string? exceptionType,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionFollowUpRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProductionFollowUpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var canManage = await ProductionFollowUpEndpointHelpers.HasExceptionManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var canCreateTask = await AiTaskEndpointHelpers.HasCreateAccessAsync(
            facilityId.Value, facilityAccessResolver, cancellationToken);

        var workspace = await repository.GetFollowUpWorkspaceAsync(context, canManage, canCreateTask, cancellationToken);
        var rows = await repository.ListFollowUpRowsAsync(context, followUpStatus, exceptionType, cancellationToken);

        return TypedResults.Ok(new
        {
            automationDisclaimer = workspace.AutomationDisclaimer,
            kpis = new
            {
                requiresReviewCount = workspace.RequiresReviewCount,
                acknowledgedCount = workspace.AcknowledgedCount,
                taskLinkedCount = workspace.TaskLinkedCount,
                blockedOrDelayedCount = workspace.BlockedOrDelayedCount,
                dataReviewCount = workspace.DataReviewCount,
            },
            capabilities = new
            {
                canRead = workspace.CanRead,
                canManage = workspace.CanManage,
                canCreateTask = workspace.CanCreateTask,
            },
            exceptions = rows.Select(ProductionFollowUpApiMapper.MapRow).ToList(),
        });
    }

    private static async Task<IResult> GetDetailAsync(
        Guid exceptionId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionFollowUpRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProductionFollowUpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var detail = await repository.GetFollowUpDetailAsync(context, exceptionId, cancellationToken);
        return detail is null
            ? TypedResults.NotFound(new { code = ProductionFollowUpApiFailureCodes.NotFound, message = "Üretim istisnası bulunamadı." })
            : TypedResults.Ok(ProductionFollowUpApiMapper.MapDetail(detail));
    }

    private static async Task<IResult> CreateTaskDraftAsync(
        Guid exceptionId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionFollowUpRepository repository,
        IAiTaskRepository aiTaskRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (readContext, readFail) = await ProductionFollowUpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (readContext is null) return readFail!;

        var (createContext, createFail) = await AiTaskEndpointHelpers.ResolveCreateAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (createContext is null) return createFail!;

        var followUp = await repository.GetFollowUpDetailAsync(readContext, exceptionId, cancellationToken);
        if (followUp is null)
        {
            return MapFailure(ProductionOrderFailureCodes.ExceptionNotFound, "Üretim istisnası bulunamadı.");
        }

        if (followUp.LinkedAiTaskId.HasValue)
        {
            return TypedResults.Ok(new
            {
                taskId = followUp.LinkedAiTaskId.Value,
                exceptionId,
                idempotentReplay = true,
                exception = ProductionFollowUpApiMapper.MapDetail(followUp),
            });
        }

        if (!followUp.CanCreateTaskDraft)
        {
            return MapFailure("validation-failed", "Bu üretim istisnası için görev taslağı oluşturulamaz.");
        }

        var taskPurpose = followUp.TaskDraftPurpose;
        var idempotencyKey = ProductionFollowUpPresentationBuilder.BuildTaskDraftIdempotencyKey(exceptionId, taskPurpose);
        var exceptionRecord = new ProductionExecutionExceptionRecord
        {
            ExceptionType = followUp.ExceptionType,
            Severity = followUp.Severity,
            Status = followUp.ExceptionStatus,
            Message = followUp.Message,
        };
        var order = new ProductionOrder
        {
            ReferenceCode = followUp.ProductionOrderReference,
            MachineReference = followUp.MachineReference,
            MoldReference = followUp.MoldReference,
            ProductReference = followUp.ProductReference,
        };
        var priority = ProductionFollowUpPresentationBuilder.ResolveTaskPriority(exceptionRecord);
        var deepLink = ProductionFollowUpPresentationBuilder.BuildProductionDeepLink(exceptionId);
        var isDataReview = taskPurpose == ProductionFollowUpConstants.TaskPurposeDataReviewDraft;

        var taskResult = await aiTaskRepository.CreateSourceLinkedTaskAsync(
            createContext,
            new CreateSourceLinkedAiTaskCommand(
                Title: ProductionFollowUpPresentationBuilder.BuildTaskTitle(exceptionRecord, order),
                Summary: ProductionFollowUpPresentationBuilder.BuildTaskSummary(exceptionRecord, order),
                Category: AiTaskCategories.LiveProduction,
                Priority: priority,
                RiskLevel: priority,
                DetectedIssue: followUp.EvidenceSummary,
                Reasoning: $"Üretim istisnası {exceptionId:D} için insan incelemeli görev taslağı.",
                Recommendation: new AiTaskRecommendationModel(
                    isDataReview ? "Veri incelemesi" : "Üretim incelemesi",
                    isDataReview
                        ? "Üretim istisna kanıtı ve emir bağlamı doğrulanmalıdır."
                        : "Yetkili ekip üretim istisnasını incelemelidir. Otomatik üretim emri, makine, stok veya kalite kararı uygulanmaz.",
                    AiTaskActionTypes.Review,
                    RequiresApproval: true,
                    Reversible: true,
                    TargetSystem: "crevafactos"),
                Confidence: new AiTaskConfidenceModel(
                    isDataReview ? 0.45m : 0.72m,
                    $"İstisna: {followUp.ExceptionType}. Takip durumu: {followUp.FollowUpStatus}.",
                    null),
                Impact: new AiTaskImpactModel(
                    null, null, null, null, null, null, null,
                    followUp.EvidenceSummary),
                Sources:
                [
                    new AiTaskSourceReferenceModel(
                        ProductionFollowUpConstants.SourceModule,
                        exceptionId.ToString(),
                        $"Emir {followUp.ProductionOrderReference}",
                        followUp.RaisedAtUtc,
                        followUp.EvidenceSummary),
                    new AiTaskSourceReferenceModel(
                        ProductionFollowUpConstants.SourceModule,
                        followUp.ProductionOrderReference,
                        followUp.ProductionOrderTitle,
                        followUp.RaisedAtUtc,
                        deepLink),
                ],
                SourceModule: ProductionFollowUpConstants.SourceModule,
                SourceEntityId: exceptionId.ToString(),
                IdempotencyKey: idempotencyKey,
                SiteId: null,
                ProductionLineId: null,
                MachineId: followUp.MachineReference,
                MoldId: followUp.MoldReference,
                ProductionOrderId: followUp.ProductionOrderId.ToString(),
                SalesOrderId: null,
                MaterialId: followUp.ProductReference,
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
            new LinkProductionFollowUpTaskCommand(exceptionId, taskId),
            cancellationToken);

        if (!linkResult.Succeeded)
        {
            return MapFollowUpWriteResult(linkResult);
        }

        return TypedResults.Ok(new
        {
            taskId,
            exceptionId,
            idempotentReplay = taskResult.IdempotentReplay,
            exception = linkResult.Detail is null
                ? ProductionFollowUpApiMapper.MapDetail(followUp)
                : ProductionFollowUpApiMapper.MapDetail(linkResult.Detail),
        });
    }

    private static async Task<IResult> TransitionFollowUpAsync(
        Guid exceptionId,
        [FromBody] TransitionProductionFollowUpRequest body,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProductionFollowUpRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ProductionFollowUpEndpointHelpers.ResolveExceptionManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.TransitionFollowUpAsync(
            context,
            new TransitionProductionFollowUpCommand(
                exceptionId,
                body.ExpectedVersion,
                body.TargetFollowUpStatus,
                body.Note),
            cancellationToken);

        return MapFollowUpWriteResult(result);
    }

    private static IResult MapFollowUpWriteResult(ProductionFollowUpWriteResult result)
    {
        if (result.Succeeded)
        {
            return TypedResults.Ok(new
            {
                entityId = result.EntityId,
                idempotentReplay = result.IdempotentReplay,
                exception = result.Detail is null ? null : ProductionFollowUpApiMapper.MapDetail(result.Detail),
            });
        }

        return MapFailure(result.FailureCode!, result.FailureReason!);
    }

    private static IResult MapFailure(string code, string message) =>
        code switch
        {
            ProductionOrderFailureCodes.ExceptionNotFound => TypedResults.NotFound(new { code, message }),
            "validation-failed" => TypedResults.Problem(
                title: message, statusCode: StatusCodes.Status400BadRequest,
                extensions: ProductionFollowUpEndpointHelpers.FailureExt(ProductionFollowUpApiFailureCodes.ValidationFailed)),
            ProductionOrderFailureCodes.InvalidTransition or ProductionOrderFailureCodes.ConcurrencyConflict =>
                TypedResults.Problem(
                    title: message, statusCode: StatusCodes.Status409Conflict,
                    extensions: ProductionFollowUpEndpointHelpers.FailureExt(ProductionFollowUpApiFailureCodes.InvalidTransition)),
            _ => TypedResults.Problem(
                title: message, statusCode: StatusCodes.Status400BadRequest,
                extensions: ProductionFollowUpEndpointHelpers.FailureExt(ProductionFollowUpApiFailureCodes.ValidationFailed)),
        };

    private sealed record TransitionProductionFollowUpRequest(
        uint ExpectedVersion,
        string TargetFollowUpStatus,
        string? Note);
}

internal static class ProductionFollowUpEndpointHelpers
{
    internal static async Task<(ProductionOrderOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required."));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<(ProductionOrderOperationContext? Context, IResult? Failure)> ResolveExceptionManageAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required."));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, ProductionExecutionPermissionKeys.Exception),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<bool> HasExceptionManageAccessAsync(
        Guid facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId, ProductionExecutionPermissionKeys.Exception),
            cancellationToken);

        return accessResult.Succeeded && accessResult.Access is not null;
    }

    internal static ProductionOrderOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    internal static Dictionary<string, object?> FailureExt(string failureCode) =>
        new() { ["failureCode"] = failureCode };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication is required.",
                statusCode: StatusCodes.Status401Unauthorized),
            _ => TypedResults.Problem(
                title: "Authorization denied.",
                statusCode: StatusCodes.Status403Forbidden),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapAccessDenied(string failureCode, string reason) =>
        failureCode == FacilityAccessFailureCodes.FacilityMissing
            ? TypedResults.Problem(title: reason, statusCode: StatusCodes.Status400BadRequest)
            : TypedResults.Problem(title: reason, statusCode: StatusCodes.Status403Forbidden);
}

internal static class ProductionFollowUpApiFailureCodes
{
    public const string NotFound = "production-follow-up.not-found";
    public const string ValidationFailed = "production-follow-up.validation-failed";
    public const string InvalidTransition = "production-follow-up.invalid-transition";
}

internal static class ProductionFollowUpApiMapper
{
    public static object MapRow(ProductionFollowUpRowReadModel model) => new
    {
        id = model.Id,
        productionOrderId = model.ProductionOrderId,
        productionOrderReference = model.ProductionOrderReference,
        exceptionType = model.ExceptionType,
        severity = model.Severity,
        exceptionStatus = model.ExceptionStatus,
        message = model.Message,
        raisedAtUtc = model.RaisedAtUtc,
        followUpStatus = model.FollowUpStatus,
        linkedAiTaskId = model.LinkedAiTaskId,
        version = model.Version,
        canCreateTaskDraft = model.CanCreateTaskDraft,
        taskDraftPurpose = model.TaskDraftPurpose,
        hasInsufficientEvidence = model.HasInsufficientEvidence,
        isBlockedOrDelayed = model.IsBlockedOrDelayed,
    };

    public static object MapDetail(ProductionFollowUpDetailReadModel model) => new
    {
        id = model.Id,
        productionOrderId = model.ProductionOrderId,
        productionOrderReference = model.ProductionOrderReference,
        productionOrderTitle = model.ProductionOrderTitle,
        productionOrderStatus = model.ProductionOrderStatus,
        machineReference = model.MachineReference,
        moldReference = model.MoldReference,
        productReference = model.ProductReference,
        exceptionType = model.ExceptionType,
        severity = model.Severity,
        exceptionStatus = model.ExceptionStatus,
        message = model.Message,
        raisedAtUtc = model.RaisedAtUtc,
        raisedBySubjectId = model.RaisedBySubjectId,
        followUpStatus = model.FollowUpStatus,
        linkedAiTaskId = model.LinkedAiTaskId,
        version = model.Version,
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
    };
}
