import Link from "next/link";

import { BULK_DATA_ONBOARDING_ROUTE } from "@/features/bulk-data-onboarding/domain/bulk-data-onboarding-labels";

import type { AppsOperationalOverviewModel } from "../server/apps-operational-overview-reader";
import {
  AppsOperationalOutputTrendChart,
  AppsOperationalQuantityBarChart,
} from "./apps-operational-analytics-charts";

type AppsOperationalOverviewSectionProps = Readonly<{
  overview: AppsOperationalOverviewModel;
}>;

function formatPercent(value: number | null): string {
  if (value === null || Number.isNaN(value)) {
    return "—";
  }

  return `%${value.toLocaleString("tr-TR", { maximumFractionDigits: 1 })}`;
}

export function AppsOperationalOverviewSection({ overview }: AppsOperationalOverviewSectionProps) {
  return (
    <section className="fact-section apps-operational-overview" data-testid="apps-operational-overview">
      <header className="fact-section__header">
        <div>
          <p className="fact-workspace__context">OPERASYONEL GENEL BAKIŞ</p>
          <h2 className="fact-section__title">Fabrika performansı ve öncelik sinyalleri</h2>
        </div>
      </header>
      <div className="fact-section__body">
        {overview.state === "empty" ? (
          <div className="apps-operational-overview__empty" data-testid="apps-operational-overview-empty">
            <p>Henüz operasyonel veri yok.</p>
            <div className="apps-operational-overview__actions">
              <Link href="/fabrika" className="commint-button commint-button--ghost">
                Fabrika yapısını yükle
              </Link>
              <Link href={BULK_DATA_ONBOARDING_ROUTE} className="commint-button commint-button--primary">
                Veri Aktarım Merkezi&apos;ni aç
              </Link>
            </div>
          </div>
        ) : (
          <>
            <div className="apps-operational-overview__metrics" data-testid="apps-operational-overview-metrics">
              {overview.kpis.map((metric) => (
                <div key={metric.label} className="apps-operational-overview__metric">
                  <span className="apps-operational-overview__metric-label">{metric.label}</span>
                  <strong className="apps-operational-overview__metric-value">{metric.value}</strong>
                </div>
              ))}
            </div>

            <div className="apps-operational-analytics-grid" data-testid="apps-operational-analytics-charts">
              <article className="apps-analytics-panel apps-analytics-panel--wide">
                <h3 className="apps-analytics-panel__title">Üretim / iyi çıktı trendi</h3>
                <AppsOperationalOutputTrendChart points={overview.outputTrend} />
              </article>
              <article className="apps-analytics-panel">
                <AppsOperationalQuantityBarChart
                  title="Kaynak tip dağılımı"
                  bars={overview.resourceTypeBars}
                  testId="apps-resource-type-chart"
                />
              </article>
              <article className="apps-analytics-panel">
                <AppsOperationalQuantityBarChart
                  title="Kaynak durum dağılımı"
                  bars={overview.resourceStateBars}
                  testId="apps-resource-state-chart"
                />
              </article>
              {overview.scrapSummary ? (
                <article className="apps-analytics-panel apps-analytics-panel--compact">
                  <h3 className="apps-analytics-panel__title">Kalite / fire</h3>
                  <dl className="apps-analytics-summary">
                    <div>
                      <dt>İyi</dt>
                      <dd>{overview.scrapSummary.goodQuantity.toLocaleString("tr-TR")}</dd>
                    </div>
                    <div>
                      <dt>Fire</dt>
                      <dd>{overview.scrapSummary.scrapQuantity.toLocaleString("tr-TR")}</dd>
                    </div>
                    <div>
                      <dt>Fire oranı</dt>
                      <dd>{formatPercent(overview.scrapSummary.scrapRatePercent)}</dd>
                    </div>
                  </dl>
                </article>
              ) : null}
              {overview.downtimeSummary ? (
                <article className="apps-analytics-panel apps-analytics-panel--compact">
                  <h3 className="apps-analytics-panel__title">Duruş</h3>
                  <dl className="apps-analytics-summary">
                    <div>
                      <dt>Aktif duruş</dt>
                      <dd>{overview.downtimeSummary.activeCount}</dd>
                    </div>
                    <div>
                      <dt>Toplam dakika</dt>
                      <dd>{overview.downtimeSummary.totalMinutes.toLocaleString("tr-TR")}</dd>
                    </div>
                  </dl>
                </article>
              ) : null}
            </div>

            {overview.riskHighlights.length > 0 ? (
              <div className="apps-operational-overview__risks" data-testid="apps-operational-overview-risks">
                <h3 className="apps-operational-overview__distribution-title">Öncelikli risk sinyalleri</h3>
                <ul>
                  {overview.riskHighlights.map((label) => (
                    <li key={label}>{label}</li>
                  ))}
                </ul>
              </div>
            ) : null}
          </>
        )}
      </div>
    </section>
  );
}
