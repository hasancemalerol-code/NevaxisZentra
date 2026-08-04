using System.Text.RegularExpressions;
using CrevaFactOS.Persistence.Authorization;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace CrevaFactOS.Persistence.Audit;

internal sealed partial class PostgreSqlAuditWriter(
    IOptions<PersistenceOptions> options,
    IAuthorizationContext authorizationContext,
    CorrelationContext correlationContext) : IAuditWriter
{
    public async Task<AuditWriteResult> WriteAsync(
        AuditWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validationFailure = Validate(request);
        if (validationFailure is not null)
        {
            return AuditWriteResult.Failure(validationFailure);
        }

        if (!authorizationContext.IsAvailable
            || authorizationContext.Current is not { } actor
            || !correlationContext.IsAvailable)
        {
            return AuditWriteResult.Failure("audit.context.unavailable");
        }

        if (!AuditMetadataSanitizer.TrySanitize(
                request.Metadata,
                out var metadataJson,
                out var metadataFailure))
        {
            return AuditWriteResult.Failure(metadataFailure!);
        }

        var id = Guid.NewGuid();
        var occurredAt = request.OccurredAtUtc ?? DateTimeOffset.UtcNow;
        try
        {
            await using var connection = new NpgsqlConnection(
                PostgreSqlConnectionFactory.BuildConnectionString(options.Value));
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using (var setTenant = new NpgsqlCommand(
                "SELECT set_config('crevafactos.tenant_id', @tenant_id, true)", connection, transaction))
            {
                setTenant.Parameters.AddWithValue("tenant_id", actor.TenantId.ToString());
                await setTenant.ExecuteScalarAsync(cancellationToken);
            }

            await using var command = new NpgsqlCommand(
                """
                SELECT append_tenant_audit_record(
                    @id, @tenant_id, @identity_subject_id, @tenant_membership_id,
                    @event_type, @action, @resource_type, @resource_id, @outcome,
                    @permission_key, @correlation_id, @causation_id, @request_id,
                    @source, @occurred_at_utc, @metadata_json, @failure_code)
                """,
                connection,
                transaction);
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("tenant_id", actor.TenantId);
            command.Parameters.AddWithValue("identity_subject_id", actor.IdentitySubjectId);
            command.Parameters.AddWithValue("tenant_membership_id", actor.MembershipId);
            command.Parameters.AddWithValue("event_type", request.EventType);
            command.Parameters.AddWithValue("action", request.Action);
            command.Parameters.AddWithValue("resource_type", request.ResourceType);
            command.Parameters.AddWithValue("resource_id", (object?)request.ResourceId ?? DBNull.Value);
            command.Parameters.AddWithValue("outcome", request.Outcome.ToString());
            command.Parameters.AddWithValue("permission_key", (object?)request.PermissionKey ?? DBNull.Value);
            command.Parameters.AddWithValue("correlation_id", correlationContext.CorrelationId);
            command.Parameters.AddWithValue("causation_id", (object?)request.CausationId ?? DBNull.Value);
            command.Parameters.AddWithValue("request_id", (object?)correlationContext.RequestId ?? DBNull.Value);
            command.Parameters.AddWithValue("source", request.Source.ToString());
            command.Parameters.AddWithValue("occurred_at_utc", occurredAt);
            command.Parameters.Add("metadata_json", NpgsqlDbType.Jsonb).Value = metadataJson;
            command.Parameters.AddWithValue("failure_code", (object?)request.FailureCode ?? DBNull.Value);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result is Guid recordedId && recordedId == id
                ? AuditWriteResult.Success(id)
                : AuditWriteResult.Failure("audit.persistence.unexpected_result");
        }
        catch (PostgresException)
        {
            return AuditWriteResult.Failure("audit.persistence.rejected");
        }
        catch (NpgsqlException)
        {
            return AuditWriteResult.Failure("audit.persistence.unavailable");
        }
    }

    private static string? Validate(AuditWriteRequest request)
    {
        if (!AuditEventTypes.All.Contains(request.EventType))
        {
            return "audit.event_type.invalid";
        }

        if (!AuditActions.All.Contains(request.Action))
        {
            return "audit.action.invalid";
        }

        if (string.IsNullOrWhiteSpace(request.ResourceType)
            || request.ResourceType.Length > 100
            || !StableKeyPattern().IsMatch(request.ResourceType))
        {
            return "audit.resource_type.invalid";
        }

        if (request.ResourceId?.Length > 200
            || request.PermissionKey?.Length > 150
            || request.RequestedFailureCodeIsInvalid())
        {
            return "audit.field.invalid";
        }

        if (request.PermissionKey is not null && !StableKeyPattern().IsMatch(request.PermissionKey))
        {
            return "audit.permission_key.invalid";
        }

        if (request.Source == AuditSource.System)
        {
            return "audit.system_boundary.required";
        }

        if (request.OccurredAtUtc is { Offset: var offset } && offset != TimeSpan.Zero)
        {
            return "audit.timestamp.not_utc";
        }

        return null;
    }

    [GeneratedRegex("^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableKeyPattern();
}

internal static class AuditWriteRequestValidationExtensions
{
    public static bool RequestedFailureCodeIsInvalid(this AuditWriteRequest request) =>
        request.FailureCode is { Length: > 100 }
        || request.FailureCode is not null
            && request.FailureCode.Any(character =>
                !(char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-'));
}
