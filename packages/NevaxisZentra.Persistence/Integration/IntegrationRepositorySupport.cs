using System.Text.Json;
using NevaxisZentra.Persistence.Audit;

namespace NevaxisZentra.Persistence.Integration;

internal static class IntegrationRepositorySupport
{
    internal static void AddAudit(
        NevaxisZentraDbContext dbContext,
        IntegrationOperationContext context,
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
            IdentitySubjectId = context.IdentitySubjectId == Guid.Empty ? null : context.IdentitySubjectId,
            TenantMembershipId = context.MembershipId == Guid.Empty ? null : context.MembershipId,
            EventType = MapIntegrationEventType(eventType),
            Action = AuditActions.ValidateConfiguration,
            ResourceType = MapIntegrationResourceType(resourceType),
            ResourceId = resourceId.ToString(),
            Outcome = AuditOutcome.Success,
            PermissionKey = context.PermissionKey,
            CorrelationId = context.CorrelationId ?? Guid.NewGuid(),
            Source = AuditSource.Api,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            MetadataJson = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["integrationEventType"] = eventType,
                ["data"] = metadata,
            }),
        });
    }

    private static string MapIntegrationResourceType(string resourceType) =>
        resourceType switch
        {
            "integration_mapping_version" => "integration.mapping.version",
            "integration_connector" => "integration.connector",
            "integration_sync_run" => "integration.sync.run",
            _ => resourceType,
        };

    private static string MapIntegrationEventType(string eventType) =>
        eventType switch
        {
            var value when value.Contains("mapping.created", StringComparison.Ordinal) => "erp.connection.created",
            var value when value.Contains("mapping.validated", StringComparison.Ordinal) => "erp.mapping.confirmed",
            var value when value.Contains("mapping.activated", StringComparison.Ordinal) => "erp.mapping.confirmed",
            var value when value.Contains("file.dry-run", StringComparison.Ordinal) => "edge.event.ingested",
            var value when value.Contains("file.import", StringComparison.Ordinal) => "edge.event.ingested",
            var value when value.Contains("sync.completed", StringComparison.Ordinal) => "erp.sync.completed",
            _ => AuditEventTypes.InfrastructureConfigurationRejected,
        };
}
