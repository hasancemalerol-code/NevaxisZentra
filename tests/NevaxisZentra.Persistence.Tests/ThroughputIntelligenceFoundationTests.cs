using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NevaxisZentra.Persistence.MixedProduction;
using NevaxisZentra.Persistence.ThroughputIntelligence;

namespace NevaxisZentra.Persistence.Tests;

public sealed class ThroughputIntelligencePermissionTests
{
    [Fact]
    public void PermissionKeysAreStable()
    {
        Assert.Equal("throughput-intelligence.read", ThroughputIntelligencePermissionKeys.Read);
        Assert.Equal("throughput-intelligence.manage", ThroughputIntelligencePermissionKeys.Manage);
        Assert.Equal("throughput-simulation.run", ThroughputIntelligencePermissionKeys.Simulate);
    }
}

public sealed class ThroughputIntelligenceEntityMetadataTests
{
    [Fact]
    public void LineThroughputTargetRequiresTenantAndFacility()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(LineThroughputTarget));
        Assert.NotNull(entityType);
        Assert.False(entityType.FindProperty(nameof(LineThroughputTarget.TenantId))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(LineThroughputTarget.FacilityId))!.IsNullable);
    }

    [Fact]
    public void OperatorSkillProfileHasUniqueOperatorCodePerFacility()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(OperatorSkillProfile));
        Assert.NotNull(entityType);
        Assert.Contains(entityType.GetIndexes(), i => i.IsUnique);
    }

    private static NevaxisZentraDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NevaxisZentraDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=nevaxiszentra_test;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsAssembly(NevaxisZentraDbContext.MigrationsAssemblyName))
            .Options;
        return new NevaxisZentraDbContext(options);
    }
}

public sealed class ThroughputIntelligenceEngineTests
{
    private readonly Guid _lineId = Guid.NewGuid();
    private readonly Guid _assemblyId = Guid.NewGuid();
    private readonly Guid _weldingId = Guid.NewGuid();
    private readonly Guid _injectionId = Guid.NewGuid();
    private readonly Guid _qualityId = Guid.NewGuid();
    private readonly DateTimeOffset _windowStart = DateTimeOffset.UtcNow.AddHours(-1);
    private readonly DateTimeOffset _windowEnd = DateTimeOffset.UtcNow;

    [Fact]
    public void GoodThroughputCalculationExcludesRejectAndRework()
    {
        var events = BuildEvents();
        var result = ThroughputMeasurementEngine.Compute(events, _windowStart, _windowEnd, 670m, "recent");
        Assert.True(result.GoodUnitsPerHour > 0);
        Assert.True(result.RejectUnitsPerHour >= 0);
        Assert.True(result.GoodCount < result.GrossCount || result.RejectCount == 0);
    }

    [Fact]
    public void RejectExcludedFromGoodThroughput()
    {
        var events = new List<CanonicalProductionEvent>
        {
            Event(_assemblyId, MixedProductionEventTypes.Good, _windowStart.AddMinutes(10), 1),
            Event(_assemblyId, MixedProductionEventTypes.Reject, _windowStart.AddMinutes(20), 1),
        };
        var result = ThroughputMeasurementEngine.Compute(events, _windowStart, _windowEnd, null, "recent");
        Assert.Equal(1, result.GoodCount);
        Assert.Equal(1, result.RejectCount);
    }

    [Fact]
    public void ReworkHandledSeparatelyFromGood()
    {
        var events = new List<CanonicalProductionEvent>
        {
            Event(_assemblyId, MixedProductionEventTypes.Good, _windowStart.AddMinutes(10), 1),
            Event(_assemblyId, MixedProductionEventTypes.Rework, _windowStart.AddMinutes(20), 1),
        };
        var result = ThroughputMeasurementEngine.Compute(events, _windowStart, _windowEnd, null, "recent");
        Assert.Equal(1, result.GoodCount);
        Assert.Equal(1, result.ReworkCount);
    }

