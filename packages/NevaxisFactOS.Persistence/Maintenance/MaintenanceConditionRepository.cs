using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Edge;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Maintenance;

public sealed class MaintenanceConditionRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IConditionEvaluationService conditionEvaluationService) : IMaintenanceConditionRepository
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(30);

    public Task<MaintenanceConditionOverviewReadModel?> GetOverviewAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync<MaintenanceConditionOverviewReadModel?>(async ct =>
        {
            var profiles = await dbContext.MaintenanceConditionProfiles
                .AsNoTracking()
                .Where(p => p.FacilityId == context.FacilityId)
                .ToListAsync(ct);

            return new MaintenanceConditionOverviewReadModel(
                profiles.Count,
                profiles.Count(p => p.ConditionStatus == MaintenanceConditionStatuses.Normal),
                profiles.Count(p => p.ConditionStatus == MaintenanceConditionStatuses.Watch),
                profiles.Count(p => p.ConditionStatus == MaintenanceConditionStatuses.Degraded),
                profiles.Count(p => p.ConditionStatus == MaintenanceConditionStatuses.Critical),
                profiles.Count(p =>
                    p.ConditionStatus == MaintenanceConditionStatuses.Unknown ||
                    p.DataSufficiency == MaintenanceConditionDataSufficiencyLevels.Insufficient),
                profiles.MaxBy(p => p.LastEvaluatedAtUtc)?.LastEvaluatedAtUtc);
        }, cancellationToken);

    public Task<IReadOnlyList<MaintenanceConditionAssetRowReadModel>> ListAssetRowsAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var mappings = await LoadMappingsAsync(context.FacilityId, ct);
            var profiles = await dbContext.MaintenanceConditionProfiles
                .AsNoTracking()
                .Where(p => p.FacilityId == context.FacilityId)
                .ToDictionaryAsync(p => p.AssetReference, StringComparer.OrdinalIgnoreCase, ct);
            var edgeContext = BuildEdgeContext(context);

            var rows = new List<MaintenanceConditionAssetRowReadModel>();
            foreach (var mapping in mappings)
            {
                var machine = await GetEdgeMachineAsync(edgeContext, mapping.MachineId, ct);
                profiles.TryGetValue(mapping.AssetReference, out var profile);
                rows.Add(new MaintenanceConditionAssetRowReadModel(
                    mapping.AssetReference,
                    mapping.AssetName,
                    machine?.MachineCode,
                    machine?.Name,
                    mapping.MachineId,
                    profile?.ConditionStatus ?? MaintenanceConditionStatuses.Unknown,
                    profile?.ActiveAnomalyCount ?? 0,
                    profile?.HighestAnomalySeverity,
                    profile?.DataFreshness ?? MaintenanceConditionDataFreshnessLevels.Unknown,
                    profile?.DataSufficiency ?? MaintenanceConditionDataSufficiencyLevels.Insufficient,
                    profile?.LastEvaluatedAtUtc));
            }

            return (IReadOnlyList<MaintenanceConditionAssetRowReadModel>)rows
                .OrderBy(r => r.AssetReference, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }, cancellationToken);

    public Task<MaintenanceConditionProfileReadModel?> GetAssetConditionAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var mapping = await dbContext.MaintenanceAssetMachineMappings
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.FacilityId == context.FacilityId &&
                         m.AssetReference == assetReference,
                    ct);

            if (mapping is null)
            {
                return null;
            }

            var profile = await dbContext.MaintenanceConditionProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.FacilityId == context.FacilityId &&
                         p.AssetReference == assetReference,
                    ct);

            var edgeContext = BuildEdgeContext(context);
            var machine = await GetEdgeMachineAsync(edgeContext, mapping.MachineId, ct);
            var metricDefinitions = await LoadEnabledMetricDefinitionsAsync(context.FacilityId, ct);
            var metricSnapshots = await BuildMetricSnapshotsAsync(
                edgeContext,
                mapping,
                metricDefinitions,
                profile,
                ct);

            var activeAnomalies = await dbContext.MaintenanceConditionAnomalies
                .AsNoTracking()
                .Where(a => a.FacilityId == context.FacilityId &&
                            a.AssetReference == assetReference &&
                            a.Status == MaintenanceAnomalyStatuses.Open)
                .OrderByDescending(a => a.DetectedAtUtc)
                .Take(20)
                .ToListAsync(ct);

            var displayNames = metricDefinitions.ToDictionary(d => d.MetricKey, d => d.DisplayName, StringComparer.OrdinalIgnoreCase);
            var anomalyModels = activeAnomalies
                .Select(a => MapAnomaly(a, displayNames.GetValueOrDefault(a.MetricKey, a.MetricKey)))
                .ToList();

            var signals = await ListPersistedSignalsAsync(context.FacilityId, assetReference, ct);
            var maintenanceHistory = await LoadMaintenanceHistoryContextAsync(context, assetReference, ct);
            var alarmContext = await LoadAlarmContextAsync(edgeContext, mapping.MachineId, ct);
            var downtimeContext = await LoadDowntimeContextAsync(edgeContext, mapping.MachineId, ct);

            return new MaintenanceConditionProfileReadModel(
                mapping.AssetReference,
                mapping.AssetName,
                mapping.MachineId,
                machine?.MachineCode,
                machine?.Name,
                profile?.ConditionStatus ?? MaintenanceConditionStatuses.Unknown,
                profile?.OverallConditionScore,
                profile?.LastEvaluatedAtUtc,
                profile?.DataFreshness ?? MaintenanceConditionDataFreshnessLevels.Unknown,
                profile?.DataSufficiency ?? MaintenanceConditionDataSufficiencyLevels.Insufficient,
                profile?.ActiveAnomalyCount ?? 0,
                profile?.HighestAnomalySeverity,
                profile?.TrendState ?? MaintenanceConditionTrendStates.Insufficient,
                signals,
                anomalyModels,
                new MaintenanceConditionContextReadModel(
                    alarmContext,
                    downtimeContext,
                    maintenanceHistory,
                    metricSnapshots));
        }, cancellationToken);

    public Task<IReadOnlyList<MaintenanceConditionAnomalyReadModel>> ListAnomaliesAsync(
        MaintenanceOperationContext context,
        string assetReference,
        int limit,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var normalizedLimit = Math.Clamp(limit, 1, 200);
            var metricDefinitions = await LoadEnabledMetricDefinitionsAsync(context.FacilityId, ct);
            var displayNames = metricDefinitions.ToDictionary(d => d.MetricKey, d => d.DisplayName, StringComparer.OrdinalIgnoreCase);

            var anomalies = await dbContext.MaintenanceConditionAnomalies
                .AsNoTracking()
                .Where(a => a.FacilityId == context.FacilityId && a.AssetReference == assetReference)
                .OrderByDescending(a => a.DetectedAtUtc)
                .Take(normalizedLimit)
                .ToListAsync(ct);

            return (IReadOnlyList<MaintenanceConditionAnomalyReadModel>)anomalies
                .Select(a => MapAnomaly(a, displayNames.GetValueOrDefault(a.MetricKey, a.MetricKey)))
                .ToList();
        }, cancellationToken);

    public Task<IReadOnlyList<MaintenancePredictiveConditionSignalReadModel>> ListConditionSignalsAsync(
        MaintenanceOperationContext context,
        string? assetReference,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(assetReference))
            {
                var mappings = await LoadMappingsAsync(context.FacilityId, ct);
                var combined = new List<MaintenancePredictiveConditionSignalReadModel>();
                foreach (var mapping in mappings)
                {
                    combined.AddRange(await ListPersistedSignalsAsync(context.FacilityId, mapping.AssetReference, ct));
                }

                return (IReadOnlyList<MaintenancePredictiveConditionSignalReadModel>)combined;
            }

            return (IReadOnlyList<MaintenancePredictiveConditionSignalReadModel>)await ListPersistedSignalsAsync(
                context.FacilityId,
                assetReference,
                ct);
        }, cancellationToken);

    public Task<bool> EvaluateFacilityAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            var mappings = await LoadMappingsAsync(context.FacilityId, ct);
            if (mappings.Count == 0)
            {
                return false;
            }

            var metricDefinitions = await LoadEnabledMetricDefinitionsAsync(context.FacilityId, ct);
            var edgeContext = BuildEdgeContext(context);
            var evaluatedAtUtc = DateTimeOffset.UtcNow;

            foreach (var mapping in mappings)
            {
                var evaluationInput = await BuildEvaluationInputAsync(
                    context,
                    mapping,
                    metricDefinitions,
                    edgeContext,
                    evaluatedAtUtc,
                    ct);
                var evaluationResult = conditionEvaluationService.EvaluateAsset(evaluationInput);
                await PersistEvaluationAsync(context, mapping, evaluationResult, evaluatedAtUtc, ct);
            }

            await dbContext.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);

    private async Task<ConditionAssetEvaluationInput> BuildEvaluationInputAsync(
        MaintenanceOperationContext context,
        MaintenanceAssetMachineMapping mapping,
        IReadOnlyList<MaintenanceConditionMetricDefinition> metricDefinitions,
        EdgeOperationContext edgeContext,
        DateTimeOffset evaluatedAtUtc,
        CancellationToken cancellationToken)
    {
        var metrics = new List<ConditionMetricEvaluationInput>();
        var latestTelemetryAt = (DateTimeOffset?)null;

        foreach (var definition in metricDefinitions)
        {
            var history = await ListEdgeTelemetryHistoryAsync(
                edgeContext,
                mapping.MachineId,
                definition.MetricKey,
                evaluatedAtUtc.AddHours(-definition.EvaluationWindowHours),
                evaluatedAtUtc,
                200,
                cancellationToken);

            var samples = history
                .Where(h => h.NumericValue.HasValue)
                .Select(h => new ConditionTelemetrySampleInput(
                    (decimal)h.NumericValue!.Value,
                    h.Quality,
                    h.SourceTimestampUtc))
                .ToList();

            var latest = history.OrderByDescending(h => h.SourceTimestampUtc).FirstOrDefault();
            if (latest is not null &&
                (latestTelemetryAt is null || latest.SourceTimestampUtc > latestTelemetryAt))
            {
                latestTelemetryAt = latest.SourceTimestampUtc;
            }

            metrics.Add(new ConditionMetricEvaluationInput(
                definition.MetricKey,
                definition.DisplayName,
                definition.ExpectedUnit,
                definition.EvaluationWindowHours,
                definition.MinimumSamples,
                definition.BaselineMethod,
                definition.DeviationThresholdPercent,
                definition.TrendThresholdPercent,
                definition.VolatilityThresholdPercent,
                samples,
                latest?.SourceTimestampUtc));
        }

        var cycles = await ListEdgeCycleHistoryAsync(edgeContext, mapping.MachineId, 100, cancellationToken);
        var cycleSamples = cycles
            .Select(c => new ConditionCycleSampleInput(c.CycleDurationMs, c.SourceTimestampUtc))
            .ToList();

        var sourceIsStale = latestTelemetryAt is null ||
                            evaluatedAtUtc - latestTelemetryAt.Value > StaleThreshold;

        var maintenanceHistory = await LoadMaintenanceHistoryContextAsync(context, mapping.AssetReference, cancellationToken);
        var alarmContext = await LoadAlarmContextAsync(edgeContext, mapping.MachineId, cancellationToken);
        var downtimeContext = await LoadDowntimeContextAsync(edgeContext, mapping.MachineId, cancellationToken);
        var machine = await GetEdgeMachineAsync(edgeContext, mapping.MachineId, cancellationToken);

        return new ConditionAssetEvaluationInput(
            mapping.AssetReference,
            mapping.MachineId,
            machine?.MachineCode,
            machine?.Name,
            evaluatedAtUtc,
            metrics,
            cycleSamples,
            alarmContext,
            downtimeContext,
            maintenanceHistory,
            sourceIsStale);
    }

    private async Task PersistEvaluationAsync(
        MaintenanceOperationContext context,
        MaintenanceAssetMachineMapping mapping,
        ConditionAssetEvaluationResult evaluation,
        DateTimeOffset evaluatedAtUtc,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.MaintenanceConditionProfiles
            .FirstOrDefaultAsync(
                p => p.FacilityId == context.FacilityId &&
                     p.AssetReference == mapping.AssetReference,
                cancellationToken);

        if (profile is null)
        {
            profile = new MaintenanceConditionProfile
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                AssetReference = mapping.AssetReference,
                MachineId = mapping.MachineId,
                CreatedAtUtc = evaluatedAtUtc,
                Version = 1,
            };
            dbContext.MaintenanceConditionProfiles.Add(profile);
        }

        profile.ConditionStatus = evaluation.ConditionStatus;
        profile.OverallConditionScore = evaluation.OverallConditionScore;
        profile.LastEvaluatedAtUtc = evaluatedAtUtc;
        profile.DataFreshness = evaluation.DataFreshness;
        profile.DataSufficiency = evaluation.DataSufficiency;
        profile.ActiveAnomalyCount = evaluation.ActiveAnomalyCount;
        profile.HighestAnomalySeverity = evaluation.HighestAnomalySeverity;
        profile.TrendState = evaluation.TrendState;
        profile.EvaluationIdentity = evaluation.EvaluationIdentity;
        profile.UpdatedAtUtc = evaluatedAtUtc;
        profile.Version += 1;

        foreach (var anomaly in evaluation.Anomalies)
        {
            var exists = await dbContext.MaintenanceConditionAnomalies.AnyAsync(
                a => a.FacilityId == context.FacilityId &&
                     a.EvaluationIdentity == anomaly.EvaluationIdentity,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            dbContext.MaintenanceConditionAnomalies.Add(new MaintenanceConditionAnomaly
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                AssetReference = mapping.AssetReference,
                MachineId = mapping.MachineId,
                MetricKey = anomaly.MetricKey,
                DetectedAtUtc = evaluatedAtUtc,
                WindowStartUtc = anomaly.WindowStartUtc,
                WindowEndUtc = anomaly.WindowEndUtc,
                ObservedValue = anomaly.ObservedValue,
                BaselineValue = anomaly.BaselineValue,
                DeviationPercent = anomaly.DeviationPercent,
                Severity = anomaly.Severity,
                Direction = anomaly.Direction,
                EvidenceSummary = anomaly.EvidenceSummary,
                SourceFreshness = anomaly.SourceFreshness,
                DataQuality = anomaly.DataQuality,
                Status = MaintenanceAnomalyStatuses.Open,
                EvaluationIdentity = anomaly.EvaluationIdentity,
                CreatedAtUtc = evaluatedAtUtc,
            });
        }

        await NormalizeResolvedAnomaliesAsync(context.FacilityId, mapping.AssetReference, evaluation, evaluatedAtUtc, cancellationToken);
    }

    private async Task NormalizeResolvedAnomaliesAsync(
        Guid facilityId,
        string assetReference,
        ConditionAssetEvaluationResult evaluation,
        DateTimeOffset evaluatedAtUtc,
        CancellationToken cancellationToken)
    {
        var openAnomalies = await dbContext.MaintenanceConditionAnomalies
            .Where(a => a.FacilityId == facilityId &&
                        a.AssetReference == assetReference &&
                        a.Status == MaintenanceAnomalyStatuses.Open)
            .ToListAsync(cancellationToken);

        var activeMetricKeys = evaluation.Anomalies
            .Select(a => a.MetricKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var open in openAnomalies)
        {
            if (!activeMetricKeys.Contains(open.MetricKey))
            {
                open.Status = MaintenanceAnomalyStatuses.Normalized;
            }
        }

        if (evaluation.ConditionStatus == MaintenanceConditionStatuses.Normal)
        {
            foreach (var open in openAnomalies.Where(a => a.Status == MaintenanceAnomalyStatuses.Open))
            {
                if (!activeMetricKeys.Contains(open.MetricKey))
                {
                    open.Status = MaintenanceAnomalyStatuses.Normalized;
                }
            }
        }

        _ = evaluatedAtUtc;
    }

    private async Task<IReadOnlyList<MaintenanceConditionMetricSnapshotReadModel>> BuildMetricSnapshotsAsync(
        EdgeOperationContext edgeContext,
        MaintenanceAssetMachineMapping mapping,
        IReadOnlyList<MaintenanceConditionMetricDefinition> metricDefinitions,
        MaintenanceConditionProfile? profile,
        CancellationToken cancellationToken)
    {
        var snapshots = new List<MaintenanceConditionMetricSnapshotReadModel>();
        var evaluatedAtUtc = profile?.LastEvaluatedAtUtc ?? DateTimeOffset.UtcNow;

        foreach (var definition in metricDefinitions)
        {
            var history = await ListEdgeTelemetryHistoryAsync(
                edgeContext,
                mapping.MachineId,
                definition.MetricKey,
                evaluatedAtUtc.AddHours(-definition.EvaluationWindowHours),
                evaluatedAtUtc,
                200,
                cancellationToken);

            var metricInput = new ConditionMetricEvaluationInput(
                definition.MetricKey,
                definition.DisplayName,
                definition.ExpectedUnit,
                definition.EvaluationWindowHours,
                definition.MinimumSamples,
                definition.BaselineMethod,
                definition.DeviationThresholdPercent,
                definition.TrendThresholdPercent,
                definition.VolatilityThresholdPercent,
                history.Where(h => h.NumericValue.HasValue).Select(h => new ConditionTelemetrySampleInput(
                    (decimal)h.NumericValue!.Value,
                    h.Quality,
                    h.SourceTimestampUtc)).ToList(),
                history.OrderByDescending(h => h.SourceTimestampUtc).FirstOrDefault()?.SourceTimestampUtc);

            var result = ConditionEvaluationService.EvaluateMetric(
                metricInput,
                evaluatedAtUtc,
                profile?.DataFreshness == MaintenanceConditionDataFreshnessLevels.Stale);

            snapshots.Add(new MaintenanceConditionMetricSnapshotReadModel(
                definition.MetricKey,
                definition.DisplayName,
                definition.ExpectedUnit,
                result.DataSufficiency,
                result.DataFreshness,
                result.TrendState,
                result.BaselineValue,
                result.ObservedValue,
                result.DeviationPercent));
        }

        return snapshots;
    }

    private async Task<IReadOnlyList<MaintenancePredictiveConditionSignalReadModel>> ListPersistedSignalsAsync(
        Guid facilityId,
        string assetReference,
        CancellationToken cancellationToken)
    {
        var openAnomalies = await dbContext.MaintenanceConditionAnomalies
            .AsNoTracking()
            .Where(a => a.FacilityId == facilityId &&
                        a.AssetReference == assetReference &&
                        a.Status == MaintenanceAnomalyStatuses.Open)
            .OrderByDescending(a => a.DetectedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        return openAnomalies
            .Select(a => new MaintenancePredictiveConditionSignalReadModel(
                a.MetricKey,
                a.MetricKey,
                a.Severity,
                a.EvidenceSummary))
            .ToList();
    }

    private async Task<ConditionMaintenanceHistoryInput> LoadMaintenanceHistoryContextAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-90);
        var requests = await dbContext.MaintenanceRequests
            .AsNoTracking()
            .Where(r => r.FacilityId == context.FacilityId &&
                        r.AssetReference == assetReference &&
                        r.ReportedAtUtc >= since)
            .ToListAsync(cancellationToken);

        var completedWorkOrders = await dbContext.MaintenanceWorkOrders
            .AsNoTracking()
            .Where(w => w.FacilityId == context.FacilityId &&
                        w.AssetReference == assetReference &&
                        w.Status == MaintenanceWorkOrderStatus.Completed &&
                        w.CompletedAtUtc != null)
            .OrderByDescending(w => w.CompletedAtUtc)
            .Take(1)
            .ToListAsync(cancellationToken);

        var latestFailure = requests
            .Where(r => r.Type == MaintenanceRequestType.Fault)
            .OrderByDescending(r => r.ReportedAtUtc)
            .FirstOrDefault();

        return new ConditionMaintenanceHistoryInput(
            requests.Count(r => r.Type == MaintenanceRequestType.Fault),
            latestFailure?.FailureCategory.ToString(),
            completedWorkOrders.FirstOrDefault()?.CompletedAtUtc);
    }

    private async Task<ConditionAlarmContextInput> LoadAlarmContextAsync(
        EdgeOperationContext edgeContext,
        Guid machineId,
        CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddHours(-24);
        var alarms = await ListEdgeAlarmHistoryAsync(edgeContext, machineId, 200, cancellationToken);
        var recent = alarms.Where(a => a.SourceTimestampUtc >= since).ToList();
        return new ConditionAlarmContextInput(
            recent.Count(a => string.Equals(a.EventKind, MachineEventKinds.AlarmRaised, StringComparison.OrdinalIgnoreCase)),
            recent.OrderByDescending(a => a.Severity).Select(a => a.Severity).FirstOrDefault());
    }

    private async Task<ConditionDowntimeContextInput> LoadDowntimeContextAsync(
        EdgeOperationContext edgeContext,
        Guid machineId,
        CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddHours(-24);
        var episodes = await ListEdgeDowntimeEpisodesAsync(edgeContext, machineId, 100, cancellationToken);
        var recent = episodes.Where(e => e.StartedAtSourceUtc >= since).ToList();
        var totalMinutes = recent.Sum(e => e.DurationMs.HasValue ? (decimal)e.DurationMs.Value / 60000m : 0m);
        return new ConditionDowntimeContextInput(recent.Count, totalMinutes);
    }

    private Task<List<MaintenanceAssetMachineMapping>> LoadMappingsAsync(
        Guid facilityId,
        CancellationToken cancellationToken) =>
        dbContext.MaintenanceAssetMachineMappings
            .AsNoTracking()
            .Where(m => m.FacilityId == facilityId)
            .OrderBy(m => m.AssetReference)
            .ToListAsync(cancellationToken);

    private Task<List<MaintenanceConditionMetricDefinition>> LoadEnabledMetricDefinitionsAsync(
        Guid facilityId,
        CancellationToken cancellationToken) =>
        dbContext.MaintenanceConditionMetricDefinitions
            .AsNoTracking()
            .Where(d => d.FacilityId == facilityId && d.Enabled)
            .OrderBy(d => d.MetricKey)
            .ToListAsync(cancellationToken);

    private static EdgeOperationContext BuildEdgeContext(MaintenanceOperationContext context) =>
        new(
            context.TenantId,
            context.FacilityId,
            context.MembershipId,
            context.IdentitySubjectId,
            EdgePermissionKeys.Read,
            context.CorrelationId);

    private Task<MachineRegistryReadModel?> GetEdgeMachineAsync(
        EdgeOperationContext context,
        Guid machineId,
        CancellationToken cancellationToken) =>
        dbContext.MachineRegistries
            .AsNoTracking()
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
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<IReadOnlyList<TelemetrySampleReadModel>> ListEdgeTelemetryHistoryAsync(
        EdgeOperationContext context,
        Guid machineId,
        string? metricKey,
        DateTimeOffset? from,
        DateTimeOffset? toUtc,
        int limit,
        CancellationToken cancellationToken)
    {
        var effectiveLimit = Math.Clamp(limit, 1, 1000);
        var query = dbContext.MachineTelemetrySamples
            .AsNoTracking()
            .Where(t => t.FacilityId == context.FacilityId && t.MachineId == machineId);
        if (!string.IsNullOrWhiteSpace(metricKey))
        {
            query = query.Where(t => t.MetricKey == metricKey);
        }

        if (from.HasValue)
        {
            query = query.Where(t => t.SourceTimestampUtc >= from.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(t => t.SourceTimestampUtc <= toUtc.Value);
        }

        return await query
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
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ObservedCycleReadModel>> ListEdgeCycleHistoryAsync(
        EdgeOperationContext context,
        Guid machineId,
        int limit,
        CancellationToken cancellationToken)
    {
        var effectiveLimit = Math.Clamp(limit, 1, 500);
        return await dbContext.MachineObservedCycles
            .AsNoTracking()
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
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AlarmObservationReadModel>> ListEdgeAlarmHistoryAsync(
        EdgeOperationContext context,
        Guid machineId,
        int limit,
        CancellationToken cancellationToken)
    {
        var effectiveLimit = Math.Clamp(limit, 1, 200);
        return await dbContext.MachineAlarmObservations
            .AsNoTracking()
            .Where(o => o.FacilityId == context.FacilityId && o.MachineId == machineId)
            .OrderByDescending(o => o.SourceTimestampUtc)
            .Take(effectiveLimit)
            .Select(o => new AlarmObservationReadModel(
                o.Id,
                o.MachineId,
                o.AlarmIdentity,
                o.ExternalAlarmCode,
                o.AlarmText,
                o.Severity,
                o.EventKind,
                o.SourceTimestampUtc,
                o.ReceivedAtUtc,
                o.SourceSequence,
                o.Quality,
                o.IsDuplicate,
                o.IsOutOfOrder,
                o.IsUnmatchedClear))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<DowntimeEpisodeReadModel>> ListEdgeDowntimeEpisodesAsync(
        EdgeOperationContext context,
        Guid machineId,
        int limit,
        CancellationToken cancellationToken)
    {
        var effectiveLimit = Math.Clamp(limit, 1, 200);
        return await dbContext.MachineDowntimeEpisodes
            .AsNoTracking()
            .Where(e => e.FacilityId == context.FacilityId && e.MachineId == machineId)
            .OrderByDescending(e => e.StartedAtSourceUtc)
            .Take(effectiveLimit)
            .Select(e => new DowntimeEpisodeReadModel(
                e.Id,
                e.MachineId,
                e.ExternalEpisodeId,
                e.StartedAtSourceUtc,
                e.EndedAtSourceUtc,
                e.DurationMs,
                e.ReasonCode,
                e.ReasonCategory,
                e.ReasonText,
                e.IsMicroStop,
                e.Quality,
                e.IsClosed,
                e.IsOutOfOrder,
                e.StartSourceSequence,
                e.EndSourceSequence))
            .ToListAsync(cancellationToken);
    }

    private static MaintenanceConditionAnomalyReadModel MapAnomaly(
        MaintenanceConditionAnomaly anomaly,
        string metricDisplayName) =>
        new(
            anomaly.Id,
            anomaly.MetricKey,
            metricDisplayName,
            anomaly.DetectedAtUtc,
            anomaly.ObservedValue,
            anomaly.BaselineValue,
            anomaly.DeviationPercent,
            anomaly.Severity,
            anomaly.Direction,
            anomaly.EvidenceSummary,
            anomaly.SourceFreshness,
            anomaly.DataQuality,
            anomaly.Status);
}

public sealed class UnavailableMaintenanceConditionRepository : IMaintenanceConditionRepository
{
    public Task<MaintenanceConditionOverviewReadModel?> GetOverviewAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken) =>
        Task.FromResult<MaintenanceConditionOverviewReadModel?>(null);

    public Task<IReadOnlyList<MaintenanceConditionAssetRowReadModel>> ListAssetRowsAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MaintenanceConditionAssetRowReadModel>>([]);

    public Task<MaintenanceConditionProfileReadModel?> GetAssetConditionAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken) =>
        Task.FromResult<MaintenanceConditionProfileReadModel?>(null);

    public Task<IReadOnlyList<MaintenanceConditionAnomalyReadModel>> ListAnomaliesAsync(
        MaintenanceOperationContext context,
        string assetReference,
        int limit,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MaintenanceConditionAnomalyReadModel>>([]);

    public Task<IReadOnlyList<MaintenancePredictiveConditionSignalReadModel>> ListConditionSignalsAsync(
        MaintenanceOperationContext context,
        string? assetReference,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MaintenancePredictiveConditionSignalReadModel>>([]);

    public Task<bool> EvaluateFacilityAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
