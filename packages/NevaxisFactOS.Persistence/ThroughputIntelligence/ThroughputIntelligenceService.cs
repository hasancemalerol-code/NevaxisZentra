using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.MixedProduction;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.ThroughputIntelligence;

public interface IThroughputIntelligenceService
{
    Task<ThroughputIntelligenceOverview> GetLineOverviewAsync(
        Guid facilityId,
        Guid lineId,
        string? analysisWindow,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken = default);

    Task<ScenarioMetricSnapshot> RunScenarioAsync(
        Guid facilityId,
        Guid lineId,
        RunThroughputScenarioRequest request,
        Guid tenantId,
        string subjectId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScenarioComparisonRow>> CompareScenariosAsync(
        Guid facilityId,
        Guid lineId,
        CancellationToken cancellationToken = default);

    Task<ThroughputIntelligenceSeedResult> SeedMixLine01Mp2ExtensionAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public sealed record RunThroughputScenarioRequest(
    string ScenarioCode,
    string ScenarioType,
    string Title,
    string AnalysisWindow,
    DateTimeOffset? WindowStartUtc,
    DateTimeOffset? WindowEndUtc,
    IReadOnlyList<ScenarioIntervention> Interventions,
    IReadOnlyList<string>? Assumptions,
    IReadOnlyList<string>? Guardrails,
    bool Persist = true);

public sealed record ThroughputIntelligenceSeedResult(
    Guid LineId,
    int EventsSeeded,
    int SkillsSeeded,
    int QualificationsSeeded,
    string Message);

public sealed class ThroughputIntelligenceService : IThroughputIntelligenceService
{
    private static readonly string[] DefaultRequestedOutputs =
    [
        "good-throughput", "constraint-movement", "quality", "wip", "risk",
    ];

    private readonly NevaxisFactOSDbContext db;
    private readonly TenantDatabaseSession tenantDatabaseSession;

    public ThroughputIntelligenceService(
        NevaxisFactOSDbContext db,
        TenantDatabaseSession tenantDatabaseSession)
    {
        this.db = db;
        this.tenantDatabaseSession = tenantDatabaseSession;
    }

    public Task<ThroughputIntelligenceOverview> GetLineOverviewAsync(
        Guid facilityId,
        Guid lineId,
        string? analysisWindow,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetLineOverviewCoreAsync(facilityId, lineId, analysisWindow, fromUtc, toUtc, ct),
            cancellationToken);

    private async Task<ThroughputIntelligenceOverview> GetLineOverviewCoreAsync(
        Guid facilityId,
        Guid lineId,
        string? analysisWindow,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        var context = await BuildAnalysisContextAsync(
            facilityId, lineId, analysisWindow, fromUtc, toUtc, cancellationToken);
        return ThroughputIntelligenceOrchestrator.Analyze(context);
    }

    public Task<ScenarioMetricSnapshot> RunScenarioAsync(
        Guid facilityId,
        Guid lineId,
        RunThroughputScenarioRequest request,
        Guid tenantId,
        string subjectId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RunScenarioCoreAsync(facilityId, lineId, request, tenantId, subjectId, ct),
            cancellationToken);

    private async Task<ScenarioMetricSnapshot> RunScenarioCoreAsync(
        Guid facilityId,
        Guid lineId,
        RunThroughputScenarioRequest request,
        Guid tenantId,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var context = await BuildAnalysisContextAsync(
            facilityId,
            lineId,
            request.AnalysisWindow,
            request.WindowStartUtc,
            request.WindowEndUtc,
            cancellationToken);

        var overview = ThroughputIntelligenceOrchestrator.Analyze(context);
        var stationCodes = context.Stations.ToDictionary(s => s.Id, s => s.Code);
        var cycles = StationCycleTimeEngine.ComputeAll(context, stationCodes);
        var baselineStates = SimulationStateBuilder.BuildBaseline(context, cycles);

        var snapshot = ScenarioSimulationEngine.Simulate(
            context,
            baselineStates,
            request.Interventions,
            overview.Measurement,
            overview.Constraints,
            overview.OperatorSkills);

        if (request.Persist)
        {
            var now = DateTimeOffset.UtcNow;
            var definition = new ThroughputScenarioDefinition
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                LineId = lineId,
                ScenarioCode = request.ScenarioCode,
                ScenarioType = request.ScenarioType,
                Title = request.Title,
                AnalysisWindow = request.AnalysisWindow,
                WindowStartUtc = context.WindowStartUtc,
                WindowEndUtc = context.WindowEndUtc,
                InterventionsJson = JsonSerializer.Serialize(request.Interventions),
                AssumptionsJson = JsonSerializer.Serialize(request.Assumptions ?? Array.Empty<string>()),
                GuardrailsJson = JsonSerializer.Serialize(request.Guardrails ?? Array.Empty<string>()),
                RequestedOutputsJson = JsonSerializer.Serialize(DefaultRequestedOutputs),
                CreatedBySubjectId = subjectId,
                CreatedAtUtc = now,
            };

            db.ThroughputScenarioDefinitions.Add(definition);
            db.ThroughputScenarioResults.Add(new ThroughputScenarioResult
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                ScenarioDefinitionId = definition.Id,
                Eligibility = snapshot.Eligibility,
                DataQuality = snapshot.DataQuality,
                ResultJson = JsonSerializer.Serialize(snapshot),
                EngineVersion = ThroughputIntelligenceEngineVersions.Scenario,
                EventsConsidered = context.Events.Count,
                StationsEvaluated = context.Stations.Count,
                AnalysisDurationMs = overview.AnalysisDurationMs,
                ComputedAtUtc = now,
            });

            await db.SaveChangesAsync(cancellationToken);
        }

        return snapshot;
    }

