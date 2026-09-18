namespace NevaxisZentra.Persistence.AiTasks;

public sealed class UnavailableAiTaskDecisionIntelligenceRepository : IAiTaskDecisionIntelligenceRepository
{
    private static readonly AiTaskDecisionWorkspaceReadModel UnavailableWorkspace = new(
        null,
        [],
        [],
        [],
        false,
        "AI değerlendirmesi şu anda kullanılamıyor.");

    public Task<AiTaskDecisionWorkspaceReadModel> GetWorkspaceAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(UnavailableWorkspace);

    public Task<AiTaskDecisionWriteResult> AnalyzeAsync(
        AiTaskOperationContext context,
        AnalyzeAiTaskDecisionCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AiTaskDecisionWriteResult.Fail(
            AiTaskFailureCodes.Unavailable,
            "AI değerlendirmesi şu anda kullanılamıyor."));

    public Task<AiTaskDecisionWriteResult> AcceptRecommendationAsync(
        AiTaskOperationContext context,
        AcceptAiTaskRecommendationCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AiTaskDecisionWriteResult.Fail(
            AiTaskFailureCodes.Unavailable,
            "AI değerlendirmesi şu anda kullanılamıyor."));

    public Task<AiTaskDecisionWriteResult> RejectRecommendationAsync(
        AiTaskOperationContext context,
        RejectAiTaskRecommendationCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AiTaskDecisionWriteResult.Fail(
            AiTaskFailureCodes.Unavailable,
            "AI değerlendirmesi şu anda kullanılamıyor."));

    public Task<AiTaskDecisionWriteResult> ConfirmRelationshipAsync(
        AiTaskOperationContext context,
        ConfirmAiTaskRelationshipCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AiTaskDecisionWriteResult.Fail(
            AiTaskFailureCodes.Unavailable,
            "AI değerlendirmesi şu anda kullanılamıyor."));

    public Task<AiTaskDecisionClosureReviewResult?> ReviewClosureAsync(
        AiTaskOperationContext context,
        Guid taskId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AiTaskDecisionClosureReviewResult?>(null);
}
