namespace NevaxisFactOS.Persistence.MarketRates;

public sealed record ExchangeRateQuoteRecord(
    string BaseCurrency,
    string QuoteCurrency,
    string PairLabel,
    decimal Rate,
    decimal? ChangePercent);

public sealed record ExchangeRateProviderStatus(
    string ProviderKey,
    string ProviderMode,
    bool IsConfigured,
    string? ConfigurationMessage,
    IReadOnlyList<string> MissingConfigurationKeys);

public sealed record ExchangeRateFetchResult(
    bool Success,
    string ProviderKey,
    string SourceLabel,
    string? SourceAttributionUrl,
    DateTimeOffset? ProviderObservedAtUtc,
    string FreshnessState,
    string AvailabilityState,
    string? FailureCode,
    string? ConfigurationMessage,
    IReadOnlyList<string> MissingConfigurationKeys,
    IReadOnlyList<ExchangeRateQuoteRecord> Quotes,
    string SourceFingerprint);

public interface IExchangeRateProvider
{
    string ProviderKey { get; }

    ExchangeRateProviderStatus GetStatus();

    Task<ExchangeRateFetchResult> FetchLatestAsync(CancellationToken cancellationToken = default);
}
