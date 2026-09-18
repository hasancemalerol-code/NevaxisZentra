using NevaxisZentra.Persistence.Migrations.Migrations;

namespace NevaxisZentra.Persistence.Tests;

public sealed class AddProductionPlanningDraftWriteSupportMigrationTests
{
    private static string ReadMigrationSource() =>
        ReadEmbeddedMigrationFile(
            "20260806232517_AddProductionPlanningDraftWriteSupport.cs");

    [Fact]
    public void UpMigrationSeedsProductionPlanningWritePermission()
    {
        var source = ReadMigrationSource();

        Assert.Contains("'production.planning.write'", source, StringComparison.Ordinal);
        Assert.Contains("10000000-0000-4000-8000-000000000010", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationGrantsRuntimeInsertOnProductionPlans()
    {
        var source = ReadMigrationSource();

        Assert.Contains("GRANT INSERT", source, StringComparison.Ordinal);
        Assert.Contains("production_plans TO crevafactos_runtime", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationGrantsRuntimeUpdateOnProductionPlans()
    {
        var source = ReadMigrationSource();

        Assert.Contains("GRANT INSERT, UPDATE ON TABLE production_plans TO crevafactos_runtime",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationDoesNotGrantDeleteOnProductionPlans()
    {
        var source = ReadMigrationSource();

        Assert.DoesNotContain("GRANT DELETE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GRANT ALL", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpMigrationAddsInsertRlsPolicyWithTenantIsolation()
    {
        var source = ReadMigrationSource();

        Assert.Contains("CREATE POLICY production_plans_insert_current", source, StringComparison.Ordinal);
        Assert.Contains("FOR INSERT TO crevafactos_runtime", source, StringComparison.Ordinal);
        Assert.Contains("WITH CHECK (tenant_id = current_tenant_id())", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationAddsUpdateRlsPolicyWithTenantIsolation()
    {
        var source = ReadMigrationSource();

        Assert.Contains("CREATE POLICY production_plans_update_current", source, StringComparison.Ordinal);
        Assert.Contains("FOR UPDATE TO crevafactos_runtime", source, StringComparison.Ordinal);
        Assert.Contains("USING (tenant_id = current_tenant_id())", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationDoesNotAddBlanketTruePolicy()
    {
        var source = ReadMigrationSource();

        Assert.DoesNotContain("USING (true)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WITH CHECK (true)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationExpandsAuditActionTaxonomyForDraftOperations()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production.plan.draft.create", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.draft.update", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationExpandsAuditEventTaxonomyForDraftOperations()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production.plan.draft.created", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.draft.updated", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DownMigrationRevokesInsertUpdateGrants()
    {
        var source = ReadMigrationSource();

        Assert.Contains("REVOKE INSERT, UPDATE ON TABLE production_plans FROM crevafactos_runtime",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void DownMigrationDropsWritePolicies()
    {
        var source = ReadMigrationSource();

        Assert.Contains("DROP POLICY IF EXISTS production_plans_update_current", source, StringComparison.Ordinal);
        Assert.Contains("DROP POLICY IF EXISTS production_plans_insert_current", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DownMigrationDeletesPermissionDefinition()
    {
        var source = ReadMigrationSource();

        Assert.Contains("DELETE FROM permission_definitions", source, StringComparison.Ordinal);
        Assert.Contains("10000000-0000-4000-8000-000000000010", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DownMigrationDoesNotDropFacilitiesOrMembershipPolicies()
    {
        var source = ReadMigrationSource();

        Assert.DoesNotContain("DROP POLICY IF EXISTS facilities_select_current", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP POLICY IF EXISTS facility_memberships_select_current", source, StringComparison.Ordinal);
    }

    private static string ReadEmbeddedMigrationFile(string fileName)
    {
        var migrationsAssembly = typeof(AddProductionPlanningDraftWriteSupport).Assembly;
        var resourceName = migrationsAssembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(fileName, StringComparison.Ordinal));

        if (resourceName is not null)
        {
            using var stream = migrationsAssembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream!);
            return reader.ReadToEnd();
        }

        var repoRoot = LocateRepositoryRoot();
        var filePath = Path.Combine(
            repoRoot,
            "packages",
            "NevaxisZentra.Persistence.Migrations",
            "Migrations",
            fileName);

        return File.ReadAllText(filePath);
    }

    private static string LocateRepositoryRoot()
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

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
