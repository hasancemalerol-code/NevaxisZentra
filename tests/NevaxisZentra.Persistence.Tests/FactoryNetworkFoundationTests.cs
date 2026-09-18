using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.FactoryNetworkOptimization;
using NevaxisZentra.Persistence.ProductionManager;
using static NevaxisZentra.Persistence.Tests.FactoryNetworkFoundationTestHelpers;

namespace NevaxisZentra.Persistence.Tests;

public sealed class FactoryNetworkPermissionKeyTests
{
    [Fact]
    public void PermissionKeysFollowFactoryNetworkConvention()
    {
        Assert.Equal("factory-network.read", FactoryNetworkPermissionKeys.Read);
        Assert.Equal("factory-network.simulate", FactoryNetworkPermissionKeys.Simulate);
        Assert.Equal("factory-network.manage", FactoryNetworkPermissionKeys.Manage);
        Assert.Equal("factory-network.approve", FactoryNetworkPermissionKeys.Approve);
        Assert.Equal("factory-network.execute", FactoryNetworkPermissionKeys.Execute);
        Assert.Equal("factory-network.policy.manage", FactoryNetworkPermissionKeys.PolicyManage);
    }
}

public sealed class NetworkCapacityMapEngineTests
{
    [Fact]
    public void BuildMarksOverloadedFacilityAsNotRealizableWhenMaintenanceOpen()
    {
        var overloaded = SampleContext(
            Guid.Parse("bb000001-0005-4000-8000-000000000005"),
            "Plant A",
            92m,
            maintenanceJson: """{"maintenanceCases":1,"openMaintenance":true}""");
        var receiver = SampleContext(
            Guid.Parse("bb000001-0005-4000-8000-000000000008"),
            "Plant B",
            58m);

        var map = NetworkCapacityMapEngine.Build([overloaded, receiver]);

        Assert.Equal(2, map.Count);
        Assert.True(map.First(c => c.FacilityName == "Plant A").CommittedPercent >= 90m);
        Assert.False(map.First(c => c.FacilityName == "Plant A").Realizable);
        Assert.True(map.First(c => c.FacilityName == "Plant B").Realizable);
    }
}

