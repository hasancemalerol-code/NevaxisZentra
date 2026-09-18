using NevaxisZentra.Persistence.Migrations.Migrations;

namespace NevaxisZentra.Persistence.Tests;

public sealed class AddProductionPlanningHandoffAndMakerCheckerMigrationTests
{
    private static string ReadMigrationSource() =>
        ReadEmbeddedMigrationFile("20260807150000_AddProductionPlanningHandoffAndMakerChecker.cs");

    [Fact]
    public void UpMigrationSeedsHandoffPermission()
    {
        var source = ReadMigrationSource();

        Assert.Contains("'production.planning.handoff'", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationAddsLastModifiedBySubjectIdColumn()
    {
        var source = ReadMigrationSource();

        Assert.Contains("last_modified_by_subject_id", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationCreatesHandoffsTable()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production_plan_handoffs", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationAddsUniqueIdempotencyConstraint()
    {
        var source = ReadMigrationSource();

        Assert.Contains("idempotency_key", source, StringComparison.Ordinal);
        Assert.Contains("UNIQUE", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpMigrationGrantsRuntimeSelectInsertUpdateOnHandoffs()
    {
        var source = ReadMigrationSource();

        Assert.Contains("GRANT SELECT, INSERT, UPDATE ON TABLE production_plan_handoffs", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpMigrationDoesNotGrantDeleteOnHandoffs()
    {
        var source = ReadMigrationSource();

        Assert.DoesNotContain("GRANT DELETE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GRANT ALL", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpMigrationCreatesRlsPoliciesOnHandoffs()
    {
        var source = ReadMigrationSource();

        Assert.Contains("ROW LEVEL SECURITY", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE POLICY", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpMigrationExpandsAuditActionTaxonomyForHandoff()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production.plan.handoff.prepare", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationExpandsAuditEventTaxonomyForHandoff()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production.plan.handoff.prepared", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationExpandsOutboxMessageTypeTaxonomyForHandoff()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production-plan-handoff-prepared", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationUsesConflictDoNothingForPermissions()
    {
        var source = ReadMigrationSource();

        Assert.Contains("ON CONFLICT (id) DO NOTHING", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DownMigrationDropsHandoffsTable()
    {
        var source = ReadMigrationSource();

        Assert.Contains("DROP TABLE", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("production_plan_handoffs", source, StringComparison.Ordinal);
    }

    private static string ReadEmbeddedMigrationFile(string fileName)
    {
        var migrationsAssembly = typeof(AddProductionPlanningHandoffAndMakerChecker).Assembly;
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

        throw new InvalidOperationException(
            "Repository root with NevaxisZentra.sln could not be located.");
    }
}
