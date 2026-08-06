using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CrevaFactOS.Persistence.Development;

public static class DevelopmentPilotServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSDevelopmentPilot(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        if (!environment.IsDevelopment())
        {
            return services;
        }

        var persistenceEnabled = configuration.GetValue<bool?>("Persistence:Enabled") ?? false;

        services.AddOptions<DevelopmentPilotOptions>()
            .Bind(configuration.GetSection(DevelopmentPilotOptions.SectionName))
            .Validate(
                options =>
                {
                    if (!options.Enabled)
                    {
                        return true;
                    }

                    return !string.IsNullOrWhiteSpace(options.TenantKey)
                        && !string.IsNullOrWhiteSpace(options.DevelopmentIssuer)
                        && !string.IsNullOrWhiteSpace(options.DevelopmentSubject)
                        && !string.IsNullOrWhiteSpace(options.RoleKey)
                        && !string.IsNullOrWhiteSpace(options.FacilityCode)
                        && !string.IsNullOrWhiteSpace(options.FacilityTimeZoneId);
                },
                "Development pilot options are incomplete.")
            .ValidateOnStart();

        if (persistenceEnabled)
        {
            services.AddScoped<IDevelopmentPilotBootstrapService, DevelopmentPilotBootstrapService>();
        }

        return services;
    }

    public static void ValidateDevelopmentPilotProductionGuard(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        var enabled = configuration
            .GetSection(DevelopmentPilotOptions.SectionName)
            .GetValue<bool?>(nameof(DevelopmentPilotOptions.Enabled))
            ?? false;

        if (enabled)
        {
            throw new InvalidOperationException(
                "Development pilot bootstrap cannot be enabled outside the Development environment.");
        }
    }
}
