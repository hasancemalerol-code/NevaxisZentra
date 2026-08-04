namespace CrevaFactOS.Persistence.AiDecisions;

public interface IAiDecisionLedgerWriter
{
    Task<AiDecisionWriteResult> WriteAsync(
        AiDecisionWriteRequest request,
        CancellationToken cancellationToken = default);
}
