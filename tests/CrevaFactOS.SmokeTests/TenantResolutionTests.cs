using System.Net;
using System.Net.Http.Json;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CrevaFactOS.SmokeTests;

public sealed class TenantResolutionTests
{
    private static readonly Guid ActiveTenantId = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd");

    [Fact]
    public async Task DevelopmentDiagnosticReportsResolvedActiveTenant()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/internal/tenant-context");
        request.Headers.Add("X-CrevaFactOS-Tenant", "active");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<TenantContextResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.HasTenantContext);
        Assert.Equal(ActiveTenantId, body.TenantId);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("unknown")]
    public async Task InactiveOrUnknownTenantIsRejected(string tenantIdentifier)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/internal/tenant-context");
        request.Headers.Add("X-CrevaFactOS-Tenant", tenantIdentifier);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MissingTenantHeaderRemainsAllowedOnTenantNeutralEndpoint()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ITenantResolver>();
                    services.AddScoped<ITenantResolver, StubTenantResolver>();
                });
            });

    private sealed class StubTenantResolver : ITenantResolver
    {
        public Task<TenantContext?> ResolveActiveTenantAsync(
            string tenantIdentifier,
            CancellationToken cancellationToken = default)
        {
            TenantContext? context = tenantIdentifier == "active"
                ? new TenantContext(ActiveTenantId, "active")
                : null;
            return Task.FromResult(context);
        }
    }

    private sealed record TenantContextResponse(bool HasTenantContext, Guid? TenantId);
}
