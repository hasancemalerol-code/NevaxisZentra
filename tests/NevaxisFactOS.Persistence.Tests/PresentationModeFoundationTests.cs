using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Development;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class PresentationModePermissionKeyTests
{
    [Fact]
    public void AccessPermissionKeyIsStable()
    {
        Assert.Equal("presentation-mode.access", PresentationModePermissionKeys.Access);
    }
}

public sealed class DevelopmentPilotPresentationModeAccessSeedTests
{
    [Fact]
    public void PresentationPermissionKeysIncludePresentationModeAccess()
    {
        var keys = DevelopmentPilotOperatorPresentationAccessSeed.PresentationPermissionKeys;

        Assert.Contains(PresentationModePermissionKeys.Access, keys);
        Assert.Contains(GoldenDemoP1PermissionKeys.Read, keys);
        Assert.Contains(GoldenDemoCustomsFilePermissionKeys.Read, keys);
        Assert.Contains(FactoryOrchestrationPermissionKeys.Read, keys);
        Assert.Contains(FactoryOrchestrationPermissionKeys.Manage, keys);
        Assert.Contains(FactoryOrchestrationPermissionKeys.Verify, keys);
    }

    [Fact]
    public void OperationalReadOnlyPermissionSetDoesNotIncludePresentationModeAccess()
    {
        string[] operationalReadOnlyKeys =
        [
            ProductionExecutionPermissionKeys.Read,
            ProductionPlanningPermissionKeys.Read,
            QualityPermissionKeys.Read,
        ];

        Assert.DoesNotContain(PresentationModePermissionKeys.Access, operationalReadOnlyKeys);
    }
}
