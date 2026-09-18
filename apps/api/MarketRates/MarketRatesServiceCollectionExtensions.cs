using NevaxisZentra.Persistence.MarketRates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NevaxisZentra.Api.MarketRates;

public static class MarketRatesServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraMarketRates(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ExchangeRateProviderOptions>()
            .Bind(configuration.GetSection(ExchangeRateProviderOptions.SectionName))
            .PostConfigure(options => ApplyEnvironmentOverrides(configuration, options));

        services.AddHttpClient(nameof(OpenExchangeRatesExchangeRateProvider));

        services.AddSingleton<IExchangeRateProvider>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<ExchangeRateProviderOptions>>().Value;

            if (IsOpenExchangeRatesConfigured(options))
            {
                return ActivatorUtilities.CreateInstance<OpenExchangeRatesExchangeRateProvider>(serviceProvider);
            }

            return new NotConfiguredExchangeRateProvider();
        });

        services.AddScoped<MarketRateSourceSynchronizer>();
        services.AddScoped<IMarketRateRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentra.Persistence.NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableMarketRateRepository()
                : new MarketRateRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<NevaxisZentra.Persistence.Tenancy.TenantDatabaseSession>(),
                    serviceProvider.GetRequiredService<MarketRateSourceSynchronizer>(),
                    serviceProvider.GetRequiredService<IExchangeRateProvider>());
        });

        return services;
    }

    private static void ApplyEnvironmentOverrides(IConfiguration configuration, ExchangeRateProviderOptions options)
    {
        options.ProviderMode = TrimMarketRatesValue(GetMarketRatesEnvironmentValue(
            "PROVIDER_MODE",
            configuration,
            options.ProviderMode));
        options.OpenExchangeRatesBaseUrl = TrimMarketRatesValue(GetMarketRatesEnvironmentValue(
            "OPEN_EXCHANGE_RATES_BASE_URL",
            configuration,
            options.OpenExchangeRatesBaseUrl));
        options.OpenExchangeRatesAppId = TrimMarketRatesValue(GetMarketRatesEnvironmentValue(
            "OPEN_EXCHANGE_RATES_APP_ID",
            configuration,
            options.OpenExchangeRatesAppId));
    }

    private static string TrimMarketRatesValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string GetMarketRatesEnvironmentValue(
        string suffix,
        IConfiguration configuration,
        string fallback) =>
        Environment.GetEnvironmentVariable($"NEVAXIS_FACTOS__MARKET_RATES__{suffix}")
        ?? configuration[$"NevaxisZentra:MARKET_RATES:{suffix}"]
        ?? configuration[$"NEVAXIS_FACTOS__MARKET_RATES__{suffix}"]
        ?? configuration[$"{ExchangeRateProviderOptions.SectionName}:{MapConfigKey(suffix)}"]
        ?? fallback;

    private static string MapConfigKey(string suffix) =>
        suffix switch
        {
            "PROVIDER_MODE" => nameof(ExchangeRateProviderOptions.ProviderMode),
            "OPEN_EXCHANGE_RATES_BASE_URL" => nameof(ExchangeRateProviderOptions.OpenExchangeRatesBaseUrl),
            "OPEN_EXCHANGE_RATES_APP_ID" => nameof(ExchangeRateProviderOptions.OpenExchangeRatesAppId),
            _ => suffix,
        };

    private static bool IsOpenExchangeRatesConfigured(ExchangeRateProviderOptions options) =>
        string.Equals(options.ProviderMode, MarketRatesProviderModes.OpenExchangeRates, StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(options.OpenExchangeRatesAppId)
        && !string.IsNullOrWhiteSpace(options.OpenExchangeRatesBaseUrl);
}
