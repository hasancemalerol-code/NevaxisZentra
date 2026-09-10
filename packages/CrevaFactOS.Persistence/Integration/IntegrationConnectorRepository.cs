using CrevaFactOS.Persistence.Integration.Common;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Integration;

public interface IIntegrationConnectorRepository
{
    Task<IReadOnlyList<IntegrationConnectorSummaryReadModel>> ListAsync(
        IntegrationOperationContext context,
        CancellationToken cancellationToken = default);

    Task<IntegrationConnectorSummaryReadModel?> GetByIdAsync(
        IntegrationOperationContext context,
        Guid connectorId,
        CancellationToken cancellationToken = default);

    Task<IntegrationConnector?> GetEntityByIdAsync(
        IntegrationOperationContext context,
        Guid connectorId,
        CancellationToken cancellationToken = default);

    Task<IntegrationWriteResult> CreateAsync(
        IntegrationOperationContext context,
        CreateIntegrationConnectorCommand command,
        CancellationToken cancellationToken = default);

    Task<IntegrationWriteResult> UpdateAsync(
        IntegrationOperationContext context,
        UpdateIntegrationConnectorCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class IntegrationConnectorRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IIntegrationConnectorRepository
{
    public Task<IReadOnlyList<IntegrationConnectorSummaryReadModel>> ListAsync(
        IntegrationOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var rows = await dbContext.IntegrationConnectors.AsNoTracking()
                .Where(c => c.TenantId == context.TenantId && c.FacilityId == context.FacilityId)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);

            return (IReadOnlyList<IntegrationConnectorSummaryReadModel>)rows.Select(MapSummary).ToList();
        }, cancellationToken);

