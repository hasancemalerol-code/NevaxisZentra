using NevaxisZentra.Persistence.Tenancy;
using Microsoft.Extensions.Options;
using Npgsql;

namespace NevaxisZentra.Persistence.AiDecisions;

internal sealed class PostgreSqlAiDecisionLedgerReader(
    IOptions<PersistenceOptions> options,
    ITenantContextAccessor tenantContext) : IAiDecisionLedgerReader
{
    public async Task<IReadOnlyList<AiDecisionSummary>> ReadRecentAsync(
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
            SELECT id, decision_type, capability_key, recommendation_code,
                   recommendation_summary, confidence_score, risk_level,
                   human_review_required, status, correlation_id, created_at_utc
            FROM ai_decision_records
            ORDER BY created_at_utc DESC, id DESC
            LIMIT @limit
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("limit", limit);
        var records = new List<AiDecisionSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new AiDecisionSummary(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                Enum.Parse<AiDecisionRiskLevel>(reader.GetString(6), ignoreCase: false),
                reader.GetBoolean(7),
                Enum.Parse<AiDecisionStatus>(reader.GetString(8), ignoreCase: false),
                reader.GetGuid(9),
                reader.GetFieldValue<DateTimeOffset>(10)));
        }

        await reader.CloseAsync();
        await transaction.CommitAsync(cancellationToken);
        return records;
    }
}
