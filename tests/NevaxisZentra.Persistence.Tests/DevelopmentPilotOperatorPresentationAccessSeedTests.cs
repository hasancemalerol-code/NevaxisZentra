using NevaxisZentra.Persistence.AdminAccess;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Development;
using NevaxisZentra.Persistence.Edge;
using NevaxisZentra.Persistence.Energy;
using NevaxisZentra.Persistence.Export;
using NevaxisZentra.Persistence.IotPlc;

namespace NevaxisZentra.Persistence.Tests;

public sealed class DevelopmentPilotOperatorPresentationAccessSeedTests
{
    [Fact]
    public void PresentationPermissionKeysIncludeFactoryDemoModules()
    {
        var keys = DevelopmentPilotOperatorPresentationAccessSeed.PresentationPermissionKeys;

        Assert.Contains(MoldPermissionKeys.Read, keys);
        Assert.Contains(EdgePermissionKeys.Read, keys);
        Assert.Contains(MaterialPermissionKeys.Read, keys);
        Assert.Contains(MaintenancePermissionKeys.Read, keys);
        Assert.Contains(QualityPermissionKeys.Read, keys);
        Assert.Contains(EnergyPermissionKeys.Read, keys);
        Assert.Contains(IotPlcPermissionKeys.Read, keys);
        Assert.Contains(ProductionExecutionPermissionKeys.Read, keys);
        Assert.Contains(ExportPermissionKeys.Read, keys);
        Assert.Contains(PresentationModePermissionKeys.Access, keys);
        Assert.Contains(GoldenDemoP1PermissionKeys.Read, keys);
        Assert.Contains(GoldenDemoCustomsFilePermissionKeys.Read, keys);
    }

    [Fact]
    public void PresentationPermissionKeysDoNotGrantAdminOrWildcardAccess()
    {
        var keys = DevelopmentPilotOperatorPresentationAccessSeed.PresentationPermissionKeys;

        Assert.DoesNotContain(AdminAccessPermissionKeys.GrantOwner, keys);
        Assert.DoesNotContain(AdminAccessPermissionKeys.Manage, keys);
        Assert.All(keys, key => Assert.DoesNotContain('*', key));
    }
}
