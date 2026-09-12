using System.Data.Common;

namespace NevaxisFactOS.Persistence.Outbox;

internal sealed class UnavailableOutboxWriter : IOutboxWriter
{
    public Task<OutboxWriteResult> WriteAsync(DbConnection connection, DbTransaction transaction, OutboxWriteRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(OutboxWriteResult.Failure("outbox.persistence.disabled"));
}

internal sealed class UnavailableOutboxReader : IOutboxReader
{
    public Task<IReadOnlyList<OutboxMessageSummary>> ReadRecentAsync(int limit, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OutboxMessageSummary>>([]);
}

internal sealed class UnavailableOutboxDiagnosticService : IOutboxDiagnosticService
{
    public Task<OutboxWriteResult> AppendProbeAsync(OutboxProbeRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(OutboxWriteResult.Failure("outbox.persistence.disabled"));
}
