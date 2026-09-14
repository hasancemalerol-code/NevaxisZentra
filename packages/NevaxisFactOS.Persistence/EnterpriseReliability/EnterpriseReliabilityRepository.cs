using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.Integration;

namespace NevaxisFactOS.Persistence.EnterpriseReliability;

public interface IEnterpriseReliabilityRepository
{
    Task<EnterpriseReliabilityPolicy> GetOrCreatePolicyAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SystemComponentHealthSnapshot>> GetLatestComponentHealthAsync(
        Guid tenantId,
        CancellationToken cancellationToken);
    Task RecordComponentHealthAsync(SystemComponentHealthSnapshot snapshot, CancellationToken cancellationToken);
    Task<IntegrationCircuitBreakerState> GetOrCreateCircuitBreakerAsync(
        Guid tenantId,
        Guid facilityId,
        Guid connectorId,
        CancellationToken cancellationToken);
    Task SaveCircuitBreakerAsync(IntegrationCircuitBreakerState state, CancellationToken cancellationToken);
    Task<IReadOnlyList<IntegrationConnector>> GetConnectorsAsync(
        Guid tenantId,
        Guid? facilityId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<EdgeAgentRegistration>> GetEdgeAgentsAsync(
        Guid tenantId,
        Guid? facilityId,
        CancellationToken cancellationToken);
    Task<EdgeAgentRegistration?> GetEdgeAgentByKeyAsync(
        Guid tenantId,
        string agentKey,
        CancellationToken cancellationToken);
    Task UpsertEdgeAgentAsync(EdgeAgentRegistration registration, CancellationToken cancellationToken);
    Task<IReadOnlyList<TechnicalOperationalAlert>> GetOpenAlertsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);
    Task UpsertAlertAsync(TechnicalOperationalAlert alert, CancellationToken cancellationToken);
    Task<RecoveryReconciliationRun> CreateReconciliationRunAsync(
        RecoveryReconciliationRun run,
        CancellationToken cancellationToken);
    Task CompleteReconciliationRunAsync(
        Guid runId,
        string status,
        string summaryJson,
        string? conflictJson,
        CancellationToken cancellationToken);
    Task<bool> TryAcquireJobLeaseAsync(
        Guid tenantId,
        string jobKey,
        string ownerInstanceId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);
    Task ReleaseJobLeaseAsync(Guid tenantId, string jobKey, CancellationToken cancellationToken);
    Task<IntegrationQuarantineReplayAudit?> FindReplayAuditAsync(
        Guid tenantId,
        string replayIdempotencyKey,
        CancellationToken cancellationToken);
    Task RecordReplayAuditAsync(IntegrationQuarantineReplayAudit audit, CancellationToken cancellationToken);
    Task<IntegrationIngestionQuarantine?> GetQuarantineAsync(
        Guid tenantId,
        Guid quarantineId,
        CancellationToken cancellationToken);
    Task<BackupVerificationRun> CreateBackupVerificationRunAsync(
        BackupVerificationRun run,
        CancellationToken cancellationToken);
    Task CompleteBackupVerificationRunAsync(
        Guid runId,
        string status,
        string resultJson,
        CancellationToken cancellationToken);
}

public sealed class EnterpriseReliabilityRepository : IEnterpriseReliabilityRepository
{
    private readonly NevaxisFactOSDbContext dbContext;

    public EnterpriseReliabilityRepository(NevaxisFactOSDbContext dbContext) => this.dbContext = dbContext;

