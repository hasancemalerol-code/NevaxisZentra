using NevaxisZentra.Persistence.IotPlc;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotIotPlcIntelligenceSeed
{
    private static readonly DateTimeOffset EvalTime = DevelopmentPilotIds.IotPlc2PilotEvaluationTimeUtc;
    private static readonly DateTimeOffset WindowStart = EvalTime.AddHours(-1);
    private static readonly DateTimeOffset WindowEnd = EvalTime;
    private const string Provenance = IotPlcIntelligenceConstants.DevelopmentPilotProvenance;

    internal static async Task EnsureIotPlcIntelligencePilotRecordsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await EnsureProfilesAsync(dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
        await EnsureScenarioHealthObservationsAsync(dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
        await EnsureScenarioObservationsAsync(dbContext, tenantId, facilityId, actorSubjectId, seedTimestampUtc, cancellationToken);
    }

    private static async Task EnsureProfilesAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await EnsureProfileAsync(
            dbContext,
            DevelopmentPilotIds.IotPlc2PilotProfileStandardId,
            tenantId,
            facilityId,
            "QP-IOT-STANDARD",
            "Development Standard Signal Quality Profile",
            expectedIntervalSeconds: 1800,
            freshnessToleranceSeconds: 600,
            staleThresholdSeconds: 3600,
            maximumClockSkewToleranceSeconds: 30,
            minimumExpectedObservationCount: 2,
            minimumGoodQualityRatio: 0.50m,
            maximumBadQualityRatio: 0.20m,
            maximumUncertainQualityRatio: 0.30m,
            maximumSourceConflictRatio: 0.10m,
            sequenceGapEvaluationEnabled: true,
            IotPlcQualityProfileStatuses.Approved,
            Provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);

        await EnsureProfileAsync(
            dbContext,
            DevelopmentPilotIds.IotPlc2PilotProfileStrictId,
            tenantId,
            facilityId,
            "QP-IOT-STRICT",
            "Development Strict Profile (High Minimum Count)",
            expectedIntervalSeconds: 60,
            freshnessToleranceSeconds: 120,
            staleThresholdSeconds: 600,
            maximumClockSkewToleranceSeconds: 5,
            minimumExpectedObservationCount: 10,
            minimumGoodQualityRatio: 0.80m,
            maximumBadQualityRatio: 0.05m,
            maximumUncertainQualityRatio: 0.10m,
            maximumSourceConflictRatio: 0m,
            sequenceGapEvaluationEnabled: false,
            IotPlcQualityProfileStatuses.Approved,
            Provenance,
            actorSubjectId,
            seedTimestampUtc,
            cancellationToken);
    }

    private static async Task EnsureProfileAsync(
        NevaxisZentraDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        string profileCode,
        string displayName,
        int expectedIntervalSeconds,
        int freshnessToleranceSeconds,
        int staleThresholdSeconds,
        int maximumClockSkewToleranceSeconds,
        int minimumExpectedObservationCount,
        decimal minimumGoodQualityRatio,
        decimal maximumBadQualityRatio,
        decimal maximumUncertainQualityRatio,
        decimal maximumSourceConflictRatio,
        bool sequenceGapEvaluationEnabled,
        string status,
        string provenance,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.IotPlcQualityProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (existing is null)
        {
            dbContext.IotPlcQualityProfiles.Add(new IotPlcQualityProfile
            {
                Id = id,
                TenantId = tenantId,
                FacilityId = facilityId,
                ProfileCode = profileCode,
                DisplayName = displayName,
                ExpectedObservationIntervalSeconds = expectedIntervalSeconds,
                FreshnessToleranceSeconds = freshnessToleranceSeconds,
                StaleThresholdSeconds = staleThresholdSeconds,
                MaximumClockSkewToleranceSeconds = maximumClockSkewToleranceSeconds,
                MinimumExpectedObservationCount = minimumExpectedObservationCount,
                MinimumGoodQualityRatio = minimumGoodQualityRatio,
                MaximumBadQualityRatio = maximumBadQualityRatio,
                MaximumUncertainQualityRatio = maximumUncertainQualityRatio,
                MaximumSourceConflictRatio = maximumSourceConflictRatio,
                SequenceGapEvaluationEnabled = sequenceGapEvaluationEnabled,
                Status = status,
                SourceProvenance = provenance,
                Version = 1,
                CreatedAtUtc = seedTimestampUtc,
                UpdatedAtUtc = seedTimestampUtc,
                CreatedBy = actorSubjectId,
                UpdatedBy = actorSubjectId,
            });
        }
        else
        {
            existing.DisplayName = displayName;
            existing.ExpectedObservationIntervalSeconds = expectedIntervalSeconds;
            existing.FreshnessToleranceSeconds = freshnessToleranceSeconds;
            existing.StaleThresholdSeconds = staleThresholdSeconds;
            existing.MaximumClockSkewToleranceSeconds = maximumClockSkewToleranceSeconds;
            existing.MinimumExpectedObservationCount = minimumExpectedObservationCount;
            existing.MinimumGoodQualityRatio = minimumGoodQualityRatio;
            existing.MaximumBadQualityRatio = maximumBadQualityRatio;
            existing.MaximumUncertainQualityRatio = maximumUncertainQualityRatio;
            existing.MaximumSourceConflictRatio = maximumSourceConflictRatio;
            existing.SequenceGapEvaluationEnabled = sequenceGapEvaluationEnabled;
            existing.Status = status;
            existing.SourceProvenance = provenance;
            existing.UpdatedAtUtc = seedTimestampUtc;
            existing.UpdatedBy = actorSubjectId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureScenarioHealthObservationsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        await EnsureHealthObservationAsync(
            dbContext, DevelopmentPilotIds.IotPlc2PilotHealthOnlineFreshId, tenantId, facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId, EvalTime.AddMinutes(-5), EvalTime.AddMinutes(-5),
            IotPlcConnectionStates.Online, IotPlcEvidenceStates.Simulated, actorSubjectId, seedTimestampUtc, cancellationToken);

        await EnsureHealthObservationAsync(
            dbContext, DevelopmentPilotIds.IotPlc2PilotHealthDegradedFreshId, tenantId, facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointEdgeId, EvalTime.AddMinutes(-3), EvalTime.AddMinutes(-3),
            IotPlcConnectionStates.Degraded, IotPlcEvidenceStates.Simulated, actorSubjectId, seedTimestampUtc, cancellationToken);

        await EnsureHealthObservationAsync(
            dbContext, DevelopmentPilotIds.IotPlc2PilotHealthOfflineFreshId, tenantId, facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointMqttId, EvalTime.AddMinutes(-2), EvalTime.AddMinutes(-2),
            IotPlcConnectionStates.Offline, IotPlcEvidenceStates.Simulated, actorSubjectId, seedTimestampUtc, cancellationToken);
    }

    private static async Task EnsureHealthObservationAsync(
        NevaxisZentraDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        Guid endpointId,
        DateTimeOffset observedAtUtc,
        DateTimeOffset receivedAtUtc,
        string healthState,
        string evidenceState,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.IotPlcConnectionHealthObservations
            .IgnoreQueryFilters()
            .AnyAsync(h => h.Id == id, cancellationToken);
        if (exists) return;

        dbContext.IotPlcConnectionHealthObservations.Add(new IotPlcConnectionHealthObservation
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            EndpointId = endpointId,
            ObservedAtUtc = observedAtUtc,
            ReceivedAtUtc = receivedAtUtc,
            HealthState = healthState,
            Source = "development-simulator",
            EvidenceState = evidenceState,
            ReasonCode = $"pilot2-{healthState}",
            LatencyMs = healthState == IotPlcConnectionStates.Degraded ? 850 : 45,
            LastSuccessfulObservationAtUtc = observedAtUtc.AddMinutes(-1),
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureScenarioObservationsAsync(
        NevaxisZentraDbContext dbContext,
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var provenance = IotPlcConstants.DevelopmentPilotProvenance;

        await EnsureObservationAsync(dbContext, DevelopmentPilotIds.IotPlc2PilotScenarioObsHealthy1Id, tenantId, facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId, DevelopmentPilotIds.IotPlcPilotSignalInj1StateId,
            EvalTime.AddMinutes(-5), EvalTime.AddMinutes(-5), true, null, null, IotPlcQualityStates.Good,
            IotPlcEvidenceStates.Simulated, "PILOT2-HEALTHY-1", provenance, actorSubjectId, seedTimestampUtc, cancellationToken);

        await EnsureObservationAsync(dbContext, DevelopmentPilotIds.IotPlc2PilotScenarioObsHealthy2Id, tenantId, facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId, DevelopmentPilotIds.IotPlcPilotSignalInj1StateId,
            EvalTime.AddMinutes(-10), EvalTime.AddMinutes(-10), true, null, null, IotPlcQualityStates.Good,
            IotPlcEvidenceStates.Simulated, "PILOT2-HEALTHY-2", provenance, actorSubjectId, seedTimestampUtc, cancellationToken);

        await EnsureObservationAsync(dbContext, DevelopmentPilotIds.IotPlc2PilotScenarioObsStaleId, tenantId, facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId, DevelopmentPilotIds.IotPlcPilotSignalInj1CycleId,
            EvalTime.AddMinutes(-90), EvalTime.AddMinutes(-90), null, 1000L, null, IotPlcQualityStates.Stale,
            IotPlcEvidenceStates.Simulated, "PILOT2-STALE-1", provenance, actorSubjectId, seedTimestampUtc, cancellationToken);

        await EnsureObservationAsync(dbContext, DevelopmentPilotIds.IotPlc2PilotScenarioObsStale2Id, tenantId, facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId, DevelopmentPilotIds.IotPlcPilotSignalInj1CycleId,
            EvalTime.AddMinutes(-85), EvalTime.AddMinutes(-85), null, 1001L, null, IotPlcQualityStates.Stale,
            IotPlcEvidenceStates.Simulated, "PILOT2-STALE-2", provenance, actorSubjectId, seedTimestampUtc, cancellationToken);

        await EnsureObservationAsync(dbContext, DevelopmentPilotIds.IotPlc2PilotScenarioObsClockSkewId, tenantId, facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointEdgeId, DevelopmentPilotIds.IotPlcPilotSignalInj2GoodId,
            EvalTime.AddMinutes(-2), EvalTime.AddMinutes(5), null, 500L, null, IotPlcQualityStates.Good,
            IotPlcEvidenceStates.Simulated, "PILOT2-SKEW-1", provenance, actorSubjectId, seedTimestampUtc, cancellationToken);

        await EnsureObservationAsync(dbContext, DevelopmentPilotIds.IotPlc2PilotScenarioObsClockSkewCompanionId, tenantId, facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointEdgeId, DevelopmentPilotIds.IotPlcPilotSignalInj2GoodId,
            EvalTime.AddMinutes(-15), EvalTime.AddMinutes(-15), null, 499L, null, IotPlcQualityStates.Good,
            IotPlcEvidenceStates.Simulated, "PILOT2-SKEW-2", provenance, actorSubjectId, seedTimestampUtc, cancellationToken);

        await EnsureObservationAsync(dbContext, DevelopmentPilotIds.IotPlc2PilotScenarioObsSeq1Id, tenantId, facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId, DevelopmentPilotIds.IotPlcPilotSignalInj1CycleId,
            EvalTime.AddMinutes(-8), EvalTime.AddMinutes(-8), null, 1001L, null, IotPlcQualityStates.Good,
            IotPlcEvidenceStates.Simulated, "1001", provenance, actorSubjectId, seedTimestampUtc, cancellationToken);

        await EnsureObservationAsync(dbContext, DevelopmentPilotIds.IotPlc2PilotScenarioObsSeq2Id, tenantId, facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId, DevelopmentPilotIds.IotPlcPilotSignalInj1CycleId,
            EvalTime.AddMinutes(-6), EvalTime.AddMinutes(-6), null, 1002L, null, IotPlcQualityStates.Good,
            IotPlcEvidenceStates.Simulated, "1002", provenance, actorSubjectId, seedTimestampUtc, cancellationToken);

        await EnsureObservationAsync(dbContext, DevelopmentPilotIds.IotPlc2PilotScenarioObsSeq4Id, tenantId, facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId, DevelopmentPilotIds.IotPlcPilotSignalInj1CycleId,
            EvalTime.AddMinutes(-4), EvalTime.AddMinutes(-4), null, 1004L, null, IotPlcQualityStates.Good,
            IotPlcEvidenceStates.Simulated, "1004", provenance, actorSubjectId, seedTimestampUtc, cancellationToken);

        await EnsureObservationAsync(dbContext, DevelopmentPilotIds.IotPlc2PilotScenarioObsConflict1Id, tenantId, facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId, DevelopmentPilotIds.IotPlcPilotSignalChillerPowerId,
            EvalTime.AddMinutes(-15), EvalTime.AddMinutes(-15), null, null, 10.5m, IotPlcQualityStates.SourceConflict,
            IotPlcEvidenceStates.SourceConflict, "PILOT2-CONFLICT-1", provenance, actorSubjectId, seedTimestampUtc, cancellationToken);

        await EnsureObservationAsync(dbContext, DevelopmentPilotIds.IotPlc2PilotScenarioObsConflict2Id, tenantId, facilityId,
            DevelopmentPilotIds.IotPlcPilotEndpointOpcId, DevelopmentPilotIds.IotPlcPilotSignalChillerPowerId,
            EvalTime.AddMinutes(-12), EvalTime.AddMinutes(-12), null, null, 99.9m, IotPlcQualityStates.SourceConflict,
            IotPlcEvidenceStates.SourceConflict, "PILOT2-CONFLICT-2", provenance, actorSubjectId, seedTimestampUtc, cancellationToken);
    }

    private static async Task EnsureObservationAsync(
        NevaxisZentraDbContext dbContext,
        Guid id,
        Guid tenantId,
        Guid facilityId,
        Guid endpointId,
        Guid signalId,
        DateTimeOffset observedAtUtc,
        DateTimeOffset receivedAtUtc,
        bool? booleanValue,
        long? integerValue,
        decimal? decimalValue,
        string qualityState,
        string evidenceState,
        string sourceSequenceReference,
        string sourceProvenance,
        string actorSubjectId,
        DateTimeOffset seedTimestampUtc,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.IotPlcSignalObservations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (existing is not null)
        {
            existing.ObservedAtUtc = observedAtUtc;
            existing.ReceivedAtUtc = receivedAtUtc;
            existing.IntegerValue = integerValue;
            existing.DecimalValue = decimalValue;
            existing.BooleanValue = booleanValue;
            existing.QualityState = qualityState;
            existing.EvidenceState = evidenceState;
            existing.SourceSequenceReference = sourceSequenceReference;
            existing.SourceProvenance = sourceProvenance;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        dbContext.IotPlcSignalObservations.Add(new IotPlcSignalObservation
        {
            Id = id,
            TenantId = tenantId,
            FacilityId = facilityId,
            EndpointId = endpointId,
            SignalDefinitionId = signalId,
            MappedAssetType = IotPlcAssetTypes.Machine,
            MappedCanonicalAssetId = DevelopmentPilotIds.EdgePilotPressMachineId,
            ObservedAtUtc = observedAtUtc,
            ReceivedAtUtc = receivedAtUtc,
            BooleanValue = booleanValue,
            IntegerValue = integerValue,
            DecimalValue = decimalValue,
            QualityState = qualityState,
            EvidenceState = evidenceState,
            SourceSequenceReference = sourceSequenceReference,
            SourceProvenance = sourceProvenance,
            CreatedAtUtc = seedTimestampUtc,
            CreatedBy = actorSubjectId,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