public sealed class CrossPlantFeasibilityEngineTests
{
    [Fact]
    public void RejectsQualityHoldAsHardConstraint()
    {
        var source = SampleContext(Guid.NewGuid(), "Plant A", 90m);
        var destination = SampleContext(
            Guid.NewGuid(),
            "Plant B",
            40m,
            qualityJson: """{"hold":true,"qualityBlocks":1,"classification":"observed"}""");
        var demand = SampleDemand(materialReady: true, moldReady: true);

        var result = CrossPlantFeasibilityEngine.Evaluate(
            source,
            destination,
            demand,
            NetworkOptionActions.MoveFullOrder);

        Assert.False(result.IsFeasible);
        Assert.Contains(result.HardViolations, v => v.Contains("kalite hold", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RejectsMaterialShortageAtDestination()
    {
        var source = SampleContext(Guid.NewGuid(), "Plant A", 88m);
        var destination = SampleContext(
            Guid.NewGuid(),
            "Plant B",
            35m,
            materialJson: """{"shortageSignals":2,"classification":"observed"}""");
        var demand = SampleDemand(materialReady: false, moldReady: true);

        var result = CrossPlantFeasibilityEngine.Evaluate(
            source,
            destination,
            demand,
            NetworkOptionActions.MoveFullOrder);

        Assert.False(result.IsFeasible);
        Assert.Contains(result.HardViolations, v => v.Contains("Malzeme", StringComparison.Ordinal));
    }

    [Fact]
    public void WarnsOnMoldCompatibilityWhenMoldNotReady()
    {
        var source = SampleContext(Guid.NewGuid(), "Plant A", 88m);
        var destination = SampleContext(
            Guid.NewGuid(),
            "Plant B",
            35m,
            moldJson: """{"moldCases":1,"classification":"observed"}""");
        var demand = SampleDemand(materialReady: true, moldReady: false);

        var result = CrossPlantFeasibilityEngine.Evaluate(
            source,
            destination,
            demand,
            NetworkOptionActions.MoveFullOrder);

        Assert.True(result.IsFeasible);
        Assert.Contains(result.SoftWarnings, w => w.Contains("Kalıp", StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsMachineFaultAtDestination()
    {
        var source = SampleContext(Guid.NewGuid(), "Plant A", 88m);
        var destination = SampleContext(
            Guid.NewGuid(),
            "Plant B",
            35m,
            machineJson: """{"running":0,"fault":1}""");

        var result = CrossPlantFeasibilityEngine.Evaluate(
            source,
            destination,
            SampleDemand(),
            NetworkOptionActions.MoveFullOrder);

        Assert.False(result.IsFeasible);
        Assert.Contains(result.HardViolations, v => v.Contains("makine", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class NetworkQuantityConservationEngineTests
{
    [Fact]
    public void SplitAllocationConservesTotalQuantity()
    {
        var conservation = NetworkQuantityConservationEngine.Verify(240m, 120m, 120m);

        Assert.Equal(100m, conservation);
        NetworkQuantityConservationEngine.Ensure(conservation);
    }

    [Fact]
    public void RejectsQuantityViolationBelowThreshold()
    {
        var ex = Assert.Throws<FactoryNetworkException>(() =>
            NetworkQuantityConservationEngine.Ensure(90m));

        Assert.Equal(FactoryNetworkFailureCodes.QuantityViolation, ex.FailureCode);
    }
}

public sealed class NetworkScoringEngineTests
{
    [Fact]
    public void ScoresFeasibleDestinationDeterministically()
    {
        var destination = SampleContext(Guid.NewGuid(), "Plant B", 40m);
        var demand = SampleDemand();
        var feasibility = new FeasibilityEvaluation(true, [], [], null);

        var score = NetworkScoringEngine.Score(feasibility, destination, demand, "KNOWN");

        Assert.True(score > 0m);
        Assert.True(score <= 1.1m);
    }

    [Fact]
    public void InfeasibleDestinationScoresZero()
    {
        var destination = SampleContext(Guid.NewGuid(), "Plant B", 40m);
        var demand = SampleDemand();
        var feasibility = new FeasibilityEvaluation(false, ["blocked"], [], "blocked");

        Assert.Equal(0m, NetworkScoringEngine.Score(feasibility, destination, demand, "KNOWN"));
    }
}

public sealed class NetworkSafetyDominanceEngineTests
{
    [Fact]
    public void BlocksInfeasibleScenarioBeforeScoring()
    {
        var feasibility = new FeasibilityEvaluation(false, ["Hedef tesis kapasitesi yetersiz."], [], "blocked");

        var ex = Assert.Throws<FactoryNetworkException>(() => NetworkSafetyDominanceEngine.Ensure(feasibility));

        Assert.Equal(FactoryNetworkFailureCodes.Infeasible, ex.FailureCode);
    }
}

public sealed class NetworkHardConstraintEngineTests
{
    [Fact]
    public void EnsureThrowsInfeasibleFailureCode()
    {
        var feasibility = new FeasibilityEvaluation(false, ["Hedef tesis bakım penceresinde."], [], "maintenance");

        var ex = Assert.Throws<FactoryNetworkException>(() => NetworkHardConstraintEngine.Ensure(feasibility));

        Assert.Equal(FactoryNetworkFailureCodes.Infeasible, ex.FailureCode);
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }
}

public sealed class NetworkConflictGuardTests
{
    [Fact]
    public void RejectsCrossFacilityResourceConflict()
    {
        var contexts = new[]
        {
            SampleContext(Guid.NewGuid(), "Plant A", 80m, constraintsJson: """{"conflict":"shared-mold"}"""),
            SampleContext(Guid.NewGuid(), "Plant B", 50m),
        };

        var ex = Assert.Throws<FactoryNetworkException>(() => NetworkConflictGuard.Ensure(contexts));

        Assert.Equal(FactoryNetworkFailureCodes.Conflict, ex.FailureCode);
    }
}

public sealed class NetworkFreshnessGuardTests
{
    [Fact]
    public void BlocksStaleNetworkStateUnlessOverridden()
    {
        var stale = DateTimeOffset.UtcNow.AddHours(-3);

        var ex = Assert.Throws<FactoryNetworkException>(() =>
            NetworkFreshnessGuard.Ensure(stale, DateTimeOffset.UtcNow, overrideStaleGuard: false));

        Assert.Equal(FactoryNetworkFailureCodes.StaleData, ex.FailureCode);
        NetworkFreshnessGuard.Ensure(stale, DateTimeOffset.UtcNow, overrideStaleGuard: true);
    }
}

public sealed class NetworkAutonomyEngineTests
{
    [Fact]
    public void LimitedAutonomyAllowsSmallBoundedTransfer()
    {
        var policy = new FactoryNetworkPolicy
        {
            AutonomyMode = NetworkAutonomyModes.Limited,
            PolicyEnvelopeJson = """{"approvalThresholdOrders":2,"maxTransferQuantity":500}""",
        };

        Assert.True(NetworkAutonomyEngine.CanAutoExecute(policy, 1, 0.72m));
        Assert.True(NetworkAutonomyEngine.RequiresApproval(policy, 3, 0.72m));
    }

    [Fact]
    public void ObserveModeAlwaysRequiresApproval()
    {
        var policy = new FactoryNetworkPolicy
        {
            AutonomyMode = NetworkAutonomyModes.Observe,
            PolicyEnvelopeJson = """{"approvalThresholdOrders":99}""",
        };

        Assert.False(NetworkAutonomyEngine.CanAutoExecute(policy, 1, 0.9m));
        Assert.True(NetworkAutonomyEngine.RequiresApproval(policy, 1, 0.9m));
    }
}

public sealed class NetworkPolicyEnvelopeEngineTests
{
    [Fact]
    public void DetectsProhibitedFacilityPair()
    {
        var sourceId = Guid.Parse("bb000001-0005-4000-8000-000000000005");
        var destinationId = Guid.Parse("bb000001-0005-4000-8000-000000000008");
        var policy = new FactoryNetworkPolicy
        {
            PolicyEnvelopeJson =
                $$"""{"prohibitedPairs":[{"sourceFacilityId":"{{sourceId}}","destinationFacilityId":"{{destinationId}}"}]}""",
        };

        Assert.True(NetworkPolicyEnvelopeEngine.IsProhibitedPair(policy, sourceId, destinationId));
        Assert.False(NetworkPolicyEnvelopeEngine.IsProhibitedPair(policy, destinationId, sourceId));
    }

    [Fact]
    public void ReadsApprovalThresholdFromEnvelope()
    {
        var policy = new FactoryNetworkPolicy
        {
            PolicyEnvelopeJson = """{"approvalThresholdOrders":4}""",
        };

        Assert.Equal(4, NetworkPolicyEnvelopeEngine.ApprovalThresholdOrders(policy));
    }
}

public sealed class FactoryFailoverEngineTests
{
    [Fact]
    public void SelectsHighestAvailableFailoverTarget()
    {
        var failedId = Guid.Parse("bb000001-0005-4000-8000-000000000005");
        var contexts = new[]
        {
            SampleContext(failedId, "Plant A", 95m),
            SampleContext(Guid.Parse("bb000001-0005-4000-8000-000000000008"), "Plant B", 58m),
        };

        var target = FactoryFailoverEngine.SelectFailoverTarget(contexts, failedId);

        Assert.Equal(Guid.Parse("bb000001-0005-4000-8000-000000000008"), target);
    }
}

public sealed class NetworkLearningBridgeTests
{
    [Fact]
    public void BuildMemoryCapturesExpectedVsActualBridge()
    {
        var tenantId = Guid.NewGuid();
        var facilityId = Guid.NewGuid();
        var allocation = new NetworkAllocation
        {
            Id = Guid.NewGuid(),
            NetworkId = Guid.NewGuid(),
            ActionType = NetworkOptionActions.MoveFullOrder,
            ReasonJson = """{"summary":"PO-1842 transfer"}""",
            ExpectedImpactJson = """{"expectedBenefitScore":0.78}""",
        };
        var outcome = new NetworkOutcomeVerificationResult(
            allocation.Id,
            "verified-success",
            allocation.ExpectedImpactJson,
            """{"adherencePercent":82,"recoveredOrders":1}""",
            100m,
            null);

        var memory = NetworkLearningBridge.BuildMemory(tenantId, facilityId, allocation, outcome);

        Assert.Equal("network-cross-plant-allocation", memory.ProblemPattern);
        Assert.Equal(allocation.ActionType, memory.InterventionSummary);
        Assert.Equal(outcome.ActualImpactJson, memory.ResultSummary);
        Assert.Equal(1m, memory.EffectivenessScore);
    }
}

public sealed class NetworkOutcomeVerificationEngineTests
{
    [Fact]
    public void VerifyComparesExpectedAndActualImpact()
    {
        var allocation = new NetworkAllocation
        {
            Id = Guid.NewGuid(),
            ExpectedImpactJson = """{"expectedBenefitScore":0.78}""",
            PublishedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
        };
        var items = new List<NetworkAllocationItem>
        {
            new()
            {
                TotalQuantity = 240m,
                SourceQuantity = 0m,
                DestinationQuantity = 240m,
            },
        };

        var outcome = NetworkOutcomeVerificationEngine.Verify(allocation, items, 78m);

        Assert.Equal("verified-success", outcome.OutcomeStatus);
        Assert.Equal(100m, outcome.QuantityConservationPercent);
        Assert.Contains("adherencePercent", outcome.ActualImpactJson, StringComparison.Ordinal);
        Assert.Equal(allocation.ExpectedImpactJson, outcome.ExpectedImpactJson);
    }
}

public sealed class NetworkWhatIfSimulatorTests
{
    [Fact]
    public void SplitOrderScenarioAppearsForLargeDemand()
    {
        var tenantId = Guid.NewGuid();
        var networkId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        var source = SampleContext(sourceId, "Plant A", 91m);
        var destination = SampleContext(destinationId, "Plant B", 35m);
        var demand = SampleDemand(orderId: "PO-1842", remainingQuantity: 240m);
        var request = new SimulateNetworkRequest(
            networkId,
            sourceId,
            destinationId,
            "PO-1842",
            240m,
            NetworkOptionActions.SplitOrder,
            3);

        var results = NetworkWhatIfSimulator.Simulate(
            tenantId,
            networkId,
            request,
            [source, destination],
            demand,
            DateTimeOffset.UtcNow);

        Assert.Contains(results, r => r.Scenario.ActionType == NetworkOptionActions.SplitOrder);
        Assert.All(results, r => Assert.True(r.Feasibility.IsFeasible));
    }
}

public sealed class FactoryNetworkFailureCodeTests
{
    [Fact]
    public void ConcurrencyAndDuplicateExecutionCodesAreDistinct()
    {
        Assert.NotEqual(
            FactoryNetworkFailureCodes.ConcurrencyConflict,
            FactoryNetworkFailureCodes.DuplicateExecution);
        Assert.Equal("factory-network.concurrency-conflict", FactoryNetworkFailureCodes.ConcurrencyConflict);
        Assert.Equal("factory-network.duplicate-execution", FactoryNetworkFailureCodes.DuplicateExecution);
    }
}

internal static class FactoryNetworkFoundationTestHelpers
{
    internal static NetworkFacilityContext SampleContext(
        Guid facilityId,
        string name,
        decimal loadPercent,
        string qualityJson = """{"qualityBlocks":0,"classification":"observed"}""",
        string materialJson = """{"shortageSignals":0,"classification":"observed"}""",
        string moldJson = """{"moldCases":0,"classification":"observed"}""",
        string maintenanceJson = """{"maintenanceCases":0,"classification":"observed"}""",
        string machineJson = """{"running":4,"stopped":0,"classification":"observed"}""",
        string constraintsJson = "{}") =>
        new(
            facilityId,
            name,
            "Europe/Istanbul",
            new FactoryStateSnapshot
            {
                QualityStateJson = qualityJson,
                MaterialStateJson = materialJson,
                MoldStateJson = moldJson,
                MaintenanceStateJson = maintenanceJson,
                MachineStateJson = machineJson,
                CapacityStateJson = $$"""{"utilizationPercent":{{loadPercent}}}""",
                ConstraintsJson = constraintsJson,
                FreshnessJson = """{"production":"2026-09-13T16:00:00Z"}""",
                CapturedAtUtc = DateTimeOffset.UtcNow,
            },
            new FacilityCapabilityProfile
            {
                MoldCompatibilityJson = """["M-014","injection-molding"]""",
                MaterialsJson = """["PP-HF"]""",
            },
            loadPercent,
            Math.Max(0, 100 - loadPercent),
            DateTimeOffset.UtcNow);

    internal static NetworkDemandRow SampleDemand(
        string orderId = "PO-1842",
        decimal remainingQuantity = 240m,
        bool materialReady = true,
        bool moldReady = true) =>
        new(
            orderId,
            Guid.Empty,
            remainingQuantity,
            DateTimeOffset.UtcNow.AddHours(18),
            "high",
            "injection-molding",
            materialReady,
            moldReady);
}
