using NevaxisFactOS.Persistence.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class CurrentIdentityTests
{
    [Fact]
    public void ResolvedIdentityIsAvailableAndCannotBeReplaced()
    {
        var accessor = new CurrentIdentityAccessor();
        var identity = new AuthenticatedIdentity(Guid.NewGuid(), "issuer", "subject");

        accessor.Set(identity);

        Assert.True(accessor.IsResolved);
        Assert.Equal(identity.IdentitySubjectId, accessor.RequireIdentitySubjectId());
        Assert.Throws<InvalidOperationException>(() =>
            accessor.Set(new AuthenticatedIdentity(Guid.NewGuid(), "other", "other")));
    }

    [Fact]
    public void MissingIdentityFailsClearly()
    {
        var accessor = new CurrentIdentityAccessor();

        Assert.Throws<InvalidOperationException>(() => accessor.RequireIdentitySubjectId());
    }

    [Fact]
    public void IdentityDoesNotLeakBetweenDependencyInjectionScopes()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Enabled"] = "false",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddNevaxisFactOSPersistence(configuration);
        using var provider = services.BuildServiceProvider();

        using var firstScope = provider.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<CurrentIdentityAccessor>();
        first.Set(new AuthenticatedIdentity(Guid.NewGuid(), "issuer", "subject"));

        using var secondScope = provider.CreateScope();
        var second = secondScope.ServiceProvider.GetRequiredService<CurrentIdentityAccessor>();

        Assert.True(first.IsResolved);
        Assert.False(second.IsResolved);
        Assert.NotSame(first, second);
    }
}
