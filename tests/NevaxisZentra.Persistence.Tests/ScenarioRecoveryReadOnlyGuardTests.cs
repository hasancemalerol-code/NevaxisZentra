using System.Text.RegularExpressions;
using NevaxisZentra.Persistence.ScenarioRecovery;

namespace NevaxisZentra.Persistence.Tests;

/// <summary>Source-level guards: viewing scenario recovery is read-only, creation is explicit and audited.</summary>
public sealed class ScenarioRecoveryReadOnlyGuardTests
{
    [Fact]
    public void OnlyThePostCreateEndpointCreatesARunAndItIsSimulateGatedAndAudited()
    {
        var api = Read("apps", "api", "ScenarioRecovery", "ScenarioRecoveryEndpointRouteBuilderExtensions.cs");

        Assert.Contains("group.MapPost(\"/runs\", CreateRunAsync)", api);
        Assert.DoesNotContain("MapGet(\"/runs\"", api);

        foreach (var readOnly in new[] { "GetWorkspaceAsync(", "ListHistoryAsync(" })
        {
            var member = ExtractMember(api, "private static async Task<IResult> " + readOnly);
            Assert.DoesNotContain("CreateRunAsync", member);
            Assert.DoesNotContain("GenerateOptionsAsync", member);
            Assert.Contains("ScenarioRecoveryPermissionKeys.Read", member);
        }

        var create = ExtractMember(api, "private static async Task<IResult> CreateRunAsync(");
        Assert.Contains("ScenarioRecoveryPermissionKeys.Simulate", create);
        Assert.Contains("access.TenantId", create);
        Assert.Contains("ScenarioRecoveryRunAudit", create);
    }

    [Fact]
    public void TheRunCreationIsAuditedInTheSameTransactionAsTheRunAndOnlyWhenExplicit()
    {
        var service = Read("packages", "NevaxisZentra.Persistence", "ScenarioRecovery", "ScenarioRecoveryService.cs");
        var create = ExtractMember(service, "private async Task<ScenarioRecoveryWorkspace> CreateRunCoreAsync(");

        var add = create.IndexOf("db.ScenarioRecoveryRunRecords.Add(run)", StringComparison.Ordinal);
        var audit = create.IndexOf("if (audit is not null)", StringComparison.Ordinal);
        var save = create.IndexOf("await db.SaveChangesAsync", StringComparison.Ordinal);
        Assert.True(add >= 0 && audit > add && save > audit, "run add, audit add and SaveChanges must be one unit of work, in that order");
        Assert.Contains("ScenarioRecoveryAuditTaxonomy.EventType", create);
        Assert.Contains("AuditOutcome.Success", create);

        // No other method of the service writes an audit record: reads never audit.
        Assert.Equal(1, Regex.Count(service, @"db\.AuditRecords\.Add\("));
    }

    [Fact]
    public void TheRunCreationAuditUsesATaxonomyTheDatabaseAccepts()
    {
        // The audit_records CHECK constraints (ck_audit_records_event_type / _action / _resource_type) forbid hyphens.
        Assert.Matches(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)+$", ScenarioRecoveryAuditTaxonomy.EventType);
        Assert.Matches(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)*$", ScenarioRecoveryAuditTaxonomy.Action);
        Assert.Matches(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)*$", ScenarioRecoveryAuditTaxonomy.ResourceType);

        var migration = Read("packages", "NevaxisZentra.Persistence.Migrations", "Migrations",
            "20261007120000_AddScenarioRecoveryRunCreationAuditTaxonomy.cs");
        Assert.Contains($"'{ScenarioRecoveryAuditTaxonomy.EventType}'", migration);
        Assert.Contains($"'{ScenarioRecoveryAuditTaxonomy.Action}'", migration);
        Assert.Contains("ck_audit_records_event_taxonomy", migration);
        Assert.Contains("ck_audit_records_action_taxonomy", migration);
    }

    private static string Read(params string[] segments) =>
        File.ReadAllText(Path.Combine([RepositoryRoot(), .. segments]));

    private static string ExtractMember(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"member not found: {signature}");
        var rest = source[start..];
        var next = Regex.Match(rest[signature.Length..], @"\n    (public|private|internal) ");
        return next.Success ? rest[..(signature.Length + next.Index)] : rest;
    }

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
