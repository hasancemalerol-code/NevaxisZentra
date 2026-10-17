using System.Text.RegularExpressions;

namespace NevaxisZentra.Persistence.Tests;

/// <summary>
/// One business fact, one calculation: list/summary/detail pairs must read the same canonical source.
/// </summary>
public sealed class CanonicalMetricSourceTests
{
    [Fact]
    public void ShiftOverviewReadinessUsesTheClosingPreviewCalculation()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "packages", "NevaxisZentra.Persistence", "ShiftOperations", "ShiftOperationsService.cs"));

        var metrics = Regex.Match(
            source,
            @"private async Task<ShiftMetricsSnapshot> GetMetricsCoreAsync\(.*?\n    \}",
            RegexOptions.Singleline);
        Assert.True(metrics.Success);
        Assert.Contains("GetClosingPreviewCoreAsync(", metrics.Value, StringComparison.Ordinal);
        Assert.DoesNotContain("ComputeReadiness(", metrics.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void BulkCommitPersistsEachEntityTypeBeforeCommittingItsDependents()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "packages", "NevaxisZentra.Persistence", "BulkDataOnboarding", "BulkDataOnboardingService.cs"));

        // Lines look their area up in the database, stations their line, resources their station: the per-type loop must save.
        var loop = Regex.Match(
            source,
            @"foreach \(var entityType in commitOrder\)\s*\{(?<body>.*?)\n        \}",
            RegexOptions.Singleline);
        Assert.True(loop.Success);
        var body = loop.Groups["body"].Value;
        var innerEnd = body.LastIndexOf("            }", StringComparison.Ordinal);
        Assert.Contains("SaveChangesAsync", body[innerEnd..], StringComparison.Ordinal);
    }

    [Fact]
    public void BulkCommitNeverReportsCompletedWhenRowsFailed()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "packages", "NevaxisZentra.Persistence", "BulkDataOnboarding", "BulkDataOnboardingService.cs"));

        Assert.Matches(@"job\.Status = failed == 0\s*\?\s*BulkDataImportJobStatuses\.Completed\s*:\s*imported \+ updated > 0\s*\?\s*BulkDataImportJobStatuses\.Partial\s*:\s*BulkDataImportJobStatuses\.Failed;", source);
        Assert.Contains("job.FailedRows = failed;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MaintenanceOverdueCountsShareOneRule()
    {
        var maintenance = Path.Combine(FindRepoRoot(), "packages", "NevaxisZentra.Persistence", "Maintenance");
        var summary = File.ReadAllText(Path.Combine(maintenance, "MaintenanceWorkOrderRepository.cs"));
        var reliability = File.ReadAllText(Path.Combine(maintenance, "MaintenanceReliabilityRepository.cs"));

        // The workspace summary (/bakim) and the reliability/reporting overview must count the same thing.
        Assert.Contains("MaintenanceOverdueRule.IsOverdueOpenDue(", summary, StringComparison.Ordinal);
        Assert.Contains("MaintenanceOverdueRule.IsOverdueOpenDue(", reliability, StringComparison.Ordinal);
        Assert.DoesNotContain("p.NextDueAtUtc.Value < DateTimeOffset.UtcNow", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiDateParsingNeverDependsOnTheServerTimeZone()
    {
        // A date-only query value parsed as local time yields a non-zero offset that Npgsql rejects (HTTP 500 on any
        // machine outside UTC, e.g. Türkiye). Every parse must be explicitly UTC.
        var apiRoot = Path.Combine(FindRepoRoot(), "apps", "api");
        var offenders = Directory.EnumerateFiles(apiRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(path => File.ReadAllLines(path).Select((line, index) => (path, line, index)))
            .Where(entry => entry.line.Contains("DateTimeOffset.TryParse(", StringComparison.Ordinal)
                         && !entry.line.Contains("DateTimeStyles.AssumeUniversal", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetFileName(entry.path)}:{entry.index + 1}")
            .ToList();

        Assert.Empty(offenders);
    }

    internal static string FindRepoRoot()
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

        throw new InvalidOperationException("Repository root not found.");
    }
}
