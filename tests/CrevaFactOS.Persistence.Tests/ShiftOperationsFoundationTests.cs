using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.ShiftOperations;

namespace CrevaFactOS.Persistence.Tests;

public sealed class ShiftOperationsPermissionKeyTests
{
    [Fact]
    public void PermissionKeysAreStableAndBounded()
    {
        Assert.Equal("shift.read", ShiftOperationsPermissionKeys.Read);
        Assert.Equal("shift.manage", ShiftOperationsPermissionKeys.Manage);
        Assert.Equal("shift.start", ShiftOperationsPermissionKeys.Start);
        Assert.Equal("shift.close", ShiftOperationsPermissionKeys.Close);
        Assert.Equal("shift.handover.prepare", ShiftOperationsPermissionKeys.HandoverPrepare);
        Assert.Equal("shift.handover.submit", ShiftOperationsPermissionKeys.HandoverSubmit);
        Assert.Equal("shift.handover.accept", ShiftOperationsPermissionKeys.HandoverAccept);
        Assert.Equal("shift.open-item.manage", ShiftOperationsPermissionKeys.OpenItemManage);
    }
}

public sealed class ShiftHandoverPriorityEngineTests
{
    [Fact]
    public void SafetySeverityMapsToCritical()
    {
        var priority = ShiftHandoverPriorityEngine.ResolvePriority(
            ShiftOpenItemCategories.Maintenance,
            "critical",
            false,
            0,
            null);
        Assert.Equal(ShiftHandoverPriorities.Critical, priority);
    }

    [Fact]
    public void CarryoverEscalatesPriority()
    {
        var priority = ShiftHandoverPriorityEngine.ResolvePriority(
            ShiftOpenItemCategories.Maintenance,
            "medium",
            false,
            3,
            null);
        Assert.Equal(ShiftHandoverPriorities.Critical, priority);
    }
}

public sealed class ShiftNothingFallsThroughGateTests
{
    [Fact]
    public void BlocksMissingMandatoryItems()
    {
        var items = new[]
        {
            new ShiftOpenItem
            {
                Title = "IM-07 bakım",
                SourceType = "maintenance",
                SourceId = "im-07",
                RequiresHandover = true,
                Status = ShiftOpenItemStatuses.Open,
            },
        };

        var result = ShiftNothingFallsThroughGate.Evaluate(items, []);
        Assert.False(result.Allowed);
        Assert.Contains("IM-07 bakım", result.MissingMandatoryItems);
    }

    [Fact]
    public void AllowsWhenAllMandatoryIncluded()
    {
        var items = new[]
        {
            new ShiftOpenItem
            {
                Title = "IM-07 bakım",
                SourceType = "maintenance",
                SourceId = "im-07",
                RequiresHandover = true,
                Status = ShiftOpenItemStatuses.Open,
            },
        };

        var result = ShiftNothingFallsThroughGate.Evaluate(items, ["maintenance:im-07"]);
        Assert.True(result.Allowed);
    }
}

public sealed class ShiftHandoverConflictDetectorTests
{
    [Fact]
    public void DetectsHumanNoteResolvedVsOpenSource()
    {
        var items = new[]
        {
            new ShiftOpenItem
            {
                Title = "NCR-0918",
                SourceId = "ncr-0918",
                SourceType = "quality",
                RequiresHandover = true,
                Status = ShiftOpenItemStatuses.Open,
            },
        };

        var conflicts = ShiftHandoverConflictDetector.Detect(
            items,
            new Dictionary<string, string> { ["ncr-0918"] = "Sorun çözüldü" });

        Assert.Contains(conflicts, c => c.Code == "human-note-vs-open-source");
    }
}
