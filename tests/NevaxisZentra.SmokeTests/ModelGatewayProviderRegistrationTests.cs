using Microsoft.Extensions.DependencyInjection;
using NevaxisZentra.Api.ModelGateway;
using NevaxisZentra.Persistence.IntelligenceRuntime;
using NevaxisZentra.Persistence.ModelGateway;

namespace NevaxisZentra.SmokeTests;

/// <summary>A synthetic model must never be one of the providers a customer deployment can route to.</summary>
public sealed class ModelGatewayProviderRegistrationTests
{
    [Fact]
    public void TheMockCandidateModelIsRegisteredOnlyWhenSyntheticProvidersAreAllowed()
    {
        var production = new ServiceCollection();
        production.AddNevaxisZentraModelGateway(allowSyntheticProviders: false);
        var development = new ServiceCollection();
        development.AddNevaxisZentraModelGateway(allowSyntheticProviders: true);

        Assert.DoesNotContain(typeof(MockNevaxisCandidateModelProvider), ProviderTypes(production));
        Assert.Contains(typeof(LocalModelProvider), ProviderTypes(production));
        Assert.Contains(typeof(FrontierModelProvider), ProviderTypes(production));
        Assert.Contains(typeof(MockNevaxisCandidateModelProvider), ProviderTypes(development));
    }

    [Fact]
    public void TheDefaultRegistrationIsTheSafeOne()
    {
        var services = new ServiceCollection();
        services.AddNevaxisZentraModelGateway();

        Assert.DoesNotContain(typeof(MockNevaxisCandidateModelProvider), ProviderTypes(services));
    }

    private static IEnumerable<Type?> ProviderTypes(IServiceCollection services) =>
        services.Where(d => d.ServiceType == typeof(IModelProvider)).Select(d => d.ImplementationType);
}
