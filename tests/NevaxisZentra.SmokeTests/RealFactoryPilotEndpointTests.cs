using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NevaxisZentra.Api.Authentication;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Api.RealFactoryPilot;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.RealFactoryPilot;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NevaxisZentra.SmokeTests;

/// <summary>
/// Production must never create synthetic pilot data (PILOT-MES-SOURCE, PILOT-PLC-SOURCE, SYNTHETIC_PILOT_PASS) merely
/// because a page was viewed. Reading is a side-effect-free GET; initialization is an explicit, permission-gated,
/// audited POST that exists only for Development / Demo / Presentation deployments.
/// </summary>
public sealed class RealFactoryPilotEndpointTests
{
    private static readonly Guid IdentityId = Guid.Parse("99000010-0000-4000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("99000010-0000-4000-8000-000000000002");
    private static readonly Guid MembershipId = Guid.Parse("99000010-0000-4000-8000-000000000003");
    private static readonly Guid FacilityId = Guid.Parse("99000010-0000-4000-8000-000000000004");

    [Theory]
    [InlineData("Production", null, false)]
    [InlineData("Production", "", false)]
    [InlineData("Production", "PRODUCTION", false)]
    [InlineData("Production", "production", false)]
    [InlineData("Production", "SOMETHING-ELSE", false)]
    [InlineData("Staging", null, false)]
    [InlineData("Production", "DEMO", true)]
    [InlineData("Production", "presentation", true)]
    [InlineData("Development", null, true)]
    [InlineData("Testing", null, true)]
    public void SyntheticInitializationIsAllowedOnlyForDevelopmentDemoAndPresentation(
        string environmentName,
        string? channel,
        bool expected)
    {
        Assert.Equal(expected, SyntheticPilotInitializationPolicy.IsAllowed(environmentName, channel));
    }

    [Fact]
    public async Task GetCurrentNeverInitializesAndReportsAnEmptyTenantTruthfully()
    {
        var service = new RecordingPilotService();
        await using var factory = CreateFactory(new GrantingAccessResolver(), service);
        using var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            using var request = CreateRequest(HttpMethod.Get, $"/api/real-factory-pilot/current?facilityId={FacilityId:D}");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("workspace").ValueKind);
        }

        Assert.Equal(3, service.CurrentReads);
        Assert.Equal(0, service.Initializations);
        Assert.Equal(0, service.GoldenRuns);
    }

    [Fact]
    public async Task InitializeIsNotReachableByGet()
    {
        var service = new RecordingPilotService();
        await using var factory = CreateFactory(new GrantingAccessResolver(), service);
        using var client = factory.CreateClient();

        using var request = CreateRequest(HttpMethod.Get, $"/api/real-factory-pilot/initialize?facilityId={FacilityId:D}");
        using var response = await client.SendAsync(request);

        Assert.True(
            response.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound,
            $"GET /initialize answered {(int)response.StatusCode}");
        Assert.Equal(0, service.Initializations);
    }

    [Fact]
    public async Task InitializeIsAuthorizationGatedAndDoesNothingWhenDenied()
    {
        var service = new RecordingPilotService();
        await using var factory = CreateFactory(new DenyingAccessResolver(), service);
        using var client = factory.CreateClient();

        using var request = CreateRequest(HttpMethod.Post, $"/api/real-factory-pilot/initialize?facilityId={FacilityId:D}");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, service.Initializations);
        Assert.Equal(new[] { RealFactoryPilotPermissionKeys.Manage }, DenyingAccessResolver.RequestedPermissions);
    }

    [Fact]
    public async Task ExplicitInitializationInDevelopmentRequiresManageAndCarriesTheAuditContext()
    {
        var service = new RecordingPilotService();
        await using var factory = CreateFactory(new GrantingAccessResolver(), service);
        using var client = factory.CreateClient();

        using var request = CreateRequest(HttpMethod.Post, $"/api/real-factory-pilot/initialize?facilityId={FacilityId:D}");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, service.Initializations);
        var audit = Assert.IsType<RealFactoryPilotInitializationAudit>(service.LastAudit);
        Assert.Equal(IdentityId, audit.IdentitySubjectId);
        Assert.Equal(MembershipId, audit.MembershipId);
        Assert.Equal(RealFactoryPilotPermissionKeys.Manage, audit.PermissionKey);
        Assert.NotEqual(Guid.Empty, audit.CorrelationId);
    }

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
        IRealFactoryPilotService pilotService)
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
                    services.RemoveAll<IRealFactoryPilotService>();
                    services.AddSingleton(pilotService);
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
        public static readonly List<string> RequestedPermissions = [];

        public Task<FacilityAccessResolutionResult> ResolveAsync(
            FacilityAccessRequest request,
            CancellationToken cancellationToken = default)
        {
            RequestedPermissions.Clear();
            RequestedPermissions.Add(request.PermissionKey);
            return Task.FromResult(FacilityAccessResolutionResult.Deny(
                FacilityAccessFailureCodes.PermissionDenied,
                "Denied in test.",
                DateTimeOffset.UtcNow,
                facilityId: request.FacilityId,
                permissionKey: request.PermissionKey));
        }
    }

    private sealed class RecordingPilotService : IRealFactoryPilotService
    {
        public int CurrentReads { get; private set; }
        public int Initializations { get; private set; }
        public int GoldenRuns { get; private set; }
        public RealFactoryPilotInitializationAudit? LastAudit { get; private set; }

        public Task<RealFactoryPilotWorkspace> GetWorkspaceAsync(
            Guid facilityId, Guid pilotRunId, CancellationToken cancellationToken = default) =>
            throw new RealFactoryPilotException(RealFactoryPilotFailureCodes.NotFound, "none");

        public Task<RealFactoryPilotWorkspace?> GetCurrentWorkspaceAsync(
            Guid facilityId, CancellationToken cancellationToken = default)
        {
            CurrentReads++;
            return Task.FromResult<RealFactoryPilotWorkspace?>(null);
        }

        public Task<RealFactoryPilotWorkspace> InitializePilotAsync(
            Guid facilityId, Guid tenantId, string subjectId, RealFactoryPilotInitializationAudit? audit = null,
            CancellationToken cancellationToken = default)
        {
            Initializations++;
            LastAudit = audit;
            return Task.FromResult(new RealFactoryPilotWorkspace(
                Guid.NewGuid(), Guid.NewGuid(), "LINE-TEST", RealFactoryPilotPhases.Connect,
                RealFactoryPilotAcceptanceKinds.Synthetic, [], null, null, null, null, null, DateTimeOffset.UtcNow));
        }

        public Task<RealFactoryGoldenPilotResult> RunGoldenPilotAsync(
            Guid facilityId, Guid tenantId, string subjectId, CancellationToken cancellationToken = default)
        {
            GoldenRuns++;
            throw new InvalidOperationException("not used");
        }
    }
}
