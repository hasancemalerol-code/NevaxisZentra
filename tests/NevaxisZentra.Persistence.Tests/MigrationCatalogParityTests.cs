using System.Reflection;
using System.Text.RegularExpressions;
using NevaxisZentra.Persistence.AiTasks;
using Xunit;

namespace NevaxisZentra.Persistence.Tests;

/// <summary>
/// Guards the class of defect found by the packaged-Production browser sweep: code, model and web pages that
/// reference database objects no migration creates. A database built from the migration chain (or from a release
/// template) then rejects every feature that needs them.
/// </summary>
public sealed class MigrationCatalogParityTests
{
    private static readonly Regex PermissionRow = new(
        @"\(\s*'(?<id>[0-9a-fA-F-]{36})'\s*,\s*'(?<key>[a-z0-9.-]+)'\s*,\s*'(?<norm>[a-z0-9.-]+)'",
        RegexOptions.Compiled);

    [Fact]
    public void PermissionDefinitionIdsAndKeysAreUniqueAcrossMigrations()
    {
        var idToKeys = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var keyToIds = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var file in MigrationFiles())
        {
            foreach (Match match in PermissionRow.Matches(File.ReadAllText(file)))
            {
                var id = match.Groups["id"].Value;
                var key = match.Groups["key"].Value;
                if (!key.Contains('.'))
                {
                    continue;
                }

                (idToKeys.TryGetValue(id, out var keys) ? keys : idToKeys[id] = new()).Add(key);
                (keyToIds.TryGetValue(key, out var ids) ? ids : keyToIds[key] = new()).Add(id);
            }
        }

        // An id claimed by two different keys makes "ON CONFLICT DO NOTHING" silently skip the second definition.
        var idCollisions = idToKeys.Where(entry => entry.Value.Count > 1).Select(entry => $"{entry.Key}: {string.Join(", ", entry.Value)}").ToList();
        Assert.True(idCollisions.Count == 0, "permission ids claimed by several keys: " + string.Join("; ", idCollisions));
        var keyCollisions = keyToIds.Where(entry => entry.Value.Count > 1).Select(entry => $"{entry.Key}: {string.Join(", ", entry.Value)}").ToList();
        Assert.True(keyCollisions.Count == 0, "permission keys defined with several ids: " + string.Join("; ", keyCollisions));
    }

    [Fact]
    public void EveryAiTaskPermissionKeyConstantIsDefinedByAMigration()
    {
        var defined = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in MigrationFiles())
        {
            foreach (Match match in PermissionRow.Matches(File.ReadAllText(file)))
            {
                defined.Add(match.Groups["key"].Value);
            }
        }

        var constants = typeof(AiTaskPermissionKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

        Assert.True(constants.Count >= 32);
        var missing = constants.Where(key => !defined.Contains(key)).ToList();
        Assert.True(missing.Count == 0, "AiTaskPermissionKeys without a permission_definitions migration: " + string.Join(", ", missing));
    }

    [Fact]
    public void EveryTableMappedByTheModelSnapshotIsCreatedByAMigration()
    {
        var root = RepositoryRoot();
        var snapshot = File.ReadAllText(Path.Combine(MigrationsDirectory(), "NevaxisFactOSDbContextModelSnapshot.cs"));
        var modelTables = Regex.Matches(snapshot, @"ToTable\(""(?<name>[a-z0-9_]+)""")
            .Select(match => match.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);

        var created = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in MigrationFiles())
        {
            var text = File.ReadAllText(file);
            foreach (Match match in Regex.Matches(text, @"CreateTable\(\s*name:\s*""(?<name>[a-z0-9_]+)"""))
            {
                created.Add(match.Groups["name"].Value);
            }

            foreach (Match match in Regex.Matches(text, @"CREATE TABLE (?:IF NOT EXISTS )?\{?(?<name>[a-z0-9_]+)\}?", RegexOptions.IgnoreCase))
            {
                created.Add(match.Groups["name"].Value.ToLowerInvariant());
            }

            // Helper methods build names from a parameter (CreateTenantFacilityTable("verified_...", ...)).
            foreach (Match match in Regex.Matches(text, @"""(?<name>[a-z][a-z0-9]*(?:_[a-z0-9]+)+)"""))
            {
                if (modelTables.Contains(match.Groups["name"].Value) && text.Contains("CREATE TABLE {"))
                {
                    created.Add(match.Groups["name"].Value);
                }
            }
        }

        Assert.True(modelTables.Count > 400, $"snapshot parsed from {root}");
        var uncreated = modelTables.Where(table => !created.Contains(table)).OrderBy(table => table, StringComparer.Ordinal).ToList();
        Assert.True(uncreated.Count == 0, "model tables no migration creates: " + string.Join(", ", uncreated));
    }

    [Fact]
    public void TheRepairMigrationGrantsRuntimeAccessAndIsolatesTenantsOnEveryRepairedTable()
    {
        var text = File.ReadAllText(Path.Combine(MigrationsDirectory(), "20261005120000_RepairAiTaskCenterAndFactoryOutcomeSchema.cs"));
        foreach (var table in new[]
                 {
                     "ai_task_playbooks", "ai_task_playbook_runs", "ai_task_automation_rules", "ai_task_ai_analyses",
                     "ai_task_relationships", "factory_outcome_execution_actions", "factory_outcome_event_receipts",
                     "factory_outcome_learning_records",
                 })
        {
            Assert.Contains($"(\"{table}\"", text);
        }

        Assert.Contains("ENABLE ROW LEVEL SECURITY", text);
        Assert.Contains("FORCE ROW LEVEL SECURITY", text);
        Assert.Contains("REVOKE ALL ON TABLE {table} FROM PUBLIC", text);
        Assert.Contains("GRANT SELECT, INSERT ON TABLE {table} TO crevafactos_runtime", text);
        Assert.DoesNotContain("GRANT ALL", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NoSourceFileAssignsAnUnspecifiedKindDateToALocalThatIsComparedWithTimestamptzColumns()
    {
        // `var today = DateTimeOffset.UtcNow.Date;` yields a DateTime (Kind Unspecified). Used against a
        // DateTimeOffset column it is converted with the machine's local offset and Npgsql rejects it
        // ("Cannot write DateTimeOffset with Offset=03:00:00"): an HTTP 500 on every non-UTC installation.
        var root = RepositoryRoot();
        var offenders = new List<string>();
        foreach (var directory in new[] { "packages", "apps" })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, directory), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }

                if (Regex.IsMatch(File.ReadAllText(file), @"=\s*DateTimeOffset\.UtcNow\.Date\s*;"))
                {
                    offenders.Add(Path.GetRelativePath(root, file));
                }
            }
        }

        Assert.True(offenders.Count == 0, "use new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero): " + string.Join(", ", offenders));
    }

    private static IEnumerable<string> MigrationFiles() =>
        Directory.EnumerateFiles(MigrationsDirectory(), "*.cs")
            .Where(file => !file.EndsWith(".Designer.cs", StringComparison.Ordinal)
                           && !file.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal));

    private static string MigrationsDirectory() =>
        Path.Combine(RepositoryRoot(), "packages", "NevaxisZentra.Persistence.Migrations", "Migrations");

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "NevaxisZentra.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root (NevaxisZentra.sln) not found.");
    }
}
