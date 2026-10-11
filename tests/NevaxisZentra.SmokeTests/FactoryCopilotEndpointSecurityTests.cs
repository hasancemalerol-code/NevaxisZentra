using System.Net;
using System.Net.Http.Json;
using NevaxisZentra.Api.Authentication;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.FactoryCopilot;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NevaxisZentra.SmokeTests;

/// <summary>The Factory Copilot endpoints are authenticated, permission-gated and scoped to the resolved tenant.</summary>
public sealed class FactoryCopilotEndpointSecurityTests
{
    private static readonly Guid IdentityId = Guid.Parse("99000030-0000-4000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("99000030-0000-4000-8000-000000000002");
    private static readonly Guid MembershipId = Guid.Parse("99000030-0000-4000-8000-000000000003");
    private static readonly Guid FacilityId = Guid.Parse("99000030-0000-4000-8000-000000000004");

    [Fact]
    public async Task AnUnauthenticatedQueryIsRefusedAndNeverReachesTheService()
    {
        var service = new RecordingCopilotService();
        await using var factory = CreateFactory(new GrantingAccessResolver(), service);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/factory-copilot/query?facilityId={FacilityId:D}",
            new { queryText = "x", context = new { entryPoint = "LINE" } });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, service.Queries);
    }

    [Fact]
    public async Task AQueryWithoutThePermissionOrForAForeignFacilityIsForbiddenAndNeverReachesTheService()
    {
        var service = new RecordingCopilotService();
        var resolver = new DenyingAccessResolver();
        await using var factory = CreateFactory(resolver, service);
        using var client = factory.CreateClient();

        using var request = CreateRequest(HttpMethod.Post, $"/api/factory-copilot/query?facilityId={FacilityId:D}");
        request.Content = JsonContent.Create(new { queryText = "x", context = new { entryPoint = "LINE" } });
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, service.Queries);
        Assert.Equal(new[] { FactoryCopilotPermissionKeys.Query }, resolver.RequestedPermissions);
    }

    [Fact]
    public async Task AGrantedQueryUsesTheResolvedTenantAndFacilityNeverTheBody()
    {
        var service = new RecordingCopilotService();
        await using var factory = CreateFactory(new GrantingAccessResolver(), service);
        using var client = factory.CreateClient();

        using var request = CreateRequest(HttpMethod.Post, $"/api/factory-copilot/query?facilityId={FacilityId:D}");
        request.Content = JsonContent.Create(new
        {
            queryText = "Which machine had the most downtime?",
            tenantId = Guid.NewGuid(),
            context = new { entryPoint = "LINE", lineCode = "L1" },
        });
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, service.Queries);
        Assert.Equal(TenantId, service.LastTenantId);
        Assert.Equal(FacilityId, service.LastFacilityId);
    }

    [Fact]
    public async Task AnOversizedQueryIsAValidationErrorNotAServerError()
    {
        var service = new RecordingCopilotService
        {
            Throw = new FactoryCopilotException(FactoryCopilotFailureCodes.ValidationFailed, "Query text exceeds 1000 characters."),
        };
        await using var factory = CreateFactory(new GrantingAccessResolver(), service);
        using var client = factory.CreateClient();

        using var request = CreateRequest(HttpMethod.Post, $"/api/factory-copilot/query?facilityId={FacilityId:D}");
        request.Content = JsonContent.Create(new { queryText = new string('x', 1500), context = new { entryPoint = "LINE" } });
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("validation-failed", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(DevelopmentAuthenticationHandler.IssuerHeader, "local-tests");
        request.Headers.Add(DevelopmentAuthenticationHandler.SubjectHeader, "active-subject");
        request.Headers.Add("X-NevaxisZentra-Tenant", "tenant-a");
        return request;
    }

    private static WebApplicationFactory<Program> CreateFactory(IFacilityAccessResolver facilityResolver, IFactoryCopilotService copilot) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
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
                services.RemoveAll<IFactoryCopilotService>();
                services.AddSingleton(copilot);
            });
        });

    private sealed class StubIdentityResolver : IIdentitySubjectResolver
    {
        public Task<AuthenticatedIdentity?> ResolveActiveIdentityAsync(
            string externalIssuer, string externalSubject, CancellationToken cancellationToken = default) =>
            Task.FromResult<AuthenticatedIdentity?>(
                externalIssuer == "local-tests" && externalSubject == "active-subject"
                    ? new AuthenticatedIdentity(IdentityId, externalIssuer, externalSubject)
                    : null);
    }

    private sealed class StubMembershipResolver : ITenantMembershipResolver
    {
        public Task<TenantContext?> ResolveActiveMembershipAsync(
            Guid identitySubjectId, string tenantIdentifier, CancellationToken cancellationToken = default) =>
            Task.FromResult<TenantContext?>(
                identitySubjectId == IdentityId && tenantIdentifier == "tenant-a"
                    ? new TenantContext(TenantId, "tenant-a", MembershipId)
                    : null);
    }

    private sealed class GrantingAccessResolver : IFacilityAccessResolver
    {
        public Task<FacilityAccessResolutionResult> ResolveAsync(
            FacilityAccessRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(FacilityAccessResolutionResult.Grant(
                new FacilityAccessContext(TenantId, request.FacilityId, MembershipId, IdentityId, request.PermissionKey, "test", true),
                DateTimeOffset.UtcNow));
    }

    private sealed class DenyingAccessResolver : IFacilityAccessResolver
    {
        public List<string> RequestedPermissions { get; } = [];

        public Task<FacilityAccessResolutionResult> ResolveAsync(
            FacilityAccessRequest request, CancellationToken cancellationToken = default)
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

    private sealed class RecordingCopilotService : IFactoryCopilotService
    {
        public int Queries { get; private set; }
        public Guid LastTenantId { get; private set; }
        public Guid LastFacilityId { get; private set; }
        public Exception? Throw { get; init; }

        public Task<FactoryCopilotQueryResult> QueryAsync(
            Guid facilityId, Guid tenantId, string subjectId, FactoryCopilotQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            Queries++;
            LastTenantId = tenantId;
            LastFacilityId = facilityId;
            if (Throw is not null)
            {
                throw Throw;
            }

            return Task.FromResult(new FactoryCopilotQueryResult(
                FactoryCopilotIntents.GeneralQuestion,
                FactoryCopilotRoutingClasses.ModelRequired,
                false,
                null,
                new FactoryCopilotStructuredResponse("a", null, [], [], null, null, null, [], []),
                new FactoryCopilotProvenance(null, [], [], null, null, false, "x", "x", "SUFFICIENT", 1),
                [],
                new FactoryCopilotContextHeader("LINE", null, null, null, null, null, null),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow));
        }

        public Task<FactoryCopilotSuggestedQuestionsResult> GetSuggestedQuestionsAsync(
            Guid facilityId, FactoryCopilotContextInput context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("not used");
    }
}