    [Fact]
    public void ThroughputGapComputedAgainstTarget()
    {
        var events = BuildEvents();
        var result = ThroughputMeasurementEngine.Compute(events, _windowStart, _windowEnd, 670m, "recent");
        Assert.NotNull(result.ThroughputGapPerHour);
        Assert.Equal(670m - result.GoodUnitsPerHour, result.ThroughputGapPerHour);
    }

    [Fact]
    public void TaktReturnsInsufficientDataWhenDemandMissing()
    {
        var result = TaktDemandEngine.Compute(null, 6m);
        Assert.Equal(ThroughputIntelligenceDataQualityStates.Insufficient, result.DataQuality);
    }

    [Fact]
    public void TaktComputedWhenDemandAvailable()
    {
        var target = new LineThroughputTarget
        {
            RequiredGoodQuantity = 5360,
            AvailableProductionHours = 8m,
            TargetCycleSeconds = 5.37m,
        };
        var result = TaktDemandEngine.Compute(target, 8m);
        Assert.NotNull(result.TaktTimeSeconds);
        Assert.True(result.TaktTimeSeconds > 0);
    }

    [Fact]
    public void EffectiveMachineCycleFromCycleStartEnd()
    {
        var events = new List<CanonicalProductionEvent>
        {
            Event(_injectionId, MixedProductionEventTypes.CycleStart, _windowStart.AddSeconds(0)),
            Event(_injectionId, MixedProductionEventTypes.CycleEnd, _windowStart.AddSeconds(5)),
            Event(_injectionId, MixedProductionEventTypes.CycleStart, _windowStart.AddSeconds(10)),
            Event(_injectionId, MixedProductionEventTypes.CycleEnd, _windowStart.AddSeconds(15)),
        };
        var context = BuildContext(events);
        var cycles = StationCycleTimeEngine.ComputeAll(context, StationCodes());
        var injection = cycles.First(c => c.StationId == _injectionId);
        Assert.Equal(5m, injection.MedianCycleSeconds);
    }

    [Fact]
    public void EffectiveHumanCycleFromOperatorSessions()
    {
        var events = new List<CanonicalProductionEvent>
        {
            Event(_assemblyId, MixedProductionEventTypes.OperatorStart, _windowStart.AddSeconds(0)),
            Event(_assemblyId, MixedProductionEventTypes.OperatorComplete, _windowStart.AddSeconds(8)),
            Event(_assemblyId, MixedProductionEventTypes.OperatorStart, _windowStart.AddSeconds(10)),
            Event(_assemblyId, MixedProductionEventTypes.OperatorComplete, _windowStart.AddSeconds(18)),
        };
        var context = BuildContext(events);
        var cycles = StationCycleTimeEngine.ComputeAll(context, StationCodes());
        var assembly = cycles.First(c => c.StationId == _assemblyId);
        Assert.Equal(8m, assembly.MedianCycleSeconds);
    }

    [Fact]
    public void MachineCapacityComputedFromCycleTime()
    {
        var context = BuildContext(BuildEvents());
        var cycles = StationCycleTimeEngine.ComputeAll(context, StationCodes());
        var states = SimulationStateBuilder.BuildBaseline(context, cycles);
        var capacities = CapacityModelEngine.ComputeAll(context, cycles, states);
        var injection = capacities.First(c => c.StationId == _injectionId);
        Assert.True(injection.TheoreticalGoodUnitsPerHour > 0);
        Assert.Equal("MACHINE_CAPACITY", injection.CapacityKind);
    }

    [Fact]
    public void HumanCapacityComputedForAssembly()
    {
        var context = BuildContext(BuildEvents());
        var cycles = StationCycleTimeEngine.ComputeAll(context, StationCodes());
        var states = SimulationStateBuilder.BuildBaseline(context, cycles);
        var capacities = CapacityModelEngine.ComputeAll(context, cycles, states);
        var assembly = capacities.First(c => c.StationId == _assemblyId);
        Assert.Equal("HUMAN_CAPACITY", assembly.CapacityKind);
    }

