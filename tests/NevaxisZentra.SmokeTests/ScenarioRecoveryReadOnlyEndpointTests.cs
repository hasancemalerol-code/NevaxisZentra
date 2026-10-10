using System.Net;
using System.Net.Http.Json;
using NevaxisZentra.Api.Authentication;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.ScenarioRecovery;
using NevaxisZentra.Persistence.ThroughputIntelligence;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NevaxisZentra.SmokeTests;

/// <summary>
/// Viewing /senaryo-kurtarma must never create a scenario run or its options. Reading is side-effect-free GET;
/// creation is an explicit, simulate-gated, tenant-scoped, audited POST.
/// </summary>
public sealed class ScenarioRecoveryReadOnlyEndpointTests
{
    private static readonly Guid IdentityId = Guid.Parse("99000020-0000-4000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("99000020-0000-4000-8000-000000000002");
    private static readonly Guid MembershipId = Guid.Parse("99000020-0000-4000-8000-000000000003");
    private static readonly Guid FacilityId = Guid.Parse("99000020-0000-4000-8000-000000000004");
    private static readonly Guid LineId = Guid.Parse("99000020-0000-4000-8000-000000000005");

    [Fact]
    public async Task RepeatedGetsOnlyReadAndNeverCreateAnything()
    {
        var service = new RecordingScenarioService();
        await using var factory = CreateFactory(new GrantingAccessResolver(), service);
        using var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            using var history = await client.SendAsync(CreateRequest(HttpMethod.Get, $"/api/scenario-recovery/history?facilityId={FacilityId:D}&lineId={LineId:D}"));
            Assert.Equal(HttpStatusCode.OK, history.StatusCode);
            using var workspace = await client.SendAsync(CreateRequest(HttpMethod.Get, $"/api/scenario-recovery/runs/{Guid.NewGuid():D}/workspace?facilityId={FacilityId:D}"));
            Assert.Equal(HttpStatusCode.NotFound, workspace.StatusCode);
        }

        Assert.Equal(3, service.HistoryReads);
        Assert.Equal(3, service.WorkspaceReads);
        Assert.Equal(0, service.RunCreations);
        Assert.Equal(0, service.OptionGenerations);
    }

    [Theory]
    [InlineData("/api/scenario-recovery/runs")]
    [InlineData("/api/scenario-recovery/runs/00000000-0000-4000-8000-000000000001/generate-options")]
    public async Task CreationAndGenerationAreNotReachableByGet(string path)
    {
        var service = new RecordingScenarioService();
        await using var factory = CreateFactory(new GrantingAccessResolver(), service);
        using var client = factory.CreateClient();

        using var response = await client.SendAsync(CreateRequest(HttpMethod.Get, $"{path}?facilityId={FacilityId:D}"));

        Assert.True(
            response.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound,
            $"GET {path} answered {(int)response.StatusCode}");
        Assert.Equal(0, service.RunCreations);
        Assert.Equal(0, service.OptionGenerations);
    }

