using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NevaxisFactOS.Configuration;
using NevaxisFactOS.Persistence.Tenancy;
using NevaxisFactOS.Persistence.Identity;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.AiDecisions;
using NevaxisFactOS.Persistence.Outbox;
using NevaxisFactOS.Persistence.SaasIdentity;
using NevaxisFactOS.Persistence.FactoryNetworkOptimization;
using NevaxisFactOS.Persistence.EnterpriseReliability;
using NevaxisFactOS.Persistence.GoldenDemoP1;
using NevaxisFactOS.Persistence.GoldenDemoP2;
using NevaxisFactOS.Persistence.GoldenDemoP2B;

namespace NevaxisFactOS.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisFactOSPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName("crevafactos");
        if (environment?.IsDevelopment() == true)
        {
            var keyRingDirectory = new DirectoryInfo(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "crevafactos",
                "DataProtection-Keys"));
            Directory.CreateDirectory(keyRingDirectory.FullName);
            dataProtectionBuilder.PersistKeysToFileSystem(keyRingDirectory);
            if (OperatingSystem.IsWindows())
            {
                dataProtectionBuilder.ProtectKeysWithDpapi();
            }
        }

        services.AddScoped<SaasIdentityMfaSecretProtector>();
        services.AddScoped<IIdentityEmailDeliveryProvider, UnavailableIdentityEmailDeliveryProvider>();
        services.AddScoped<ISaasIdentityRepository, UnavailableSaasIdentityRepository>();
        services.AddOptions<DevelopmentSaasIdentityOptions>()
            .Bind(configuration.GetSection(DevelopmentSaasIdentityOptions.SectionName))
            .PostConfigure(options =>
            {
                if (environment?.IsDevelopment() != true)
                {
                    options.Enabled = false;
                    return;
                }

                options.TenantOwnerPassword = GetSaasIdentityEnvironmentValue(
                    nameof(DevelopmentSaasIdentityOptions.TenantOwnerPassword),
                    options.TenantOwnerPassword);
                options.ActiveUserPassword = GetSaasIdentityEnvironmentValue(
                    nameof(DevelopmentSaasIdentityOptions.ActiveUserPassword),
                    options.ActiveUserPassword);
                options.DeactivatedUserPassword = GetSaasIdentityEnvironmentValue(
                    nameof(DevelopmentSaasIdentityOptions.DeactivatedUserPassword),
                    options.DeactivatedUserPassword);
                options.NoMembershipUserPassword = GetSaasIdentityEnvironmentValue(
                    nameof(DevelopmentSaasIdentityOptions.NoMembershipUserPassword),
                    options.NoMembershipUserPassword);
                options.MfaSharedSecret = GetSaasIdentityEnvironmentValue(
                    nameof(DevelopmentSaasIdentityOptions.MfaSharedSecret),
                    options.MfaSharedSecret);
            });

        if (environment?.IsDevelopment() == true)
        {
            services.AddScoped<IDevelopmentTokenDeliverySink, DevelopmentTokenDeliverySink>();
        }

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
        services.AddScoped<CorrelationContext>();
        services.AddScoped<IAuditWriter, UnavailableAuditWriter>();
        services.AddScoped<IAuditReader, UnavailableAuditReader>();
        services.AddScoped<IAiDecisionLedgerWriter, UnavailableAiDecisionLedgerWriter>();
        services.AddScoped<IAiDecisionLedgerReader, UnavailableAiDecisionLedgerReader>();
        services.AddScoped<IOutboxWriter, UnavailableOutboxWriter>();
        services.AddScoped<IOutboxReader, UnavailableOutboxReader>();
        services.AddScoped<IOutboxDiagnosticService, UnavailableOutboxDiagnosticService>();

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
        services.AddDbContext<NevaxisFactOSDbContext>(dbContextOptions =>
            dbContextOptions.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(NevaxisFactOSDbContext.MigrationsAssemblyName);
                    npgsqlOptions.MigrationsHistoryTable(NevaxisFactOSDbContext.MigrationHistoryTableName);
                    npgsqlOptions.CommandTimeout(options.CommandTimeoutSeconds);
                }));
        services.AddScoped<TenantDatabaseSession>();
        services.AddNevaxisFactOSFactoryNetwork();
        services.AddNevaxisFactOSEnterpriseReliability();
        services.AddNevaxisFactOSGoldenDemoP1();
        services.AddNevaxisFactOSGoldenDemoP2();
        services.AddNevaxisFactOSGoldenDemoP2B();
        services.AddScoped<IIdentitySubjectResolver, PostgreSqlIdentitySubjectResolver>();
        services.AddScoped<ITenantMembershipResolver, PostgreSqlTenantMembershipResolver>();
        services.AddScoped<IPermissionEvaluator, PostgreSqlPermissionEvaluator>();
        services.AddScoped<IAuditWriter, PostgreSqlAuditWriter>();
        services.AddScoped<IAuditReader, PostgreSqlAuditReader>();
        services.AddScoped<IAiDecisionLedgerWriter, PostgreSqlAiDecisionLedgerWriter>();
        services.AddScoped<IAiDecisionLedgerReader, PostgreSqlAiDecisionLedgerReader>();
        services.AddScoped<IOutboxWriter, PostgreSqlOutboxWriter>();
        services.AddScoped<IOutboxReader, PostgreSqlOutboxReader>();
        services.AddScoped<IOutboxDiagnosticService, PostgreSqlOutboxDiagnosticService>();
        services.AddScoped<ISaasIdentityRepository>(serviceProvider =>
            new SaasIdentityRepository(
                serviceProvider.GetRequiredService<NevaxisFactOSDbContext>(),
                serviceProvider.GetRequiredService<SaasIdentityMfaSecretProtector>(),
                serviceProvider.GetRequiredService<IIdentityEmailDeliveryProvider>(),
                serviceProvider.GetRequiredService<TenantContextAccessor>(),
                serviceProvider.GetService<IDevelopmentTokenDeliverySink>()));

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
        LegacyEnvironmentVariables.GetSectioned("PERSISTENCE", suffix) ?? fallback;

    private static int GetEnvironmentInt32(string suffix, int fallback)
    {
        var value = LegacyEnvironmentVariables.GetSectioned("PERSISTENCE", suffix);
        return string.IsNullOrWhiteSpace(value) ? fallback : int.Parse(value, provider: null);
    }

    private static bool GetEnvironmentBoolean(string suffix, bool fallback)
    {
        var value = LegacyEnvironmentVariables.GetSectioned("PERSISTENCE", suffix);
        return string.IsNullOrWhiteSpace(value) ? fallback : bool.Parse(value);
    }

    private static string GetSaasIdentityEnvironmentValue(string propertyName, string fallback) =>
        Environment.GetEnvironmentVariable($"DevelopmentSaasIdentity__{propertyName}") ?? fallback;
}
