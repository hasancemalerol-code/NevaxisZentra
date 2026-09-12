using Microsoft.Extensions.Options;
using Npgsql;

namespace NevaxisFactOS.Persistence.Outbox;

internal sealed class PostgreSqlOutboxDiagnosticService(
    IOptions<PersistenceOptions> options,
    IOutboxWriter writer) : IOutboxDiagnosticService
{
    public async Task<OutboxWriteResult> AppendProbeAsync(
        OutboxProbeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var now = DateTimeOffset.UtcNow;
        var writeRequest = new OutboxWriteRequest(
            OutboxMessageTypes.Probe,
            OutboxMessageTypes.SchemaVersion,
            request.IdempotencyKey,
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["probe.id"] = request.ProbeId,
                ["outcome.code"] = request.OutcomeCode,
                ["summary"] = request.Summary,
            },
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["source"] = "api.diagnostic",
            },
            now,
            request.AvailableAtUtc ?? now);

        await using var connection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(options.Value));
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var result = await writer.WriteAsync(connection, transaction, writeRequest, cancellationToken);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return result;
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
