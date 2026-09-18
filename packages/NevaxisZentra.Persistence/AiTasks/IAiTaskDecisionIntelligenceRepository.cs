namespace NevaxisZentra.Persistence.AiTasks;

public interface IAiTaskDecisionIntelligenceRepository
{
    Task<AiTaskDecisionWorkspaceReadModel> GetWorkspaceAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task<AiTaskDecisionWriteResult> AnalyzeAsync(
        AiTaskOperationContext context,
        AnalyzeAiTaskDecisionCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskDecisionWriteResult> AcceptRecommendationAsync(
        AiTaskOperationContext context,
        AcceptAiTaskRecommendationCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskDecisionWriteResult> RejectRecommendationAsync(
        AiTaskOperationContext context,
        RejectAiTaskRecommendationCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskDecisionWriteResult> ConfirmRelationshipAsync(
        AiTaskOperationContext context,
        ConfirmAiTaskRelationshipCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskDecisionClosureReviewResult?> ReviewClosureAsync(
        AiTaskOperationContext context,
        Guid taskId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
