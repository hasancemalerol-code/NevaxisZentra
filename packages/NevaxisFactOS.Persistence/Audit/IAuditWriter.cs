namespace NevaxisFactOS.Persistence.Audit;

public interface IAuditWriter
{
    Task<AuditWriteResult> WriteAsync(
        AuditWriteRequest request,
        CancellationToken cancellationToken = default);
}
