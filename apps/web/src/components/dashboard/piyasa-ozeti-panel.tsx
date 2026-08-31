import { readMarketRatesSummary } from "@/features/dashboard/server/market-rates-queries";

function formatRate(rate: number | null | undefined): string {
  if (rate == null || Number.isNaN(rate)) {
    return "—";
  }

  return new Intl.NumberFormat("tr-TR", {
    minimumFractionDigits: 4,
    maximumFractionDigits: 4,
  }).format(rate);
}

export async function PiyasaOzetiPanel() {
  const result = await readMarketRatesSummary();
  const summary = result.summary;
  const showRates = summary.availabilityState === "available";
  const statusLabel = showRates
    ? summary.freshnessLabel
    : summary.availabilityLabel;
  const badgeState = showRates
    ? summary.freshnessState
    : summary.availabilityState;

  return (
    <section
      aria-labelledby="piyasa-ozeti-title"
      className="command-center__market-rates-card dashboard-panel--market-rates"
      data-testid="dashboard-market-rates-panel"
    >
      <div className="command-center__market-rates-card-head">
        <h2
          className="command-center__market-rates-card-title"
          id="piyasa-ozeti-title"
        >
          Piyasa Özeti
        </h2>

        <span
          className={`command-center__market-rates-card-badge command-center__market-rates-card-badge--${badgeState}`}
        >
          {statusLabel}
        </span>
      </div>

      <div className="command-center__market-rates-card-quotes">
        {summary.quotes.map((quote) => (
          <div
            className="command-center__market-rates-card-quote"
            key={quote.pairLabel}
          >
            <span className="command-center__market-rates-card-pair">
              {quote.pairLabel}
            </span>
            <strong className="command-center__market-rates-card-rate">
              {showRates ? formatRate(quote.rate) : "—"}
            </strong>
          </div>
        ))}
      </div>
    </section>
  );
}
