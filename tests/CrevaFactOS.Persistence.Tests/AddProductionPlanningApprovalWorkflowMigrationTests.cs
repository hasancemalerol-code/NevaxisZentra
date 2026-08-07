using CrevaFactOS.Persistence.Migrations.Migrations;

namespace CrevaFactOS.Persistence.Tests;

public sealed class AddProductionPlanningApprovalWorkflowMigrationTests
{
    private static string ReadMigrationSource() =>
        ReadEmbeddedMigrationFile("20260807002410_AddProductionPlanningApprovalWorkflow.cs");

    [Fact]
    public void UpMigrationSeedsProductionPlanningReviewPermission()
    {
        var source = ReadMigrationSource();

        Assert.Contains("'production.planning.review'", source, StringComparison.Ordinal);
        Assert.Contains("10000000-0000-4000-8000-000000000011", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationSeedsProductionPlanningApprovePermission()
    {
        var source = ReadMigrationSource();

        Assert.Contains("'production.planning.approve'", source, StringComparison.Ordinal);
        Assert.Contains("10000000-0000-4000-8000-000000000012", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationGrantsRuntimeInsertOnAuditRecords()
    {
        var source = ReadMigrationSource();

        Assert.Contains("GRANT INSERT ON TABLE audit_records TO crevafactos_runtime", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationGrantsRuntimeInsertOnOutboxMessages()
    {
        var source = ReadMigrationSource();

        Assert.Contains("GRANT INSERT ON TABLE outbox_messages TO crevafactos_runtime", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationDoesNotGrantDeleteOnAuditOrOutbox()
    {
        var source = ReadMigrationSource();

        Assert.DoesNotContain("GRANT DELETE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GRANT ALL", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpMigrationExpandsAuditActionTaxonomyForWorkflow()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production.plan.workflow.submit.for.review", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.workflow.request.revision", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.workflow.approve", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.workflow.reject", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationExpandsAuditEventTaxonomyForWorkflow()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production.plan.submitted.for.review", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.revision.requested", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.approved", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.rejected", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationExpandsOutboxMessageTypeTaxonomyForWorkflow()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production-plan-submitted-for-review", source, StringComparison.Ordinal);
        Assert.Contains("production-plan-revision-requested", source, StringComparison.Ordinal);
        Assert.Contains("production-plan-approved", source, StringComparison.Ordinal);
        Assert.Contains("production-plan-rejected", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationRetainsExistingDraftAuditTaxonomy()
    {
        var source = ReadMigrationSource();

        Assert.Contains("production.plan.draft.create", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.draft.update", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.draft.created", source, StringComparison.Ordinal);
        Assert.Contains("production.plan.draft.updated", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UpMigrationUsesConflictDoNothingForPermissions()
    {
        var source = ReadMigrationSource();

        Assert.Contains("ON CONFLICT (id) DO NOTHING", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DownMigrationRevokesInsertGrants()
    {
        var source = ReadMigrationSource();

        Assert.Contains("REVOKE INSERT ON TABLE audit_records FROM crevafactos_runtime", source, StringComparison.Ordinal);
        Assert.Contains("REVOKE INSERT ON TABLE outbox_messages FROM crevafactos_runtime", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DownMigrationRestoresOriginalOutboxTypeTaxonomy()
    {
        var source = ReadMigrationSource();

        Assert.Contains("'infrastructure.audit.recorded'", source, StringComparison.Ordinal);
        Assert.Contains("'infrastructure.ai-decision.recorded'", source, StringComparison.Ordinal);
        Assert.Contains("'infrastructure.outbox.probe'", source, StringComparison.Ordinal);
    }

    private static string ReadEmbeddedMigrationFile(string fileName)
    {
        var migrationsAssembly = typeof(AddProductionPlanningApprovalWorkflow).Assembly;
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
            "CrevaFactOS.Persistence.Migrations",
            "Migrations",
            fileName);

        return File.ReadAllText(filePath);
    }

    private static string LocateRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CrevaFactOS.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
