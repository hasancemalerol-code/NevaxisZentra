using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.GoldenDemoP1;
using NevaxisZentra.Persistence.GoldenDemoP2;

namespace NevaxisZentra.Persistence.Tests;

public sealed class GoldenDemoP2FoundationTests
{
    [Fact]
    public void PermissionKeysFollowGoldenDemoConvention()
    {
        Assert.Equal("golden-demo-p2.read", GoldenDemoP2PermissionKeys.Read);
        Assert.Equal("golden-demo-p2.reset", GoldenDemoP2PermissionKeys.Reset);
    }

    [Fact]
    public void PresentationEngineMapsActiveEventToRecoveryStep()
    {
        var evt = new GoldenDemoProductionEvent
        {
            Id = Guid.NewGuid(),
            MachineCode = "INJ-001",
            Status = GoldenDemoP1EventStatuses.Active,
        };

        var status = GoldenDemoP2PresentationEngine.Build(evt, 42);

        Assert.Equal("recovery-opportunity", status.PresentationStepCode);
        Assert.Equal(4, status.StepNumber);
        Assert.False(status.IsComplete);
        Assert.False(status.CanReset);
        Assert.Equal(42, status.ProductionRecoveryGoodParts);
    }

    [Fact]
    public void PresentationEngineMarksVerifiedEventCompleteAndResettable()
    {
        var evt = new GoldenDemoProductionEvent
        {
            Id = Guid.NewGuid(),
            MachineCode = "INJ-001",
            Status = GoldenDemoP1EventStatuses.Verified,
        };

        var status = GoldenDemoP2PresentationEngine.Build(evt, 122);

        Assert.True(status.IsComplete);
        Assert.True(status.CanReset);
        Assert.Equal("business-impact", status.PresentationStepCode);
    }
}