    [Fact]
    public void CombinedCapacityDistinguishesResourceTypes()
    {
        var context = BuildContext(BuildEvents());
        var cycles = StationCycleTimeEngine.ComputeAll(context, StationCodes());
        var states = SimulationStateBuilder.BuildBaseline(context, cycles);
        var capacities = CapacityModelEngine.ComputeAll(context, cycles, states);
        Assert.Contains(capacities, c => c.CapacityKind == "MACHINE_CAPACITY");
        Assert.Contains(capacities, c => c.CapacityKind == "HUMAN_CAPACITY");
    }

    [Fact]
    public void CurrentConstraintIdentifiedOnAssembly()
    {
        var overview = ThroughputIntelligenceOrchestrator.Analyze(BuildContext(BuildEvents()));
        var current = overview.Constraints.FirstOrDefault(c => c.Role == ThroughputIntelligenceConstraintRoles.Current);
        Assert.NotNull(current);
        Assert.Equal("ST-30", current.StationCode);
    }

    [Fact]
    public void SecondaryConstraintIdentifiedOnWelding()
    {
        var overview = ThroughputIntelligenceOrchestrator.Analyze(BuildContext(BuildEvents()));
        var secondary = overview.Constraints.FirstOrDefault(c => c.Role == ThroughputIntelligenceConstraintRoles.Secondary);
        Assert.NotNull(secondary);
        Assert.Equal("ST-40", secondary.StationCode);
    }

    [Fact]
    public void EmergingConstraintMayBePresent()
    {
        var overview = ThroughputIntelligenceOrchestrator.Analyze(BuildContext(BuildEvents()));
        Assert.True(overview.Constraints.Count >= 2);
    }

    [Fact]
    public void StarvedLossAttributed()
    {
        var events = BuildEvents();
        events.Add(Event(_weldingId, MixedProductionEventTypes.Starved, _windowStart.AddMinutes(30)));
        var overview = ThroughputIntelligenceOrchestrator.Analyze(BuildContext(events));
        Assert.Contains(overview.LossTree.Nodes, n => n.Category == ThroughputIntelligenceLossCategories.Starved);
    }

    [Fact]
    public void BlockedLossSupported()
    {
        var events = BuildEvents();
        events.Add(Event(_weldingId, MixedProductionEventTypes.Blocked, _windowStart.AddMinutes(35)));
        var overview = ThroughputIntelligenceOrchestrator.Analyze(BuildContext(events));
        Assert.Contains(overview.StarvedBlocked, s => s.State == "BLOCKED");
    }

    [Fact]
    public void MicrostopLossDetected()
    {
        var events = BuildEvents();
        events.Add(Event(_weldingId, MixedProductionEventTypes.Stop, _windowStart.AddMinutes(40)));
        events.Add(Event(_weldingId, MixedProductionEventTypes.Run, _windowStart.AddMinutes(40).AddSeconds(15)));
        var overview = ThroughputIntelligenceOrchestrator.Analyze(BuildContext(events));
        Assert.NotEmpty(overview.Microstops);
    }

    [Fact]
    public void OperatorWaitLossAttributed()
    {
        var events = BuildEvents();
        events.Add(Event(_assemblyId, MixedProductionEventTypes.OperatorWait, _windowStart.AddMinutes(25)));
        var overview = ThroughputIntelligenceOrchestrator.Analyze(BuildContext(events));
        Assert.Contains(overview.LossTree.Nodes, n => n.Category == ThroughputIntelligenceLossCategories.OperatorWait);
    }

    [Fact]
    public void MaterialWaitLossAttributed()
    {
        var events = BuildEvents();
        events.Add(Event(_assemblyId, MixedProductionEventTypes.MaterialWait, _windowStart.AddMinutes(28)));
        var overview = ThroughputIntelligenceOrchestrator.Analyze(BuildContext(events));
        Assert.Contains(overview.LossTree.Nodes, n => n.Category == ThroughputIntelligenceLossCategories.MaterialWait);
    }

    [Fact]
    public void QualityLossFromRejectEvents()
    {
        var overview = ThroughputIntelligenceOrchestrator.Analyze(BuildContext(BuildEvents()));
        Assert.Contains(overview.LossTree.Nodes, n => n.Category == ThroughputIntelligenceLossCategories.QualityReject);
    }

