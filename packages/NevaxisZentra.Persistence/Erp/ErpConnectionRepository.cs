using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Erp;

public interface IErpConnectionRepository
{
    Task<IReadOnlyList<ErpConnectionSummaryReadModel>> ListAsync(
        ErpOperationContext context,
        CancellationToken cancellationToken = default);

    Task<ErpConnectionSummaryReadModel?> GetByIdAsync(
        ErpOperationContext context,
        Guid connectionId,
        CancellationToken cancellationToken = default);

    Task<ErpWriteResult> CreateAsync(
        ErpOperationContext context,
        CreateErpConnectionCommand command,
        CancellationToken cancellationToken = default);

    Task<ErpWriteResult> UpdateAsync(
        ErpOperationContext context,
        UpdateErpConnectionCommand command,
        CancellationToken cancellationToken = default);

    Task<ErpHealthCheckReadModel?> CheckHealthAsync(
        ErpOperationContext context,
        Guid connectionId,
        CancellationToken cancellationToken = default);

    Task<ErpWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        ErpOperationContext context,
        CancellationToken cancellationToken = default);
}

public sealed class ErpConnectionRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IErpAdapterResolver adapterResolver) : IErpConnectionRepository
{
    public Task<IReadOnlyList<ErpConnectionSummaryReadModel>> ListAsync(
        ErpOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var rows = await dbContext.ErpConnectionProfiles.AsNoTracking()
                .Where(c => c.TenantId == context.TenantId && c.FacilityId == context.FacilityId)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);

