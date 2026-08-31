using CrevaFactOS.Persistence.MarketRates;

namespace CrevaFactOS.Api.MarketRates;

public sealed class NotConfiguredExchangeRateProvider : IExchangeRateProvider
{
    public string ProviderKey => MarketRatesProviderKeys.NotConfigured;

    public ExchangeRateProviderStatus GetStatus() =>
        new(
            ProviderKey,
            MarketRatesProviderModes.NotConfigured,
            false,
            "Piyasa kuru sağlayıcısı yapılandırılmadı.",
            MissingConfigurationKeys());

    public Task<ExchangeRateFetchResult> FetchLatestAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new ExchangeRateFetchResult(
            false,
            ProviderKey,
            string.Empty,
            null,
            null,
            MarketRatesFreshnessStates.Unknown,
            MarketRatesAvailabilityStates.NotConfigured,
            MarketRatesFailureCodes.ProviderNotConfigured,
            "Piyasa kuru sağlayıcısı yapılandırılmadı.",
            MissingConfigurationKeys(),
            [],
            "not-configured"));

    private static IReadOnlyList<string> MissingConfigurationKeys() =>
    [
        $"{ExchangeRateProviderOptions.SectionName}:{nameof(ExchangeRateProviderOptions.ProviderMode)}",
        $"{ExchangeRateProviderOptions.SectionName}:{nameof(ExchangeRateProviderOptions.OpenExchangeRatesAppId)}",
    ];
}
