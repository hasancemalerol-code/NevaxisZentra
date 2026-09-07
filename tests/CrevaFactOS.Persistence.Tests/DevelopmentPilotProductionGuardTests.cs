using CrevaFactOS.Persistence.Development;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace CrevaFactOS.Persistence.Tests;

public sealed class DevelopmentPilotProductionGuardTests
{
    [Fact]
    public void ProductionGuardThrowsWhenPilotEnabledOutsideDevelopment()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DevelopmentPilotOptions.SectionName}:{nameof(DevelopmentPilotOptions.Enabled)}"] = "true",
            })
            .Build();

        var environment = new TestHostEnvironment("Production");

        var exception = Assert.Throws<InvalidOperationException>(
            () => DevelopmentPilotServiceCollectionExtensions.ValidateDevelopmentPilotProductionGuard(
                configuration,
                environment));

        Assert.Contains("Development pilot bootstrap cannot be enabled", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionGuardAllowsDisabledPilotOutsideDevelopment()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DevelopmentPilotOptions.SectionName}:{nameof(DevelopmentPilotOptions.Enabled)}"] = "false",
            })
            .Build();

        var environment = new TestHostEnvironment("Production");

        DevelopmentPilotServiceCollectionExtensions.ValidateDevelopmentPilotProductionGuard(
            configuration,
            environment);
    }

    [Fact]
    public async Task DisabledBootstrapReturnsWithoutExecuting()
    {
        var service = new DevelopmentPilotBootstrapService(
            Microsoft.Extensions.Options.Options.Create(new DevelopmentPilotOptions
            {
                Enabled = false,
            }),
            Microsoft.Extensions.Options.Options.Create(new Persistence.PersistenceOptions()),
            Microsoft.Extensions.Options.Options.Create(new SaasIdentity.DevelopmentSaasIdentityOptions()),
            new SaasIdentity.SaasIdentityMfaSecretProtector(
                Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create("test")));

        var result = await service.BootstrapAsync();

        Assert.False(result.Executed);
        Assert.Equal(0, result.ProductionPlanCount);
    }

    private static CrevaFactOSDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CrevaFactOSDbContext>()
            .UseNpgsql(
                PostgreSqlConnectionFactory.BuildConnectionString(CreateOptions()),
                npgsql => npgsql.MigrationsAssembly(CrevaFactOSDbContext.MigrationsAssemblyName))
            .Options;
        return new CrevaFactOSDbContext(options);
    }

    private static PersistenceOptions CreateOptions() => new()
    {
        Enabled = true,
        Host = "db.example.test",
        Port = 5544,
        Database = "foundation",
        Username = "foundation-user",
        Password = "not-for-logs",
        SslMode = "Disable",
        ConnectionTimeoutSeconds = 12,
        CommandTimeoutSeconds = 34,
        Pooling = false,
    };

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "CrevaFactOS.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
