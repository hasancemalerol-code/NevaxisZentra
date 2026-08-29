using CrevaFactOS.Api.AiTasks;
using CrevaFactOS.Api.Erp;
using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.AiTasks;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Erp;
using CrevaFactOS.Persistence.Procurement;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.Procurement;

internal static class ProcurementDeliveryRiskFollowUpEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapProcurementDeliveryRiskFollowUpEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{assessmentId:guid}/task-draft", CreateTaskDraftAsync);
        group.MapPost("/{assessmentId:guid}/follow-up", TransitionFollowUpAsync);

        return group;
    }

    private static async Task<IResult> CreateTaskDraftAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcurementDeliveryRiskRepository repository,
        IAiTaskRepository aiTaskRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (readContext, readFail) = await ErpEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (readContext is null) return readFail!;

        var (createContext, createFail) = await AiTaskEndpointHelpers.ResolveCreateAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (createContext is null) return createFail!;

        var followUp = await repository.GetFollowUpDetailAsync(readContext, assessmentId, cancellationToken);
        if (followUp is null)
        {
            return MapFailure(ProcurementDeliveryRiskFailureCodes.NotFound, "Delivery risk assessment not found.");
        }

        if (followUp.LinkedAiTaskId.HasValue)
        {
            return TypedResults.Ok(new
            {
                taskId = followUp.LinkedAiTaskId.Value,
                assessmentId,
                idempotentReplay = true,
                assessment = ProcurementDeliveryRiskFollowUpApiMapper.MapDetail(followUp),
            });
        }

        if (!followUp.CanCreateTaskDraft)
        {
            return MapFailure(
                ProcurementDeliveryRiskFailureCodes.ValidationFailed,
                "Bu tedarik risk değerlendirmesi için görev taslağı oluşturulamaz.");
        }

        var taskPurpose = followUp.TaskDraftPurpose;
        var idempotencyKey = ProcurementDeliveryRiskFollowUpPresentationBuilder.BuildTaskDraftIdempotencyKey(
            assessmentId,
            taskPurpose);
        var priority = ProcurementDeliveryRiskFollowUpPresentationBuilder.ResolveTaskPriority(followUp.Assessment.Result);
        var deepLink = ProcurementDeliveryRiskFollowUpPresentationBuilder.BuildProcurementRiskDeepLink(assessmentId);
        var isDataReview = taskPurpose == ProcurementDeliveryRiskFollowUpConstants.TaskPurposeDataReviewDraft;
        var orderLabel = followUp.Assessment.PurchaseOrderNumber;
        var title = isDataReview
            ? $"Veri inceleme görev taslağı — {orderLabel}"
            : $"Tedarik inceleme görev taslağı — {orderLabel}";
        var summary = ProcurementDeliveryRiskFollowUpPresentationBuilder.BuildTaskSummary(followUp.Assessment);

        var taskResult = await aiTaskRepository.CreateSourceLinkedTaskAsync(
            createContext,
            new CreateSourceLinkedAiTaskCommand(
                Title: title,
                Summary: summary,
                Category: AiTaskCategories.DeliveryRisk,
                Priority: priority,
                RiskLevel: priority,
                DetectedIssue: followUp.EvidenceSummary,
                Reasoning:
                    $"Tedarik teslimat risk değerlendirmesi {assessmentId:D} için insan incelemeli görev taslağı.",
                Recommendation: new AiTaskRecommendationModel(
                    isDataReview ? "Veri incelemesi" : "Tedarik incelemesi",
                    isDataReview
                        ? "Veri bütünlüğü ve kanıt yeterliliği doğrulanmalıdır."
                        : "Yetkili ekip tedarik teslimat riskini incelemelidir. Otomatik satın alma, tedarikçi iletişimi, stok veya üretim işlemi uygulanmaz.",
                    AiTaskActionTypes.Review,
                    RequiresApproval: true,
                    Reversible: true,
                    TargetSystem: "crevafactos"),
                Confidence: new AiTaskConfidenceModel(
                    isDataReview ? 0.45m : 0.72m,
                    $"Risk durumu: {followUp.Assessment.Result}. Takip durumu: {followUp.FollowUpStatus}.",
                    null),
                Impact: new AiTaskImpactModel(
                    null, null, null, null, null, null, null,
                    followUp.EvidenceSummary),
                Sources:
                [
                    new AiTaskSourceReferenceModel(
                        ProcurementDeliveryRiskFollowUpConstants.SourceModule,
                        assessmentId.ToString(),
                        $"{orderLabel} risk değerlendirmesi",
                        followUp.Assessment.EvaluatedAtUtc,
                        followUp.EvidenceSummary),
                    new AiTaskSourceReferenceModel(
                        ProcurementDeliveryRiskFollowUpConstants.SourceModule,
                        followUp.Assessment.PurchaseOrderId.ToString(),
                        orderLabel,
                        followUp.Assessment.EvaluatedAtUtc,
                        deepLink),
                ],
                SourceModule: ProcurementDeliveryRiskFollowUpConstants.SourceModule,
                SourceEntityId: assessmentId.ToString(),
                IdempotencyKey: idempotencyKey,
                SiteId: null,
                ProductionLineId: null,
                MachineId: null,
                MoldId: null,
                ProductionOrderId: followUp.Assessment.PurchaseOrderNumber,
                SalesOrderId: null,
                MaterialId: null,
                SupplierId: followUp.Assessment.ExternalSupplierId,
                CustomerId: null,
                DueAtUtc: followUp.Assessment.CommitmentDateUtc),
            cancellationToken);

        if (!taskResult.Succeeded)
        {
            return AiTaskEndpointHelpers.MapFailure(taskResult.FailureCode!, taskResult.FailureReason!);
        }

        var taskId = taskResult.EntityId!.Value;
        var linkResult = await repository.LinkTaskAsync(
            readContext,
            new LinkProcurementDeliveryRiskFollowUpTaskCommand(assessmentId, taskId),
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
                ? ProcurementDeliveryRiskFollowUpApiMapper.MapDetail(followUp)
                : ProcurementDeliveryRiskFollowUpApiMapper.MapDetail(linkResult.Detail),
        });
    }

    private static async Task<IResult> TransitionFollowUpAsync(
        Guid assessmentId,
        [FromBody] TransitionProcurementDeliveryRiskFollowUpRequest body,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IProcurementDeliveryRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await ErpEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            ErpPermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.TransitionFollowUpAsync(
            context,
            new TransitionProcurementDeliveryRiskFollowUpCommand(
                assessmentId,
                body.ExpectedVersion,
                body.TargetFollowUpStatus,
                body.Note),
            cancellationToken);

        return MapFollowUpWriteResult(result);
    }

    internal static IResult MapFollowUpWriteResult(ProcurementDeliveryRiskFollowUpWriteResult result)
    {
        if (result.Succeeded)
        {
            return TypedResults.Ok(new
            {
                entityId = result.EntityId,
                idempotentReplay = result.IdempotentReplay,
                assessment = result.Detail is null
                    ? null
                    : ProcurementDeliveryRiskFollowUpApiMapper.MapDetail(result.Detail),
            });
        }

        return MapFailure(result.FailureCode!, result.FailureReason);
    }

    private static IResult MapFailure(string failureCode, string? reason) =>
        failureCode switch
        {
            ProcurementDeliveryRiskFailureCodes.NotFound => TypedResults.NotFound(new { code = failureCode, message = reason }),
            ProcurementDeliveryRiskFailureCodes.AssessmentConcurrencyConflict
                or ProcurementDeliveryRiskFailureCodes.InvalidTransition => TypedResults.Conflict(new { code = failureCode, message = reason }),
            _ => TypedResults.BadRequest(new { code = failureCode, message = reason }),
        };

    private sealed record TransitionProcurementDeliveryRiskFollowUpRequest(
        uint ExpectedVersion,
        string TargetFollowUpStatus,
        string? Note);
}

