using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CrevaFactOS.Persistence.Tests;

public sealed class PersistenceOptionsTests
{
    [Fact]
    public void DisabledPersistenceDoesNotRequireDatabaseConfiguration()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Persistence:Enabled"] = "false",
        });

        services.AddCrevaFactOSPersistence(configuration);

        Assert.NotNull(services.BuildServiceProvider().GetRequiredService<IOptions<PersistenceOptions>>());
    }

    [Theory]
    [InlineData("Persistence:Host", "", "Persistence:Host")]
    [InlineData("Persistence:Port", "0", "Persistence:Port")]
    [InlineData("Persistence:Database", "", "Persistence:Database")]
    [InlineData("Persistence:Username", "", "Persistence:Username")]
    [InlineData("Persistence:Password", "", "Persistence:Password")]
    public void EnabledPersistenceRejectsInvalidRequiredConfiguration(
        string invalidKey,
        string invalidValue,
        string expectedFailure)
    {
        var values = ValidConfiguration();
        values[invalidKey] = invalidValue;

        var exception = Assert.Throws<OptionsValidationException>(() =>
            new ServiceCollection().AddCrevaFactOSPersistence(CreateConfiguration(values)));

        Assert.Contains(expectedFailure, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("test-password", exception.Message, StringComparison.Ordinal);
    }

    private static Dictionary<string, string?> ValidConfiguration() => new()
    {
        ["Persistence:Enabled"] = "true",
        ["Persistence:Host"] = "localhost",
        ["Persistence:Port"] = "5432",
        ["Persistence:Database"] = "crevafactos_local",
        ["Persistence:Username"] = "crevafactos",
        ["Persistence:Password"] = "test-password",
        ["Persistence:SslMode"] = "Disable",
        ["Persistence:ConnectionTimeoutSeconds"] = "15",
        ["Persistence:CommandTimeoutSeconds"] = "30",
    };

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
