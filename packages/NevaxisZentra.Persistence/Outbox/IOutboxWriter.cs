using System.Data.Common;

namespace NevaxisZentra.Persistence.Outbox;

public interface IOutboxWriter
{
    Task<OutboxWriteResult> WriteAsync(
        DbConnection connection,
        DbTransaction transaction,
        OutboxWriteRequest request,
        CancellationToken cancellationToken = default);
}