    public Task<IntegrationConnectorSummaryReadModel?> GetByIdAsync(
        IntegrationOperationContext context,
        Guid connectorId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var row = await dbContext.IntegrationConnectors.AsNoTracking()
                .SingleOrDefaultAsync(
                    c => c.TenantId == context.TenantId
                        && c.FacilityId == context.FacilityId
                        && c.Id == connectorId,
                    ct);

            return row is null ? null : MapSummary(row);
        }, cancellationToken);

    public Task<IntegrationConnector?> GetEntityByIdAsync(
        IntegrationOperationContext context,
        Guid connectorId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
            await dbContext.IntegrationConnectors
                .SingleOrDefaultAsync(
                    c => c.TenantId == context.TenantId
                        && c.FacilityId == context.FacilityId
                        && c.Id == connectorId,
                    ct), cancellationToken);

    public Task<IntegrationWriteResult> CreateAsync(
        IntegrationOperationContext context,
        CreateIntegrationConnectorCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var validation = ValidateCreate(command);
            if (validation is not null)
            {
                return IntegrationWriteResult.Fail(IntegrationFailureCodes.ValidationFailed, validation);
            }

            var now = DateTimeOffset.UtcNow;
            var connector = new IntegrationConnector
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                Name = command.Name.Trim(),
                Description = command.Description?.Trim(),
                ConnectorKind = command.ConnectorKind,
                SourceSystemType = command.SourceSystemType.Trim(),
                SourceSystemName = command.SourceSystemName.Trim(),
                Environment = command.Environment,
                Direction = IntegrationDirections.IngestOnly,
                Status = IntegrationConnectorStatuses.Configured,
                Enabled = false,
                AuthenticationMode = command.AuthenticationMode,
                SecretReference = NormalizeSecretReference(command.SecretReference),
                EndpointOrLocation = command.EndpointOrLocation?.Trim(),
                SynchronizationMode = command.SynchronizationMode,
                ScheduleExpression = command.ScheduleExpression?.Trim(),
                DomainBindingKind = command.DomainBindingKind,
                DomainBindingId = command.DomainBindingId,
                OwnerSubjectId = context.SubjectId,
                ConfigurationJson = command.ConfigurationJson,
                IngressKey = ResolveIngressKey(command.ConnectorKind, command.ConfigurationJson),
                HealthStatus = IntegrationHealthStatuses.Unknown,
                FreshnessState = IntegrationFreshnessStates.Unknown,
                QualityState = IntegrationQualityStates.Unknown,
                RetryState = IntegrationRetryStates.Idle,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Version = 1,
            };

            dbContext.IntegrationConnectors.Add(connector);
            await dbContext.SaveChangesAsync(ct);
            return IntegrationWriteResult.Ok(connector.Id);
        }, cancellationToken);

    public Task<IntegrationWriteResult> UpdateAsync(
        IntegrationOperationContext context,
        UpdateIntegrationConnectorCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var connector = await dbContext.IntegrationConnectors
                .SingleOrDefaultAsync(
                    c => c.TenantId == context.TenantId
                        && c.FacilityId == context.FacilityId
                        && c.Id == command.ConnectorId,
                    ct);

            if (connector is null)
            {
                return IntegrationWriteResult.Fail(IntegrationFailureCodes.NotFound, "Connector not found.");
            }

            if (connector.Version != command.ExpectedVersion)
            {
                return IntegrationWriteResult.Fail(
                    IntegrationFailureCodes.ConcurrencyConflict,
                    "Connector was updated by another session.");
            }

            connector.Name = command.Name.Trim();
            connector.Description = command.Description?.Trim();
            connector.Environment = command.Environment;
            connector.Enabled = command.Enabled;
            connector.AuthenticationMode = command.AuthenticationMode;
            connector.SecretReference = NormalizeSecretReference(command.SecretReference);
            connector.EndpointOrLocation = command.EndpointOrLocation?.Trim();
            connector.SynchronizationMode = command.SynchronizationMode;
            connector.ScheduleExpression = command.ScheduleExpression?.Trim();
            connector.ConfigurationJson = command.ConfigurationJson;
            connector.UpdatedAtUtc = DateTimeOffset.UtcNow;
            connector.Version += 1;

            await dbContext.SaveChangesAsync(ct);
            return IntegrationWriteResult.Ok(connector.Id);
        }, cancellationToken);

    private static IntegrationConnectorSummaryReadModel MapSummary(IntegrationConnector row) =>
        new(
            row.Id,
            row.FacilityId,
            row.Name,
            row.ConnectorKind,
            row.SourceSystemType,
            row.SourceSystemName,
            row.Environment,
            row.Status,
            row.Enabled,
            row.HealthStatus,
            row.FreshnessState,
            row.QualityState,
            row.LastSuccessfulSyncAtUtc,
            row.LastAttemptedSyncAtUtc,
            row.DomainBindingKind,
            row.DomainBindingId,
            row.Version);

    private static string? ValidateCreate(CreateIntegrationConnectorCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return "Connector name is required.";
        }

        if (string.IsNullOrWhiteSpace(command.ConnectorKind))
        {
            return "Connector kind is required.";
        }

        if (string.IsNullOrWhiteSpace(command.SourceSystemType)
            || string.IsNullOrWhiteSpace(command.SourceSystemName))
        {
            return "Source system metadata is required.";
        }

        return null;
    }

    private static string? NormalizeSecretReference(string? secretReference) =>
        string.IsNullOrWhiteSpace(secretReference) ? null : secretReference.Trim();

    private static string? ResolveIngressKey(string connectorKind, string configurationJson)
    {
        if (!string.Equals(connectorKind, IntegrationConnectorKinds.Webhook, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var config = IntegrationWebhookConnectorConfiguration.TryParse(configurationJson);
        return string.IsNullOrWhiteSpace(config?.IngressKey) ? null : config.IngressKey.Trim();
    }
}

public sealed class UnavailableIntegrationConnectorRepository : IIntegrationConnectorRepository
{
    public Task<IReadOnlyList<IntegrationConnectorSummaryReadModel>> ListAsync(
        IntegrationOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IntegrationConnectorSummaryReadModel>>([]);

    public Task<IntegrationConnectorSummaryReadModel?> GetByIdAsync(
        IntegrationOperationContext context,
        Guid connectorId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IntegrationConnectorSummaryReadModel?>(null);

    public Task<IntegrationConnector?> GetEntityByIdAsync(
        IntegrationOperationContext context,
        Guid connectorId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IntegrationConnector?>(null);

    public Task<IntegrationWriteResult> CreateAsync(
        IntegrationOperationContext context,
        CreateIntegrationConnectorCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(IntegrationWriteResult.Fail("integration.unavailable", "Persistence unavailable."));

    public Task<IntegrationWriteResult> UpdateAsync(
        IntegrationOperationContext context,
        UpdateIntegrationConnectorCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(IntegrationWriteResult.Fail("integration.unavailable", "Persistence unavailable."));
}
