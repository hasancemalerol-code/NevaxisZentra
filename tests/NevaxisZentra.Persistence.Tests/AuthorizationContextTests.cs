using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NevaxisZentra.Persistence.Tests;

public sealed class AuthorizationContextTests
{
    [Fact]
    public void ContextRequiresBothResolvedIdentityAndTenantMembership()
    {
        var identityAccessor = new CurrentIdentityAccessor();
        var tenantAccessor = new TenantContextAccessor();
        var context = new CurrentAuthorizationContext(identityAccessor, tenantAccessor);

        Assert.False(context.IsAvailable);
        identityAccessor.Set(new AuthenticatedIdentity(Guid.NewGuid(), "issuer", "subject"));
        Assert.False(context.IsAvailable);

        tenantAccessor.Set(new TenantContext(Guid.NewGuid(), "tenant", Guid.NewGuid()));

        Assert.True(context.IsAvailable);
        Assert.NotNull(context.Current);
    }

    [Fact]
    public void AuthorizationContextDoesNotLeakAcrossScopes()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Enabled"] = "false",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddNevaxisZentraPersistence(configuration);
        using var provider = services.BuildServiceProvider();

        using var firstScope = provider.CreateScope();
        firstScope.ServiceProvider.GetRequiredService<CurrentIdentityAccessor>()
            .Set(new AuthenticatedIdentity(Guid.NewGuid(), "issuer", "subject"));
        firstScope.ServiceProvider.GetRequiredService<TenantContextAccessor>()
            .Set(new TenantContext(Guid.NewGuid(), "tenant-a", Guid.NewGuid()));
        Assert.True(firstScope.ServiceProvider.GetRequiredService<IAuthorizationContext>().IsAvailable);

        using var secondScope = provider.CreateScope();
        Assert.False(secondScope.ServiceProvider.GetRequiredService<IAuthorizationContext>().IsAvailable);
    }
}
