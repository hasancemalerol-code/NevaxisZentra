using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CrevaFactOS.Persistence.Tenancy;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.Authorization;

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
            .PostConfigure(ApplyEnvironmentOverrides)
            .ValidateOnStart();
        services.AddScoped<TenantContextAccessor>();
        services.AddScoped<ITenantContextAccessor>(serviceProvider =>
            serviceProvider.GetRequiredService<TenantContextAccessor>());
        services.AddScoped<CurrentIdentityAccessor>();
        services.AddScoped<ICurrentIdentityAccessor>(serviceProvider =>
            serviceProvider.GetRequiredService<CurrentIdentityAccessor>());
        services.AddScoped<IAuthorizationContext, CurrentAuthorizationContext>();
        services.AddScoped<IPermissionEvaluator, DenyAllPermissionEvaluator>();

        var options = configuration
            .GetSection(PersistenceOptions.SectionName)
            .Get<PersistenceOptions>() ?? new PersistenceOptions();
        ApplyEnvironmentOverrides(options);

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
        services.AddScoped<IIdentitySubjectResolver, PostgreSqlIdentitySubjectResolver>();
        services.AddScoped<ITenantMembershipResolver, PostgreSqlTenantMembershipResolver>();
        services.AddScoped<IPermissionEvaluator, PostgreSqlPermissionEvaluator>();

        if (options.HealthChecksEnabled)
        {
            services.AddHealthChecks().AddCheck<PostgreSqlReadinessHealthCheck>(
                "postgresql",
                tags: ["ready"]);
        }

        return services;
    }

    private static void ApplyEnvironmentOverrides(PersistenceOptions options)
    {
        options.Enabled = GetEnvironmentBoolean("ENABLED", options.Enabled);
        options.HealthChecksEnabled = GetEnvironmentBoolean(
            "HEALTH_CHECKS_ENABLED",
            options.HealthChecksEnabled);
        options.Host = GetEnvironmentValue("HOST", options.Host);
        options.Port = GetEnvironmentInt32("PORT", options.Port);
        options.Database = GetEnvironmentValue("DATABASE", options.Database);
        options.Username = GetEnvironmentValue("USERNAME", options.Username);
        options.Password = GetEnvironmentValue("PASSWORD", options.Password);
        options.SslMode = GetEnvironmentValue("SSL_MODE", options.SslMode);
        options.ConnectionTimeoutSeconds = GetEnvironmentInt32(
            "CONNECTION_TIMEOUT_SECONDS",
            options.ConnectionTimeoutSeconds);
        options.CommandTimeoutSeconds = GetEnvironmentInt32(
            "COMMAND_TIMEOUT_SECONDS",
            options.CommandTimeoutSeconds);
        options.Pooling = GetEnvironmentBoolean("POOLING", options.Pooling);
    }

    private static string GetEnvironmentValue(string suffix, string fallback) =>
        Environment.GetEnvironmentVariable($"CREVAFACTOS__PERSISTENCE__{suffix}") ?? fallback;

    private static int GetEnvironmentInt32(string suffix, int fallback)
    {
        var value = Environment.GetEnvironmentVariable($"CREVAFACTOS__PERSISTENCE__{suffix}");
        return string.IsNullOrWhiteSpace(value) ? fallback : int.Parse(value, provider: null);
    }

    private static bool GetEnvironmentBoolean(string suffix, bool fallback)
    {
        var value = Environment.GetEnvironmentVariable($"CREVAFACTOS__PERSISTENCE__{suffix}");
        return string.IsNullOrWhiteSpace(value) ? fallback : bool.Parse(value);
    }
}
