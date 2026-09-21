using System.IO;

namespace NevaxisZentra.Desktop.Tests;

public sealed class ReleaseStagingMigrationTests
{
    [Fact]
    public void PublishableSchemaVersionRequiresDesignerRegistration()
    {
        var repositoryRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var migrationsDir = Path.Combine(
            repositoryRoot,
            "packages",
            "NevaxisZentra.Persistence.Migrations",
            "Migrations");

        if (!Directory.Exists(migrationsDir))
        {
            return;
        }

        var registered = Directory.GetFiles(migrationsDir, "*.Designer.cs")
            .Select(path => Path.GetFileName(path)!.Replace(".Designer.cs", string.Empty, StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

        var latest = Directory.GetFiles(migrationsDir, "*.cs")
            .Select(path => Path.GetFileName(path))
            .Where(name => name is not null
                           && !name.Contains("Designer", StringComparison.Ordinal)
                           && !name.Contains("Snapshot", StringComparison.Ordinal)
                           && registered.Contains(name.Replace(".cs", string.Empty, StringComparison.Ordinal)))
            .OrderByDescending(name => name!, StringComparer.Ordinal)
            .FirstOrDefault();

        Assert.NotNull(latest);
        Assert.EndsWith(".cs", latest, StringComparison.Ordinal);
        Assert.True(
            registered.Contains(latest.Replace(".cs", string.Empty, StringComparison.Ordinal)),
            "Latest publishable migration must have a Designer registration.");
    }
}
