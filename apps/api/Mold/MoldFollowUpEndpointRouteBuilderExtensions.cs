using CrevaFactOS.Api.AiTasks;
using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.AiTasks;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Mold;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Mold;

internal static class MoldFollowUpEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapMoldFollowUpEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/follow-up/workspace", GetWorkspaceAsync);
        group.MapGet("/follow-up/{moldId:guid}", GetDetailAsync);
        group.MapPost("/follow-up/{moldId:guid}/task-draft", CreateTaskDraftAsync);
        group.MapPost("/follow-up/{moldId:guid}/follow-up", TransitionFollowUpAsync);

        return group;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? followUpStatus,
        [FromQuery] string? lifecycleState,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldFollowUpRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var canManage = await MoldEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var canCreateTask = await AiTaskEndpointHelpers.HasCreateAccessAsync(
            facilityId.Value, facilityAccessResolver, cancellationToken);

        var workspace = await repository.GetFollowUpWorkspaceAsync(context, canManage, canCreateTask, cancellationToken);
        var rows = await repository.ListFollowUpRowsAsync(context, followUpStatus, lifecycleState, cancellationToken);

        return TypedResults.Ok(new
        {
            automationDisclaimer = workspace.AutomationDisclaimer,
            kpis = new
            {
                requiresReviewCount = workspace.RequiresReviewCount,
                acknowledgedCount = workspace.AcknowledgedCount,
                taskLinkedCount = workspace.TaskLinkedCount,
                maintenanceStateCount = workspace.MaintenanceStateCount,
                dataReviewCount = workspace.DataReviewCount,
            },
            capabilities = new
            {
                canRead = workspace.CanRead,
                canManage = workspace.CanManage,
                canCreateTask = workspace.CanCreateTask,
            },
            molds = rows.Select(MoldFollowUpApiMapper.MapRow).ToList(),
        });
    }

    private static async Task<IResult> GetDetailAsync(
        Guid moldId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldFollowUpRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var detail = await repository.GetFollowUpDetailAsync(context, moldId, cancellationToken);
        return detail is null
            ? TypedResults.NotFound(new { code = MoldApiFailureCodes.NotFound, message = "Kalıp kaydı bulunamadı." })
            : TypedResults.Ok(MoldFollowUpApiMapper.MapDetail(detail));
    }

    private static async Task<IResult> CreateTaskDraftAsync(
        Guid moldId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldFollowUpRepository repository,
        IAiTaskRepository aiTaskRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (readContext, readFail) = await MoldEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (readContext is null) return readFail!;

        var (createContext, createFail) = await AiTaskEndpointHelpers.ResolveCreateAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (createContext is null) return createFail!;

        var followUp = await repository.GetFollowUpDetailAsync(readContext, moldId, cancellationToken);
        if (followUp is null)
        {
            return MapFailure(MoldFailureCodes.NotFound, "Kalıp kaydı bulunamadı.");
        }

        if (followUp.LinkedAiTaskId.HasValue)
        {
            return TypedResults.Ok(new
            {
                taskId = followUp.LinkedAiTaskId.Value,
                moldId,
                idempotentReplay = true,
                mold = MoldFollowUpApiMapper.MapDetail(followUp),
            });
        }

        if (!followUp.CanCreateTaskDraft)
        {
            return MapFailure(
                MoldFailureCodes.ValidationFailed,
                "Bu kalıp kaydı için görev taslağı oluşturulamaz.");
        }

        var taskPurpose = followUp.TaskDraftPurpose;
        var idempotencyKey = MoldFollowUpPresentationBuilder.BuildTaskDraftIdempotencyKey(moldId, taskPurpose);
        var priority = MoldFollowUpPresentationBuilder.ResolveTaskPriority(
            new MoldRegistryEntry
            {
                LifecycleState = MoldValueConversions.LifecycleFromStorage(followUp.Detail.LifecycleState),
                NominalShotLife = followUp.Detail.NominalShotLife,
                CurrentShotCount = followUp.Detail.CurrentShotCount,
            });
        var deepLink = MoldFollowUpPresentationBuilder.BuildMoldDeepLink(moldId);
        var mold = followUp.Detail;
        var isDataReview = taskPurpose == MoldFollowUpConstants.TaskPurposeDataReviewDraft;

        var taskResult = await aiTaskRepository.CreateSourceLinkedTaskAsync(
            createContext,
            new CreateSourceLinkedAiTaskCommand(
                Title: MoldFollowUpPresentationBuilder.BuildTaskTitle(new MoldRegistryEntry
                {
                    Code = mold.Code,
                    Name = mold.Name,
                    LifecycleState = MoldValueConversions.LifecycleFromStorage(mold.LifecycleState),
                    NominalShotLife = mold.NominalShotLife,
                    CurrentShotCount = mold.CurrentShotCount,
                }),
                Summary: MoldFollowUpPresentationBuilder.BuildTaskSummary(new MoldRegistryEntry
                {
                    Code = mold.Code,
                    Name = mold.Name,
                    LifecycleState = MoldValueConversions.LifecycleFromStorage(mold.LifecycleState),
                    NominalShotLife = mold.NominalShotLife,
                    CurrentShotCount = mold.CurrentShotCount,
                }),
                Category: AiTaskCategories.Maintenance,
                Priority: priority,
                RiskLevel: priority,
                DetectedIssue: followUp.EvidenceSummary,
                Reasoning: $"Kalıp kaydı {moldId:D} için insan incelemeli görev taslağı.",
                Recommendation: new AiTaskRecommendationModel(
                    isDataReview ? "Veri incelemesi" : "Teknik inceleme",
                    isDataReview
                        ? "Kalıp yaşam döngüsü ve sayaç kanıtı doğrulanmalıdır."
                        : "Yetkili ekip kalıp kaydını incelemelidir. Otomatik bakım emri veya yaşam döngüsü kararı uygulanmaz.",
                    AiTaskActionTypes.Review,
                    RequiresApproval: true,
                    Reversible: true,
                    TargetSystem: "crevafactos"),
                Confidence: new AiTaskConfidenceModel(
                    isDataReview ? 0.45m : 0.72m,
                    $"Yaşam döngüsü: {mold.LifecycleState}. Takip durumu: {followUp.FollowUpStatus}.",
                    null),
                Impact: new AiTaskImpactModel(
                    null, null, null, null, null, null, null,
                    followUp.EvidenceSummary),
                Sources:
                [
                    new AiTaskSourceReferenceModel(
                        MoldFollowUpConstants.SourceModule,
                        moldId.ToString(),
                        $"Kalıp {mold.Code}",
                        mold.UpdatedAtUtc,
                        followUp.EvidenceSummary),
                    new AiTaskSourceReferenceModel(
                        MoldFollowUpConstants.SourceModule,
                        mold.Code,
                        mold.Name,
                        mold.UpdatedAtUtc,
                        deepLink),
                ],
                SourceModule: MoldFollowUpConstants.SourceModule,
                SourceEntityId: moldId.ToString(),
                IdempotencyKey: idempotencyKey,
                SiteId: null,
                ProductionLineId: null,
                MachineId: null,
                MoldId: moldId.ToString(),
                ProductionOrderId: null,
                SalesOrderId: null,
                MaterialId: mold.MaterialFamilyReference,
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
            new LinkMoldFollowUpTaskCommand(moldId, taskId),
            cancellationToken);

        if (!linkResult.Succeeded)
        {
            return MapFollowUpWriteResult(linkResult);
        }

        return TypedResults.Ok(new
        {
            taskId,
            moldId,
            idempotentReplay = taskResult.IdempotentReplay,
            mold = linkResult.Detail is null
                ? MoldFollowUpApiMapper.MapDetail(followUp)
                : MoldFollowUpApiMapper.MapDetail(linkResult.Detail),
        });
    }

    private static async Task<IResult> TransitionFollowUpAsync(
        Guid moldId,
        [FromBody] TransitionFollowUpRequest body,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMoldFollowUpRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MoldEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.TransitionFollowUpAsync(
            context,
            new TransitionMoldFollowUpCommand(
                moldId,
                body.ExpectedVersion,
                body.TargetFollowUpStatus,
                body.Note),
            cancellationToken);

        return MapFollowUpWriteResult(result);
    }

    private static IResult MapFollowUpWriteResult(MoldFollowUpWriteResult result)
    {
        if (result.Succeeded)
        {
            return TypedResults.Ok(new
            {
                entityId = result.EntityId,
                idempotentReplay = result.IdempotentReplay,
                mold = result.Detail is null ? null : MoldFollowUpApiMapper.MapDetail(result.Detail),
            });
        }

        return MapFailure(result.FailureCode!, result.FailureReason!);
    }

    private static IResult MapFailure(string code, string message) =>
        code switch
        {
            MoldFailureCodes.NotFound => TypedResults.NotFound(new { code, message }),
            MoldFailureCodes.ValidationFailed => TypedResults.Problem(
                title: message, statusCode: StatusCodes.Status400BadRequest,
                extensions: MoldEndpointHelpers.FailureExt(MoldApiFailureCodes.ValidationFailed)),
            MoldFailureCodes.InvalidTransition or MoldFailureCodes.ConcurrencyConflict =>
                TypedResults.Problem(
                    title: message, statusCode: StatusCodes.Status409Conflict,
                    extensions: MoldEndpointHelpers.FailureExt(MoldApiFailureCodes.InvalidTransition)),
            _ => TypedResults.Problem(
                title: message, statusCode: StatusCodes.Status400BadRequest,
                extensions: MoldEndpointHelpers.FailureExt(MoldApiFailureCodes.ValidationFailed)),
        };

    private sealed record TransitionFollowUpRequest(
        uint ExpectedVersion,
        string TargetFollowUpStatus,
        string? Note);
}

