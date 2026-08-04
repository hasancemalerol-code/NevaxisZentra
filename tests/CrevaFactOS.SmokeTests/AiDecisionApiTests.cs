using System.Net;
using System.Net.Http.Json;
using CrevaFactOS.Api.Authentication;
using CrevaFactOS.Persistence.AiDecisions;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CrevaFactOS.SmokeTests;

public sealed class AiDecisionApiTests
{
    private static readonly Guid IdentityId = Guid.Parse("78000000-0000-4000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("78000000-0000-4000-8000-000000000002");
    private static readonly Guid MembershipId = Guid.Parse("78000000-0000-4000-8000-000000000003");

    [Fact]
    public async Task ExactPermissionsGrantWriteAndReadDiagnostics()
    {
        await using var factory = CreateFactory(true);
        using var client = factory.CreateClient();
        using var post = CreateRequest(HttpMethod.Post, "/internal/ai-decisions");
        post.Content = JsonContent.Create(CreateWriteRequest());
        using var writeResponse = await client.SendAsync(post);
        using var get = CreateRequest(HttpMethod.Get, "/internal/ai-decisions/recent");
        using var readResponse = await client.SendAsync(get);

        Assert.Equal(HttpStatusCode.Created, writeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
    }

    [Fact]
    public async Task MembershipAloneCannotReadOrWrite()
    {
        await using var factory = CreateFactory(false);
        using var client = factory.CreateClient();
        using var post = CreateRequest(HttpMethod.Post, "/internal/ai-decisions");
        post.Content = JsonContent.Create(CreateWriteRequest());
        using var writeResponse = await client.SendAsync(post);
        using var get = CreateRequest(HttpMethod.Get, "/internal/ai-decisions/recent");
        using var readResponse = await client.SendAsync(get);

        Assert.Equal(HttpStatusCode.Forbidden, writeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, readResponse.StatusCode);
    }

    [Fact]
    public async Task EndpointsAreAbsentInProduction()
    {
        await using var production = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var productionClient = production.CreateClient();
        using var absent = await productionClient.GetAsync("/internal/ai-decisions/recent");

        Assert.NotEqual(HttpStatusCode.OK, absent.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(bool grant) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = DevelopmentAuthenticationHandler.SchemeName;
                        options.DefaultChallengeScheme = DevelopmentAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
                        DevelopmentAuthenticationHandler.SchemeName, _ => { });
                services.RemoveAll<IIdentitySubjectResolver>();
                services.RemoveAll<ITenantMembershipResolver>();
                services.RemoveAll<IPermissionEvaluator>();
                services.RemoveAll<IAiDecisionLedgerWriter>();
                services.RemoveAll<IAiDecisionLedgerReader>();
                services.AddScoped<IIdentitySubjectResolver, StubIdentityResolver>();
                services.AddScoped<ITenantMembershipResolver, StubMembershipResolver>();
                services.AddScoped<IPermissionEvaluator>(_ => new StubPermissionEvaluator(grant));
                services.AddScoped<IAiDecisionLedgerWriter, StubWriter>();
                services.AddScoped<IAiDecisionLedgerReader, StubReader>();
            });
        });

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(DevelopmentAuthenticationHandler.IssuerHeader, "ai-decision-tests");
        request.Headers.Add(DevelopmentAuthenticationHandler.SubjectHeader, "active-subject");
        request.Headers.Add("X-CrevaFactOS-Tenant", "tenant-a");
        return request;
    }

    private static AiDecisionWriteRequest CreateWriteRequest() => new(
        "diagnostic.recommendation", "foundation.diagnostic", "test-provider", "test-model",
        "recommendation.inspect", "Inspect evidence.", AiDecisionRiskLevel.Low, false,
        AiDecisionStatus.Proposed,
        [new("diagnostic.reference", "ref-1", "test.fixture", DateTimeOffset.UtcNow, "1")]);

    private sealed class StubIdentityResolver : IIdentitySubjectResolver
    {
        public Task<AuthenticatedIdentity?> ResolveActiveIdentityAsync(string issuer, string subject, CancellationToken cancellationToken = default) =>
            Task.FromResult<AuthenticatedIdentity?>(new(IdentityId, issuer, subject));
    }

    private sealed class StubMembershipResolver : ITenantMembershipResolver
    {
        public Task<TenantContext?> ResolveActiveMembershipAsync(Guid identitySubjectId, string tenantIdentifier, CancellationToken cancellationToken = default) =>
            Task.FromResult<TenantContext?>(new(TenantId, "tenant-a", MembershipId));
    }

    private sealed class StubPermissionEvaluator(bool grant) : IPermissionEvaluator
    {
        public Task<PermissionCheckResult> EvaluateAsync(Guid identitySubjectId, Guid tenantId, Guid membershipId, string permissionKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(grant && permissionKey is InfrastructurePermissionKeys.SystemAiDecisionWrite or InfrastructurePermissionKeys.SystemAiDecisionRead
                ? PermissionCheckResult.Grant() : PermissionCheckResult.Deny("Not assigned."));
    }

    private sealed class StubWriter : IAiDecisionLedgerWriter
    {
        public Task<AiDecisionWriteResult> WriteAsync(AiDecisionWriteRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(AiDecisionWriteResult.Success(Guid.NewGuid(), Guid.NewGuid()));
    }

    private sealed class StubReader : IAiDecisionLedgerReader
    {
        public Task<IReadOnlyList<AiDecisionSummary>> ReadRecentAsync(int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AiDecisionSummary>>([]);
    }
}
