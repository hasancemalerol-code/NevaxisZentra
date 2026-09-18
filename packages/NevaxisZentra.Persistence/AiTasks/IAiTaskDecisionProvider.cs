namespace NevaxisZentra.Persistence.AiTasks;

public interface IAiTaskDecisionProvider
{
    string ProviderName { get; }
    string ModelName { get; }
    bool IsDevelopmentProvider { get; }
    bool IsAvailable { get; }

    Task<AiTaskDecisionProviderResult> AnalyzeAsync(
        AiTaskDecisionProviderRequest request,
        CancellationToken cancellationToken = default);
}
