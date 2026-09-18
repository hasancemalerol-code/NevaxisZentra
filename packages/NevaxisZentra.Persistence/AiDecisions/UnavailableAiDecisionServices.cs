namespace NevaxisZentra.Persistence.AiDecisions;

internal sealed class UnavailableAiDecisionLedgerWriter : IAiDecisionLedgerWriter
{
    public Task<AiDecisionWriteResult> WriteAsync(
        AiDecisionWriteRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AiDecisionWriteResult.Failure("ai_decision.persistence.unavailable"));
}

internal sealed class UnavailableAiDecisionLedgerReader : IAiDecisionLedgerReader
{
    public Task<IReadOnlyList<AiDecisionSummary>> ReadRecentAsync(
        int limit,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AiDecisionSummary>>([]);
}
