using NevaxisZentra.Persistence.AdminAccess;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Development;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.FactoryCopilot;
using NevaxisZentra.Persistence.MixedProduction;

namespace NevaxisZentra.Persistence.Tests;

public sealed class AcceptanceRoleContractTests
{
    [Fact]
        public void ContractIsTheOnlyAcceptancePermissionList()
    {
        Assert.Equal(
            AcceptanceRoleContract.RequiredPermissionKeys,
            DevelopmentPilotOperatorPresentationAccessSeed.PresentationPermissionKeys);
        Assert.Equal("production-execution-operator", AcceptanceRoleContract.RoleKey);
        Assert.Equal("production-pilot-operator", AcceptanceRoleContract.Subject);
        Assert.Equal("2026.09.22.1", AcceptanceRoleContract.CatalogVersion);
    }

    [Fact]
        public void RequiredSurfacePermissionsMatchBackendKeys()
    {
        Assert.Equal(MixedProductionPermissionKeys.Read, AcceptanceRoleContract.Route("fabrika").PermissionKey);
        Assert.Equal(MixedProductionPermissionKeys.Read, AcceptanceRoleContract.Route("hatlar").PermissionKey);
        Assert.Equal(MixedProductionPermissionKeys.Read, AcceptanceRoleContract.Route("kaynaklar").PermissionKey);
        Assert.Equal("production.planning.read", AcceptanceRoleContract.Route("plan").PermissionKey);
        Assert.Equal(DigitalWorkforcePermissionKeys.Read, AcceptanceRoleContract.Route("isler").PermissionKey);
        Assert.Equal(FactoryCopilotPermissionKeys.Read, AcceptanceRoleContract.Route("factory-copilot").PermissionKey);
        Assert.Equal(FactoryCopilotPermissionKeys.Query, AcceptanceRoleContract.Route("factory-copilot").QueryPermissionKey);
        Assert.Equal(BulkDataOnboardingPermissionKeys.Read, AcceptanceRoleContract.Route("veri-aktarim").PermissionKey);
        Assert.Equal(BulkDataOnboardingPermissionKeys.Commit, AcceptanceRoleContract.Route("veri-aktarim").ForbiddenPermissionKey);
        Assert.DoesNotContain(BulkDataOnboardingPermissionKeys.Commit, AcceptanceRoleContract.RequiredPermissionKeys);
        Assert.Contains(BulkDataOnboardingPermissionKeys.Run, AcceptanceRoleContract.RequiredPermissionKeys);
        Assert.DoesNotContain(AdminAccessPermissionKeys.Manage, AcceptanceRoleContract.RequiredPermissionKeys);
        Assert.DoesNotContain(AdminAccessPermissionKeys.GrantOwner, AcceptanceRoleContract.RequiredPermissionKeys);
    }

    [Fact]
        public void ReconcileIsIdempotentAndRepairsMissingGrantsWithoutDroppingExtras()
    {
        var catalog = AcceptanceRoleContract.RequiredPermissionKeys
            .Concat(AcceptanceRoleContract.ForbiddenPermissionKeys)
            .Append("production.execution.write")
            .ToHashSet(StringComparer.Ordinal);
        var granted = new HashSet<string>(StringComparer.Ordinal)
        {
            "production.execution.write",
            BulkDataOnboardingPermissionKeys.Commit,
        };

        var first = AcceptanceRoleContract.Reconcile(catalog, granted);
        Assert.NotEmpty(first.GrantsToAdd);
        Assert.Contains(BulkDataOnboardingPermissionKeys.Commit, first.ForbiddenGrantsToRemove);
        Assert.DoesNotContain("production.execution.write", first.ForbiddenGrantsToRemove);

        foreach (var key in first.GrantsToAdd)
        {
            granted.Add(key);
        }

        granted.Remove(BulkDataOnboardingPermissionKeys.Commit);
        var second = AcceptanceRoleContract.Reconcile(catalog, granted);
        Assert.True(second.IsSatisfied);

        var third = AcceptanceRoleContract.Reconcile(catalog, granted);
        Assert.True(third.IsSatisfied);
    }

    [Fact]
        public void CustomerOwnerCatalogIncludesCoreWorkflowKeys()
    {
        foreach (var key in new[]
        {
            MixedProductionPermissionKeys.Read,
            FactoryCopilotPermissionKeys.Read,
            BulkDataOnboardingPermissionKeys.Read,
            "operational-value.read",
            "intelligence-runtime.read",
            "scenario-recovery.read",
            DigitalWorkforcePermissionKeys.Read,
        })
        {
            Assert.Contains(key, CustomerCatalogKeys.AllCustomerKeys);
        }
    }

    [Fact]
        public void MigrationsDoNotDeleteRoleGrantsOnUpgrade()
    {
        var root = FindRepositoryRoot();
        var migrationsDir = Path.Combine(
            root,
            "packages",
            "NevaxisZentra.Persistence.Migrations",
            "Migrations");
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(migrationsDir, "*.cs"))
        {
            var name = Path.GetFileName(file);
            if (name.Contains("Designer", StringComparison.Ordinal) || name.Contains("Snapshot", StringComparison.Ordinal))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            var downIndex = source.IndexOf("protected override void Down", StringComparison.Ordinal);
            var upSource = downIndex >= 0 ? source[..downIndex] : source;
            if (upSource.Contains("DELETE FROM tenant_role_permissions", StringComparison.OrdinalIgnoreCase)
                || upSource.Contains("TRUNCATE TABLE tenant_role_permissions", StringComparison.OrdinalIgnoreCase))
            {
                offenders.Add(name);
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void AcceptanceCatalogMigrationsAreRegisteredWithEf()
    {
        var root = FindRepositoryRoot();
        var migrationsDir = Path.Combine(
            root,
            "packages",
            "NevaxisZentra.Persistence.Migrations",
            "Migrations");
        string[] migrationIds =
        [
            "20260918120000_AddBulkDataOnboardingFoundation",
            "20260920180000_AddFactoryCopilotFoundation",
            "20260920200000_AddContinuousOptimizationFoundation",
            "20260921080000_AddIntelligenceRuntimeFoundation",
        ];

        foreach (var migrationId in migrationIds)
        {
            var designerPath = Path.Combine(migrationsDir, migrationId + ".Designer.cs");
            Assert.True(File.Exists(designerPath), migrationId);
            var designer = File.ReadAllText(designerPath);
            Assert.Contains($"[Migration(\"{migrationId}\")]", designer, StringComparison.Ordinal);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NevaxisZentra.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
