using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NevaxisZentra.Api.SaasIdentity;
using NevaxisZentra.Configuration.Bootstrap;
using NevaxisZentra.Persistence.Bootstrap;

namespace NevaxisZentra.SmokeTests;

internal sealed class SetRemoteAddressStartupFilter(IPAddress? address) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.Use(async (context, pipeline) =>
            {
                context.Connection.RemoteIpAddress = address;
                await pipeline();
            });
            next(app);
        };
}

internal sealed class FakeEnrollmentService : IFirstAccountEnrollmentService
{
    public bool Available { get; set; } = true;

    public FirstAccountEnrollmentResult Result { get; set; } = FirstAccountEnrollmentResult.Of(FirstAccountEnrollmentStatus.Enrolled);

    public int EnrollCalls { get; private set; }

    public FirstAccountEnrollmentRequest? LastRequest { get; private set; }

    public BootstrapAvailability GetAvailability() =>
        new(Available, Available ? new DateTimeOffset(2026, 10, 5, 9, 0, 0, TimeSpan.Zero) : null);

    public Task<FirstAccountEnrollmentResult> EnrollAsync(FirstAccountEnrollmentRequest request, CancellationToken cancellationToken)
    {
        EnrollCalls++;
        LastRequest = request;
        return Task.FromResult(Result);
    }
}

public sealed class BootstrapEnrollmentEndpointTests
{
    private const string Token = "TOKEN-VALUE-FOR-ENDPOINT-TESTS";
    private const string Password = "Str0ng-Passphrase-For-Tests!";

    private static DefaultHttpContext Context(string? remote, string host = "localhost", string? forwardedFor = null, string? origin = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = remote is null ? null : IPAddress.Parse(remote);
        context.Request.Host = new HostString(host);
        if (forwardedFor is not null)
        {
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        }

        if (origin is not null)
        {
            context.Request.Headers.Origin = origin;
        }

        return context;
    }

    [Theory]
    [InlineData("127.0.0.1", "127.0.0.1:5000", null, null, true)]
    [InlineData("::1", "localhost:3001", null, null, true)]
    [InlineData("::ffff:127.0.0.1", "localhost", null, null, true)]
    [InlineData("127.0.0.1", "localhost", "::1", "http://127.0.0.1:3001", true)]
    [InlineData("192.168.1.20", "localhost", null, null, false)]
    [InlineData("10.0.0.5", "127.0.0.1", null, null, false)]
    [InlineData(null, "localhost", null, null, false)]
    [InlineData("127.0.0.1", "evil.example", null, null, false)]
    [InlineData("127.0.0.1", "localhost", "203.0.113.9", null, false)]
    [InlineData("127.0.0.1", "localhost", "127.0.0.1, 203.0.113.9", null, false)]
    [InlineData("127.0.0.1", "localhost", null, "https://evil.example", false)]
    [InlineData("127.0.0.1", "localhost", null, "null", false)]
    public void OnlyLoopbackPeersAddressingLoopbackWithoutForeignForwardingOrOriginAreLocal(
        string? remote, string host, string? forwardedFor, string? origin, bool expected)
    {
        Assert.Equal(expected, BootstrapEnrollmentEndpoints.IsLocalRequest(Context(remote, host, forwardedFor, origin)));
    }

