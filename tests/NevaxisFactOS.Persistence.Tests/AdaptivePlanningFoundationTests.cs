using NevaxisFactOS.Persistence.AdaptiveProductionPlanning;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.ProductionManager;
using NevaxisFactOS.Persistence.ProductionPlanning;
using System.Globalization;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class AdaptivePlanningPermissionKeyTests
{
    [Fact]
    public void PermissionKeysFollowProductionPlanningConvention()
    {
        Assert.Equal("production.planning.read", AdaptivePlanningPermissionKeys.Read);
        Assert.Equal("production.planning.simulate", AdaptivePlanningPermissionKeys.Simulate);
        Assert.Equal("production.planning.write", AdaptivePlanningPermissionKeys.Manage);
        Assert.Equal("production.planning.publish", AdaptivePlanningPermissionKeys.Publish);
        Assert.Equal("production.planning.approve", AdaptivePlanningPermissionKeys.Approve);
        Assert.Equal("production.planning.autonomy.manage", AdaptivePlanningPermissionKeys.AutonomyManage);
        Assert.Equal("production.planning.policy.manage", AdaptivePlanningPermissionKeys.PolicyManage);
    }
}

public sealed class PlanFeasibilityEngineTests
{
    [Fact]
    public void RejectsQualityHoldAsHardConstraint()
    {
        var payload = new ConstraintSnapshotPayload(
            [new ConstraintOrderRow("PO-1", "PO-1", DateTimeOffset.UtcNow.AddHours(4), 80, "ready", 100, ["IM-07"], ["M-014"], true)],
            [new ConstraintMachineRow("IM-07", "Line-1", true, false, ["M-014"], 12, 70)],
            [],
            [],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        var result = PlanFeasibilityEngine.Evaluate(payload, []);

        Assert.False(result.IsFeasible);
        Assert.Contains(result.HardViolations, v => v.Contains("Kalite hold", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExplainsInfeasibleMoldConflictInTurkish()
    {
        var payload = new ConstraintSnapshotPayload(
            [new ConstraintOrderRow("PO-1842", "PO-1842", DateTimeOffset.UtcNow.AddHours(4), 80, "ready", 100, ["IM-07"], ["M-014"], false)],
            [new ConstraintMachineRow("IM-07", "Line-1", false, true, ["M-014"], 12, 70)],
            [],
            [],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        var hard = new List<string> { "Kalıp uyumsuz: PO-1842 / M-014 / IM-09" };

        var explanation = PlanInfeasibilityExplainer.Build(payload, hard);

        Assert.Contains("PO-1842", explanation);
        Assert.Contains("M-014", explanation);
    }
}

public sealed class FrozenWindowEngineTests
{
    [Fact]
    public void BlocksNearTermMachineMoveUnlessCritical()
    {
        var policy = new AdaptivePlanningPolicy { FrozenWindowMinutes = 60 };
        var now = DateTimeOffset.UtcNow;
        var current = new List<ProductionPlanOperation>
        {
            new()
            {
                OrderId = "PO-1",
                MachineId = "IM-07",
                PlannedStartUtc = now.AddMinutes(20),
                PlannedEndUtc = now.AddHours(2),
            },
        };
        var proposed = new List<ProductionPlanOperation>
        {
            new()
            {
                OrderId = "PO-1",
                MachineId = "IM-09",
                PlannedStartUtc = now.AddMinutes(20),
                PlannedEndUtc = now.AddHours(2),
            },
        };

        Assert.True(FrozenWindowEngine.WouldViolate(policy, proposed, current, now, criticalIncident: false));
        Assert.False(FrozenWindowEngine.WouldViolate(policy, proposed, current, now, criticalIncident: true));
    }
}

public sealed class PlanNervousnessGuardTests
{
    [Fact]
    public void BlocksWhenBenefitBelowThreshold()
    {
        var policy = new AdaptivePlanningPolicy { MinBenefitThreshold = 0.10m, NervousnessCooldownMinutes = 0 };
        Assert.True(PlanNervousnessGuard.ShouldBlock(policy, null, 0.05m, DateTimeOffset.UtcNow));
    }
}

public sealed class ReplanNecessityEngineTests
{
    [Fact]
    public void MachineFailurePrefersLocalAdjustment()
    {
        var payload = new ConstraintSnapshotPayload(
            [],
            [new ConstraintMachineRow("IM-07", "Line-1", false, true, ["M-014"], 12, 70),
             new ConstraintMachineRow("IM-09", "Line-2", true, false, ["M-014"], 10, 60)],
            [],
            [],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        Assert.Equal(ReplanActions.LocalAdjustment, ReplanNecessityEngine.Evaluate("machine-failure", payload));
    }
}

public sealed class PlanningObjectiveEngineTests
{
    [Fact]
    public void ScoresFeasiblePlanDeterministically()
    {
        var payload = new ConstraintSnapshotPayload(
            [new ConstraintOrderRow("PO-1", "PO-1", DateTimeOffset.UtcNow.AddHours(8), 90, "ready", 10, ["IM-07"], ["M-014"], false)],
            [new ConstraintMachineRow("IM-07", "Line-1", true, false, ["M-014"], 12, 70)],
            [],
            [],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        var ops = new List<ProductionPlanOperation>
        {
            new()
            {
                OrderId = "PO-1",
                PlannedStartUtc = DateTimeOffset.UtcNow,
                PlannedEndUtc = DateTimeOffset.UtcNow.AddHours(2),
                SetupMinutes = 15,
            },
        };
        var feasibility = PlanFeasibilityEngine.Evaluate(payload, ops);
        var score = PlanningObjectiveEngine.Score(feasibility, ops, payload, "{}");

        Assert.True(score > 0m);
        Assert.True(score <= 1m);
    }
}

public sealed class PlanDiffEngineTests
{
    [Fact]
    public void DiffShowsMachineAndStartChanges()
    {
        var from = new List<ProductionPlanOperation>
        {
            new()
            {
                OrderId = "PO-1842",
                MachineId = "IM-07",
                PlannedStartUtc = DateTimeOffset.Parse("2026-09-13T14:20:00Z", CultureInfo.InvariantCulture),
                PlannedEndUtc = DateTimeOffset.Parse("2026-09-13T16:20:00Z", CultureInfo.InvariantCulture),
            },
        };
        var to = new List<ProductionPlanOperation>
        {
            new()
            {
                OrderId = "PO-1842",
                MachineId = "IM-09",
                PlannedStartUtc = DateTimeOffset.Parse("2026-09-13T15:05:00Z", CultureInfo.InvariantCulture),
                PlannedEndUtc = DateTimeOffset.Parse("2026-09-13T17:05:00Z", CultureInfo.InvariantCulture),
            },
        };

        var diff = PlanDiffEngine.Diff(from, to);

        Assert.Contains(diff, row => row.Field == "machine" && row.OldValue == "IM-07" && row.NewValue == "IM-09");
        Assert.Contains(diff, row => row.Field == "start");
    }
}

public sealed class PlanningAutonomyEngineTests
{
    [Fact]
    public void LimitedAutonomyAllowsSmallBoundedChange()
    {
        var policy = new AdaptivePlanningPolicy
        {
            AutonomyMode = PlanningAutonomyModes.Limited,
            PolicyEnvelopeJson = """{"maxOrdersAffected":2,"maxScheduleMovementMinutes":90,"maxLatenessImpactHours":2}""",
        };

        Assert.True(PlanningAutonomyEngine.CanAutoPublish(policy, 1, 30, 0.5m));
        Assert.True(PlanningAutonomyEngine.RequiresApproval(policy, 5, 5m));
    }
}
