namespace CrevaFactOS.Persistence.MarketRates;

public sealed class ExchangeRateProviderOptions
{
    public const string SectionName = "MarketRates";

    public string ProviderMode { get; set; } = MarketRatesProviderModes.NotConfigured;

    public string OpenExchangeRatesBaseUrl { get; set; } = "https://openexchangerates.org/api";

    public string OpenExchangeRatesAppId { get; set; } = string.Empty;
}
