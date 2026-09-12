using NevaxisFactOS.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NevaxisFactOS.SmokeTests;

public sealed class InfrastructureOptionsTests
{
    [Fact]
    public void OptionalInfrastructureDoesNotRequireCredentials()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddInfrastructureOptions(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<InfrastructureOptions>>().Value;

        Assert.Equal("localhost", options.PostgreSql.Host);
        Assert.Equal(5432, options.PostgreSql.Port);
        Assert.Equal("localhost", options.RabbitMq.Host);
        Assert.Equal(5672, options.RabbitMq.AmqpPort);
    }

    [Fact]
    public void RequiredPostgreSqlRejectsMissingCredentials()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddInfrastructureOptions(
            configuration,
            InfrastructureDependency.PostgreSql);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<InfrastructureOptions>>().Value);
    }
}