internal static class ProcurementDeliveryRiskFollowUpApiMapper
{
    internal static object MapDetail(ProcurementDeliveryRiskFollowUpDetailReadModel model)
    {
        var assessment = model.Assessment;
        return new
        {
            id = assessment.Id,
            facilityId = assessment.FacilityId,
            purchaseOrderId = assessment.PurchaseOrderId,
            purchaseOrderLineId = assessment.PurchaseOrderLineId,
            purchaseOrderNumber = assessment.PurchaseOrderNumber,
            externalSupplierId = assessment.ExternalSupplierId,
            externalSupplierCode = assessment.ExternalSupplierCode,
            supplierName = assessment.SupplierName,
            orderStatus = assessment.OrderStatus,
            lineNumber = assessment.LineNumber,
            externalProductId = assessment.ExternalProductId,
            externalProductCode = assessment.ExternalProductCode,
            lineDescription = assessment.LineDescription,
            commitmentDateUtc = assessment.CommitmentDateUtc,
            orderedQuantity = assessment.OrderedQuantity,
            receivedQuantity = assessment.ReceivedQuantity,
            openQuantity = assessment.OpenQuantity,
            unit = assessment.Unit,
            result = assessment.Result,
            ruleSetVersion = assessment.RuleSetVersion,
            inputFingerprint = assessment.InputFingerprint,
            sourceUpdatedAtUtc = assessment.SourceUpdatedAtUtc,
            sourceFreshness = assessment.SourceFreshness,
            drivers = assessment.Drivers.Select(d => new { code = d.Code, summary = d.Summary, reference = d.Reference, state = d.State }).ToList(),
            evidence = assessment.Evidence.Select(e => new
            {
                field = e.Field,
                value = e.Value,
                source = e.Source,
                recordedAtUtc = e.RecordedAtUtc,
                isUnknown = e.IsUnknown,
            }).ToList(),
            evaluatedAtUtc = assessment.EvaluatedAtUtc,
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
