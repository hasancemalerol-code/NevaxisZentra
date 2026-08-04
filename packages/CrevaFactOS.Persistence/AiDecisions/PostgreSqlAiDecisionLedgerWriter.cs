using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Authorization;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace CrevaFactOS.Persistence.AiDecisions;

internal sealed class PostgreSqlAiDecisionLedgerWriter(
    IOptions<PersistenceOptions> options,
    IAuthorizationContext authorizationContext,
    CorrelationContext correlationContext) : IAiDecisionLedgerWriter
{
    public async Task<AiDecisionWriteResult> WriteAsync(
        AiDecisionWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!authorizationContext.IsAvailable
            || authorizationContext.Current is not { } actor
            || !correlationContext.IsAvailable)
        {
            return AiDecisionWriteResult.Failure("ai_decision.context.unavailable");
        }

        var createdAtUtc = DateTimeOffset.UtcNow;
        if (!AiDecisionPayloadValidator.TrySerialize(
                request,
                createdAtUtc,
                out var evidenceJson,
                out var alternativesJson,
                out var policyJson,
                out var validationFailure))
        {
            return AiDecisionWriteResult.Failure(validationFailure!);
        }

        var decisionId = Guid.NewGuid();
        var auditId = Guid.NewGuid();
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
                SELECT decision_record_id, audit_record_id
                FROM append_ai_decision_record(
                    @decision_id, @audit_id, @tenant_id, @identity_id, @membership_id,
                    @decision_type, @capability_key, @model_provider, @model_name,
                    @model_version, @configuration_fingerprint, @recommendation_code,
                    @recommendation_summary, @confidence_score, @risk_level,
                    @human_review_required, @status, @correlation_id, @causation_id,
                    @evidence_json, @alternatives_json, @policy_json, @created_at_utc,
                    @expires_at_utc, @supersedes_decision_id, @failure_code)
                """,
                connection,
                transaction);
            AddParameters(
                command,
                request,
                actor,
                decisionId,
                auditId,
                correlationContext.CorrelationId,
                createdAtUtc,
                evidenceJson,
                alternativesJson,
                policyJson);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)
                || reader.GetGuid(0) != decisionId
                || reader.GetGuid(1) != auditId)
            {
                return AiDecisionWriteResult.Failure("ai_decision.persistence.unexpected_result");
            }

            await reader.CloseAsync();
            await transaction.CommitAsync(cancellationToken);
            return AiDecisionWriteResult.Success(decisionId, auditId);
        }
        catch (PostgresException)
        {
            return AiDecisionWriteResult.Failure("ai_decision.persistence.rejected");
        }
        catch (NpgsqlException)
        {
            return AiDecisionWriteResult.Failure("ai_decision.persistence.unavailable");
        }
    }

    private static void AddParameters(
        NpgsqlCommand command,
        AiDecisionWriteRequest request,
        AuthorizationContext actor,
        Guid decisionId,
        Guid auditId,
        Guid correlationId,
        DateTimeOffset createdAtUtc,
        string evidenceJson,
        string? alternativesJson,
        string? policyJson)
    {
        command.Parameters.AddWithValue("decision_id", decisionId);
        command.Parameters.AddWithValue("audit_id", auditId);
        command.Parameters.AddWithValue("tenant_id", actor.TenantId);
        command.Parameters.AddWithValue("identity_id", actor.IdentitySubjectId);
        command.Parameters.AddWithValue("membership_id", actor.MembershipId);
        command.Parameters.AddWithValue("decision_type", request.DecisionType);
        command.Parameters.AddWithValue("capability_key", request.CapabilityKey);
        command.Parameters.AddWithValue("model_provider", request.ModelProvider);
        command.Parameters.AddWithValue("model_name", request.ModelName);
        command.Parameters.AddWithValue("model_version", (object?)request.ModelVersion ?? DBNull.Value);
        command.Parameters.AddWithValue("configuration_fingerprint", (object?)request.ConfigurationFingerprint ?? DBNull.Value);
        command.Parameters.AddWithValue("recommendation_code", request.RecommendationCode);
        command.Parameters.AddWithValue("recommendation_summary", request.RecommendationSummary);
        command.Parameters.AddWithValue("confidence_score", (object?)request.ConfidenceScore ?? DBNull.Value);
        command.Parameters.AddWithValue("risk_level", request.RiskLevel.ToString());
        command.Parameters.AddWithValue("human_review_required", request.HumanReviewRequired);
        command.Parameters.AddWithValue("status", request.Status.ToString());
        command.Parameters.AddWithValue("correlation_id", correlationId);
        command.Parameters.AddWithValue("causation_id", (object?)request.CausationId ?? DBNull.Value);
        command.Parameters.Add("evidence_json", NpgsqlDbType.Jsonb).Value = evidenceJson;
        command.Parameters.Add("alternatives_json", NpgsqlDbType.Jsonb).Value = (object?)alternativesJson ?? DBNull.Value;
        command.Parameters.Add("policy_json", NpgsqlDbType.Jsonb).Value = (object?)policyJson ?? DBNull.Value;
        command.Parameters.AddWithValue("created_at_utc", createdAtUtc);
        command.Parameters.AddWithValue("expires_at_utc", (object?)request.ExpiresAtUtc ?? DBNull.Value);
        command.Parameters.AddWithValue("supersedes_decision_id", (object?)request.SupersedesDecisionId ?? DBNull.Value);
        command.Parameters.AddWithValue("failure_code", (object?)request.FailureCode ?? DBNull.Value);
    }
}