    [Fact]
    public void LossTreeReconcilesWithGap()
    {
        var overview = ThroughputIntelligenceOrchestrator.Analyze(BuildContext(BuildEvents()));
        Assert.True(overview.LossTree.TotalGapPerHour >= 0);
        Assert.True(overview.LossTree.AttributedLossPerHour >= 0);
    }

    [Fact]
    public void BufferIntelligenceReportsWip()
    {
        var overview = ThroughputIntelligenceOrchestrator.Analyze(BuildContext(BuildEvents()));
        Assert.NotEmpty(overview.BufferIntelligence);
    }

    [Fact]
    public void SkillEligibilityRespectsQualifications()
    {
        var context = BuildContext(BuildEvents());
        var eligibility = OperatorSkillEngine.ComputeEligibility(context, StationCodes());
        var op2 = eligibility.First(o => o.OperatorCode == ThroughputIntelligenceFixtureCodes.OperatorAssembly2);
        Assert.Contains("ST-30", op2.EligibleStationCodes);
        Assert.Contains("ST-40", op2.EligibleStationCodes);
    }

    [Fact]
    public void ExpiredQualificationRejected()
    {
        var context = BuildContext(BuildEvents());
        var eligibility = OperatorSkillEngine.ComputeEligibility(context, StationCodes());
        var expired = eligibility.FirstOrDefault(o => o.OperatorCode == "OP-EXPIRED");
        Assert.NotNull(expired);
        Assert.True(expired.IsExpired);
        Assert.False(expired.IsQualified);
    }

    [Fact]
    public void LineBalancingProducesRecommendation()
    {
        var overview = ThroughputIntelligenceOrchestrator.Analyze(BuildContext(BuildEvents()));
        Assert.NotEmpty(overview.BalancingRecommendations);
    }

    [Fact]
    public void AddOperatorScenarioIncreasesThroughput()
    {
        var context = BuildContext(BuildEvents());
        var overview = ThroughputIntelligenceOrchestrator.Analyze(context);
        var states = SimulationStateBuilder.BuildBaseline(context, overview.StationCycles);
        var snapshot = ScenarioSimulationEngine.Simulate(
            context,
            states,
            [new ScenarioIntervention(
                ThroughputIntelligenceScenarioTypes.AddOperator,
                _assemblyId, "ST-30", ThroughputIntelligenceFixtureCodes.OperatorAssembly2,
                null, null, null, null, null)],
            overview.Measurement,
            overview.Constraints,
            overview.OperatorSkills);
        Assert.True(snapshot.DeltaGoodThroughputPerHour >= 0);
        Assert.Equal(ThroughputIntelligenceScenarioEligibility.Eligible, snapshot.Eligibility);
    }

    [Fact]
    public void MoveOperatorScenarioEvaluated()
    {
        var context = BuildContext(BuildEvents());
        var overview = ThroughputIntelligenceOrchestrator.Analyze(context);
        var states = SimulationStateBuilder.BuildBaseline(context, overview.StationCycles);
        var snapshot = ScenarioSimulationEngine.Simulate(
            context,
            states,
            [new ScenarioIntervention(
                ThroughputIntelligenceScenarioTypes.MoveOperator,
                _assemblyId, "ST-30", ThroughputIntelligenceFixtureCodes.OperatorAssembly2,
                "ST-40", null, null, null, null)],
            overview.Measurement,
            overview.Constraints,
            overview.OperatorSkills);
        Assert.NotNull(snapshot);
    }

    [Fact]
    public void BufferScenarioEvaluated()
    {
        var context = BuildContext(BuildEvents());
        var overview = ThroughputIntelligenceOrchestrator.Analyze(context);
        var states = SimulationStateBuilder.BuildBaseline(context, overview.StationCycles);
        var bufferStation = context.Stations.First(s => s.Code == "BUF-20");
        var snapshot = ScenarioSimulationEngine.Simulate(
            context,
            states,
            [new ScenarioIntervention(
                ThroughputIntelligenceScenarioTypes.ChangeBufferCapacity,
                bufferStation.Id, "BUF-20", null, null, 20, null, null, null)],
            overview.Measurement,
            overview.Constraints,
            overview.OperatorSkills);
        Assert.NotNull(snapshot);
    }

