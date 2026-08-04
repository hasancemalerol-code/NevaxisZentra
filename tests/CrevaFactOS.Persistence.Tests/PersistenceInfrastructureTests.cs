using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace CrevaFactOS.Persistence.Tests;

public sealed class PersistenceInfrastructureTests
{
    private const string Password = "not-for-logs";

    [Fact]
    public void ConnectionStringContainsExpectedSettings()
    {
        var connectionString = PostgreSqlConnectionFactory.BuildConnectionString(CreateOptions());
        var parsed = new NpgsqlConnectionStringBuilder(connectionString);

        Assert.Equal("db.example.test", parsed.Host);
        Assert.Equal(5544, parsed.Port);
        Assert.Equal("foundation", parsed.Database);
        Assert.Equal("foundation-user", parsed.Username);
        Assert.Equal(Password, parsed.Password);
        Assert.Equal(12, parsed.Timeout);
        Assert.Equal(34, parsed.CommandTimeout);
        Assert.False(parsed.Pooling);
    }

    [Fact]
    public void DiagnosticsAndRedactionDoNotExposePasswordOrCompleteConnectionString()
    {
        var options = CreateOptions();
        var connectionString = PostgreSqlConnectionFactory.BuildConnectionString(options);
        var diagnostics = PostgreSqlConnectionFactory.GetSafeEndpointDescription(options);
        var redacted = PostgreSqlConnectionFactory.RedactConnectionString(connectionString);

        Assert.DoesNotContain(Password, diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain(options.Username, diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, redacted, StringComparison.Ordinal);
        Assert.NotEqual(connectionString, redacted);
    }

    [Fact]
    public void DbContextConstructsWithNoProductEntityMappings()
    {
        using var context = CreateContext();

        Assert.Empty(context.Model.GetEntityTypes());
    }

    [Fact]
    public void MigrationAssemblyContainsOnlyBaselineMigration()
    {
        using var context = CreateContext();
        var migrations = context.Database.GetMigrations().ToArray();

        Assert.Single(migrations);
        Assert.EndsWith("_InitialInfrastructureBaseline", migrations[0], StringComparison.Ordinal);
        Assert.Equal(
            CrevaFactOSDbContext.MigrationsAssemblyName,
            context.GetService<IMigrationsAssembly>().Assembly.GetName().Name);
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
        Password = Password,
        SslMode = "Disable",
        ConnectionTimeoutSeconds = 12,
        CommandTimeoutSeconds = 34,
        Pooling = false,
    };
}
