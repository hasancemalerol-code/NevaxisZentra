namespace NevaxisFactOS.Persistence.Outbox;

public interface IOutboxDiagnosticService
{
    Task<OutboxWriteResult> AppendProbeAsync(
        OutboxProbeRequest request,
        CancellationToken cancellationToken = default);
}
