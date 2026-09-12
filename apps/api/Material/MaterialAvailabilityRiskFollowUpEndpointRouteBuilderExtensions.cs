using NevaxisFactOS.Api.AiTasks;
using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.AiTasks;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Material;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisFactOS.Api.Material;

internal static class MaterialAvailabilityRiskFollowUpEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapMaterialAvailabilityRiskFollowUpEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{assessmentId:guid}/task-draft", CreateTaskDraftAsync);
        group.MapPost("/{assessmentId:guid}/follow-up", TransitionFollowUpAsync);

        return group;
    }

    private static async Task<IResult> CreateTaskDraftAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialAvailabilityRiskRepository repository,
        IAiTaskRepository aiTaskRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (readContext, readFail) = await MaterialEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (readContext is null) return readFail!;

        var (createContext, createFail) = await AiTaskEndpointHelpers.ResolveCreateAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (createContext is null) return createFail!;

        var followUp = await repository.GetFollowUpDetailAsync(readContext, assessmentId, cancellationToken);
        if (followUp is null)
        {
            return MaterialEndpointHelpers.MapRequirementFailure(
                MaterialRequirementFailureCodes.NotFound,
                "Availability risk assessment not found.");
        }

        if (followUp.LinkedAiTaskId.HasValue)
        {
            return TypedResults.Ok(new
            {
                taskId = followUp.LinkedAiTaskId.Value,
                assessmentId,
                idempotentReplay = true,
                assessment = MaterialAvailabilityRiskFollowUpApiMapper.MapDetail(followUp),
            });
        }

        if (!followUp.CanCreateTaskDraft)
        {
            return MaterialEndpointHelpers.MapRequirementFailure(
                MaterialRequirementFailureCodes.ValidationFailed,
                "Bu malzeme risk değerlendirmesi için görev taslağı oluşturulamaz.");
        }

        var assessment = followUp.Assessment;
        var taskPurpose = followUp.TaskDraftPurpose;
        var idempotencyKey =
            MaterialAvailabilityRiskFollowUpPresentationBuilder.BuildTaskDraftIdempotencyKey(
                assessmentId,
                taskPurpose);
        var priority = MaterialAvailabilityRiskFollowUpPresentationBuilder.ResolveTaskPriority(assessment.Status);
        var deepLink = MaterialAvailabilityRiskFollowUpPresentationBuilder.BuildMaterialRiskDeepLink(assessmentId);
        var isDataReview =
            taskPurpose == MaterialAvailabilityRiskFollowUpConstants.TaskPurposeDataReviewDraft;
        var materialLabel = assessment.MaterialCode;
        var title = isDataReview
            ? $"Veri inceleme görev taslağı — {materialLabel}"
            : $"Stok ve malzeme inceleme görev taslağı — {materialLabel}";
        var summary = MaterialAvailabilityRiskFollowUpPresentationBuilder.BuildTaskSummary(assessment);

        var taskResult = await aiTaskRepository.CreateSourceLinkedTaskAsync(
            createContext,
            new CreateSourceLinkedAiTaskCommand(
                Title: title,
                Summary: summary,
                Category: AiTaskCategories.MaterialRisk,
                Priority: priority,
                RiskLevel: priority,
                DetectedIssue: followUp.EvidenceSummary,
                Reasoning:
                    $"Malzeme kullanılabilirlik risk değerlendirmesi {assessmentId:D} için insan incelemeli görev taslağı.",
                Recommendation: new AiTaskRecommendationModel(
                    isDataReview ? "Veri incelemesi" : "Stok ve malzeme incelemesi",
                    isDataReview
                        ? "Veri bütünlüğü ve kanıt yeterliliği doğrulanmalıdır."
                        : "Yetkili ekip malzeme kullanılabilirlik riskini incelemelidir. Otomatik stok, satın alma veya üretim işlemi uygulanmaz.",
                    AiTaskActionTypes.Review,
                    RequiresApproval: true,
                    Reversible: true,
                    TargetSystem: "crevafactos"),
                Confidence: new AiTaskConfidenceModel(
                    isDataReview ? 0.45m : 0.72m,
                    $"Risk durumu: {assessment.Status}. Takip durumu: {followUp.FollowUpStatus}.",
                    null),
                Impact: new AiTaskImpactModel(
                    null, null, null, null, null, null, null,
                    followUp.EvidenceSummary),
                Sources:
                [
                    new AiTaskSourceReferenceModel(
                        MaterialAvailabilityRiskFollowUpConstants.SourceModule,
                        assessmentId.ToString(),
                        $"{materialLabel} risk değerlendirmesi",
                        assessment.EvaluatedAtUtc,
                        followUp.EvidenceSummary),
                    new AiTaskSourceReferenceModel(
                        MaterialAvailabilityRiskFollowUpConstants.SourceModule,
                        assessment.MaterialId.ToString(),
                        materialLabel,
                        assessment.EvaluatedAtUtc,
                        deepLink),
                ],
                SourceModule: MaterialAvailabilityRiskFollowUpConstants.SourceModule,
                SourceEntityId: assessmentId.ToString(),
                IdempotencyKey: idempotencyKey,
                SiteId: null,
                ProductionLineId: null,
                MachineId: null,
                MoldId: assessment.MoldReference,
                ProductionOrderId: assessment.ProductionOrderReference,
                SalesOrderId: null,
                MaterialId: assessment.MaterialId.ToString(),
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
            new LinkMaterialAvailabilityRiskFollowUpTaskCommand(assessmentId, taskId),
            cancellationToken);

        if (!linkResult.Succeeded)
        {
            return MapFollowUpWriteResult(linkResult);
        }

        return TypedResults.Ok(new
        {
            taskId,
            assessmentId,
            idempotentReplay = taskResult.IdempotentReplay,
            assessment = linkResult.Detail is null
                ? MaterialAvailabilityRiskFollowUpApiMapper.MapDetail(followUp)
                : MaterialAvailabilityRiskFollowUpApiMapper.MapDetail(linkResult.Detail),
        });
    }

    private static async Task<IResult> TransitionFollowUpAsync(
        Guid assessmentId,
        [FromBody] TransitionMaterialAvailabilityRiskFollowUpRequest body,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaterialAvailabilityRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaterialEndpointHelpers.ResolveWriteAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.TransitionFollowUpAsync(
            context,
            new TransitionMaterialAvailabilityRiskFollowUpCommand(
                assessmentId,
                body.ExpectedVersion,
                body.TargetFollowUpStatus,
                body.Note),
            cancellationToken);

        return MapFollowUpWriteResult(result);
    }

    internal static IResult MapFollowUpWriteResult(MaterialAvailabilityRiskFollowUpWriteResult result)
    {
        if (result.Succeeded)
        {
            return TypedResults.Ok(new
            {
                entityId = result.EntityId,
                idempotentReplay = result.IdempotentReplay,
                assessment = result.Detail is null
                    ? null
                    : MaterialAvailabilityRiskFollowUpApiMapper.MapDetail(result.Detail),
            });
        }

        return MaterialEndpointHelpers.MapRequirementFailure(result.FailureCode!, result.FailureReason!);
    }

    private sealed record TransitionMaterialAvailabilityRiskFollowUpRequest(
        uint ExpectedVersion,
        string TargetFollowUpStatus,
        string? Note);
}

