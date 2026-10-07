using System.Text.RegularExpressions;
using NevaxisZentra.Persistence.RealFactoryPilot;
using Xunit;

namespace NevaxisZentra.Persistence.Tests;

/// <summary>
/// Reading the real-factory pilot must never create synthetic pilot data (PILOT-* connectors, a SYNTHETIC_PILOT_PASS
/// run, fixture factory/line/product). Only the explicit, audited initialization may.
/// </summary>
public sealed class RealFactoryPilotSideEffectGuardTests
{
    private static readonly string[] ExpectedSeedingCallers =
    [
        "RealFactoryPilot/RealFactoryPilotFixtureSeeder.cs",
        "RealFactoryPilot/RealFactoryPilotService.cs",
    ];

    [Fact]
    public void TheCurrentWorkspaceReadIsAPureRead()
    {
        var source = File.ReadAllText(Path.Combine(PackageDirectory(), "RealFactoryPilot", "RealFactoryPilotService.cs"));
        var method = ExtractMember(source, "public Task<RealFactoryPilotWorkspace?> GetCurrentWorkspaceAsync(");

        Assert.Contains("ExecuteReadAsync", method);
        Assert.Contains("AsNoTracking", method);
        foreach (var forbidden in new[] { "ExecuteWriteAsync", "SaveChanges", "EnsurePilotFactoryAsync", ".Add(", "InitializePilot", "Seed" })
        {
            Assert.DoesNotContain(forbidden, method);
        }
    }

    [Fact]
    public void OnlyExplicitInitializationAndTheDevelopmentGoldenPilotSeedFixtures()
    {
        var root = PackageDirectory();
        var callers = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                           && !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(file => File.ReadAllText(file).Contains("EnsurePilotFactoryAsync("))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToList();

        // the seeder itself + the service (InitializePilotCoreAsync, reached by the audited POST and the dev golden run)
        Assert.Equal(ExpectedSeedingCallers, callers);
    }

    [Fact]
    public void ApiExposesInitializationOnlyAsAPostAndNeverFromTheReadEndpoints()
    {
        var api = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "apps", "api", "RealFactoryPilot", "RealFactoryPilotEndpointRouteBuilderExtensions.cs"));

        Assert.Contains("group.MapPost(\"/initialize\"", api);
        Assert.DoesNotContain("MapGet(\"/initialize\"", api);
        Assert.Contains("group.MapGet(\"/current\"", api);

        var current = ExtractMember(api, "private static async Task<IResult> GetCurrentAsync(");
        Assert.DoesNotContain("InitializePilotAsync", current);
        Assert.DoesNotContain("RunGoldenPilotAsync", current);

        var initialize = ExtractMember(api, "private static async Task<IResult> InitializeAsync(");
        Assert.Contains("SyntheticInitializationAllowed", initialize);
        Assert.Contains("RealFactoryPilotPermissionKeys.Manage", initialize);
        Assert.Contains("RealFactoryPilotInitializationAudit", initialize);
    }

    [Fact]
    public void TheInitializationAuditUsesATaxonomyTheDatabaseAccepts()
    {
        Assert.Matches(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)+$", RealFactoryPilotAuditTaxonomy.EventType);
        Assert.Matches(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)*$", RealFactoryPilotAuditTaxonomy.Action);
        Assert.Matches(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)*$", RealFactoryPilotAuditTaxonomy.ResourceType);

        var migration = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "packages", "NevaxisZentra.Persistence.Migrations", "Migrations",
            "20261005160000_AddRealFactoryPilotInitializationAuditTaxonomy.cs"));
        Assert.Contains($"'{RealFactoryPilotAuditTaxonomy.EventType}'", migration);
        Assert.Contains($"'{RealFactoryPilotAuditTaxonomy.Action}'", migration);
        Assert.Contains("ck_audit_records_event_taxonomy", migration);
        Assert.Contains("ck_audit_records_action_taxonomy", migration);

        var service = File.ReadAllText(Path.Combine(PackageDirectory(), "RealFactoryPilot", "RealFactoryPilotService.cs"));
        Assert.Contains("RealFactoryPilotAuditTaxonomy.EventType", service);
        Assert.Contains("AuditOutcome.Success", service);
    }

    private static string ExtractMember(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"member not found: {signature}");
        var rest = source[start..];
        var next = Regex.Match(rest[signature.Length..], @"\n    (public|private|internal) ");
        return next.Success ? rest[..(signature.Length + next.Index)] : rest;
    }

    private static string PackageDirectory() =>
        Path.Combine(RepositoryRoot(), "packages", "NevaxisZentra.Persistence");

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "NevaxisZentra.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
