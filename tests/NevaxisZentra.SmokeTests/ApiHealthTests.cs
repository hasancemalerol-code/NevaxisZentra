using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NevaxisZentra.SmokeTests;

public sealed class ApiHealthTests
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpointReturnsHealthy(string path)
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(SmokeTestServiceStubs.ConfigureIsolatedDevelopmentHost);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
    }
}