    public async Task<EnterpriseReliabilityPolicy> GetOrCreatePolicyAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var policy = await dbContext.EnterpriseReliabilityPolicies
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);
        if (policy is not null)
        {
            return policy;
        }

        policy = new EnterpriseReliabilityPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OperatingMode = SystemOperatingModes.FullyOperational,
            PolicyEnvelopeJson = "{}",
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.EnterpriseReliabilityPolicies.Add(policy);
        await dbContext.SaveChangesAsync(cancellationToken);
        return policy;
    }

    public async Task<IReadOnlyList<SystemComponentHealthSnapshot>> GetLatestComponentHealthAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var snapshots = await dbContext.SystemComponentHealthSnapshots
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.ObservedAtUtc)
            .ToListAsync(cancellationToken);

        return snapshots
            .GroupBy(s => $"{s.ComponentKind}:{s.ComponentKey}")
            .Select(g => g.First())
            .ToList();
    }

    public async Task RecordComponentHealthAsync(
        SystemComponentHealthSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        dbContext.SystemComponentHealthSnapshots.Add(snapshot);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IntegrationCircuitBreakerState> GetOrCreateCircuitBreakerAsync(
        Guid tenantId,
        Guid facilityId,
        Guid connectorId,
        CancellationToken cancellationToken)
    {
        var state = await dbContext.IntegrationCircuitBreakerStates
            .FirstOrDefaultAsync(s => s.ConnectorId == connectorId, cancellationToken);
        if (state is not null)
        {
            return state;
        }

        state = IntegrationCircuitBreakerEngine.CreateDefault(
            tenantId,
            facilityId,
            connectorId,
            DateTimeOffset.UtcNow);
        dbContext.IntegrationCircuitBreakerStates.Add(state);
        await dbContext.SaveChangesAsync(cancellationToken);
        return state;
    }

    public async Task SaveCircuitBreakerAsync(
        IntegrationCircuitBreakerState state,
        CancellationToken cancellationToken)
    {
        state.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IntegrationConnector>> GetConnectorsAsync(
        Guid tenantId,
        Guid? facilityId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.IntegrationConnectors.Where(c => c.TenantId == tenantId);
        if (facilityId.HasValue)
        {
            query = query.Where(c => c.FacilityId == facilityId.Value);
        }

        return await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EdgeAgentRegistration>> GetEdgeAgentsAsync(
        Guid tenantId,
        Guid? facilityId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.EdgeAgentRegistrations.Where(a => a.TenantId == tenantId);
        if (facilityId.HasValue)
        {
            query = query.Where(a => a.FacilityId == facilityId.Value);
        }

        return await query.OrderBy(a => a.DisplayName).ToListAsync(cancellationToken);
    }

    public Task<EdgeAgentRegistration?> GetEdgeAgentByKeyAsync(
        Guid tenantId,
        string agentKey,
        CancellationToken cancellationToken) =>
        dbContext.EdgeAgentRegistrations
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.AgentKey == agentKey, cancellationToken);

    public async Task UpsertEdgeAgentAsync(
        EdgeAgentRegistration registration,
        CancellationToken cancellationToken)
    {
        var existing = await GetEdgeAgentByKeyAsync(
            registration.TenantId,
            registration.AgentKey,
            cancellationToken);
        if (existing is null)
        {
            dbContext.EdgeAgentRegistrations.Add(registration);
        }
        else
        {
            existing.DisplayName = registration.DisplayName;
            existing.AgentVersion = registration.AgentVersion;
            existing.ConnectivityStatus = registration.ConnectivityStatus;
            existing.BufferBacklogCount = registration.BufferBacklogCount;
            existing.BufferCapacity = registration.BufferCapacity;
            existing.LastHeartbeatAtUtc = registration.LastHeartbeatAtUtc;
            existing.LastSyncAtUtc = registration.LastSyncAtUtc;
            existing.MachineScopeJson = registration.MachineScopeJson;
            existing.UpdatedAtUtc = registration.UpdatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<TechnicalOperationalAlert>> GetOpenAlertsAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        dbContext.TechnicalOperationalAlerts
            .Where(a => a.TenantId == tenantId && a.Status == TechnicalAlertStatuses.Open)
            .OrderByDescending(a => a.OpenedAtUtc)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<TechnicalOperationalAlert>)t.Result, cancellationToken);

    public async Task UpsertAlertAsync(TechnicalOperationalAlert alert, CancellationToken cancellationToken)
    {
        var existing = await dbContext.TechnicalOperationalAlerts
            .FirstOrDefaultAsync(
                a => a.TenantId == alert.TenantId &&
                     a.DedupKey == alert.DedupKey &&
                     a.Status == TechnicalAlertStatuses.Open,
                cancellationToken);
        if (existing is null)
        {
            dbContext.TechnicalOperationalAlerts.Add(alert);
        }
        else
        {
            existing.DetailJson = alert.DetailJson;
            existing.UpdatedAtUtc = alert.UpdatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RecoveryReconciliationRun> CreateReconciliationRunAsync(
        RecoveryReconciliationRun run,
        CancellationToken cancellationToken)
    {
        dbContext.RecoveryReconciliationRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
        return run;
    }

    public async Task CompleteReconciliationRunAsync(
        Guid runId,
        string status,
        string summaryJson,
        string? conflictJson,
        CancellationToken cancellationToken)
    {
        var run = await dbContext.RecoveryReconciliationRuns
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
        if (run is null)
        {
            return;
        }

        run.Status = status;
        run.SummaryJson = summaryJson;
        run.ConflictJson = conflictJson;
        run.CompletedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryAcquireJobLeaseAsync(
        Guid tenantId,
        string jobKey,
        string ownerInstanceId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var existing = await dbContext.BackgroundJobLeases
            .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.JobKey == jobKey, cancellationToken);
        if (existing is not null && existing.ExpiresAtUtc > now && existing.Status == "active")
        {
            return existing.OwnerInstanceId == ownerInstanceId;
        }

        if (existing is null)
        {
            dbContext.BackgroundJobLeases.Add(new BackgroundJobLease
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                JobKey = jobKey,
                OwnerInstanceId = ownerInstanceId,
                AcquiredAtUtc = now,
                ExpiresAtUtc = now.Add(leaseDuration),
                Status = "active",
            });
        }
        else
        {
            existing.OwnerInstanceId = ownerInstanceId;
            existing.AcquiredAtUtc = now;
            existing.ExpiresAtUtc = now.Add(leaseDuration);
            existing.Status = "active";
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task ReleaseJobLeaseAsync(
        Guid tenantId,
        string jobKey,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.BackgroundJobLeases
            .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.JobKey == jobKey, cancellationToken);
        if (existing is null)
        {
            return;
        }

        existing.Status = "released";
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<IntegrationQuarantineReplayAudit?> FindReplayAuditAsync(
        Guid tenantId,
        string replayIdempotencyKey,
        CancellationToken cancellationToken) =>
        dbContext.IntegrationQuarantineReplayAudits
            .FirstOrDefaultAsync(
                a => a.TenantId == tenantId && a.ReplayIdempotencyKey == replayIdempotencyKey,
                cancellationToken);

    public async Task RecordReplayAuditAsync(
        IntegrationQuarantineReplayAudit audit,
        CancellationToken cancellationToken)
    {
        dbContext.IntegrationQuarantineReplayAudits.Add(audit);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<IntegrationIngestionQuarantine?> GetQuarantineAsync(
        Guid tenantId,
        Guid quarantineId,
        CancellationToken cancellationToken) =>
        dbContext.IntegrationIngestionQuarantine
            .FirstOrDefaultAsync(q => q.TenantId == tenantId && q.Id == quarantineId, cancellationToken);

    public async Task<BackupVerificationRun> CreateBackupVerificationRunAsync(
        BackupVerificationRun run,
        CancellationToken cancellationToken)
    {
        dbContext.BackupVerificationRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
        return run;
    }

    public async Task CompleteBackupVerificationRunAsync(
        Guid runId,
        string status,
        string resultJson,
        CancellationToken cancellationToken)
    {
        var run = await dbContext.BackupVerificationRuns
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
        if (run is null)
        {
            return;
        }

        run.Status = status;
        run.ResultJson = resultJson;
        run.CompletedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