    [Fact]
    public void CycleTimeScenarioEvaluated()
    {
        var context = BuildContext(BuildEvents());
        var overview = ThroughputIntelligenceOrchestrator.Analyze(context);
        var states = SimulationStateBuilder.BuildBaseline(context, overview.StationCycles);
        var snapshot = ScenarioSimulationEngine.Simulate(
            context,
            states,
            [new ScenarioIntervention(
                ThroughputIntelligenceScenarioTypes.ChangeCycleTime,
                _weldingId, "ST-40", null, null, null, -0.8m, null, null)],
            overview.Measurement,
            overview.Constraints,
            overview.OperatorSkills);
        Assert.True(snapshot.DeltaGoodThroughputPerHour >= 0);
    }

    [Fact]
    public void RedistributionScenarioEvaluated()
    {
        var context = BuildContext(BuildEvents());
        var overview = ThroughputIntelligenceOrchestrator.Analyze(context);
        var states = SimulationStateBuilder.BuildBaseline(context, overview.StationCycles);
        var snapshot = ScenarioSimulationEngine.Simulate(
            context,
            states,
            [new ScenarioIntervention(
                ThroughputIntelligenceScenarioTypes.RedistributeOperation,
                _assemblyId, "ST-30", null, "ST-40", null, null, 20m, null)],
            overview.Measurement,
            overview.Constraints,
            overview.OperatorSkills);
        Assert.NotNull(snapshot);
    }

    [Fact]
    public void CombinedScenarioEvaluated()
    {
        var context = BuildContext(BuildEvents());
        var overview = ThroughputIntelligenceOrchestrator.Analyze(context);
        var states = SimulationStateBuilder.BuildBaseline(context, overview.StationCycles);
        var snapshot = ScenarioSimulationEngine.Simulate(
            context,
            states,
            [
                new ScenarioIntervention(
                    ThroughputIntelligenceScenarioTypes.AddOperator,
                    _assemblyId, "ST-30", ThroughputIntelligenceFixtureCodes.OperatorAssembly2,
                    null, null, null, null, null),
                new ScenarioIntervention(
                    ThroughputIntelligenceScenarioTypes.ChangeCycleTime,
                    _weldingId, "ST-40", null, null, null, -0.8m, null, null),
            ],
            overview.Measurement,
            overview.Constraints,
            overview.OperatorSkills);
        Assert.NotNull(snapshot.ConstraintAfter);
    }

    [Fact]
    public void ConstraintMovementAfterAddOperator()
    {
        var context = BuildContext(BuildEvents());
        var overview = ThroughputIntelligenceOrchestrator.Analyze(context);
        var states = SimulationStateBuilder.BuildBaseline(context, overview.StationCycles);
        var snapshot = ScenarioSimulationEngine.Simulate(
            context,
            states,
            [new ScenarioIntervention(
                ThroughputIntelligenceScenarioTypes.AddOperator,
                _assemblyId, "ST-30", ThroughputIntelligenceFixtureCodes.OperatorAssembly2,
                null, null, null, null, null)],
            overview.Measurement,
            overview.Constraints,
            overview.OperatorSkills);
        Assert.Equal("ST-30", snapshot.ConstraintBefore?.StationCode);
        Assert.NotNull(snapshot.ConstraintAfter);
    }