    [Fact]
    public async Task CreationWithoutTheSimulatePermissionDoesNothing()
    {
        var service = new RecordingScenarioService();
        var resolver = new DenyingAccessResolver();
        await using var factory = CreateFactory(resolver, service);
        using var client = factory.CreateClient();

        using var request = CreateRequest(HttpMethod.Post, $"/api/scenario-recovery/runs?facilityId={FacilityId:D}");
        request.Content = JsonContent.Create(CreateBody());
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, service.RunCreations);
        Assert.Equal(new[] { ScenarioRecoveryPermissionKeys.Simulate }, resolver.RequestedPermissions);
    }

    [Fact]
    public async Task ExplicitCreationCreatesExactlyOneRunScopedToTheResolvedTenantAndCarriesTheAuditContext()
    {
        var service = new RecordingScenarioService();
        await using var factory = CreateFactory(new GrantingAccessResolver(), service);
        using var client = factory.CreateClient();

        using var request = CreateRequest(HttpMethod.Post, $"/api/scenario-recovery/runs?facilityId={FacilityId:D}");
        request.Content = JsonContent.Create(CreateBody());
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, service.RunCreations);
        Assert.Equal(0, service.OptionGenerations);
        // The tenant and facility come from the resolved access, never from the request body.
        Assert.Equal(TenantId, service.LastTenantId);
        Assert.Equal(FacilityId, service.LastFacilityId);
        var audit = Assert.IsType<ScenarioRecoveryRunAudit>(service.LastAudit);
        Assert.Equal(IdentityId, audit.IdentitySubjectId);
        Assert.Equal(MembershipId, audit.MembershipId);
        Assert.Equal(ScenarioRecoveryPermissionKeys.Simulate, audit.PermissionKey);
        Assert.NotEqual(Guid.Empty, audit.CorrelationId);
    }

    private static object CreateBody() => new
    {
        lineId = LineId,
        category = "PRODUCTION_RECOVERY",
        entryPoint = "DIRECT",
        problem = new { summary = "test", contributingFactors = Array.Empty<string>() },
        objectives = new[] { new { objectiveType = "PROTECT_OTIF", label = "x", priority = 1, isHardConstraint = true } },
        sourceEntityType = "direct",
        sourceEntityId = LineId.ToString(),
    };

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(DevelopmentAuthenticationHandler.IssuerHeader, "local-tests");
        request.Headers.Add(DevelopmentAuthenticationHandler.SubjectHeader, "active-subject");
        request.Headers.Add("X-NevaxisZentra-Tenant", "tenant-a");
        return request;
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IFacilityAccessResolver facilityResolver,
        IScenarioRecoveryService scenarioService)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.UseEnvironment("Development");
                host.ConfigureAppConfiguration((_, cfg) =>
                    cfg.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Persistence:Enabled"] = "false",
                        ["DevelopmentPilot:Enabled"] = "false",
                    }));
                host.ConfigureServices(services =>
                {
                    services.ReplaceEntitlementEvaluatorWithAllowStub();
                    services.RemoveAll<IFacilityAccessResolver>();
                    services.AddSingleton(facilityResolver);
                    services.RemoveAll<IIdentitySubjectResolver>();
                    services.AddSingleton<IIdentitySubjectResolver>(new StubIdentityResolver());
                    services.RemoveAll<ITenantMembershipResolver>();
                    services.AddSingleton<ITenantMembershipResolver>(new StubMembershipResolver());
                    services.RemoveAll<IScenarioRecoveryService>();
                    services.AddSingleton(scenarioService);
                });
            });
    }

    private sealed class StubIdentityResolver : IIdentitySubjectResolver
    {
        public Task<AuthenticatedIdentity?> ResolveActiveIdentityAsync(
            string externalIssuer,
            string externalSubject,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AuthenticatedIdentity?>(
                externalIssuer == "local-tests" && externalSubject == "active-subject"
                    ? new AuthenticatedIdentity(IdentityId, externalIssuer, externalSubject)
                    : null);
    }

    private sealed class StubMembershipResolver : ITenantMembershipResolver
    {
        public Task<TenantContext?> ResolveActiveMembershipAsync(
            Guid identitySubjectId,
            string tenantIdentifier,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<TenantContext?>(
                identitySubjectId == IdentityId && tenantIdentifier == "tenant-a"
                    ? new TenantContext(TenantId, "tenant-a", MembershipId)
                    : null);
    }

    private sealed class GrantingAccessResolver : IFacilityAccessResolver
    {
        public Task<FacilityAccessResolutionResult> ResolveAsync(
            FacilityAccessRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(FacilityAccessResolutionResult.Grant(
                new FacilityAccessContext(TenantId, request.FacilityId, MembershipId, IdentityId, request.PermissionKey, "test", true),
                DateTimeOffset.UtcNow));
    }

    private sealed class DenyingAccessResolver : IFacilityAccessResolver
    {
        public List<string> RequestedPermissions { get; } = [];

        public Task<FacilityAccessResolutionResult> ResolveAsync(
            FacilityAccessRequest request,
            CancellationToken cancellationToken = default)
        {
            RequestedPermissions.Add(request.PermissionKey);
            return Task.FromResult(FacilityAccessResolutionResult.Deny(
                FacilityAccessFailureCodes.PermissionDenied,
                "Denied in test.",
                DateTimeOffset.UtcNow,
                facilityId: request.FacilityId,
                permissionKey: request.PermissionKey));
        }
    }

    private sealed class RecordingScenarioService : IScenarioRecoveryService
    {
        public int HistoryReads { get; private set; }
        public int WorkspaceReads { get; private set; }
        public int RunCreations { get; private set; }
        public int OptionGenerations { get; private set; }
        public Guid LastTenantId { get; private set; }
        public Guid LastFacilityId { get; private set; }
        public ScenarioRecoveryRunAudit? LastAudit { get; private set; }

        public Task<ScenarioRecoveryWorkspace> CreateRunAsync(
            Guid facilityId, Guid tenantId, CreateScenarioRecoveryRunRequest request, string subjectId,
            ScenarioRecoveryRunAudit? audit = null, CancellationToken cancellationToken = default)
        {
            RunCreations++;
            LastTenantId = tenantId;
            LastFacilityId = facilityId;
            LastAudit = audit;
            return Task.FromResult(new ScenarioRecoveryWorkspace(
                Guid.NewGuid(), null, request.LineId, "LINE-TEST", ScenarioRecoveryPhases.SnapshotCaptured, "SIMULATED",
                request.Category, request.EntryPoint, request.Problem, request.Objectives, null, null, null, null, null,
                false, false, "test", DateTimeOffset.UtcNow));
        }

        public Task<ScenarioRecoveryWorkspace> GetWorkspaceAsync(
            Guid facilityId, Guid runId, CancellationToken cancellationToken = default)
        {
            WorkspaceReads++;
            throw new ScenarioRecoveryException(ScenarioRecoveryFailureCodes.NotFound, "none");
        }

        public Task<ScenarioRecoveryWorkspace> GenerateOptionsAsync(
            Guid facilityId, Guid tenantId, Guid runId, string subjectId, CancellationToken cancellationToken = default)
        {
            OptionGenerations++;
            throw new InvalidOperationException("not used");
        }

        public Task<ScenarioRecoveryWorkspace> SelectOptionAsync(
            Guid facilityId, Guid tenantId, Guid runId, SelectScenarioRecoveryOptionRequest request, string subjectId,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("not used");

        public Task<ScenarioRecoveryWorkspace> AdvanceAsync(
            Guid facilityId, Guid tenantId, Guid runId, string subjectId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("not used");

        public Task<ScenarioRecoveryGoldenDemoResult> RunGoldenRecoveryDemoAsync(
            Guid facilityId, Guid tenantId, string subjectId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("not used");

        public Task<IReadOnlyList<ScenarioRecoveryHistoryItem>> ListHistoryAsync(
            Guid facilityId, Guid? lineId, CancellationToken cancellationToken = default)
        {
            HistoryReads++;
            return Task.FromResult<IReadOnlyList<ScenarioRecoveryHistoryItem>>([]);
        }

        public Task<ScenarioMetricSnapshot> SimulateActionAsync(
            Guid facilityId, Guid tenantId, Guid lineId, SimulateScenarioRecoveryActionRequest request, string subjectId,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("not used");
    }
}
