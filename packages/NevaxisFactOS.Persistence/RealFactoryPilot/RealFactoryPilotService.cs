using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.Integration;
using NevaxisFactOS.Persistence.MixedProduction;
using NevaxisFactOS.Persistence.Tenancy;
using NevaxisFactOS.Persistence.ThroughputIntelligence;
using NevaxisFactOS.Persistence.ThroughputOptimizer;

namespace NevaxisFactOS.Persistence.RealFactoryPilot;

public interface IRealFactoryPilotService
{
    Task<RealFactoryPilotWorkspace> GetWorkspaceAsync(Guid facilityId, Guid pilotRunId, CancellationToken cancellationToken = default);
    Task<RealFactoryPilotWorkspace> InitializePilotAsync(Guid facilityId, Guid tenantId, string subjectId, CancellationToken cancellationToken = default);
    Task<RealFactoryGoldenPilotResult> RunGoldenPilotAsync(Guid facilityId, Guid tenantId, string subjectId, CancellationToken cancellationToken = default);
}

public sealed class RealFactoryPilotService : IRealFactoryPilotService
{
    private readonly NevaxisFactOSDbContext db;
    private readonly TenantDatabaseSession tenantDatabaseSession;
    private readonly IMixedProductionService mixedProduction;
    private readonly IThroughputIntelligenceService throughputIntelligence;
    private readonly IThroughputOptimizerService throughputOptimizer;
    private readonly IDigitalWorkforceRuntimeService workforceRuntime;

    public RealFactoryPilotService(
        NevaxisFactOSDbContext db,
        TenantDatabaseSession tenantDatabaseSession,
        IMixedProductionService mixedProduction,
        IThroughputIntelligenceService throughputIntelligence,
        IThroughputOptimizerService throughputOptimizer,
        IDigitalWorkforceRuntimeService workforceRuntime)
    {
        this.db = db;
        this.tenantDatabaseSession = tenantDatabaseSession;
        this.mixedProduction = mixedProduction;
        this.throughputIntelligence = throughputIntelligence;
        this.throughputOptimizer = throughputOptimizer;
        this.workforceRuntime = workforceRuntime;
    }

    public Task<RealFactoryPilotWorkspace> GetWorkspaceAsync(
        Guid facilityId, Guid pilotRunId, CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetWorkspaceCoreAsync(facilityId, pilotRunId, ct),
            cancellationToken);

    public Task<RealFactoryPilotWorkspace> InitializePilotAsync(
        Guid facilityId, Guid tenantId, string subjectId, CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => InitializePilotCoreAsync(facilityId, tenantId, subjectId, ct),
            cancellationToken);

    public Task<RealFactoryGoldenPilotResult> RunGoldenPilotAsync(
        Guid facilityId, Guid tenantId, string subjectId, CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RunGoldenPilotCoreAsync(facilityId, tenantId, subjectId, ct),
            cancellationToken);

