using System.Data.Common;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Authorization;
using Npgsql;
using NpgsqlTypes;

namespace NevaxisFactOS.Persistence.Outbox;

internal sealed class PostgreSqlOutboxWriter(
    IAuthorizationContext authorizationContext,
    CorrelationContext correlationContext) : IOutboxWriter
{
    public async Task<OutboxWriteResult> WriteAsync(
        DbConnection connection,
        DbTransaction transaction,
        OutboxWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(request);
        if (connection is not NpgsqlConnection npgsqlConnection
            || transaction is not NpgsqlTransaction npgsqlTransaction
            || npgsqlTransaction.Connection != npgsqlConnection)
        {
            return OutboxWriteResult.Failure("outbox.transaction.invalid");
        }

        if (!authorizationContext.IsAvailable
            || authorizationContext.Current is not { } actor
            || !correlationContext.IsAvailable)
        {
            return OutboxWriteResult.Failure("outbox.context.unavailable");
        }

        if (!OutboxPayloadValidator.TrySerialize(
                request,
                correlationContext.CorrelationId,
                out var payloadJson,
                out var headersJson,
                out var failureCode))
        {
            return OutboxWriteResult.Failure(failureCode!);
        }

        try
        {
            await using (var setTenant = new NpgsqlCommand(
                "SELECT set_config('NevaxisFactOS.tenant_id', @tenant_id, true)",
                npgsqlConnection,
                npgsqlTransaction))
            {
                setTenant.Parameters.AddWithValue("tenant_id", actor.TenantId.ToString());
                await setTenant.ExecuteScalarAsync(cancellationToken);
            }

            await using var command = new NpgsqlCommand(
                """
                SELECT message_id, was_duplicate
                FROM append_outbox_message(
                    @message_id, @tenant_id, @identity_id, @membership_id,
                    @message_type, @schema_version, @aggregate_type, @aggregate_id,
                    @correlation_id, @causation_id, @idempotency_key,
                    @payload_json, @headers_json, @occurred_at_utc, @available_at_utc)
                """,
                npgsqlConnection,
                npgsqlTransaction);
            command.Parameters.AddWithValue("message_id", Guid.NewGuid());
            command.Parameters.AddWithValue("tenant_id", actor.TenantId);
            command.Parameters.AddWithValue("identity_id", actor.IdentitySubjectId);
            command.Parameters.AddWithValue("membership_id", actor.MembershipId);
            command.Parameters.AddWithValue("message_type", request.MessageType);
            command.Parameters.AddWithValue("schema_version", request.SchemaVersion);
            command.Parameters.AddWithValue("aggregate_type", (object?)request.AggregateType ?? DBNull.Value);
            command.Parameters.AddWithValue("aggregate_id", (object?)request.AggregateId ?? DBNull.Value);
            command.Parameters.AddWithValue("correlation_id", correlationContext.CorrelationId);
            command.Parameters.AddWithValue("causation_id", (object?)request.CausationId ?? DBNull.Value);
            command.Parameters.AddWithValue("idempotency_key", request.IdempotencyKey);
            command.Parameters.Add("payload_json", NpgsqlDbType.Jsonb).Value = payloadJson;
            command.Parameters.Add("headers_json", NpgsqlDbType.Jsonb).Value = headersJson;
            command.Parameters.AddWithValue("occurred_at_utc", request.OccurredAtUtc);
            command.Parameters.AddWithValue("available_at_utc", request.AvailableAtUtc);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return OutboxWriteResult.Failure("outbox.persistence.unexpected_result");
            }

            return OutboxWriteResult.Success(reader.GetGuid(0), reader.GetBoolean(1));
        }
        catch (PostgresException)
        {
            return OutboxWriteResult.Failure("outbox.persistence.rejected");
        }
        catch (NpgsqlException)
        {
            return OutboxWriteResult.Failure("outbox.persistence.unavailable");
        }
    }
}
