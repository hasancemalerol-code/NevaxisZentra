using NevaxisZentra.Persistence.Migrations.Migrations;

namespace NevaxisZentra.Persistence.Tests;

public sealed class AddPlanningRuntimeReadPoliciesMigrationTests
{
    private static string ReadMigrationSource() =>
        ReadEmbeddedMigrationFile(
            "20260806230126_AddPlanningRuntimeReadPolicies.cs");

    [Fact]
    public void UpMigrationGrantsRuntimeSelectOnPlanningTables()
    {
        var source = ReadMigrationSource();

        Assert.Contains("GRANT SELECT ON TABLE facilities TO crevafactos_runtime", source, StringComparison.Ordinal);
        Assert.Contains("GRANT SELECT ON TABLE facility_memberships TO crevafactos_runtime", source, StringComparison.Ordinal);
        Assert.Contains("GRANT SELECT ON TABLE production_plans TO crevafactos_runtime", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationEnablesTenantScopedSelectPolicies()
    {
        var source = ReadMigrationSource();

        Assert.Contains("CREATE POLICY facilities_select_current", source, StringComparison.Ordinal);
        Assert.Contains("CREATE POLICY facility_memberships_select_current", source, StringComparison.Ordinal);
        Assert.Contains("CREATE POLICY production_plans_select_current", source, StringComparison.Ordinal);
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
    public void DownMigrationRevokesPoliciesAndRuntimeSelect()
    {
        var source = ReadMigrationSource();

        Assert.Contains("DROP POLICY IF EXISTS production_plans_select_current", source, StringComparison.Ordinal);
        Assert.Contains("DROP POLICY IF EXISTS facility_memberships_select_current", source, StringComparison.Ordinal);
        Assert.Contains("DROP POLICY IF EXISTS facilities_select_current", source, StringComparison.Ordinal);
        Assert.Contains("REVOKE SELECT ON TABLE production_plans FROM crevafactos_runtime", source, StringComparison.Ordinal);
        Assert.Contains("REVOKE SELECT ON TABLE facility_memberships FROM crevafactos_runtime", source, StringComparison.Ordinal);
        Assert.Contains("REVOKE SELECT ON TABLE facilities FROM crevafactos_runtime", source, StringComparison.Ordinal);
    }

    private static string ReadEmbeddedMigrationFile(string fileName)
    {
        var migrationsAssembly = typeof(AddPlanningRuntimeReadPolicies).Assembly;
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
