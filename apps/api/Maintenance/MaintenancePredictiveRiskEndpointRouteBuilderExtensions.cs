using NevaxisZentra.Api.AiTasks;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.AiTasks;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Maintenance;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.Maintenance;

internal static class MaintenancePredictiveRiskEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapMaintenancePredictiveRiskEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/predictive-risk/workspace", GetWorkspaceAsync);
        group.MapGet("/predictive-risk/{assessmentId:guid}", GetAssessmentAsync);
        group.MapPost("/predictive-risk/{assessmentId:guid}/task-draft", CreateTaskDraftAsync);
        group.MapPost("/predictive-risk/{assessmentId:guid}/follow-up", TransitionFollowUpAsync);

        return group;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenancePredictiveRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var canManage = await HasManageAccessAsync(facilityId!.Value, facilityAccessResolver, cancellationToken);
        var canCreateTask = await AiTaskEndpointHelpers.HasCreateAccessAsync(
            facilityId.Value, facilityAccessResolver, cancellationToken);

        var overview = await repository.GetOverviewAsync(context, cancellationToken);
        var rows = await repository.ListAssetRowsAsync(context, cancellationToken);
        var workspace = await repository.GetWorkspaceAsync(context, canManage, canCreateTask, cancellationToken);

        return overview is null
            ? TypedResults.Problem(title: "Predictive maintenance risk is unavailable.", statusCode: 503)
            : TypedResults.Ok(new
            {
                ruleSetVersion = workspace.RuleSetVersion,
                automationDisclaimer = workspace.AutomationDisclaimer,
                kpis = new
                {
                    activeRiskCount = workspace.ActiveRiskCount,
                    taskLinkedCount = workspace.TaskLinkedCount,
                    humanReviewedCount = workspace.HumanReviewedCount,
                },
                capabilities = new
                {
                    canRead = workspace.CanRead,
                    canManage = workspace.CanManage,
                    canCreateTask = workspace.CanCreateTask,
                },
                overview = MaintenanceApiMapper.MapPredictiveRiskOverview(overview),
                assets = rows.Select(MaintenanceApiMapper.MapPredictiveRiskAssetRow).ToList(),
            });
    }

    private static async Task<IResult> GetAssessmentAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenancePredictiveRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var assessment = await repository.GetAssessmentByIdAsync(context, assessmentId, cancellationToken);
        return assessment is null
            ? TypedResults.NotFound(new { code = MaintenanceFailureCodes.NotFound, message = "Predictive risk assessment not found." })
            : TypedResults.Ok(MaintenanceApiMapper.MapPredictiveRiskAssessment(assessment));
    }

    private static async Task<IResult> CreateTaskDraftAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenancePredictiveRiskRepository repository,
        IAiTaskRepository aiTaskRepository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (readContext, readFail) = await MaintenanceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (readContext is null) return readFail!;

        var (createContext, createFail) = await AiTaskEndpointHelpers.ResolveCreateAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (createContext is null) return createFail!;

        var assessment = await repository.GetAssessmentByIdAsync(readContext, assessmentId, cancellationToken);
        if (assessment is null)
        {
            return MapFailure(MaintenanceFailureCodes.NotFound, "Kestirimci bakım risk değerlendirmesi bulunamadı.");
        }

        if (assessment.LinkedAiTaskId.HasValue)
        {
            return TypedResults.Ok(new
            {
                taskId = assessment.LinkedAiTaskId.Value,
                assessmentId = assessment.Id,
                idempotentReplay = true,
                assessment = MaintenanceApiMapper.MapPredictiveRiskAssessment(assessment),
            });
        }

        if (!MaintenancePredictiveRiskPresentationBuilder.CanCreateTaskDraft(assessment))
        {
            return MapFailure(
                MaintenanceFailureCodes.ValidationFailed,
                "Bu risk seviyesi için görev taslağı oluşturulamaz.");
        }

        var taskPurpose = MaintenancePredictiveRiskPresentationBuilder.ResolveTaskPurpose(assessment);
        var idempotencyKey = MaintenancePredictiveRiskPresentationBuilder.BuildTaskDraftIdempotencyKey(
            assessment.Id,
            taskPurpose);
        var priority = MaintenancePredictiveRiskPresentationBuilder.ResolveTaskPriority(assessment);
        var evidenceSummary = MaintenancePredictiveRiskPresentationBuilder.BuildEvidenceSummary(assessment);
        var deepLink = MaintenancePredictiveRiskPresentationBuilder.BuildMaintenanceDeepLink(assessment.AssetReference);

        var taskResult = await aiTaskRepository.CreateSourceLinkedTaskAsync(
            createContext,
            new CreateSourceLinkedAiTaskCommand(
                Title: MaintenancePredictiveRiskPresentationBuilder.BuildTaskTitle(assessment),
                Summary: MaintenancePredictiveRiskPresentationBuilder.BuildTaskSummary(assessment),
                Category: "maintenance",
                Priority: priority,
                RiskLevel: priority,
                DetectedIssue: assessment.Explanation,
                Reasoning: $"Kestirimci bakım risk değerlendirmesi {assessment.Id:D} için insan incelemeli görev taslağı.",
                Recommendation: new AiTaskRecommendationModel(
                    assessment.Recommendation?.Category ?? "review",
                    assessment.Recommendation?.Explanation
                        ?? "Yetkili bakım ekibi risk kanıtını doğrulamalıdır. Otomatik bakım emri oluşturulmaz.",
                    taskPurpose == MaintenancePredictiveRiskConstants.TaskPurposeDataReviewDraft
                        ? "data-review"
                        : "review",
                    RequiresApproval: true,
                    Reversible: true,
                    TargetSystem: "crevafactos"),
                Confidence: new AiTaskConfidenceModel(
                    assessment.RiskBand == MaintenancePredictiveRiskBands.InsufficientData ? 0.45m : 0.72m,
                    $"MAINT-PRED risk bandı: {assessment.RiskBand}. Veri yeterliliği: {assessment.DataSufficiency}.",
                    null),
                Impact: new AiTaskImpactModel(
                    null, null, null, null, null, null, null,
                    evidenceSummary),
                Sources:
                [
                    new AiTaskSourceReferenceModel(
                        MaintenancePredictiveRiskConstants.SourceModule,
                        assessment.Id.ToString(),
                        $"Risk değerlendirmesi {assessment.AssetReference}",
                        assessment.AssessedAtUtc,
                        evidenceSummary),
                    new AiTaskSourceReferenceModel(
                        MaintenancePredictiveRiskConstants.SourceModule,
                        assessment.MachineId.ToString(),
                        $"Makine {assessment.MachineCode ?? assessment.AssetReference}",
                        assessment.AssessedAtUtc,
                        deepLink),
                ],
                SourceModule: MaintenancePredictiveRiskConstants.SourceModule,
                SourceEntityId: assessment.Id.ToString(),
                IdempotencyKey: idempotencyKey,
                SiteId: null,
                ProductionLineId: null,
                MachineId: assessment.MachineId.ToString(),
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
        var linkResult = await repository.LinkTaskAsync(
            readContext,
            new LinkMaintenancePredictiveRiskTaskCommand(assessmentId, taskId),
            cancellationToken);

        if (!linkResult.Succeeded)
        {
            return MapWriteResult(linkResult);
        }

        return TypedResults.Ok(new
        {
            taskId,
            assessmentId,
            idempotentReplay = taskResult.IdempotentReplay,
            assessment = linkResult.Assessment is null
                ? MaintenanceApiMapper.MapPredictiveRiskAssessment(assessment)
                : MaintenanceApiMapper.MapPredictiveRiskAssessment(linkResult.Assessment),
        });
    }

    private static async Task<IResult> TransitionFollowUpAsync(
        Guid assessmentId,
        [FromBody] TransitionFollowUpRequest body,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IMaintenancePredictiveRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await MaintenanceEndpointHelpers.ResolveWriteAccessAsync(
            facilityId,
            MaintenancePermissionKeys.Manage,
            facilityAccessResolver,
            correlationContext,
            cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.TransitionFollowUpAsync(
            context,
            new TransitionMaintenancePredictiveRiskFollowUpCommand(
                assessmentId,
                body.TargetFollowUpStatus,
                body.Note),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<bool> HasManageAccessAsync(
        Guid facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId, MaintenancePermissionKeys.Manage),
            cancellationToken);
        return accessResult.Succeeded && accessResult.Access is not null;
    }

    private static IResult MapWriteResult(MaintenancePredictiveRiskWriteResult result)
    {
        if (result.Succeeded)
        {
            return TypedResults.Ok(new
            {
                entityId = result.EntityId,
                idempotentReplay = result.IdempotentReplay,
                assessment = result.Assessment is null
                    ? null
                    : MaintenanceApiMapper.MapPredictiveRiskAssessment(result.Assessment),
            });
        }

        return MapFailure(result.FailureCode!, result.FailureReason!);
    }

    private static IResult MapFailure(string code, string message) =>
        code switch
        {
            MaintenanceFailureCodes.NotFound => TypedResults.NotFound(new { code, message }),
            MaintenanceFailureCodes.ValidationFailed or MaintenanceFailureCodes.InvalidTransition =>
                TypedResults.Problem(title: message, statusCode: StatusCodes.Status400BadRequest, extensions: FailureExt(code)),
            MaintenanceFailureCodes.ConcurrencyConflict =>
                TypedResults.Problem(title: message, statusCode: StatusCodes.Status409Conflict, extensions: FailureExt(code)),
            _ => TypedResults.Problem(title: message, statusCode: StatusCodes.Status400BadRequest, extensions: FailureExt(code)),
        };

    private static Dictionary<string, object?> FailureExt(string code) =>
        new() { ["code"] = code };

    private sealed record TransitionFollowUpRequest(string TargetFollowUpStatus, string? Note);
}