internal static class MoldFollowUpApiMapper
{
    public static object MapRow(MoldFollowUpRowReadModel model) => new
    {
        id = model.Id,
        code = model.Code,
        name = model.Name,
        lifecycleState = model.LifecycleState,
        followUpStatus = model.FollowUpStatus,
        currentShotCount = model.CurrentShotCount,
        nominalShotLife = model.NominalShotLife,
        counterRecordedAtUtc = model.CounterRecordedAtUtc,
        primaryProductReference = model.PrimaryProductReference,
        updatedAtUtc = model.UpdatedAtUtc,
        linkedAiTaskId = model.LinkedAiTaskId,
        canCreateTaskDraft = model.CanCreateTaskDraft,
        taskDraftPurpose = model.TaskDraftPurpose,
        hasInsufficientEvidence = model.HasInsufficientEvidence,
    };

    public static object MapDetail(MoldFollowUpDetailReadModel model)
    {
        var mold = model.Detail;
        return new
        {
            id = mold.Id,
            code = mold.Code,
            name = mold.Name,
            description = mold.Description,
            lifecycleState = mold.LifecycleState,
            ownershipType = mold.OwnershipType,
            locationType = mold.LocationType,
            locationLabel = mold.LocationLabel,
            cavityCount = mold.CavityCount,
            nominalShotLife = mold.NominalShotLife,
            currentShotCount = mold.CurrentShotCount,
            counterRecordedAtUtc = mold.CounterRecordedAtUtc,
            counterSource = mold.CounterSource,
            primaryProductReference = mold.PrimaryProductReference,
            materialFamilyReference = mold.MaterialFamilyReference,
            manufacturer = mold.Manufacturer,
            updatedAtUtc = mold.UpdatedAtUtc,
            version = mold.Version,
            followUpStatus = model.FollowUpStatus,
            linkedAiTaskId = model.LinkedAiTaskId,
            canCreateTaskDraft = model.CanCreateTaskDraft,
            taskDraftPurpose = model.TaskDraftPurpose,
            hasInsufficientEvidence = model.HasInsufficientEvidence,
            evidenceSummary = model.EvidenceSummary,
            lifecycleEvents = model.LifecycleEvents.Select(e => new
            {
                id = e.Id,
                previousState = e.PreviousState,
                newState = e.NewState,
                reason = e.Reason,
                actorDisplayName = e.ActorDisplayName,
                occurredAtUtc = e.OccurredAtUtc,
                sourceReference = e.SourceReference,
            }).ToList(),
            auditEvents = model.AuditEvents.Select(e => new
            {
                eventType = e.EventType,
                payloadJson = e.PayloadJson,
                actor = e.Actor,
                occurredAtUtc = e.OccurredAtUtc,
            }).ToList(),
        };
    }
}