internal static class MaterialAvailabilityRiskFollowUpApiMapper
{
    internal static object MapDetail(MaterialAvailabilityRiskFollowUpDetailReadModel model)
    {
        var assessment = model.Assessment;
        return new
        {
            id = assessment.Id,
            facilityId = assessment.FacilityId,
            materialId = assessment.MaterialId,
            materialCode = assessment.MaterialCode,
            materialName = assessment.MaterialName,
            requirementProfileId = assessment.RequirementProfileId,
            productionOrderReference = assessment.ProductionOrderReference,
            productReference = assessment.ProductReference,
            moldReference = assessment.MoldReference,
            status = assessment.Status,
            ruleSetVersion = assessment.RuleSetVersion,
            inputFingerprint = assessment.InputFingerprint,
            requiredQuantityGrams = assessment.RequiredQuantityGrams,
            erpAvailableQuantity = assessment.ErpAvailableQuantity,
            erpReservedQuantity = assessment.ErpReservedQuantity,
            erpUnit = assessment.ErpUnit,
            availableAfterReservationQuantity = assessment.AvailableAfterReservationQuantity,
            shortageQuantity = assessment.ShortageQuantity,
            erpSourceUpdatedAtUtc = assessment.ErpSourceUpdatedAtUtc,
            sourceFreshness = assessment.SourceFreshness,
            drivers = assessment.Drivers.Select(d => new { code = d.Code, label = d.Label, value = d.Value, state = d.State }).ToList(),
            evidence = assessment.Evidence.Select(e => new
            {
                field = e.Field,
                value = e.Value,
                source = e.Source,
                recordedAtUtc = e.RecordedAtUtc,
                isMissing = e.IsMissing,
            }).ToList(),
            evaluatedAtUtc = assessment.EvaluatedAtUtc,
            createdAtUtc = assessment.CreatedAtUtc,
            version = assessment.Version,
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
        };
    }
}
