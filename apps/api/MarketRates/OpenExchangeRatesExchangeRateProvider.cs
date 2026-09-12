using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NevaxisFactOS.Persistence.MarketRates;
using Microsoft.Extensions.Options;

namespace NevaxisFactOS.Api.MarketRates;

public sealed class OpenExchangeRatesExchangeRateProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<ExchangeRateProviderOptions> options) : IExchangeRateProvider
{
    private const string SourceLabel = "Open Exchange Rates";
    private const string SourceAttributionUrl = "https://openexchangerates.org/terms";

    public string ProviderKey => MarketRatesProviderKeys.OpenExchangeRates;

    public ExchangeRateProviderStatus GetStatus()
    {
        var configured = IsConfigured(options.Value);
        return new ExchangeRateProviderStatus(
            ProviderKey,
            MarketRatesProviderModes.OpenExchangeRates,
            configured,
            configured ? null : "Open Exchange Rates App ID yapılandırılmadı.",
            configured ? [] : MissingConfigurationKeys());
    }

    public async Task<ExchangeRateFetchResult> FetchLatestAsync(CancellationToken cancellationToken = default)
    {
        var providerOptions = options.Value;
        if (!IsConfigured(providerOptions))
        {
            return new ExchangeRateFetchResult(
                false,
                ProviderKey,
                SourceLabel,
                SourceAttributionUrl,
                null,
                MarketRatesFreshnessStates.Unknown,
                MarketRatesAvailabilityStates.NotConfigured,
                MarketRatesFailureCodes.ProviderNotConfigured,
                "Open Exchange Rates App ID yapılandırılmadı.",
                MissingConfigurationKeys(),
                [],
                "not-configured");
        }

        var baseUrl = providerOptions.OpenExchangeRatesBaseUrl.TrimEnd('/');
        var requestUri =
            $"{baseUrl}/latest.json?app_id={Uri.EscapeDataString(providerOptions.OpenExchangeRatesAppId)}&symbols=TRY,EUR";

        try
        {
            var client = httpClientFactory.CreateClient(nameof(OpenExchangeRatesExchangeRateProvider));
            using var response = await client.GetAsync(requestUri, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return BuildFailure(
                    MarketRatesFailureCodes.ProviderFetchFailed,
                    $"Sağlayıcı yanıtı başarısız: {(int)response.StatusCode}");
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (!root.TryGetProperty("rates", out var ratesElement)
                || !ratesElement.TryGetProperty("TRY", out var tryRateElement)
                || !ratesElement.TryGetProperty("EUR", out var eurRateElement))
            {
                return BuildFailure(
                    MarketRatesFailureCodes.ProviderFetchFailed,
                    "Sağlayıcı yanıtında TRY/EUR kurları bulunamadı.");
            }

            var usdTry = tryRateElement.GetDecimal();
            var usdEur = eurRateElement.GetDecimal();
            if (usdEur <= 0)
            {
                return BuildFailure(
                    MarketRatesFailureCodes.ProviderFetchFailed,
                    "Sağlayıcı EUR/USD oranı geçersiz.");
            }

            var eurTry = usdTry / usdEur;
            DateTimeOffset? observedAtUtc = null;
            if (root.TryGetProperty("timestamp", out var timestampElement)
                && timestampElement.TryGetInt64(out var unixSeconds))
            {
                observedAtUtc = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            }

            var now = DateTimeOffset.UtcNow;
            var freshness = MarketRatesValueConversions.ComputeFreshnessState(observedAtUtc, now, ProviderKey);
            var quotes = new List<ExchangeRateQuoteRecord>
            {
                new("USD", "TRY", MarketRatesCurrencyPairs.UsdTry, usdTry, null),
                new("EUR", "TRY", MarketRatesCurrencyPairs.EurTry, eurTry, null),
            };

            var fingerprint = ComputeFingerprint(observedAtUtc, usdTry, eurTry);

            return new ExchangeRateFetchResult(
                true,
                ProviderKey,
                SourceLabel,
                SourceAttributionUrl,
                observedAtUtc,
                freshness,
                MarketRatesAvailabilityStates.Available,
                null,
                null,
                [],
                quotes,
                fingerprint);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return BuildFailure(
                MarketRatesFailureCodes.ProviderFetchFailed,
                "Sağlayıcıdan kur alınamadı.");
        }
    }

    private ExchangeRateFetchResult BuildFailure(string failureCode, string message) =>
        new(
            false,
            ProviderKey,
            SourceLabel,
            SourceAttributionUrl,
            null,
            MarketRatesFreshnessStates.Unknown,
            MarketRatesAvailabilityStates.Unavailable,
            failureCode,
            message,
            [],
            [],
            $"failed:{failureCode}");

    private static string ComputeFingerprint(DateTimeOffset? observedAtUtc, decimal usdTry, decimal eurTry)
    {
        var payload = string.Create(
            CultureInfo.InvariantCulture,
            $"{observedAtUtc?.ToUnixTimeSeconds()}|{usdTry:F6}|{eurTry:F6}");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }

    private static bool IsConfigured(ExchangeRateProviderOptions providerOptions) =>
        string.Equals(
            providerOptions.ProviderMode,
            MarketRatesProviderModes.OpenExchangeRates,
            StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(providerOptions.OpenExchangeRatesAppId)
        && !string.IsNullOrWhiteSpace(providerOptions.OpenExchangeRatesBaseUrl);

    private static IReadOnlyList<string> MissingConfigurationKeys() =>
    [
        $"{ExchangeRateProviderOptions.SectionName}:{nameof(ExchangeRateProviderOptions.ProviderMode)}",
        $"{ExchangeRateProviderOptions.SectionName}:{nameof(ExchangeRateProviderOptions.OpenExchangeRatesAppId)}",
    ];
}
