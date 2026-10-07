using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NevaxisZentra.Configuration;
using NevaxisZentra.Persistence.DataProtection;
using NevaxisZentra.Persistence.Tenancy;
using NevaxisZentra.Persistence.Identity;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.AiDecisions;
using NevaxisZentra.Persistence.Outbox;
using NevaxisZentra.Persistence.SaasIdentity;
using NevaxisZentra.Persistence.FactoryNetworkOptimization;
using NevaxisZentra.Persistence.EnterpriseReliability;
using NevaxisZentra.Persistence.GoldenDemoP1;
using NevaxisZentra.Persistence.GoldenDemoP2;
using NevaxisZentra.Persistence.GoldenDemoP2B;
using NevaxisZentra.Persistence.GoldenDemoP3;
using NevaxisZentra.Persistence.GoldenDemoCustomsFile;
namespace NevaxisZentra.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraPersistence(
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
        else if (environment is not null)
        {
            // Production-like hosts: persistent machine-local key ring (never the in-memory default), so
            // MFA secrets and other protected payloads survive API restarts and reboots.
            var keyRingDirectory = DataProtectionKeyRing.ResolveDirectory();
            DataProtectionKeyRing.Prepare(keyRingDirectory);
            DataProtectionKeyRing.Configure(dataProtectionBuilder, keyRingDirectory);
            services.AddSingleton(new DataProtectionKeyRingSettings(keyRingDirectory));
            services.AddHostedService<DataProtectionKeyRingInitializer>();
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

                if (!string.IsNullOrWhiteSpace(options.TenantOwnerPassword))
                {
                    options.Enabled = true;
                }
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
        services.AddDbContext<NevaxisZentraDbContext>(dbContextOptions =>
            dbContextOptions.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(NevaxisZentraDbContext.MigrationsAssemblyName);
                    npgsqlOptions.MigrationsHistoryTable(NevaxisZentraDbContext.MigrationHistoryTableName);
                    npgsqlOptions.CommandTimeout(options.CommandTimeoutSeconds);
                }));
        services.AddScoped<TenantDatabaseSession>();
        services.AddNevaxisZentraFactoryNetwork();
        services.AddNevaxisZentraEnterpriseReliability();
        services.AddNevaxisZentraGoldenDemoP1();
        services.AddNevaxisZentraGoldenDemoP2();
        services.AddNevaxisZentraGoldenDemoP2B();
        services.AddNevaxisZentraGoldenDemoP3();
        services.AddNevaxisZentraGoldenDemoCustomsFile();
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
                serviceProvider.GetRequiredService<NevaxisZentraDbContext>(),
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
