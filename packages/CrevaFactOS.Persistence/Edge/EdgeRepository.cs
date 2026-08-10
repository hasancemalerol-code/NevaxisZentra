using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Edge;

public interface IEdgeQueryRepository
{
    Task<IReadOnlyList<EdgeGatewayReadModel>> ListGatewaysAsync(
        EdgeOperationContext context,
        CancellationToken cancellationToken);

    Task<EdgeGatewayReadModel?> GetGatewayAsync(
        EdgeOperationContext context,
        Guid gatewayId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MachineRegistryReadModel>> ListMachinesAsync(
        EdgeOperationContext context,
        CancellationToken cancellationToken);

    Task<MachineRegistryReadModel?> GetMachineAsync(
        EdgeOperationContext context,
        Guid machineId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MachineEventReadModel>> ListMachineEventsAsync(
        EdgeOperationContext context,
        Guid machineId,
        int limit,
        CancellationToken cancellationToken);

    Task<MachineRuntimeReadModel?> GetMachineRuntimeAsync(
        EdgeOperationContext context,
        Guid machineId,
        CancellationToken cancellationToken);

    // EDGE-2
    Task<IReadOnlyList<LatestTelemetryReadModel>> ListLatestTelemetryAsync(
        EdgeOperationContext context,
        Guid machineId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TelemetrySampleReadModel>> ListTelemetryHistoryAsync(
        EdgeOperationContext context,
        Guid machineId,
        string? metricKey,
        DateTimeOffset? from,
        DateTimeOffset? toUtc,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ObservedCycleReadModel>> ListCycleHistoryAsync(
        EdgeOperationContext context,
        Guid machineId,
        int limit,
        CancellationToken cancellationToken);

    Task<CycleProjectionReadModel?> GetCycleProjectionAsync(
        EdgeOperationContext context,
        Guid machineId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StateTransitionReadModel>> ListStateTransitionsAsync(
        EdgeOperationContext context,
        Guid machineId,
        int limit,
        CancellationToken cancellationToken);

    Task<MachineSignalProjectionReadModel?> GetSignalProjectionAsync(
        EdgeOperationContext context,
        Guid machineId,
        CancellationToken cancellationToken);
}

public sealed class EdgeQueryRepository(
    CrevaFactOSDbContext dbContext,
    Tenancy.TenantDatabaseSession tenantDatabaseSession) : IEdgeQueryRepository
{
    public Task<IReadOnlyList<EdgeGatewayReadModel>> ListGatewaysAsync(
        EdgeOperationContext context,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            return (IReadOnlyList<EdgeGatewayReadModel>)await dbContext.EdgeGateways
                .Where(g => g.FacilityId == context.FacilityId)
                .OrderBy(g => g.GatewayCode)
                .Select(g => new EdgeGatewayReadModel(
                    g.Id,
                    g.FacilityId,
                    g.GatewayCode,
                    g.Name,
                    g.Description,
                    g.VendorKey,
                    g.Status,
                    g.LastSeenAtUtc,
                    g.LastHeartbeatAtUtc,
                    g.LastHealthMessage,
                    g.CreatedAtUtc,
                    g.UpdatedAtUtc,
                    g.Assignments.Count(a => a.Status == MachineAssignmentStatuses.Active)))
                .ToListAsync(ct);
        }, cancellationToken);

    public Task<EdgeGatewayReadModel?> GetGatewayAsync(
        EdgeOperationContext context,
        Guid gatewayId,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct =>
            dbContext.EdgeGateways
                .Where(g => g.FacilityId == context.FacilityId && g.Id == gatewayId)
                .Select(g => new EdgeGatewayReadModel(
                    g.Id,
                    g.FacilityId,
                    g.GatewayCode,
                    g.Name,
                    g.Description,
                    g.VendorKey,
                    g.Status,
                    g.LastSeenAtUtc,
                    g.LastHeartbeatAtUtc,
                    g.LastHealthMessage,
                    g.CreatedAtUtc,
                    g.UpdatedAtUtc,
                    g.Assignments.Count(a => a.Status == MachineAssignmentStatuses.Active)))
                .FirstOrDefaultAsync(ct),
        cancellationToken);

    public Task<IReadOnlyList<MachineRegistryReadModel>> ListMachinesAsync(
        EdgeOperationContext context,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            return (IReadOnlyList<MachineRegistryReadModel>)await dbContext.MachineRegistries
                .Where(m => m.FacilityId == context.FacilityId)
                .OrderBy(m => m.MachineCode)
                .Select(m => new MachineRegistryReadModel(
                    m.Id,
                    m.FacilityId,
                    m.MachineCode,
                    m.Name,
                    m.Description,
                    m.AssetClass,
                    m.AssetReference,
                    m.Status,
                    m.IsReadyForProduction,
                    m.LastSeenAtUtc,
                    m.LastEventAtUtc,
                    m.LastKnownStatus,
                    m.LastKnownStatusAtUtc,
                    m.CreatedAtUtc,
                    m.UpdatedAtUtc,
                    m.Assignments
                        .Where(a => a.Status == MachineAssignmentStatuses.Active)
                        .Select(a => (Guid?)a.GatewayId)
                        .FirstOrDefault(),
                    m.Assignments
                        .Where(a => a.Status == MachineAssignmentStatuses.Active)
                        .Select(a => a.Gateway!.GatewayCode)
                        .FirstOrDefault()))
                .ToListAsync(ct);
        }, cancellationToken);

    public Task<MachineRegistryReadModel?> GetMachineAsync(
        EdgeOperationContext context,
        Guid machineId,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct =>
            dbContext.MachineRegistries
                .Where(m => m.FacilityId == context.FacilityId && m.Id == machineId)
                .Select(m => new MachineRegistryReadModel(
                    m.Id,
                    m.FacilityId,
                    m.MachineCode,
                    m.Name,
                    m.Description,
                    m.AssetClass,
                    m.AssetReference,
                    m.Status,
                    m.IsReadyForProduction,
                    m.LastSeenAtUtc,
                    m.LastEventAtUtc,
                    m.LastKnownStatus,
                    m.LastKnownStatusAtUtc,
                    m.CreatedAtUtc,
                    m.UpdatedAtUtc,
                    m.Assignments
                        .Where(a => a.Status == MachineAssignmentStatuses.Active)
                        .Select(a => (Guid?)a.GatewayId)
                        .FirstOrDefault(),
                    m.Assignments
                        .Where(a => a.Status == MachineAssignmentStatuses.Active)
                        .Select(a => a.Gateway!.GatewayCode)
                        .FirstOrDefault()))
                .FirstOrDefaultAsync(ct),
        cancellationToken);

    public Task<IReadOnlyList<MachineEventReadModel>> ListMachineEventsAsync(
        EdgeOperationContext context,
        Guid machineId,
        int limit,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var effectiveLimit = Math.Clamp(limit, 1, 500);
            return (IReadOnlyList<MachineEventReadModel>)await dbContext.MachineEvents
                .Where(e => e.FacilityId == context.FacilityId && e.MachineId == machineId)
                .OrderByDescending(e => e.SourceSequence)
                .Take(effectiveLimit)
                .Select(e => new MachineEventReadModel(
                    e.Id,
                    e.MachineId,
                    e.GatewayId,
                    e.EventKind,
                    e.SourceTimestampUtc,
                    e.ReceivedAtUtc,
                    e.SourceSequence,
                    e.IdempotencyKey,
                    e.Payload,
                    e.IsDuplicate,
                    e.DuplicateOfIdempotencyKey))
                .ToListAsync(ct);
        }, cancellationToken);

    public Task<MachineRuntimeReadModel?> GetMachineRuntimeAsync(
        EdgeOperationContext context,
        Guid machineId,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct =>
            dbContext.MachineRuntimes
                .Where(r => r.FacilityId == context.FacilityId && r.MachineId == machineId)
                .Select(r => new MachineRuntimeReadModel(
                    r.Id,
                    r.MachineId,
                    r.Status,
                    r.IsReadyForProduction,
                    r.LastSeenAtUtc,
                    r.LastEventId,
                    r.LastSourceSequence,
                    r.LastSequenceOutOfOrder,
                    r.UpdatedAtUtc))
                .FirstOrDefaultAsync(ct),
        cancellationToken);

    public Task<IReadOnlyList<LatestTelemetryReadModel>> ListLatestTelemetryAsync(
        EdgeOperationContext context,
        Guid machineId,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            return (IReadOnlyList<LatestTelemetryReadModel>)await dbContext.MachineLatestTelemetries
                .Where(t => t.FacilityId == context.FacilityId && t.MachineId == machineId)
                .OrderBy(t => t.MetricKey)
                .Select(t => new LatestTelemetryReadModel(
                    t.MachineId,
                    t.MetricKey,
                    t.NumericValue,
                    t.TextValue,
                    t.BooleanValue,
                    t.Unit,
                    t.Quality,
                    t.SourceTimestampUtc,
                    t.ReceivedAtUtc,
                    t.SourceSequence,
                    t.UpdatedAtUtc))
                .ToListAsync(ct);
        }, cancellationToken);

    public Task<IReadOnlyList<TelemetrySampleReadModel>> ListTelemetryHistoryAsync(
        EdgeOperationContext context,
        Guid machineId,
        string? metricKey,
        DateTimeOffset? from,
        DateTimeOffset? toUtc,
        int limit,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var effectiveLimit = Math.Clamp(limit, 1, 1000);
            var query = dbContext.MachineTelemetrySamples
                .Where(t => t.FacilityId == context.FacilityId && t.MachineId == machineId);
            if (!string.IsNullOrWhiteSpace(metricKey))
                query = query.Where(t => t.MetricKey == metricKey);
            if (from.HasValue)
                query = query.Where(t => t.SourceTimestampUtc >= from.Value);
            if (toUtc.HasValue)
                query = query.Where(t => t.SourceTimestampUtc <= toUtc.Value);

            return (IReadOnlyList<TelemetrySampleReadModel>)await query
                .OrderByDescending(t => t.SourceTimestampUtc)
                .Take(effectiveLimit)
                .Select(t => new TelemetrySampleReadModel(
                    t.Id,
                    t.MachineId,
                    t.MetricKey,
                    t.NumericValue,
                    t.TextValue,
                    t.BooleanValue,
                    t.Unit,
                    t.Quality,
                    t.Transport,
                    t.SourceTimestampUtc,
                    t.ReceivedAtUtc,
                    t.SourceSequence,
                    t.IdempotencyKey,
                    t.IsDuplicate))
                .ToListAsync(ct);
        }, cancellationToken);

