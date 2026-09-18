using NevaxisZentra.Persistence.Migrations.Migrations;

namespace NevaxisZentra.Persistence.Tests;

public sealed class AddProductionPlanningSchedulingAndTransferBoundaryMigrationTests
{
    private static string ReadMigrationSource() =>
        ReadEmbeddedMigrationFile("20260807092100_AddProductionPlanningSchedulingAndTransferBoundary.cs");

    // ── Permissions ──────────────────────────────────────────────────────────

    [Fact]
    public void UpMigrationSeedsSchedulePermission()
    {
        var source = ReadMigrationSource();

        Assert.Contains("'production.planning.schedule'", source, StringComparison.Ordinal);
        Assert.Contains("10000000-0000-4000-8000-000000000013", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationSeedsTransferPermission()
    {
        var source = ReadMigrationSource();

        Assert.Contains("'production.planning.transfer'", source, StringComparison.Ordinal);
        Assert.Contains("10000000-0000-4000-8000-000000000014", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationUsesConflictDoNothingForPermissions()
    {
        var source = ReadMigrationSource();

        Assert.Contains("ON CONFLICT (id) DO NOTHING", source, StringComparison.Ordinal);
    }

    // ── Transfer request table ───────────────────────────────────────────────

    [Fact]
    public void UpMigrationCreatesTransferRequestTable()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production_plan_transfer_requests", source, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpMigrationIncludesIdempotencyKeyUniqueConstraint()
    {
        var source = ReadMigrationSource();

        Assert.Contains("idempotency_key", source, StringComparison.Ordinal);
        Assert.Contains("UNIQUE", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpMigrationGrantsRuntimeSelectInsertUpdateOnTransferRequests()
    {
        var source = ReadMigrationSource();

        Assert.Contains("GRANT SELECT", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("production_plan_transfer_requests", source, StringComparison.Ordinal);
        Assert.Contains("crevafactos_runtime", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationDoesNotGrantDeleteOnTransferRequests()
    {
        var source = ReadMigrationSource();

        Assert.DoesNotContain("GRANT DELETE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GRANT ALL", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpMigrationEnablesRlsOnTransferRequests()
    {
        var source = ReadMigrationSource();

        Assert.Contains("ENABLE ROW LEVEL SECURITY", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FORCE ROW LEVEL SECURITY", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpMigrationCreatesRlsPoliciesForTransferRequests()
    {
        var source = ReadMigrationSource();

        Assert.Contains("rls_transfer_requests_tenant", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpMigrationAllowsOnlyErpAndMesAsTargetSystem()
    {
        var source = ReadMigrationSource();

        Assert.Contains("'erp'", source, StringComparison.Ordinal);
        Assert.Contains("'mes'", source, StringComparison.Ordinal);
        Assert.Contains("target_system", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationTransferStatusConstraintIncludesUncertain()
    {
        var source = ReadMigrationSource();

        Assert.Contains("'uncertain'", source, StringComparison.Ordinal);
    }

    // ── Audit/outbox taxonomy ─────────────────────────────────────────────────

    [Fact]
    public void UpMigrationExpandsAuditActionTaxonomyForScheduling()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production.plan.schedule", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationExpandsAuditActionTaxonomyForTransfer()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production.plan.transfer.request", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.transfer.start", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.transfer.succeed", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.transfer.fail", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.transfer.mark.uncertain", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationExpandsAuditEventTaxonomyForScheduling()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production.plan.scheduled", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationExpandsAuditEventTaxonomyForTransfer()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production.plan.transfer.requested", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.transfer.started", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.transfer.succeeded", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.transfer.failed", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.transfer.uncertain", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationExpandsOutboxMessageTypeTaxonomyForScheduling()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production-plan-scheduled", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationExpandsOutboxMessageTypeTaxonomyForTransfer()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production-plan-transfer-requested", source, StringComparison.Ordinal);
        Assert.Contains("production-plan-transfer-started", source, StringComparison.Ordinal);
        Assert.Contains("production-plan-transfer-succeeded", source, StringComparison.Ordinal);
        Assert.Contains("production-plan-transfer-failed", source, StringComparison.Ordinal);
        Assert.Contains("production-plan-transfer-uncertain", source, StringComparison.Ordinal);
    }

    // ── Down migration ────────────────────────────────────────────────────────

    [Fact]
    public void DownMigrationDropsTransferRequestTable()
    {
        var source = ReadMigrationSource();

        Assert.Contains("DROP TABLE", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("production_plan_transfer_requests", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DownMigrationDeletesScheduleAndTransferPermissions()
    {
        var source = ReadMigrationSource();

        Assert.Contains("DELETE FROM permission_definitions", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("10000000-0000-4000-8000-000000000013", source, StringComparison.Ordinal);
        Assert.Contains("10000000-0000-4000-8000-000000000014", source, StringComparison.Ordinal);
    }

    private static string ReadEmbeddedMigrationFile(string fileName)
    {
        var migrationsAssembly = typeof(AddProductionPlanningSchedulingAndTransferBoundary).Assembly;
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