            return (IReadOnlyList<ErpConnectionSummaryReadModel>)rows.Select(MapSummary).ToList();
        }, cancellationToken);

    public Task<ErpConnectionSummaryReadModel?> GetByIdAsync(
        ErpOperationContext context,
        Guid connectionId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var row = await dbContext.ErpConnectionProfiles.AsNoTracking()
                .SingleOrDefaultAsync(
                    c => c.TenantId == context.TenantId
                        && c.FacilityId == context.FacilityId
                        && c.Id == connectionId,
                    ct);

            return row is null ? null : MapSummary(row);
        }, cancellationToken);

    public Task<ErpWriteResult> CreateAsync(
        ErpOperationContext context,
        CreateErpConnectionCommand command,
        CancellationToken cancellationToken = default) =>
        ErpRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                var validation = ValidateCommand(
                    command.Provider,
                    command.Name,
                    command.ConnectionMode,
                    command.Status,
                    command.CredentialReference,
                    command.BaseEndpoint);
                if (validation is not null)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.ValidationFailed, validation);
                }

                if (command.Provider != ErpProviderKeys.Simulator)
                {
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.ProviderUnsupported,
                        "ERP-1L only allows creating simulator connections. Logo/Mikro/Netsis adapters are placeholders.");
                }

                var now = DateTimeOffset.UtcNow;
                var profile = new ErpConnectionProfile
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    Provider = command.Provider.Trim().ToLowerInvariant(),
                    Name = command.Name.Trim(),
                    ExternalCompanyId = NormalizeOptional(command.ExternalCompanyId),
                    ExternalCompanyCode = NormalizeOptional(command.ExternalCompanyCode),
                    BaseEndpoint = NormalizeOptional(command.BaseEndpoint),
                    ConnectionMode = command.ConnectionMode.Trim().ToLowerInvariant(),
                    CredentialReference = NormalizeOptional(command.CredentialReference),
                    Status = command.Status.Trim().ToLowerInvariant(),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Version = 1,
                };

                dbContext.ErpConnectionProfiles.Add(profile);
                ErpRepositorySupport.AddAudit(
                    dbContext,
                    context,
                    ErpAuditActions.ConnectionCreated,
                    ErpAuditEventTypes.ConnectionCreated,
                    "erp.connection",
                    profile.Id,
                    now,
                    new { profile.Id, profile.Provider, profile.Name, profile.ExternalCompanyCode });
                ErpRepositorySupport.AddOutbox(
                    dbContext,
                    context,
                    "erp-connection",
                    profile.Id,
                    profile.Version,
                    ErpOutboxMessageTypes.ConnectionCreated,
                    now,
                    new { connectionId = profile.Id.ToString(), provider = profile.Provider, name = profile.Name });

                await dbContext.SaveChangesAsync(ct);
                return ErpWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

    public Task<ErpWriteResult> UpdateAsync(
        ErpOperationContext context,
        UpdateErpConnectionCommand command,
        CancellationToken cancellationToken = default) =>
        ErpRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                var validation = ValidateCommand(
                    null,
                    command.Name,
                    command.ConnectionMode,
                    command.Status,
                    command.CredentialReference,
                    command.BaseEndpoint);
                if (validation is not null)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.ValidationFailed, validation);
                }

                var profile = await dbContext.ErpConnectionProfiles
                    .SingleOrDefaultAsync(
                        c => c.TenantId == context.TenantId
                            && c.FacilityId == context.FacilityId
                            && c.Id == command.ConnectionId,
                        ct);

                if (profile is null)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.NotFound, "Connection profile not found.");
                }

                if (profile.Version != command.ExpectedVersion)
                {
                    return ErpWriteResult.Fail(
                        ErpFailureCodes.ConcurrencyConflict,
                        $"The record was modified (have {profile.Version}, expected {command.ExpectedVersion}).");
                }

                var now = DateTimeOffset.UtcNow;
                profile.Name = command.Name.Trim();
                profile.ExternalCompanyId = NormalizeOptional(command.ExternalCompanyId);
                profile.ExternalCompanyCode = NormalizeOptional(command.ExternalCompanyCode);
                profile.BaseEndpoint = NormalizeOptional(command.BaseEndpoint);
                profile.ConnectionMode = command.ConnectionMode.Trim().ToLowerInvariant();
                profile.CredentialReference = NormalizeOptional(command.CredentialReference);
                profile.Status = command.Status.Trim().ToLowerInvariant();
                profile.UpdatedAtUtc = now;
                profile.Version = command.ExpectedVersion + 1;

                ErpRepositorySupport.AddAudit(
                    dbContext,
                    context,
                    ErpAuditActions.ConnectionUpdated,
                    ErpAuditEventTypes.ConnectionUpdated,
                    "erp.connection",
                    profile.Id,
                    now,
                    new { profile.Id, profile.Name, profile.Status, version = profile.Version });

                await dbContext.SaveChangesAsync(ct);
                return ErpWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

    public async Task<ErpHealthCheckReadModel?> CheckHealthAsync(
        ErpOperationContext context,
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        ErpHealthCheckReadModel? result = null;

        var writeResult = await ErpRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                var profile = await dbContext.ErpConnectionProfiles
                    .SingleOrDefaultAsync(
                        c => c.TenantId == context.TenantId
                            && c.FacilityId == context.FacilityId
                            && c.Id == connectionId,
                        ct);

                if (profile is null)
                {
                    return ErpWriteResult.Fail(ErpFailureCodes.NotFound, "Connection profile not found.");
                }

                var adapter = adapterResolver.Resolve(profile.Provider);
                var health = await adapter.CheckHealthAsync(
                    ErpRepositorySupport.ToAdapterContext(profile),
                    ct);

                var now = health.CheckedAtUtc;
                profile.LastHealthCheckAtUtc = now;
                profile.LastHealthLatencyMs = health.LatencyMs;
                profile.Status = health.Status;
                if (health.IsHealthy)
                {
                    profile.LastSuccessfulConnectionAtUtc = now;
                    profile.LastErrorCode = null;
                    profile.LastErrorMessage = null;
                }
                else
                {
                    profile.LastFailedConnectionAtUtc = now;
                    profile.LastErrorCode = health.ErrorCode;
                    profile.LastErrorMessage = health.ErrorMessage;
                }

                profile.UpdatedAtUtc = now;
                profile.Version += 1;

                ErpRepositorySupport.AddAudit(
                    dbContext,
                    context,
                    ErpAuditActions.ConnectionHealthChecked,
                    ErpAuditEventTypes.ConnectionHealthChecked,
                    "erp.connection",
                    profile.Id,
                    now,
                    new { profile.Id, health.Status, health.IsHealthy, health.LatencyMs, health.ErrorCode });

                await dbContext.SaveChangesAsync(ct);

                result = new ErpHealthCheckReadModel(
                    profile.Id,
                    profile.Provider,
                    profile.Status,
                    health.IsHealthy,
                    now,
                    health.LatencyMs,
                    health.ErrorCode,
                    health.ErrorMessage,
                    profile.Version,
                    health.IntegrationMetadata);

                return ErpWriteResult.Success(profile.Id, profile.Version);
            },
            cancellationToken);

        if (!writeResult.Succeeded)
        {
            return null;
        }

        return result;
    }

    public Task<ErpWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        ErpOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var connections = await dbContext.ErpConnectionProfiles.AsNoTracking()
                .Where(c => c.TenantId == context.TenantId && c.FacilityId == context.FacilityId)
                .ToListAsync(ct);

            var customerCount = await dbContext.CanonicalErpCustomers.AsNoTracking()
                .CountAsync(c => c.TenantId == context.TenantId && c.FacilityId == context.FacilityId, ct);

            var productCount = await dbContext.CanonicalErpProducts.AsNoTracking()
                .CountAsync(c => c.TenantId == context.TenantId && c.FacilityId == context.FacilityId, ct);

            var orderCount = await dbContext.CanonicalErpOrders.AsNoTracking()
                .CountAsync(c => c.TenantId == context.TenantId && c.FacilityId == context.FacilityId, ct);

            var lastSync = await dbContext.ErpSyncRuns.AsNoTracking()
                .Where(s => s.TenantId == context.TenantId
                    && s.FacilityId == context.FacilityId
                    && s.CompletedAtUtc != null)
                .OrderByDescending(s => s.CompletedAtUtc)
                .Select(s => s.CompletedAtUtc)
                .FirstOrDefaultAsync(ct);

            var healthy = connections.Count(c => c.Status == ErpConnectionStatuses.Healthy);
            var problematic = connections.Count(c =>
                c.Status is ErpConnectionStatuses.Degraded
                    or ErpConnectionStatuses.Unavailable
                    or ErpConnectionStatuses.Draft);

            return new ErpWorkspaceSummaryReadModel(
                connections.Count,
                healthy,
                problematic,
                lastSync ?? connections.MaxBy(c => c.LastSuccessfulSyncAtUtc)?.LastSuccessfulSyncAtUtc,
                customerCount,
                productCount,
                orderCount);
        }, cancellationToken);

    private static string? ValidateCommand(
        string? provider,
        string name,
        string connectionMode,
        string status,
        string? credentialReference,
        string? baseEndpoint)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Connection name is required.";
        }

        if (provider is not null)
        {
            if (!ErpRepositorySupport.IsAllowedProvider(provider.Trim().ToLowerInvariant()))
            {
                return "Provider must be one of: logo, mikro, netsis, simulator.";
            }
        }

        if (!ErpRepositorySupport.IsAllowedConnectionMode(connectionMode.Trim().ToLowerInvariant()))
        {
            return "Connection mode must be one of: api, database, file, simulator.";
        }

        if (!ErpRepositorySupport.IsAllowedConnectionStatus(status.Trim().ToLowerInvariant()))
        {
            return "Invalid connection status.";
        }

        if (ErpRepositorySupport.ContainsForbiddenSecretField(credentialReference)
            || ErpRepositorySupport.ContainsForbiddenSecretField(baseEndpoint))
        {
            return "Plaintext secrets are not allowed. Use CredentialReference only.";
        }

        return null;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ErpConnectionSummaryReadModel MapSummary(ErpConnectionProfile c) =>
        new(
            c.Id,
            c.FacilityId,
            c.Provider,
            c.Name,
            c.ExternalCompanyId,
            c.ExternalCompanyCode,
            c.ConnectionMode,
            c.Status,
            c.LastSuccessfulConnectionAtUtc,
            c.LastFailedConnectionAtUtc,
            c.LastHealthCheckAtUtc,
            c.LastHealthLatencyMs,
            c.LastErrorCode,
            c.LastErrorMessage,
            c.LastSuccessfulSyncAtUtc,
            c.CreatedAtUtc,
            c.UpdatedAtUtc,
            c.Version,
            c.CredentialReference);
}

public sealed class UnavailableErpConnectionRepository : IErpConnectionRepository
{
    public Task<IReadOnlyList<ErpConnectionSummaryReadModel>> ListAsync(
        ErpOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ErpConnectionSummaryReadModel>>([]);

    public Task<ErpConnectionSummaryReadModel?> GetByIdAsync(
        ErpOperationContext context,
        Guid connectionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ErpConnectionSummaryReadModel?>(null);

    public Task<ErpWriteResult> CreateAsync(
        ErpOperationContext context,
        CreateErpConnectionCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpWriteResult.Fail(ErpFailureCodes.SourceUnavailable, "ERP service unavailable."));

    public Task<ErpWriteResult> UpdateAsync(
        ErpOperationContext context,
        UpdateErpConnectionCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ErpWriteResult.Fail(ErpFailureCodes.SourceUnavailable, "ERP service unavailable."));

    public Task<ErpHealthCheckReadModel?> CheckHealthAsync(
        ErpOperationContext context,
        Guid connectionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ErpHealthCheckReadModel?>(null);

    public Task<ErpWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        ErpOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ErpWorkspaceSummaryReadModel(0, 0, 0, null, 0, 0, 0));
}
