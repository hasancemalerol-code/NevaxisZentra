using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.AiTasks;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisZentra.Api.AiTasks;

public static class AiTaskDecisionIntelligenceEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAiTaskDecisionIntelligenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/ai-tasks").RequireAuthorization();

        group.MapGet("/{taskId:guid}/ai", GetWorkspaceAsync);
        group.MapPost("/{taskId:guid}/ai/analyze", AnalyzeAsync);
        group.MapPost("/{taskId:guid}/ai/recommendations/{recommendationId:guid}/accept", AcceptRecommendationAsync);
        group.MapPost("/{taskId:guid}/ai/recommendations/{recommendationId:guid}/reject", RejectRecommendationAsync);
        group.MapGet("/{taskId:guid}/related", GetRelatedAsync);
        group.MapPost("/{taskId:guid}/relationships", ConfirmRelationshipAsync);
        group.MapPost("/{taskId:guid}/ai/closure-review", ClosureReviewAsync);

        return endpoints;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskDecisionIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var workspace = await repository.GetWorkspaceAsync(context, taskId, cancellationToken);
        return TypedResults.Ok(MapWorkspace(workspace));
    }

    private static async Task<IResult> AnalyzeAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] AnalyzeAiTaskDecisionRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskDecisionIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.AiAnalyze, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.AnalyzeAsync(
            context,
            new AnalyzeAiTaskDecisionCommand(
                taskId,
                request.AnalysisType ?? AiTaskDecisionAnalysisTypes.Triage,
                request.IdempotencyKey ?? $"{taskId:N}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
                request.TriggerReason),
            cancellationToken);

        return result.Succeeded
            ? TypedResults.Ok(new
            {
                analysisId = result.EntityId,
                taskVersion = result.TaskVersion,
                idempotentReplay = result.IdempotentReplay,
                analysis = result.Analysis is null ? null : MapAnalysis(result.Analysis),
            })
            : AiTaskEndpointHelpers.MapFailure(result.FailureCode!, result.FailureReason!);
    }

    private static async Task<IResult> AcceptRecommendationAsync(
        Guid taskId,
        Guid recommendationId,
        [FromQuery] Guid? facilityId,
        [FromBody] AcceptAiTaskRecommendationRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskDecisionIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.AiFeedback, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.AcceptRecommendationAsync(
            context,
            new AcceptAiTaskRecommendationCommand(
                taskId,
                recommendationId,
                request.ExpectedVersion,
                request.Reason,
                request.EditedPayloadJson,
                async (permissionKey, ct) =>
                {
                    var check = await facilityAccessResolver.ResolveAsync(
                        new FacilityAccessRequest(facilityId!.Value, permissionKey), ct);
                    return check.Succeeded && check.Access is not null;
                }),
            cancellationToken);

        return result.Succeeded
            ? TypedResults.Ok(new { recommendationId = result.EntityId, taskVersion = result.TaskVersion })
            : AiTaskEndpointHelpers.MapFailure(result.FailureCode!, result.FailureReason!);
    }

    private static async Task<IResult> RejectRecommendationAsync(
        Guid taskId,
        Guid recommendationId,
        [FromQuery] Guid? facilityId,
        [FromBody] RejectAiTaskRecommendationRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskDecisionIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.AiFeedback, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.RejectRecommendationAsync(
            context,
            new RejectAiTaskRecommendationCommand(taskId, recommendationId, request.Reason),
            cancellationToken);

        return result.Succeeded
            ? TypedResults.Ok(new { recommendationId = result.EntityId, taskVersion = result.TaskVersion })
            : AiTaskEndpointHelpers.MapFailure(result.FailureCode!, result.FailureReason!);
    }

    private static async Task<IResult> GetRelatedAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskDecisionIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var workspace = await repository.GetWorkspaceAsync(context, taskId, cancellationToken);
        return TypedResults.Ok(new
        {
            suggested = workspace.SuggestedRelated,
            relationships = workspace.Relationships.Select(MapRelationship),
        });
    }

    private static async Task<IResult> ConfirmRelationshipAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] ConfirmAiTaskRelationshipRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskDecisionIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.AiFeedback, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.ConfirmRelationshipAsync(
            context,
            new ConfirmAiTaskRelationshipCommand(
                taskId,
                request.TargetTaskId,
                request.RelationshipType,
                request.Status,
                request.Explanation),
            cancellationToken);

        return result.Succeeded
            ? TypedResults.Ok(new { relationshipId = result.EntityId, taskVersion = result.TaskVersion })
            : AiTaskEndpointHelpers.MapFailure(result.FailureCode!, result.FailureReason!);
    }

    private static async Task<IResult> ClosureReviewAsync(
        Guid taskId,
        [FromQuery] Guid? facilityId,
        [FromBody] ClosureReviewRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IAiTaskDecisionIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await AiTaskEndpointHelpers.ResolvePermissionAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, AiTaskPermissionKeys.AiAnalyze, cancellationToken);
        if (context is null) return failResult!;

        var idempotencyKey = request.IdempotencyKey ?? $"{taskId:N}-closure-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        var result = await repository.AnalyzeAsync(
            context,
            new AnalyzeAiTaskDecisionCommand(
                taskId,
                AiTaskDecisionAnalysisTypes.ClosureReview,
                idempotencyKey,
                "closure-review"),
            cancellationToken);

        if (!result.Succeeded)
        {
            return AiTaskEndpointHelpers.MapFailure(result.FailureCode!, result.FailureReason!);
        }

        return TypedResults.Ok(new
        {
            available = true,
            analysisId = result.EntityId,
            review = result.Analysis?.ClosureReview,
            idempotentReplay = result.IdempotentReplay,
        });
    }

    private static object MapWorkspace(AiTaskDecisionWorkspaceReadModel workspace) => new
    {
        latestAnalysis = workspace.LatestAnalysis is null ? null : MapAnalysis(workspace.LatestAnalysis),
        history = workspace.History.Select(MapAnalysis),
        relationships = workspace.Relationships.Select(MapRelationship),
        suggestedRelated = workspace.SuggestedRelated,
        providerAvailable = workspace.ProviderAvailable,
        providerStatusMessage = workspace.ProviderStatusMessage,
    };

    private static object MapAnalysis(AiTaskDecisionAnalysisReadModel analysis) => new
    {
        analysis.Id,
        analysis.TaskId,
        analysis.AnalysisType,
        analysis.ProviderName,
        analysis.ModelName,
        analysis.PromptTemplateVersion,
        analysis.ConfidenceBand,
        triage = analysis.Triage,
        closureReview = analysis.ClosureReview,
        analysis.Superseded,
        analysis.SupersededByAnalysisId,
        analysis.CreatedAtUtc,
        analysis.CreatedBy,
        recommendations = analysis.Recommendations.Select(MapRecommendation),
    };

    private static object MapRecommendation(AiTaskDecisionRecommendationReadModel recommendation) => new
    {
        recommendation.Id,
        recommendation.AnalysisId,
        recommendation.ActionType,
        recommendation.Title,
        recommendation.Rationale,
        recommendation.ConfidenceBand,
        recommendation.UrgencyBand,
        recommendation.RelatedEntityType,
        recommendation.RelatedEntityId,
        recommendation.PayloadJson,
        recommendation.Status,
        recommendation.FeedbackDecision,
        recommendation.FeedbackReason,
        recommendation.FeedbackAtUtc,
        recommendation.AppliedResultJson,
        recommendation.CreatedAtUtc,
    };

    private static object MapRelationship(AiTaskDecisionRelationshipReadModel relationship) => new
    {
        relationship.Id,
        relationship.SourceTaskId,
        relationship.TargetTaskId,
        relationship.TargetTaskTitle,
        relationship.TargetTaskStatus,
        relationship.TargetTaskPriority,
        relationship.TargetSourceModule,
        relationship.RelationshipType,
        relationship.Status,
        relationship.ConfidenceBand,
        relationship.Explanation,
        relationship.ConfirmedAtUtc,
        relationship.CreatedAtUtc,
    };
}

public sealed record AnalyzeAiTaskDecisionRequest(
    string? AnalysisType,
    string? IdempotencyKey,
    string? TriggerReason);

public sealed record AcceptAiTaskRecommendationRequest(
    uint ExpectedVersion,
    string? Reason,
    string? EditedPayloadJson);

public sealed record RejectAiTaskRecommendationRequest(string Reason);

public sealed record ConfirmAiTaskRelationshipRequest(
    Guid TargetTaskId,
    string RelationshipType,
    string Status,
    string? Explanation);

public sealed record ClosureReviewRequest(string? IdempotencyKey);