    public Task<IReadOnlyList<ObservedCycleReadModel>> ListCycleHistoryAsync(
        EdgeOperationContext context,
        Guid machineId,
        int limit,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var effectiveLimit = Math.Clamp(limit, 1, 500);
            return (IReadOnlyList<ObservedCycleReadModel>)await dbContext.MachineObservedCycles
                .Where(c => c.FacilityId == context.FacilityId && c.MachineId == machineId)
                .OrderByDescending(c => c.SourceTimestampUtc)
                .Take(effectiveLimit)
                .Select(c => new ObservedCycleReadModel(
                    c.Id,
                    c.MachineId,
                    c.CycleIdentity,
                    c.CycleDurationMs,
                    c.ObservedCounter,
                    c.Quality,
                    c.SourceTimestampUtc,
                    c.ReceivedAtUtc,
                    c.SourceSequence,
                    c.IdempotencyKey,
                    c.IsDuplicate,
                    c.IsOutOfOrder))
                .ToListAsync(ct);
        }, cancellationToken);

    public Task<CycleProjectionReadModel?> GetCycleProjectionAsync(
        EdgeOperationContext context,
        Guid machineId,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct =>
            dbContext.MachineCycleProjections
                .Where(p => p.FacilityId == context.FacilityId && p.MachineId == machineId)
                .Select(p => new CycleProjectionReadModel(
                    p.MachineId,
                    p.ObservedCycleCount,
                    p.LatestObservedCounter,
                    p.LastCycleAtUtc,
                    p.LastCycleDurationMs,
                    p.AverageRecentCycleDurationMs,
                    p.UpdatedAtUtc))
                .FirstOrDefaultAsync(ct),
        cancellationToken);

    public Task<IReadOnlyList<StateTransitionReadModel>> ListStateTransitionsAsync(
        EdgeOperationContext context,
        Guid machineId,
        int limit,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var effectiveLimit = Math.Clamp(limit, 1, 200);
            return (IReadOnlyList<StateTransitionReadModel>)await dbContext.MachineStateTransitions
                .Where(t => t.FacilityId == context.FacilityId && t.MachineId == machineId)
                .OrderByDescending(t => t.SourceTimestampUtc)
                .Take(effectiveLimit)
                .Select(t => new StateTransitionReadModel(
                    t.Id,
                    t.MachineId,
                    t.FromState,
                    t.ToState,
                    t.Reason,
                    t.SourceTimestampUtc,
                    t.ReceivedAtUtc,
                    t.SourceSequence,
                    t.IsOutOfOrder))
                .ToListAsync(ct);
        }, cancellationToken);

    public Task<MachineSignalProjectionReadModel?> GetSignalProjectionAsync(
        EdgeOperationContext context,
        Guid machineId,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(ct =>
            dbContext.MachineSignalProjections
                .Where(s => s.FacilityId == context.FacilityId && s.MachineId == machineId)
                .Select(s => new MachineSignalProjectionReadModel(
                    s.MachineId,
                    s.Connectivity,
                    s.ObservedState,
                    s.LastCycleAtUtc,
                    s.LastCycleDurationMs,
                    s.ObservedCycleCount,
                    s.LatestCounter,
                    s.Quality,
                    s.LastTelemetryAtUtc,
                    s.UpdatedAtUtc))
                .FirstOrDefaultAsync(ct),
        cancellationToken);
}

