using System.Text.Json;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.Outbox;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Erp;

internal static class ErpRepositorySupport
{
    internal static void AddAudit(
        CrevaFactOSDbContext dbContext,
        ErpOperationContext context,
        string action,
        string eventType,
        string resourceType,
        Guid resourceId,
        DateTimeOffset now,
        object metadata)
    {
        dbContext.AuditRecords.Add(new AuditRecord
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            IdentitySubjectId = context.IdentitySubjectId,
            TenantMembershipId = context.MembershipId,
            EventType = eventType,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId.ToString(),
            Outcome = AuditOutcome.Success,
            PermissionKey = context.PermissionKey,
            CorrelationId = context.CorrelationId ?? Guid.NewGuid(),
            Source = AuditSource.Api,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            MetadataJson = JsonSerializer.Serialize(metadata),
        });
    }

    internal static void AddOutbox(
        CrevaFactOSDbContext dbContext,
        ErpOperationContext context,
        string aggregateType,
        Guid aggregateId,
        uint version,
        string messageType,
        DateTimeOffset now,
        object payload)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            MessageType = messageType,
            SchemaVersion = OutboxMessageTypes.SchemaVersion,
            AggregateType = aggregateType,
            AggregateId = aggregateId.ToString(),
            CorrelationId = context.CorrelationId ?? Guid.NewGuid(),
            CausationId = null,
            IdempotencyKey = $"{aggregateId}:{version}:{messageType}",
            PayloadJson = JsonSerializer.Serialize(payload),
            HeadersJson = "{}",
            Status = OutboxMessageStatus.Pending,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            AvailableAtUtc = now,
            AttemptCount = 0,
        });
    }

    internal static async Task<ErpWriteResult> ExecuteWriteAsync(
        TenantDatabaseSession tenantDatabaseSession,
        ErpOperationContext context,
        Func<CancellationToken, Task<ErpWriteResult>> action,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ErpWriteResult.Fail(ErpFailureCodes.InvalidScope, "Tenant and facility are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(action, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ErpWriteResult.Fail(
                ErpFailureCodes.ConcurrencyConflict,
                "The record was modified by another user. Please reload and try again.");
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ErpWriteResult.Fail(ErpFailureCodes.SourceUnavailable, "ERP integration service is unavailable.");
        }
    }

    internal static async Task<ErpSyncResult> ExecuteSyncAsync(
        TenantDatabaseSession tenantDatabaseSession,
        ErpOperationContext context,
        Func<CancellationToken, Task<ErpSyncResult>> action,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ErpSyncResult.Fail(ErpFailureCodes.InvalidScope, "Tenant and facility are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(action, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ErpSyncResult.Fail(
                ErpFailureCodes.ConcurrencyConflict,
                "A concurrent ERP sync conflict occurred.");
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ErpSyncResult.Fail(ErpFailureCodes.SourceUnavailable, "ERP integration service is unavailable.");
        }
    }

    internal static ErpAdapterConnectionContext ToAdapterContext(ErpConnectionProfile profile) =>
        new(
            profile.Id,
            profile.Provider,
            profile.ConnectionMode,
            profile.ExternalCompanyId,
            profile.ExternalCompanyCode,
            profile.BaseEndpoint,
            profile.CredentialReference);

    internal static bool IsAllowedProvider(string provider) =>
        provider is ErpProviderKeys.Simulator
            or ErpProviderKeys.Logo
            or ErpProviderKeys.Mikro
            or ErpProviderKeys.Netsis;

    internal static bool IsAllowedConnectionMode(string mode) =>
        mode is ErpConnectionModes.Api
            or ErpConnectionModes.Database
            or ErpConnectionModes.File
            or ErpConnectionModes.Simulator;

    internal static bool IsAllowedConnectionStatus(string status) =>
        status is ErpConnectionStatuses.Draft
            or ErpConnectionStatuses.Configured
            or ErpConnectionStatuses.Healthy
            or ErpConnectionStatuses.Degraded
            or ErpConnectionStatuses.Unavailable
            or ErpConnectionStatuses.Disabled;

    internal static bool ContainsForbiddenSecretField(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var lower = value.ToLowerInvariant();
        return lower.Contains("password=")
            || lower.Contains("pwd=")
            || lower.Contains("apikey=")
            || lower.Contains("api_key=")
            || lower.Contains("token=")
            || lower.Contains("connectionstring");
    }
}
