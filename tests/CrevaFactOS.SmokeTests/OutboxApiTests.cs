using System.Net;
using System.Net.Http.Json;
using CrevaFactOS.Api.Authentication;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.Outbox;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CrevaFactOS.SmokeTests;

public sealed class OutboxApiTests
{
    private static readonly Guid IdentityId = Guid.Parse("79000000-0000-4000-8000-000000000001");
    private static readonly Guid TenantId = Guid.Parse("79000000-0000-4000-8000-000000000002");
    private static readonly Guid MembershipId = Guid.Parse("79000000-0000-4000-8000-000000000003");

    [Fact]
    public async Task ExactPermissionsGrantProbeWriteAndSummaryRead()
    {
        await using var factory = CreateFactory(true);
        using var client = factory.CreateClient();
        using var post = CreateRequest(HttpMethod.Post, "/internal/outbox");
        post.Content = JsonContent.Create(new OutboxProbeRequest("probe:001", "probe-1", "ok"));
        using var write = await client.SendAsync(post);
        using var get = CreateRequest(HttpMethod.Get, "/internal/outbox/recent");
        using var read = await client.SendAsync(get);

        Assert.Equal(HttpStatusCode.Created, write.StatusCode);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
    }

    [Fact]
    public async Task MembershipAloneCannotReadOrWrite()
    {
        await using var factory = CreateFactory(false);
        using var client = factory.CreateClient();
        using var post = CreateRequest(HttpMethod.Post, "/internal/outbox");
        post.Content = JsonContent.Create(new OutboxProbeRequest("probe:001", "probe-1", "ok"));
        using var write = await client.SendAsync(post);
        using var get = CreateRequest(HttpMethod.Get, "/internal/outbox/recent");
        using var read = await client.SendAsync(get);

        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
    }

    [Fact]
    public async Task DiagnosticsAreUnavailableInProduction()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/internal/outbox/recent");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
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
                services.RemoveAll<IOutboxDiagnosticService>();
                services.RemoveAll<IOutboxReader>();
                services.AddScoped<IIdentitySubjectResolver, StubIdentityResolver>();
                services.AddScoped<ITenantMembershipResolver, StubMembershipResolver>();
                services.AddScoped<IPermissionEvaluator>(_ => new StubPermissionEvaluator(grant));
                services.AddScoped<IOutboxDiagnosticService, StubDiagnosticService>();
                services.AddScoped<IOutboxReader, StubReader>();
            });
        });

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(DevelopmentAuthenticationHandler.IssuerHeader, "outbox-tests");
        request.Headers.Add(DevelopmentAuthenticationHandler.SubjectHeader, "active-subject");
        request.Headers.Add("X-CrevaFactOS-Tenant", "tenant-a");
        return request;
    }

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
            Task.FromResult(grant && permissionKey is InfrastructurePermissionKeys.SystemOutboxWrite or InfrastructurePermissionKeys.SystemOutboxRead
                ? PermissionCheckResult.Grant() : PermissionCheckResult.Deny("Not assigned."));
    }

    private sealed class StubDiagnosticService : IOutboxDiagnosticService
    {
        public Task<OutboxWriteResult> AppendProbeAsync(OutboxProbeRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(OutboxWriteResult.Success(Guid.NewGuid(), duplicate: false));
    }

    private sealed class StubReader : IOutboxReader
    {
        public Task<IReadOnlyList<OutboxMessageSummary>> ReadRecentAsync(int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OutboxMessageSummary>>([]);
    }
}
