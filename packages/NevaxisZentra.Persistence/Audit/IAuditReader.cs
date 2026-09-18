namespace NevaxisZentra.Persistence.Audit;

public interface IAuditReader
{
    Task<IReadOnlyList<AuditRecordSummary>> ReadRecentAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
