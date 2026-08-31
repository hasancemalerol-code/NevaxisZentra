export const MARKET_RATES_DISCLAIMER =
  "Piyasa kurları yalnızca operasyonel bağlam içindir; fiyatlandırma, sipariş veya ERP işlemi tetiklemez.";

type MarketRateQuotePresentation = Readonly<{
  pairLabel: string;
  baseCurrency: string;
  quoteCurrency: string;
  rate: number | null;
  changePercent: number | null;
  changePercentAvailable: boolean;
}>;

type MarketRateSummaryPresentation = Readonly<{
  providerKey: string;
  sourceLabel: string;
  sourceAttributionUrl: string | null;
  providerObservedAtUtc: string | null;
  synchronizedAtUtc: string | null;
  freshnessState: string;
  freshnessLabel: string;
  availabilityState: string;
  availabilityLabel: string;
  configurationMessage: string | null;
  missingConfigurationKeys: readonly string[];
  lastFetchFailureCode: string | null;
  disclaimer: string;
  quotes: readonly MarketRateQuotePresentation[];
}>;

export const MARKET_RATES_PLACEHOLDER_QUOTES: readonly MarketRateQuotePresentation[] = [
  {
    pairLabel: "USD/TRY",
    baseCurrency: "USD",
    quoteCurrency: "TRY",
    rate: null,
    changePercent: null,
    changePercentAvailable: false,
  },
  {
    pairLabel: "EUR/TRY",
    baseCurrency: "EUR",
    quoteCurrency: "TRY",
    rate: null,
    changePercent: null,
    changePercentAvailable: false,
  },
];

export function buildProviderNotConfiguredSummary(
  overrides?: Partial<MarketRateSummaryPresentation>,
): MarketRateSummaryPresentation {
  return {
    providerKey: "not-configured",
    sourceLabel: "—",
    sourceAttributionUrl: null,
    providerObservedAtUtc: null,
    synchronizedAtUtc: null,
    freshnessState: "unknown",
    freshnessLabel: "Bilinmiyor",
    availabilityState: "not-configured",
    availabilityLabel: "Yapılandırma gerekli",
    configurationMessage: "Piyasa kuru sağlayıcısı yapılandırılmadı.",
    missingConfigurationKeys: [
      "MarketRates:ProviderMode",
      "MarketRates:OpenExchangeRatesAppId",
    ],
    lastFetchFailureCode: "provider-not-configured",
    disclaimer: MARKET_RATES_DISCLAIMER,
    quotes: MARKET_RATES_PLACEHOLDER_QUOTES,
    ...overrides,
  };
}

export function buildAccessRestrictedSummary(
  message = "Piyasa kuru özeti için yetkiniz bulunmuyor.",
): MarketRateSummaryPresentation {
  return {
    providerKey: "not-configured",
    sourceLabel: "—",
    sourceAttributionUrl: null,
    providerObservedAtUtc: null,
    synchronizedAtUtc: null,
    freshnessState: "unknown",
    freshnessLabel: "Bilinmiyor",
    availabilityState: "unavailable",
    availabilityLabel: "Erişim kısıtlı",
    configurationMessage: message,
    missingConfigurationKeys: [],
    lastFetchFailureCode: null,
    disclaimer: MARKET_RATES_DISCLAIMER,
    quotes: MARKET_RATES_PLACEHOLDER_QUOTES,
  };
}

export function buildSourceUnavailableSummary(
  message = "Piyasa kuru özeti şu anda kullanılamıyor.",
): MarketRateSummaryPresentation {
  return {
    providerKey: "not-configured",
    sourceLabel: "—",
    sourceAttributionUrl: null,
    providerObservedAtUtc: null,
    synchronizedAtUtc: null,
    freshnessState: "unknown",
    freshnessLabel: "Bilinmiyor",
    availabilityState: "unavailable",
    availabilityLabel: "Kullanılamıyor",
    configurationMessage: message,
    missingConfigurationKeys: [],
    lastFetchFailureCode: "source-unavailable",
    disclaimer: MARKET_RATES_DISCLAIMER,
    quotes: MARKET_RATES_PLACEHOLDER_QUOTES,
  };
}
