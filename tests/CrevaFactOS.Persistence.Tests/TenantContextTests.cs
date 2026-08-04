using CrevaFactOS.Persistence.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CrevaFactOS.Persistence.Tests;

public sealed class TenantContextTests
{
    [Fact]
    public void TenantContextIsAvailableAfterItIsSet()
    {
        var tenantId = Guid.NewGuid();
        var accessor = new TenantContextAccessor();

        accessor.Set(new TenantContext(tenantId, "tenant-key"));

        Assert.True(accessor.IsAvailable);
        Assert.Equal(tenantId, accessor.RequireTenantId());
    }

    [Fact]
    public void MissingTenantContextFailsClearly()
    {
        var accessor = new TenantContextAccessor();

        var exception = Assert.Throws<InvalidOperationException>(() => accessor.RequireTenantId());

        Assert.Contains("required", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TenantContextCannotBeReplacedWithinScope()
    {
        var accessor = new TenantContextAccessor();
        accessor.Set(new TenantContext(Guid.NewGuid(), "tenant-a"));

        Assert.Throws<InvalidOperationException>(() =>
            accessor.Set(new TenantContext(Guid.NewGuid(), "tenant-b")));
    }

    [Fact]
    public void TenantContextDoesNotLeakBetweenDependencyInjectionScopes()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Enabled"] = "false",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddCrevaFactOSPersistence(configuration);
        using var provider = services.BuildServiceProvider();

        using var firstScope = provider.CreateScope();
        var firstAccessor = firstScope.ServiceProvider.GetRequiredService<TenantContextAccessor>();
        firstAccessor.Set(new TenantContext(Guid.NewGuid(), "tenant-a"));

        using var secondScope = provider.CreateScope();
        var secondAccessor = secondScope.ServiceProvider.GetRequiredService<TenantContextAccessor>();

        Assert.True(firstAccessor.IsAvailable);
        Assert.False(secondAccessor.IsAvailable);
        Assert.NotSame(firstAccessor, secondAccessor);
    }
}
