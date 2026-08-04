using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IValidateOptions<PersistenceOptions>, PersistenceOptionsValidator>();
        services.AddOptions<PersistenceOptions>()
            .Bind(configuration.GetSection(PersistenceOptions.SectionName))
            .ValidateOnStart();
        services.AddScoped<TenantContextAccessor>();
        services.AddScoped<ITenantContextAccessor>(serviceProvider =>
            serviceProvider.GetRequiredService<TenantContextAccessor>());

        var options = configuration
            .GetSection(PersistenceOptions.SectionName)
            .Get<PersistenceOptions>() ?? new PersistenceOptions();

        var validation = new PersistenceOptionsValidator().Validate(null, options);
        if (validation.Failed)
        {
            throw new OptionsValidationException(
                PersistenceOptions.SectionName,
                typeof(PersistenceOptions),
                validation.Failures);
        }

        if (!options.Enabled)
        {
            return services;
        }

        var connectionString = PostgreSqlConnectionFactory.BuildConnectionString(options);
        services.AddDbContext<CrevaFactOSDbContext>(dbContextOptions =>
            dbContextOptions.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(CrevaFactOSDbContext.MigrationsAssemblyName);
                    npgsqlOptions.MigrationsHistoryTable(CrevaFactOSDbContext.MigrationHistoryTableName);
                    npgsqlOptions.CommandTimeout(options.CommandTimeoutSeconds);
                }));
        services.AddScoped<TenantDatabaseSession>();
        services.AddScoped<ITenantResolver, PostgreSqlTenantResolver>();

        if (options.HealthChecksEnabled)
        {
            services.AddHealthChecks().AddCheck<PostgreSqlReadinessHealthCheck>(
                "postgresql",
                tags: ["ready"]);
        }

        return services;
    }
}
