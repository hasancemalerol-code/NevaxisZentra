using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.Integration.Common;
using NevaxisZentra.Persistence.Integration.FileImport;
using NevaxisZentra.Persistence.Integration.Mapping;
using NevaxisZentra.Persistence.Integration.Sync;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.Extensions.Options;
using Npgsql;

namespace NevaxisZentra.Persistence.Integration.Webhook;

public sealed record IntegrationWebhookIngressContext(
    string IngressKey,
    IReadOnlyDictionary<string, string> Headers,
    string RawBody,
    string? Signature,
    string? Timestamp,
    string? EventId);

public sealed record IntegrationWebhookIngestionResult(
    bool Success,
    int StatusCode,
    Guid? SyncRunId,
    string? ErrorCode,
    string? SafeMessage);

public interface IIntegrationWebhookIngestionService
{
    Task<IntegrationWebhookIngestionResult> IngestAsync(
        IntegrationWebhookIngressContext ingress,
        CancellationToken cancellationToken = default);
}

public sealed class IntegrationWebhookIngestionService(
    IOptions<PersistenceOptions> persistenceOptions,
    TenantContextAccessor tenantContextAccessor,
    ITenantMembershipResolver tenantMembershipResolver,
    TenantDatabaseSession tenantDatabaseSession,
    IIntegrationRecordIngestionOrchestrator ingestionOrchestrator) : IIntegrationWebhookIngestionService
{
    public async Task<IntegrationWebhookIngestionResult> IngestAsync(
        IntegrationWebhookIngressContext ingress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ingress.IngressKey))
        {
            return new IntegrationWebhookIngestionResult(false, 404, null, "integration.not-found", "Unknown webhook source.");
        }

        if (ingress.RawBody.Length > 1024 * 1024)
        {
            return new IntegrationWebhookIngestionResult(
                false,
                413,
                null,
                IntegrationFailureCodes.ValidationFailed,
                "Webhook payload exceeds size limit.");
        }

        var resolved = await ResolveIngressConnectorAsync(ingress.IngressKey, cancellationToken);
        if (resolved is null)
        {
            return new IntegrationWebhookIngestionResult(false, 404, null, "integration.not-found", "Unknown webhook source.");
        }

        var config = IntegrationWebhookConnectorConfiguration.TryParse(resolved.ConfigurationJson);
        if (config is null)
        {
            return new IntegrationWebhookIngestionResult(
                false,
                400,
                null,
                IntegrationFailureCodes.ValidationFailed,
                "Webhook connector configuration is invalid.");
        }

        if (!ValidateSignature(ingress, config, resolved.SecretReference))
        {
            return new IntegrationWebhookIngestionResult(
                false,
                401,
                null,
                IntegrationFailureCodes.ValidationFailed,
                "Webhook signature validation failed.");
        }

        if (!ValidateTimestamp(ingress.Timestamp, config.ReplayWindowSeconds))
        {
            return new IntegrationWebhookIngestionResult(
                false,
                401,
                null,
                IntegrationFailureCodes.ValidationFailed,
                "Webhook timestamp is outside the replay window.");
        }

        var table = ParsePayloadRecords(ingress.RawBody, config.PayloadRecordsJsonPath);
        if (table.Headers.Count == 0)
        {
            return new IntegrationWebhookIngestionResult(
                false,
                400,
                null,
                IntegrationFailureCodes.ValidationFailed,
                "Webhook payload did not contain records.");
        }

        if (!Guid.TryParse(resolved.OwnerSubjectId, out var ownerIdentitySubjectId))
        {
            return new IntegrationWebhookIngestionResult(
                false,
                503,
                null,
                IntegrationFailureCodes.ValidationFailed,
                "Webhook connector owner is not configured.");
        }

        var tenantMembership = await tenantMembershipResolver.ResolveActiveMembershipAsync(
            ownerIdentitySubjectId,
            resolved.TenantKey,
            cancellationToken);
        if (tenantMembership is null)
        {
            return new IntegrationWebhookIngestionResult(
                false,
                503,
                null,
                IntegrationFailureCodes.ValidationFailed,
                "Webhook connector owner membership is unavailable.");
        }

        var operationContext = new IntegrationOperationContext(
            resolved.TenantId,
            resolved.FacilityId,
            tenantMembership.MembershipId,
            ownerIdentitySubjectId,
            IntegrationPermissionKeys.Run,
            Guid.NewGuid());

        var idempotencyKey = string.IsNullOrWhiteSpace(ingress.EventId)
            ? null
            : $"webhook:{ingress.EventId}";

        tenantContextAccessor.Set(tenantMembership);

        return await tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var ingestion = await ingestionOrchestrator.ExecuteAsync(
                operationContext,
                new IntegrationRecordIngestionRequest(
                    resolved.ConnectorId,
                    resolved.ActiveMappingVersionId,
                    table,
                    IntegrationConnectorKinds.Webhook,
                    IntegrationContentFingerprint.ComputeTableFingerprint(table),
                    idempotencyKey,
                    Mutate: true,
                    resolved.CheckpointJson,
                    resolved.CheckpointJson),
                ct);

            if (!ingestion.Success)
            {
                var statusCode = ingestion.ErrorCode == "integration.import-idempotency-conflict" ? 409 : 400;
                return new IntegrationWebhookIngestionResult(
                    false,
                    statusCode,
                    ingestion.SyncRunId,
                    ingestion.ErrorCode,
                    ingestion.SafeMessage);
            }

            return new IntegrationWebhookIngestionResult(true, 200, ingestion.SyncRunId, null, null);
        }, cancellationToken);
    }

    private async Task<ResolvedWebhookConnector?> ResolveIngressConnectorAsync(
        string ingressKey,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(persistenceOptions.Value));
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT tenant_id, facility_id, connector_id, configuration_json, secret_reference, checkpoint_json, active_mapping_version_id, owner_subject_id, tenant_key
            FROM resolve_integration_ingress_connector(@ingress_key)
            """,
            connection);
        command.Parameters.AddWithValue("ingress_key", ingressKey);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ResolvedWebhookConnector(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.IsDBNull(3) ? "{}" : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetGuid(6),
            reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            reader.IsDBNull(8) ? string.Empty : reader.GetString(8));
    }

    private static bool ValidateSignature(
        IntegrationWebhookIngressContext ingress,
        IntegrationWebhookConnectorConfiguration config,
        string? secretReference)
    {
        if (string.IsNullOrWhiteSpace(secretReference))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(ingress.Signature))
        {
            return false;
        }

        var key = secretReference;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(ingress.RawBody));
        var expected = Convert.ToHexString(hash).ToLowerInvariant();
        var provided = ingress.Signature.Replace("sha256=", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided));
    }

    private static bool ValidateTimestamp(string? timestampHeader, int replayWindowSeconds)
    {
        if (string.IsNullOrWhiteSpace(timestampHeader))
        {
            return false;
        }

        if (!long.TryParse(timestampHeader, out var unixSeconds))
        {
            return false;
        }

        var eventTime = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        var delta = Math.Abs((DateTimeOffset.UtcNow - eventTime).TotalSeconds);
        return delta <= Math.Clamp(replayWindowSeconds, 30, 3600);
    }

    private static IntegrationFileTable ParsePayloadRecords(string rawBody, string recordsPath)
    {
        using var document = JsonDocument.Parse(rawBody);
        var current = document.RootElement;
        foreach (var segment in recordsPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return new IntegrationFileTable(IntegrationFileKinds.Json, [], []);
            }
        }

        var headers = new List<string>();
        var rows = new List<IReadOnlyDictionary<string, string>>();
        if (current.ValueKind == JsonValueKind.Array)
        {
            foreach (var record in current.EnumerateArray())
            {
                if (record.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in record.EnumerateObject())
                {
                    row[property.Name] = property.Value.ToString();
                    if (!headers.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        headers.Add(property.Name);
                    }
                }

                rows.Add(row);
            }
        }

        return new IntegrationFileTable(IntegrationFileKinds.Json, headers, rows);
    }

    private sealed record ResolvedWebhookConnector(
        Guid TenantId,
        Guid FacilityId,
        Guid ConnectorId,
        string ConfigurationJson,
        string? SecretReference,
        string? CheckpointJson,
        Guid? ActiveMappingVersionId,
        string OwnerSubjectId,
        string TenantKey);
}
