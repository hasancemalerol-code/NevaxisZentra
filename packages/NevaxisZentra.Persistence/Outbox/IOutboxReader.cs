namespace NevaxisZentra.Persistence.Outbox;

public interface IOutboxReader
{
    Task<IReadOnlyList<OutboxMessageSummary>> ReadRecentAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
