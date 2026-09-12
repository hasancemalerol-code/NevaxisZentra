namespace NevaxisFactOS.Persistence.AiDecisions;

public interface IAiDecisionLedgerReader
{
    Task<IReadOnlyList<AiDecisionSummary>> ReadRecentAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
