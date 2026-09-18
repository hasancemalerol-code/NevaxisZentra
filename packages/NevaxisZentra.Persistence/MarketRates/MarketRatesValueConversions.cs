namespace NevaxisZentra.Persistence.MarketRates;

public static class MarketRatesValueConversions
{
    public static string ComputeFreshnessState(
        DateTimeOffset? providerObservedAtUtc,
        DateTimeOffset nowUtc,
        string? providerKey = null)
    {
        if (providerObservedAtUtc is null)
        {
            return MarketRatesFreshnessStates.Unknown;
        }

        var age = nowUtc - providerObservedAtUtc.Value;
        if (!UsesHourlyProviderCadence(providerKey))
        {
            return age <= MarketRatesFreshnessPolicy.FreshLimit
                ? MarketRatesFreshnessStates.Fresh
                : MarketRatesFreshnessStates.Stale;
        }

        if (age <= MarketRatesFreshnessPolicy.FreshLimit)
        {
            return MarketRatesFreshnessStates.Fresh;
        }

        if (age <= MarketRatesFreshnessPolicy.DelayedLimit)
        {
            return MarketRatesFreshnessStates.Delayed;
        }

        return MarketRatesFreshnessStates.Stale;
    }

    public static string ToDisplayFreshnessState(string state) =>
        state switch
        {
            MarketRatesFreshnessStates.Fresh => "Güncel",
            MarketRatesFreshnessStates.Delayed => "Gecikmeli",
            MarketRatesFreshnessStates.Stale => "Bayat",
            _ => "Bilinmiyor",
        };

    public static string ToDisplayAvailabilityState(string state) =>
        state switch
        {
            MarketRatesAvailabilityStates.Available => "Kullanılabilir",
            MarketRatesAvailabilityStates.Unavailable => "Kullanılamıyor",
            MarketRatesAvailabilityStates.NotConfigured => "Yapılandırma gerekli",
            _ => "Bilinmiyor",
        };

    public static IReadOnlyList<MarketRateQuoteReadModel> BuildExpectedPairPlaceholders() =>
    [
        new(MarketRatesCurrencyPairs.UsdTry, "USD", "TRY", null, null, false),
        new(MarketRatesCurrencyPairs.EurTry, "EUR", "TRY", null, null, false),
    ];

    private static bool UsesHourlyProviderCadence(string? providerKey) =>
        string.IsNullOrWhiteSpace(providerKey)
        || string.Equals(providerKey, MarketRatesProviderKeys.OpenExchangeRates, StringComparison.OrdinalIgnoreCase);
}
