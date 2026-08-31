namespace CrevaFactOS.Persistence.MarketRates;

public static class MarketRatesProviderKeys
{
    public const string NotConfigured = "not-configured";
    public const string OpenExchangeRates = "open-exchange-rates";
}

public static class MarketRatesProviderModes
{
    public const string NotConfigured = "NotConfigured";
    public const string OpenExchangeRates = "OpenExchangeRates";
}

public static class MarketRatesAvailabilityStates
{
    public const string Available = "available";
    public const string Unavailable = "unavailable";
    public const string NotConfigured = "not-configured";
}

public static class MarketRatesFreshnessStates
{
    public const string Fresh = "fresh";
    public const string Delayed = "delayed";
    public const string Stale = "stale";
    public const string Unknown = "unknown";
}

public static class MarketRatesFreshnessPolicy
{
    // Open Exchange Rates free plan publishes at most hourly.
    public static readonly TimeSpan FreshLimit = TimeSpan.FromMinutes(90);
    public static readonly TimeSpan DelayedLimit = TimeSpan.FromMinutes(180);
}

public static class MarketRatesAutomaticRefreshPolicy
{
    // Trigger refresh once persisted observation age exceeds provider cadence + buffer.
    public static readonly TimeSpan RefreshAfter = TimeSpan.FromMinutes(70);

    // Never fetch from the hourly provider more than once per hour per refresh scope.
    public static readonly TimeSpan MinimumFetchInterval = TimeSpan.FromHours(1);

    public static bool ShouldAttemptRefresh(
        MarketRateSnapshot? snapshot,
        ExchangeRateProviderStatus status,
        DateTimeOffset nowUtc)
    {
        if (!status.IsConfigured
            || !string.Equals(status.ProviderKey, MarketRatesProviderKeys.OpenExchangeRates, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (snapshot is null)
        {
            return true;
        }

        if (snapshot.SynchronizedAtUtc is DateTimeOffset lastFetch
            && nowUtc - lastFetch < MinimumFetchInterval)
        {
            return false;
        }

        var referenceTime = snapshot.ProviderObservedAtUtc ?? snapshot.SynchronizedAtUtc;
        if (referenceTime is null)
        {
            return true;
        }

        return nowUtc - referenceTime.Value >= RefreshAfter;
    }

    public static string BuildRefreshScopeKey(MarketRatesOperationContext context) =>
        $"{context.TenantId:N}:{context.FacilityId:N}";
}

public static class MarketRatesCurrencyPairs
{
    public const string UsdTry = "USD/TRY";
    public const string EurTry = "EUR/TRY";
}

public static class MarketRatesAuditEventTypes
{
    public const string SnapshotRefreshed = "market-rates.snapshot.refreshed";
    public const string SnapshotRefreshFailed = "market-rates.snapshot.refresh-failed";
}

public static class MarketRatesPermissionKeys
{
    public const string Read = "dashboard.market-rates.read";
}

public static class MarketRatesFailureCodes
{
    public const string ProviderNotConfigured = "provider-not-configured";
    public const string ProviderFetchFailed = "provider-fetch-failed";
    public const string PersistenceUnavailable = "persistence-unavailable";
}

public static class MarketRatesDisclaimer
{
    public const string ReadOnlyContext =
        "Piyasa kurları yalnızca operasyonel bağlam içindir; fiyatlandırma, sipariş veya ERP işlemi tetiklemez.";
}