    public Task<IReadOnlyList<ScenarioComparisonRow>> CompareScenariosAsync(
        Guid facilityId,
        Guid lineId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => CompareScenariosCoreAsync(facilityId, lineId, ct),
            cancellationToken);

    private async Task<IReadOnlyList<ScenarioComparisonRow>> CompareScenariosCoreAsync(
        Guid facilityId,
        Guid lineId,
        CancellationToken cancellationToken)
    {
        var definitions = await db.ThroughputScenarioDefinitions
            .Where(s => s.FacilityId == facilityId && s.LineId == lineId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        var results = new List<ScenarioComparisonRow>();
        foreach (var definition in definitions)
        {
            var result = await db.ThroughputScenarioResults
                .Where(r => r.ScenarioDefinitionId == definition.Id)
                .OrderByDescending(r => r.ComputedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (result is null) continue;

            var snapshot = JsonSerializer.Deserialize<ScenarioMetricSnapshot>(result.ResultJson);
            if (snapshot is null) continue;

            results.Add(new ScenarioComparisonRow(
                definition.ScenarioCode,
                definition.ScenarioType,
                definition.Title,
                snapshot.PredictedGoodThroughputPerHour,
                snapshot.DeltaGoodThroughputPerHour,
                snapshot.ConstraintBefore?.StationCode,
                snapshot.ConstraintAfter?.StationCode,
                snapshot.QualityImpactScore,
                snapshot.OperationalRiskScore,
                snapshot.Eligibility,
                snapshot.DataQuality));
        }

        return results;
    }

    public Task<ThroughputIntelligenceSeedResult> SeedMixLine01Mp2ExtensionAsync(
        Guid facilityId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ThroughputIntelligenceFixtureSeeder.SeedMixLine01Mp2ExtensionAsync(
                db, tenantId, facilityId, ct),
            cancellationToken);

    private async Task<ThroughputAnalysisContext> BuildAnalysisContextAsync(
        Guid facilityId,
        Guid lineId,
        string? analysisWindow,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        var line = await db.ProductionLines.FirstOrDefaultAsync(
            l => l.FacilityId == facilityId && l.Id == lineId, cancellationToken)
            ?? throw new ThroughputIntelligenceException(
                ThroughputIntelligenceFailureCodes.NotFound, "Line not found.");

        var stations = await db.ProductionStations
            .Where(s => s.LineId == lineId)
            .OrderBy(s => s.SequenceOrder)
            .ToListAsync(cancellationToken);

        var resources = await db.ManufacturingResources
            .Where(r => stations.Select(s => s.Id).Contains(r.StationId))
            .ToListAsync(cancellationToken);

        var window = ResolveWindow(analysisWindow, fromUtc, toUtc);
        var events = await db.CanonicalProductionEvents
            .Where(e => e.LineId == lineId
                && e.OccurredAtUtc >= window.Start
                && e.OccurredAtUtc <= window.End)
            .OrderBy(e => e.OccurredAtUtc)
            .Take(5000)
            .ToListAsync(cancellationToken);

        var buffers = await db.BufferResourceStates
            .Where(b => resources.Select(r => r.Id).Contains(b.ResourceId))
            .ToListAsync(cancellationToken);

        var target = await db.LineThroughputTargets
            .Where(t => t.LineId == lineId && t.IsActive)
            .OrderByDescending(t => t.EffectiveFromUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var skills = await db.OperatorSkillProfiles
            .Where(s => s.FacilityId == facilityId && s.IsActive)
            .ToListAsync(cancellationToken);

        var qualifications = await db.OperatorStationQualifications
            .Where(q => q.FacilityId == facilityId && q.IsActive)
            .ToListAsync(cancellationToken);

        return new ThroughputAnalysisContext(
            line,
            stations,
            resources.GroupBy(r => r.StationId).ToDictionary(g => g.Key, g => g.First()),
            buffers.ToDictionary(b => b.ResourceId),
            events,
            target,
            skills,
            qualifications,
            window.Start,
            window.End,
            window.Label);
    }

    private static (DateTimeOffset Start, DateTimeOffset End, string Label) ResolveWindow(
        string? analysisWindow,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc)
    {
        var window = analysisWindow ?? ThroughputIntelligenceAnalysisWindows.Recent;
        var now = DateTimeOffset.UtcNow;

        return window switch
        {
            ThroughputIntelligenceAnalysisWindows.Explicit when fromUtc.HasValue && toUtc.HasValue =>
                (fromUtc.Value, toUtc.Value, ThroughputIntelligenceAnalysisWindows.Explicit),
            ThroughputIntelligenceAnalysisWindows.Shift =>
                (now.AddHours(-8), now, ThroughputIntelligenceAnalysisWindows.Shift),
            ThroughputIntelligenceAnalysisWindows.Order =>
                (now.AddHours(-12), now, ThroughputIntelligenceAnalysisWindows.Order),
            ThroughputIntelligenceAnalysisWindows.Current =>
                (now.AddMinutes(-15), now, ThroughputIntelligenceAnalysisWindows.Current),
            _ => (now.AddHours(-1), now, ThroughputIntelligenceAnalysisWindows.Recent),
        };
    }
}