    private static WebApplicationFactory<Program> CreateFactory(FakeEnrollmentService? service, IPAddress? remote)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new SetRemoteAddressStartupFilter(remote));
                if (service is not null)
                {
                    services.AddSingleton<IFirstAccountEnrollmentService>(service);
                }
            });
        });
    }

    private static object Body(string? token = Token) => new
    {
        token,
        organizationCode = "acme-fab",
        organizationName = "Acme Fabrika",
        ownerEmail = "owner@acme.example",
        ownerDisplayName = "Ada Owner",
        password = Password,
    };

    [Fact]
    public async Task RemoteClientsCannotSeeOrUseEnrollment()
    {
        var service = new FakeEnrollmentService();
        await using var factory = CreateFactory(service, IPAddress.Parse("192.168.1.20"));
        using var client = factory.CreateClient();

        using var status = await client.GetAsync("/api/identity/bootstrap/status");
        using var enroll = await client.PostAsJsonAsync("/api/identity/bootstrap/enroll", Body());

        Assert.Equal(HttpStatusCode.NotFound, status.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, enroll.StatusCode);
        Assert.Equal(0, service.EnrollCalls);
    }

    [Fact]
    public async Task HostileHostAndOriginHeadersAreRejectedEvenFromLoopback()
    {
        var service = new FakeEnrollmentService();
        await using var factory = CreateFactory(service, IPAddress.Loopback);
        using var client = factory.CreateClient();

        using var rebinding = new HttpRequestMessage(HttpMethod.Post, "/api/identity/bootstrap/enroll")
        {
            Content = JsonContent.Create(Body()),
        };
        rebinding.Headers.Host = "evil.example";
        using var crossSite = new HttpRequestMessage(HttpMethod.Post, "/api/identity/bootstrap/enroll")
        {
            Content = JsonContent.Create(Body()),
        };
        crossSite.Headers.Add("Origin", "https://evil.example");
        using var forwarded = new HttpRequestMessage(HttpMethod.Post, "/api/identity/bootstrap/enroll")
        {
            Content = JsonContent.Create(Body()),
        };
        forwarded.Headers.Add("X-Forwarded-For", "203.0.113.9");

        foreach (var request in new[] { rebinding, crossSite, forwarded })
        {
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        Assert.Equal(0, service.EnrollCalls);
    }

    [Fact]
    public async Task LocalStatusReportsAvailabilityWithoutAnySecret()
    {
        var service = new FakeEnrollmentService();
        await using var factory = CreateFactory(service, IPAddress.Loopback);
        using var client = factory.CreateClient();

        var text = await client.GetStringAsync("/api/identity/bootstrap/status");

        using var document = JsonDocument.Parse(text);
        Assert.True(document.RootElement.GetProperty("available").GetBoolean());
        Assert.Contains("2026-10-05", text, StringComparison.Ordinal);
        Assert.DoesNotContain("token", text, StringComparison.OrdinalIgnoreCase);

        service.Available = false;
        using var closed = JsonDocument.Parse(await client.GetStringAsync("/api/identity/bootstrap/status"));
        Assert.False(closed.RootElement.GetProperty("available").GetBoolean());
        Assert.Equal(JsonValueKind.Null, closed.RootElement.GetProperty("expiresAtUtc").ValueKind);
    }

    [Theory]
    [InlineData(FirstAccountEnrollmentStatus.Enrolled, HttpStatusCode.Created)]
    [InlineData(FirstAccountEnrollmentStatus.InvalidRequest, HttpStatusCode.BadRequest)]
    [InlineData(FirstAccountEnrollmentStatus.InvalidToken, HttpStatusCode.Forbidden)]
    [InlineData(FirstAccountEnrollmentStatus.Expired, HttpStatusCode.Forbidden)]
    [InlineData(FirstAccountEnrollmentStatus.Unavailable, HttpStatusCode.NotFound)]
    [InlineData(FirstAccountEnrollmentStatus.AlreadyEnrolled, HttpStatusCode.Conflict)]
    [InlineData(FirstAccountEnrollmentStatus.InProgress, HttpStatusCode.Conflict)]
    [InlineData(FirstAccountEnrollmentStatus.Failed, HttpStatusCode.ServiceUnavailable)]
    public async Task EnrollmentOutcomesMapToHttpStatusesWithoutEchoingSecrets(
        FirstAccountEnrollmentStatus status, HttpStatusCode expected)
    {
        var service = new FakeEnrollmentService
        {
            Result = new FirstAccountEnrollmentResult(status, status == FirstAccountEnrollmentStatus.InvalidRequest ? ["password-too-short"] : [], null, "acme-fab"),
        };
        await using var factory = CreateFactory(service, IPAddress.Loopback);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/identity/bootstrap/enroll", Body());
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(1, service.EnrollCalls);
        Assert.Equal(Token, service.LastRequest!.Token);
        Assert.DoesNotContain(Token, text, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, text, StringComparison.Ordinal);
        Assert.DoesNotContain("owner@acme.example", text, StringComparison.Ordinal);
        if (status == FirstAccountEnrollmentStatus.InvalidRequest)
        {
            Assert.Contains("password-too-short", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task RepeatedInvalidTokensAreThrottled()
    {
        var service = new FakeEnrollmentService { Result = FirstAccountEnrollmentResult.Of(FirstAccountEnrollmentStatus.InvalidToken) };
        await using var factory = CreateFactory(service, IPAddress.Loopback);
        using var client = factory.CreateClient();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var rejected = await client.PostAsJsonAsync("/api/identity/bootstrap/enroll", Body("guess-" + attempt));
            Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
        }

        using var blocked = await client.PostAsJsonAsync("/api/identity/bootstrap/enroll", Body("guess-6"));

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal(5, service.EnrollCalls);
    }

    [Fact]
    public async Task EnrollmentIsDisabledOnHostsWithoutMachineBootstrapProvisioning()
    {
        // No NEVAXIS_ZENTRA_BOOTSTRAP_DIR: development, demo and every completed installation.
        await using var factory = CreateFactory(service: null, IPAddress.Loopback);
        using var client = factory.CreateClient();

        using var status = JsonDocument.Parse(await client.GetStringAsync("/api/identity/bootstrap/status"));
        using var enroll = await client.PostAsJsonAsync("/api/identity/bootstrap/enroll", Body());

        Assert.False(status.RootElement.GetProperty("available").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, enroll.StatusCode);
    }
}