    private async Task<RealFactoryPilotWorkspace> InitializePilotCoreAsync(
        Guid facilityId, Guid tenantId, string subjectId, CancellationToken cancellationToken)
    {
        var seed = await RealFactoryPilotFixtureSeeder.EnsurePilotFactoryAsync(
            db, tenantId, facilityId, subjectId, cancellationToken);

        var run = await db.RealFactoryPilotRunRecords
            .FirstOrDefaultAsync(r => r.FacilityId == facilityId && r.LineId == seed.LineId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (run is null)
        {
            run = new RealFactoryPilotRunRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                LineId = seed.LineId,
                LineCode = seed.LineCode,
                CurrentPhase = RealFactoryPilotPhases.Connect,
                AcceptanceKind = RealFactoryPilotAcceptanceKinds.Synthetic,
                ConnectorStateJson = JsonSerializer.Serialize(new { seed.PlcConnectorId, seed.MesConnectorId }),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
            db.RealFactoryPilotRunRecords.Add(run);
        }

        run.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        return await BuildWorkspaceAsync(run, cancellationToken);
    }

    private async Task<RealFactoryGoldenPilotResult> RunGoldenPilotCoreAsync(
        Guid facilityId, Guid tenantId, string subjectId, CancellationToken cancellationToken)
    {
        var workspace = await InitializePilotCoreAsync(facilityId, tenantId, subjectId, cancellationToken);
        var run = await db.RealFactoryPilotRunRecords.FirstAsync(r => r.Id == workspace.PilotRunId, cancellationToken);
        var connectorState = JsonSerializer.Deserialize<ConnectorState>(run.ConnectorStateJson)!;

        await DiscoverAsync(run, connectorState, cancellationToken);
        run.CurrentPhase = RealFactoryPilotPhases.Map;
        await ValidateAndActivateMappingsAsync(run, connectorState, cancellationToken);
        run.CurrentPhase = RealFactoryPilotPhases.Observe;

        await ActivateConnectorsAsync(connectorState, cancellationToken);
        await RealFactoryPilotFixtureSeeder.IngestBaselineEventsAsync(
            db, mixedProduction, tenantId, facilityId, run.LineId, cancellationToken);

        var eventCount = await db.CanonicalProductionEvents.CountAsync(
            e => e.LineId == run.LineId, cancellationToken);
        var lastEvent = await db.CanonicalProductionEvents
            .Where(e => e.LineId == run.LineId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Select(e => e.OccurredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var plc = await db.IntegrationConnectors.FirstAsync(c => c.Id == connectorState.PlcConnectorId, cancellationToken);
        var mes = await db.IntegrationConnectors.FirstAsync(c => c.Id == connectorState.MesConnectorId, cancellationToken);
        plc.LastSuccessAtUtc = DateTimeOffset.UtcNow;
        plc.HealthStatus = IntegrationHealthStatuses.Healthy;
        mes.LastSuccessAtUtc = DateTimeOffset.UtcNow;
        mes.HealthStatus = IntegrationHealthStatuses.Healthy;

        var quality = RealFactoryDataQualityEngine.Assess(plc, mes, eventCount, lastEvent, 0, 0, DateTimeOffset.UtcNow);
        db.RealFactoryDataQualityRecords.Add(new RealFactoryDataQualityRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            LineId = run.LineId,
            ConnectorId = plc.Id,
            OverallState = quality.OverallState,
            AssessmentJson = JsonSerializer.Serialize(quality),
            BlocksAutonomousAction = quality.BlocksAutonomousAction,
            AssessedAtUtc = DateTimeOffset.UtcNow,
        });

        if (quality.BlocksAutonomousAction)
        {
            throw new RealFactoryPilotException(
                RealFactoryPilotFailureCodes.DataQualityBlocked,
                "Pilot blocked: data quality insufficient.");
        }

        run.CurrentPhase = RealFactoryPilotPhases.Shadow;
        await EnableShadowAsync(run, tenantId, facilityId, cancellationToken);

        var overview = await throughputIntelligence.GetLineOverviewAsync(
            facilityId, run.LineId, "recent", null, null, cancellationToken);
        var constraint = overview.Constraints.FirstOrDefault(c => c.Role == "CURRENT");

        var optimizerWorkspace = await throughputOptimizer.ObserveLineAsync(
            facilityId, tenantId, run.LineId, subjectId, cancellationToken);
        run.DigitalWorkCaseId = optimizerWorkspace.CaseId;

        while (optimizerWorkspace.CurrentPhase != ThroughputOptimizerPhases.Closed)
        {
            if (optimizerWorkspace.CurrentPhase == ThroughputOptimizerPhases.Execute)
            {
                await DisableShadowPhysicalBlockAsync(run, cancellationToken);
            }

            optimizerWorkspace = await throughputOptimizer.RunLifecycleStepAsync(
                facilityId, tenantId, optimizerWorkspace.CaseId, subjectId, cancellationToken);

            if (optimizerWorkspace.CurrentPhase == ThroughputOptimizerPhases.Approval && optimizerWorkspace.HasPendingApproval)
            {
                run.CurrentPhase = RealFactoryPilotPhases.Approve;
                await AdvanceShadowModeAsync(run, RealFactoryShadowModes.ControlledApproval, cancellationToken);
                var approval = (await workforceRuntime.ListApprovalsAsync(DigitalWorkApprovalStatuses.Pending, cancellationToken))
                    .First(a => a.CaseId == optimizerWorkspace.CaseId);
                await workforceRuntime.DecideApprovalAsync(
                    approval.Id,
                    new ApprovalDecisionRequest("approved", "Golden pilot approval", approval.ConcurrencyToken),
                    subjectId,
                    cancellationToken);
                await DisableShadowPhysicalBlockAsync(run, cancellationToken);
                optimizerWorkspace = await throughputOptimizer.RunLifecycleStepAsync(
                    facilityId, tenantId, optimizerWorkspace.CaseId, subjectId, cancellationToken);
            }

            if (optimizerWorkspace.CurrentPhase == ThroughputOptimizerPhases.ObserveResult)
            {
                run.CurrentPhase = RealFactoryPilotPhases.ObserveResult;
                await RealFactoryPilotFixtureSeeder.IngestPostInterventionEventsAsync(
                    db, mixedProduction, tenantId, facilityId, run.LineId, cancellationToken);
                optimizerWorkspace = await throughputOptimizer.RunLifecycleStepAsync(
                    facilityId, tenantId, optimizerWorkspace.CaseId, subjectId, cancellationToken);
            }
        }

        run.CurrentPhase = RealFactoryPilotPhases.Closed;
        run.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await DeactivateShadowSessionAsync(run.LineId, cancellationToken);

        var postOverview = await throughputIntelligence.GetLineOverviewAsync(
            facilityId, run.LineId, "recent", null, null, cancellationToken);
        var accuracy = RealFactoryShadowModeEngine.Compare(
            constraint?.StationCode ?? "ST-30",
            postOverview.Constraints.FirstOrDefault(c => c.Role == "CURRENT")?.StationCode,
            overview.Measurement.GoodUnitsPerHour + 20m,
            postOverview.Measurement.GoodUnitsPerHour,
            15m,
            postOverview.Measurement.GoodUnitsPerHour > overview.Measurement.GoodUnitsPerHour
                ? (postOverview.Measurement.GoodUnitsPerHour - overview.Measurement.GoodUnitsPerHour) / overview.Measurement.GoodUnitsPerHour * 100m
                : 0m);

        if (!await db.RealFactoryShadowAccuracyRecords.AnyAsync(
                a => a.PilotRunId == run.Id, cancellationToken))
        {
            db.RealFactoryShadowAccuracyRecords.Add(new RealFactoryShadowAccuracyRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                PilotRunId = run.Id,
                PredictedConstraintStation = accuracy.PredictedConstraintStation,
                ObservedConstraintStation = accuracy.ObservedConstraintStation,
                PredictedGoodThroughputPerHour = accuracy.PredictedGoodThroughputPerHour,
                ActualGoodThroughputPerHour = accuracy.ActualGoodThroughputPerHour,
                PredictedDeltaPercent = accuracy.PredictedDeltaPercent,
                ActualDeltaPercent = accuracy.ActualDeltaPercent,
                ConstraintMatched = accuracy.ConstraintMatched,
                ThroughputWithinTolerance = accuracy.ThroughputWithinTolerance,
                RecordedAtUtc = DateTimeOffset.UtcNow,
            });
        }

        var knowledgeCreated = await db.FactoryKnowledgeRecords
            .AnyAsync(k => k.SourceCaseId == optimizerWorkspace.CaseId, cancellationToken);

        var evidencePack = await GenerateEvidencePackAsync(
            run, tenantId, facilityId, quality, overview, postOverview,
            optimizerWorkspace.Improvement?.VerificationOutcome ?? "UNKNOWN",
            knowledgeCreated,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return new RealFactoryGoldenPilotResult(
            run.Id,
            run.LineId,
            run.CurrentPhase,
            RealFactoryPilotAcceptanceKinds.Synthetic,
            optimizerWorkspace.Improvement?.VerificationOutcome ?? "UNKNOWN",
            knowledgeCreated,
            evidencePack is not null,
            "SYNTHETIC PILOT PASS — Golden pilot completed.");
    }

    private async Task DiscoverAsync(
        RealFactoryPilotRunRecord run, ConnectorState connectorState, CancellationToken cancellationToken)
    {
        run.CurrentPhase = RealFactoryPilotPhases.Discover;
        var now = DateTimeOffset.UtcNow;
        var plcFields = RealFactoryDiscoveryEngine.DiscoverPlcFields();
        var mesFields = RealFactoryDiscoveryEngine.DiscoverMesFields();

        db.RealFactoryDiscoveryRecords.Add(new RealFactoryDiscoveryRecord
        {
            Id = Guid.NewGuid(),
            TenantId = run.TenantId,
            FacilityId = run.FacilityId,
            ConnectorId = connectorState.PlcConnectorId,
            DiscoveryJson = RealFactoryDiscoveryEngine.ToJson(plcFields),
            DiscoveredAtUtc = now,
        });
        db.RealFactoryDiscoveryRecords.Add(new RealFactoryDiscoveryRecord
        {
            Id = Guid.NewGuid(),
            TenantId = run.TenantId,
            FacilityId = run.FacilityId,
            ConnectorId = connectorState.MesConnectorId,
            DiscoveryJson = RealFactoryDiscoveryEngine.ToJson(mesFields),
            DiscoveredAtUtc = now,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateAndActivateMappingsAsync(
        RealFactoryPilotRunRecord run, ConnectorState connectorState, CancellationToken cancellationToken)
    {
        run.CurrentPhase = RealFactoryPilotPhases.Validate;
        var mappings = await db.RealFactoryFieldMappingRecords
            .Where(m => m.ConnectorId == connectorState.PlcConnectorId || m.ConnectorId == connectorState.MesConnectorId)
            .ToListAsync(cancellationToken);

        var rules = mappings.Select(m => new FactoryFieldMappingRule(
            m.SourceField, m.SourceType, m.TargetEntity, m.TargetEntityId,
            m.CanonicalEventType, m.Transformation, m.Unit, m.Scale, m.QualityRule, m.TimestampRule)).ToList();

        var (isValid, errors) = RealFactoryFieldMappingEngine.Validate(rules);
        if (!isValid)
        {
            throw new RealFactoryPilotException(
                RealFactoryPilotFailureCodes.MappingInvalid,
                string.Join("; ", errors));
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var mapping in mappings)
        {
            mapping.Status = RealFactoryMappingStatuses.Active;
            mapping.ActivatedAtUtc = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ActivateConnectorsAsync(ConnectorState connectorState, CancellationToken cancellationToken)
    {
        var connectors = await db.IntegrationConnectors
            .Where(c => c.Id == connectorState.PlcConnectorId || c.Id == connectorState.MesConnectorId)
            .ToListAsync(cancellationToken);

        foreach (var connector in connectors)
        {
            connector.Enabled = true;
            connector.Status = IntegrationConnectorStatuses.Enabled;
            connector.HealthStatus = IntegrationHealthStatuses.Healthy;
            connector.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnableShadowAsync(
        RealFactoryPilotRunRecord run, Guid tenantId, Guid facilityId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var session = await db.RealFactoryShadowSessionRecords
            .FirstOrDefaultAsync(s => s.LineId == run.LineId, cancellationToken);

        if (session is null)
        {
            db.RealFactoryShadowSessionRecords.Add(new RealFactoryShadowSessionRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                LineId = run.LineId,
                PilotRunId = run.Id,
                Mode = RealFactoryShadowModes.ShadowIntelligence,
                IsActive = true,
                BlocksPhysicalActions = true,
                StartedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }
        else
        {
            session.PilotRunId = run.Id;
            session.Mode = RealFactoryShadowModes.ShadowIntelligence;
            session.IsActive = true;
            session.BlocksPhysicalActions = true;
            session.RecommendationsJson = "[]";
            session.StartedAtUtc = now;
            session.EndedAtUtc = null;
            session.UpdatedAtUtc = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task DeactivateShadowSessionAsync(
        Guid lineId, CancellationToken cancellationToken)
    {
        var session = await db.RealFactoryShadowSessionRecords
            .FirstOrDefaultAsync(s => s.LineId == lineId && s.IsActive, cancellationToken);
        if (session is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        session.IsActive = false;
        session.BlocksPhysicalActions = false;
        session.EndedAtUtc = now;
        session.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task AdvanceShadowModeAsync(
        RealFactoryPilotRunRecord run, string mode, CancellationToken cancellationToken)
    {
        var session = await db.RealFactoryShadowSessionRecords
            .FirstAsync(s => s.LineId == run.LineId && s.IsActive, cancellationToken);
        session.Mode = mode;
        session.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task DisableShadowPhysicalBlockAsync(
        RealFactoryPilotRunRecord run, CancellationToken cancellationToken)
    {
        var session = await db.RealFactoryShadowSessionRecords
            .FirstAsync(s => s.LineId == run.LineId && s.IsActive, cancellationToken);
        session.Mode = RealFactoryShadowModes.LimitedAction;
        session.BlocksPhysicalActions = false;
        session.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<RealFactoryPilotEvidencePackRecord?> GenerateEvidencePackAsync(
        RealFactoryPilotRunRecord run, Guid tenantId, Guid facilityId,
        FactoryDataQualityAssessmentResult quality,
        ThroughputIntelligenceOverview baselineOverview,
        ThroughputIntelligenceOverview postOverview,
        string verificationOutcome,
        bool knowledgePromoted,
        CancellationToken cancellationToken)
    {
        if (await db.RealFactoryPilotEvidencePackRecords.AnyAsync(p => p.PilotRunId == run.Id, cancellationToken))
        {
            return await db.RealFactoryPilotEvidencePackRecords.FirstAsync(p => p.PilotRunId == run.Id, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var pack = new RealFactoryPilotEvidencePackRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = facilityId,
            PilotRunId = run.Id,
            LineId = run.LineId,
            FacilityCode = RealFactoryPilotFixtureCodes.FactoryCode,
            LineCode = run.LineCode,
            PilotPeriodStartUtc = run.CreatedAtUtc,
            PilotPeriodEndUtc = now,
            VerificationOutcome = verificationOutcome,
            KnowledgePromoted = knowledgePromoted,
            GeneratedAtUtc = now,
            PackJson = RealFactoryPilotEvidenceEngine.BuildPackJson(
                RealFactoryPilotFixtureCodes.FactoryCode,
                run.LineCode,
                run.CreatedAtUtc,
                now,
                quality,
                baselineOverview.Measurement.GoodUnitsPerHour,
                postOverview.Measurement.GoodUnitsPerHour,
                baselineOverview.Constraints.FirstOrDefault(c => c.Role == "CURRENT")?.StationCode,
                ["operator-reassignment"],
                verificationOutcome,
                [run.Id.ToString()]),
        };
        db.RealFactoryPilotEvidencePackRecords.Add(pack);
        return pack;
    }

    private async Task<RealFactoryPilotWorkspace> GetWorkspaceCoreAsync(
        Guid facilityId, Guid pilotRunId, CancellationToken cancellationToken)
    {
        var run = await db.RealFactoryPilotRunRecords
            .FirstOrDefaultAsync(r => r.FacilityId == facilityId && r.Id == pilotRunId, cancellationToken)
            ?? throw new RealFactoryPilotException(RealFactoryPilotFailureCodes.NotFound, "Pilot run not found.");
        return await BuildWorkspaceAsync(run, cancellationToken);
    }

    private async Task<RealFactoryPilotWorkspace> BuildWorkspaceAsync(
        RealFactoryPilotRunRecord run, CancellationToken cancellationToken)
    {
        var connectorState = JsonSerializer.Deserialize<ConnectorState>(run.ConnectorStateJson);
        var connectors = new List<ConnectorWorkspaceSnapshot>();
        if (connectorState is not null)
        {
            foreach (var id in new[] { connectorState.PlcConnectorId, connectorState.MesConnectorId })
            {
                var connector = await db.IntegrationConnectors.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
                if (connector is null)
                {
                    continue;
                }

                var mappingStatus = await db.RealFactoryFieldMappingRecords.AsNoTracking()
                    .Where(m => m.ConnectorId == id)
                    .Select(m => m.Status)
                    .FirstOrDefaultAsync(cancellationToken) ?? RealFactoryMappingStatuses.Draft;

                var eventsIngested = await db.CanonicalProductionEvents.CountAsync(
                    e => e.LineId == run.LineId, cancellationToken);

                connectors.Add(new ConnectorWorkspaceSnapshot(
                    connector.Id,
                    connector.Name,
                    connector.ConnectorKind,
                    connector.SourceSystemName,
                    connector.HealthStatus,
                    mappingStatus,
                    connector.LastSuccessAtUtc,
                    eventsIngested));
            }
        }

        var qualityRow = await db.RealFactoryDataQualityRecords.AsNoTracking()
            .Where(q => q.LineId == run.LineId)
            .OrderByDescending(q => q.AssessedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        FactoryDataQualityAssessmentResult? quality = null;
        if (qualityRow is not null)
        {
            quality = JsonSerializer.Deserialize<FactoryDataQualityAssessmentResult>(qualityRow.AssessmentJson);
        }

        var shadowRow = await db.RealFactoryShadowSessionRecords.AsNoTracking()
            .FirstOrDefaultAsync(s => s.LineId == run.LineId && s.IsActive, cancellationToken);

        FactoryShadowSessionSnapshot? shadow = shadowRow is null
            ? null
            : new FactoryShadowSessionSnapshot(
                shadowRow.Id, shadowRow.LineId, shadowRow.Mode,
                shadowRow.IsActive, shadowRow.BlocksPhysicalActions, shadowRow.StartedAtUtc);

        var accuracyRow = await db.RealFactoryShadowAccuracyRecords.AsNoTracking()
            .FirstOrDefaultAsync(a => a.PilotRunId == run.Id, cancellationToken);

        FactoryShadowAccuracySnapshot? accuracy = accuracyRow is null
            ? null
            : new FactoryShadowAccuracySnapshot(
                accuracyRow.PredictedConstraintStation,
                accuracyRow.ObservedConstraintStation,
                accuracyRow.PredictedGoodThroughputPerHour,
                accuracyRow.ActualGoodThroughputPerHour,
                accuracyRow.PredictedDeltaPercent,
                accuracyRow.ActualDeltaPercent,
                accuracyRow.ConstraintMatched,
                accuracyRow.ThroughputWithinTolerance);

        var evidenceRow = await db.RealFactoryPilotEvidencePackRecords.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PilotRunId == run.Id, cancellationToken);

        PilotEvidencePackSnapshot? evidence = evidenceRow is null
            ? null
            : new PilotEvidencePackSnapshot(
                evidenceRow.Id,
                evidenceRow.FacilityCode,
                evidenceRow.LineCode,
                evidenceRow.PilotPeriodStartUtc,
                evidenceRow.PilotPeriodEndUtc,
                quality?.OverallState ?? RealFactoryDataQualityStates.Insufficient,
                0m,
                null,
                null,
                evidenceRow.VerificationOutcome,
                evidenceRow.KnowledgePromoted,
                evidenceRow.PackJson);

        return new RealFactoryPilotWorkspace(
            run.Id,
            run.LineId,
            run.LineCode,
            run.CurrentPhase,
            run.AcceptanceKind,
            connectors,
            quality,
            shadow,
            run.DigitalWorkCaseId,
            accuracy,
            evidence,
            run.UpdatedAtUtc);
    }

    private sealed record ConnectorState(Guid PlcConnectorId, Guid MesConnectorId);
}
