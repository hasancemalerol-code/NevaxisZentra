using NevaxisZentra.Persistence.Tenancy;
using Microsoft.Extensions.Options;
using Npgsql;

namespace NevaxisZentra.Persistence.Audit;

internal sealed class PostgreSqlAuditReader(
    IOptions<PersistenceOptions> options,
    ITenantContextAccessor tenantContext) : IAuditReader
{
    public async Task<IReadOnlyList<AuditRecordSummary>> ReadRecentAsync(
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
            "SELECT set_config('NevaxisZentra.tenant_id', @tenant_id, true)", connection, transaction))
        {
            setTenant.Parameters.AddWithValue("tenant_id", tenantContext.Current!.TenantId.ToString());
            await setTenant.ExecuteScalarAsync(cancellationToken);
        }

        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, identity_subject_id, tenant_membership_id,
                   event_type, action, resource_type, resource_id, outcome,
                   permission_key, correlation_id, causation_id, request_id,
                   source, occurred_at_utc, recorded_at_utc, failure_code
            FROM audit_records
            ORDER BY occurred_at_utc DESC, id DESC
            LIMIT @limit
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("limit", limit);
        var records = new List<AuditRecordSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new AuditRecordSummary(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.IsDBNull(2) ? null : reader.GetGuid(2),
                reader.IsDBNull(3) ? null : reader.GetGuid(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                Enum.Parse<AuditOutcome>(reader.GetString(8), ignoreCase: false),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.GetGuid(10),
                reader.IsDBNull(11) ? null : reader.GetGuid(11),
                reader.IsDBNull(12) ? null : reader.GetString(12),
                Enum.Parse<AuditSource>(reader.GetString(13), ignoreCase: false),
                reader.GetFieldValue<DateTimeOffset>(14),
                reader.GetFieldValue<DateTimeOffset>(15),
                reader.IsDBNull(16) ? null : reader.GetString(16)));
        }

        await reader.CloseAsync();
        await transaction.CommitAsync(cancellationToken);
        return records;
    }
}
