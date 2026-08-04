using CrevaFactOS.Persistence.Tenancy;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CrevaFactOS.Persistence.Outbox;

internal sealed class PostgreSqlOutboxReader(
    IOptions<PersistenceOptions> options,
    ITenantContextAccessor tenantContext) : IOutboxReader
{
    public async Task<IReadOnlyList<OutboxMessageSummary>> ReadRecentAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (!tenantContext.IsAvailable || limit is < 1 or > 50)
        {
            return [];
        }

        await using var connection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(options.Value));
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var setTenant = new NpgsqlCommand(
            "SELECT set_config('crevafactos.tenant_id', @tenant_id, true)", connection, transaction))
        {
            setTenant.Parameters.AddWithValue("tenant_id", tenantContext.Current!.TenantId.ToString());
            await setTenant.ExecuteScalarAsync(cancellationToken);
        }

        await using var command = new NpgsqlCommand(
            """
            SELECT id, message_type, schema_version, idempotency_key, status,
                   correlation_id, occurred_at_utc, available_at_utc, attempt_count
            FROM outbox_messages
            ORDER BY occurred_at_utc DESC, id DESC
            LIMIT @limit
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("limit", limit);
        var records = new List<OutboxMessageSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new OutboxMessageSummary(
                reader.GetGuid(0), reader.GetString(1), reader.GetInt32(2), reader.GetString(3),
                Enum.Parse<OutboxMessageStatus>(reader.GetString(4), ignoreCase: false),
                reader.GetGuid(5), reader.GetFieldValue<DateTimeOffset>(6),
                reader.GetFieldValue<DateTimeOffset>(7), reader.GetInt32(8)));
        }

        await reader.CloseAsync();
        await transaction.CommitAsync(cancellationToken);
        return records;
    }
}