public sealed class UnavailableEdgeQueryRepository : IEdgeQueryRepository
{
    public Task<IReadOnlyList<EdgeGatewayReadModel>> ListGatewaysAsync(
        EdgeOperationContext context, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<EdgeGatewayReadModel>>([]);

    public Task<EdgeGatewayReadModel?> GetGatewayAsync(
        EdgeOperationContext context, Guid gatewayId, CancellationToken cancellationToken) =>
        Task.FromResult<EdgeGatewayReadModel?>(null);

    public Task<IReadOnlyList<MachineRegistryReadModel>> ListMachinesAsync(
        EdgeOperationContext context, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MachineRegistryReadModel>>([]);

    public Task<MachineRegistryReadModel?> GetMachineAsync(
        EdgeOperationContext context, Guid machineId, CancellationToken cancellationToken) =>
        Task.FromResult<MachineRegistryReadModel?>(null);

    public Task<IReadOnlyList<MachineEventReadModel>> ListMachineEventsAsync(
        EdgeOperationContext context, Guid machineId, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MachineEventReadModel>>([]);

    public Task<MachineRuntimeReadModel?> GetMachineRuntimeAsync(
        EdgeOperationContext context, Guid machineId, CancellationToken cancellationToken) =>
        Task.FromResult<MachineRuntimeReadModel?>(null);

    public Task<IReadOnlyList<LatestTelemetryReadModel>> ListLatestTelemetryAsync(
        EdgeOperationContext context, Guid machineId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LatestTelemetryReadModel>>([]);

    public Task<IReadOnlyList<TelemetrySampleReadModel>> ListTelemetryHistoryAsync(
        EdgeOperationContext context, Guid machineId, string? metricKey,
        DateTimeOffset? from, DateTimeOffset? toUtc, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TelemetrySampleReadModel>>([]);

    public Task<IReadOnlyList<ObservedCycleReadModel>> ListCycleHistoryAsync(
        EdgeOperationContext context, Guid machineId, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ObservedCycleReadModel>>([]);

    public Task<CycleProjectionReadModel?> GetCycleProjectionAsync(
        EdgeOperationContext context, Guid machineId, CancellationToken cancellationToken) =>
        Task.FromResult<CycleProjectionReadModel?>(null);

    public Task<IReadOnlyList<StateTransitionReadModel>> ListStateTransitionsAsync(
        EdgeOperationContext context, Guid machineId, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StateTransitionReadModel>>([]);

    public Task<MachineSignalProjectionReadModel?> GetSignalProjectionAsync(
        EdgeOperationContext context, Guid machineId, CancellationToken cancellationToken) =>
        Task.FromResult<MachineSignalProjectionReadModel?>(null);
}
