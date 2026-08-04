namespace CrevaFactOS.Persistence.Audit;

internal sealed class UnavailableAuditWriter : IAuditWriter
{
    public Task<AuditWriteResult> WriteAsync(
        AuditWriteRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AuditWriteResult.Failure("audit.persistence.unavailable"));
}

internal sealed class UnavailableAuditReader : IAuditReader
{
    public Task<IReadOnlyList<AuditRecordSummary>> ReadRecentAsync(
        int limit,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AuditRecordSummary>>([]);
}