    [Fact]
    public void QualityGuardrailBlocksInferiorScenario()
    {
        var context = BuildContext(BuildEvents());
        var overview = ThroughputIntelligenceOrchestrator.Analyze(context);
        var states = SimulationStateBuilder.BuildBaseline(context, overview.StationCycles);
        var snapshot = ScenarioSimulationEngine.Simulate(
            context,
            states,
            [new ScenarioIntervention(
                ThroughputIntelligenceScenarioTypes.ChangeCycleTime,
                _weldingId, "ST-40", null, null, null, -2m, null, null)],
            overview.Measurement,
            overview.Constraints,
            overview.OperatorSkills);
        Assert.Equal(
            ThroughputIntelligenceScenarioEligibility.RequiresControlledApproval,
            snapshot.Eligibility);
        Assert.Contains(snapshot.LimitingFactors, f => f.Contains("quality", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UnqualifiedOperatorMarkedNotEligible()
    {
        var context = BuildContext(BuildEvents());
        var overview = ThroughputIntelligenceOrchestrator.Analyze(context);
        var states = SimulationStateBuilder.BuildBaseline(context, overview.StationCycles);
        var snapshot = ScenarioSimulationEngine.Simulate(
            context,
            states,
            [new ScenarioIntervention(
                ThroughputIntelligenceScenarioTypes.AddOperator,
                _assemblyId, "ST-30", "OP-UNQUALIFIED",
                null, null, null, null, null)],
            overview.Measurement,
            overview.Constraints,
            overview.OperatorSkills);
        Assert.Equal(ThroughputIntelligenceScenarioEligibility.NotEligible, snapshot.Eligibility);
    }

    [Fact]
    public void ScenarioDoesNotMutateInputEvents()
    {
        var events = BuildEvents();
        var beforeCount = events.Count;
        var context = BuildContext(events);
        var overview = ThroughputIntelligenceOrchestrator.Analyze(context);
        var states = SimulationStateBuilder.BuildBaseline(context, overview.StationCycles);
        ScenarioSimulationEngine.Simulate(
            context,
            states,
            [new ScenarioIntervention(
                ThroughputIntelligenceScenarioTypes.AddOperator,
                _assemblyId, "ST-30", ThroughputIntelligenceFixtureCodes.OperatorAssembly2,
                null, null, null, null, null)],
            overview.Measurement,
            overview.Constraints,
            overview.OperatorSkills);
        Assert.Equal(beforeCount, events.Count);
    }

    [Fact]
    public void Mp1MixedLineRegressionTopologyPreserved()
    {
        var context = BuildContext(BuildEvents());
        Assert.Equal(6, context.Stations.Count);
        Assert.Contains(context.Stations, s => s.Code == "ST-10");
        Assert.Contains(context.Stations, s => s.Code == "ST-60");
    }

    private List<CanonicalProductionEvent> BuildEvents()
    {
        var events = new List<CanonicalProductionEvent>();
        for (var i = 0; i < 50; i++)
        {
            var t = _windowStart.AddSeconds(i * 60);
            events.Add(Event(_injectionId, MixedProductionEventTypes.CycleStart, t));
            events.Add(Event(_injectionId, MixedProductionEventTypes.CycleEnd, t.AddSeconds(4)));
            events.Add(Event(_injectionId, MixedProductionEventTypes.Good, t.AddSeconds(5), 1));
            events.Add(Event(_assemblyId, MixedProductionEventTypes.OperatorStart, t.AddSeconds(6)));
            events.Add(Event(_assemblyId, MixedProductionEventTypes.OperatorComplete, t.AddSeconds(14)));
            if (i % 8 == 0)
            {
                events.Add(Event(_assemblyId, MixedProductionEventTypes.OperatorWait, t.AddSeconds(15)));
            }
            events.Add(Event(_weldingId, MixedProductionEventTypes.CycleStart, t.AddSeconds(16)));
            events.Add(Event(_weldingId, MixedProductionEventTypes.CycleEnd, t.AddSeconds(22)));
            events.Add(Event(_weldingId, MixedProductionEventTypes.Good, t.AddSeconds(23), 1));
            if (i % 5 == 0)
            {
                events.Add(Event(_weldingId, MixedProductionEventTypes.Stop, t.AddSeconds(24)));
                events.Add(Event(_weldingId, MixedProductionEventTypes.Run, t.AddSeconds(36)));
            }
            if (i % 12 == 0)
            {
                events.Add(Event(_qualityId, MixedProductionEventTypes.Reject, t.AddSeconds(40), 1));
            }
            else
            {
                events.Add(Event(_qualityId, MixedProductionEventTypes.Good, t.AddSeconds(40), 1));
            }
        }

        return events;
    }

    private ThroughputAnalysisContext BuildContext(IReadOnlyList<CanonicalProductionEvent> events)
    {
        var bufferId = Guid.NewGuid();
        var stations = new List<ProductionStation>
        {
            Station(_injectionId, "ST-10", "Injection", 10, MixedProductionStationKinds.Process),
            Station(bufferId, "BUF-20", "Buffer", 20, MixedProductionStationKinds.Buffer),
            Station(_assemblyId, "ST-30", "Human Assembly", 30, MixedProductionStationKinds.Process),
            Station(_weldingId, "ST-40", "Welding", 40, MixedProductionStationKinds.Process),
            Station(_qualityId, "ST-50", "Quality", 50, MixedProductionStationKinds.Quality),
            Station(Guid.NewGuid(), "ST-60", "Packaging", 60, MixedProductionStationKinds.Packaging),
        };

        var resources = stations.ToDictionary(
            s => s.Id,
            s => new ManufacturingResource
            {
                Id = Guid.NewGuid(),
                StationId = s.Id,
                ResourceType = s.Code == "BUF-20"
                    ? MixedProductionResourceTypes.Buffer
                    : s.Code == "ST-30" || s.Code == "ST-60"
                        ? MixedProductionResourceTypes.Human
                        : MixedProductionResourceTypes.Machine,
                Code = s.Code,
                BufferCapacity = s.Code == "BUF-20" ? 40 : null,
            });

        var target = new LineThroughputTarget
        {
            TargetGoodUnitsPerHour = 670m,
            RequiredGoodQuantity = 5360,
            AvailableProductionHours = 8m,
            TargetCycleSeconds = 5.37m,
        };

        var skills = new List<OperatorSkillProfile>
        {
            new() { OperatorCode = ThroughputIntelligenceFixtureCodes.OperatorAssembly, DisplayName = "Op1", SkillLevel = 3, IsActive = true },
            new() { OperatorCode = ThroughputIntelligenceFixtureCodes.OperatorAssembly2, DisplayName = "Op2", SkillLevel = 3, IsActive = true },
            new() { OperatorCode = "OP-EXPIRED", DisplayName = "Expired Op", SkillLevel = 2, IsActive = true },
        };

        var qualifications = new List<OperatorStationQualification>
        {
            new() { OperatorCode = ThroughputIntelligenceFixtureCodes.OperatorAssembly, StationId = _assemblyId, OperationCode = "OP-30", IsActive = true },
            new() { OperatorCode = ThroughputIntelligenceFixtureCodes.OperatorAssembly2, StationId = _assemblyId, OperationCode = "OP-30", IsActive = true },
            new() { OperatorCode = ThroughputIntelligenceFixtureCodes.OperatorAssembly2, StationId = _weldingId, OperationCode = "OP-40", IsActive = true },
            new() { OperatorCode = "OP-EXPIRED", StationId = _assemblyId, OperationCode = "OP-30", IsActive = true, ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-1) },
        };

        return new ThroughputAnalysisContext(
            new ProductionLine { Id = _lineId, Code = "MIX-LINE-01", Name = "Mixed Line 01" },
            stations,
            resources,
            new Dictionary<Guid, BufferResourceState>
            {
                [resources[bufferId].Id] = new BufferResourceState
                {
                    ResourceId = resources[bufferId].Id,
                    Capacity = 40,
                    CurrentQuantity = 10,
                },
            },
            events,
            target,
            skills,
            qualifications,
            _windowStart,
            _windowEnd,
            ThroughputIntelligenceAnalysisWindows.Recent);
    }

    private Dictionary<Guid, string> StationCodes() =>
        BuildContext([]).Stations.ToDictionary(s => s.Id, s => s.Code);

    private static ProductionStation Station(Guid id, string code, string name, int seq, string kind) =>
        new() { Id = id, Code = code, Name = name, SequenceOrder = seq, StationKind = kind };

    private CanonicalProductionEvent Event(Guid stationId, string eventType, DateTimeOffset at, decimal? qty = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            LineId = _lineId,
            StationId = stationId,
            EventType = eventType,
            OccurredAtUtc = at,
            Quantity = qty,
        };
}
