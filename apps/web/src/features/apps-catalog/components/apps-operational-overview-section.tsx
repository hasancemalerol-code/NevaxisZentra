import Link from "next/link";

import { BULK_DATA_ONBOARDING_ROUTE } from "@/features/bulk-data-onboarding/domain/bulk-data-onboarding-labels";

import type { AppsOperationalOverviewModel } from "../server/apps-operational-overview-reader";

type AppsOperationalOverviewSectionProps = Readonly<{
  overview: AppsOperationalOverviewModel;
}>;

export function AppsOperationalOverviewSection({ overview }: AppsOperationalOverviewSectionProps) {
  return (
    <section className="fact-section apps-operational-overview" data-testid="apps-operational-overview">
      <header className="fact-section__header">
        <div>
          <p className="fact-workspace__context">OPERASYONEL ÖZET</p>
          <h2 className="fact-section__title">Fabrika genelinde neler oluyor?</h2>
        </div>
      </header>
      <div className="fact-section__body">
        {overview.state === "empty" ? (
          <div className="apps-operational-overview__empty" data-testid="apps-operational-overview-empty">
            <p>
              Henüz yeterli operasyon verisi yok. Üretim kaynaklarınızı içeri aldığınızda bu alan gerçek
              özet ve dağılım gösterecek.
            </p>
            <div className="apps-operational-overview__actions">
              <Link href={BULK_DATA_ONBOARDING_ROUTE} className="commint-button commint-button--primary">
                Veri Aktarım Merkezi
              </Link>
              <Link href="/fabrika" className="commint-button commint-button--ghost">
                Fabrika genel bakış
              </Link>
            </div>
          </div>
        ) : (
          <>
            <div className="apps-operational-overview__metrics" data-testid="apps-operational-overview-metrics">
              {overview.metrics.map((metric) => (
                <div key={metric.label} className="apps-operational-overview__metric">
                  <span className="apps-operational-overview__metric-label">{metric.label}</span>
                  <strong className="apps-operational-overview__metric-value">{metric.value}</strong>
                </div>
              ))}
            </div>
            {overview.resourceDistribution.length > 0 ? (
              <div
                className="apps-operational-overview__distribution"
                data-testid="apps-operational-overview-distribution"
              >
                <h3 className="apps-operational-overview__distribution-title">Kaynak tip dağılımı</h3>
                <ul className="apps-operational-overview__bars">
                  {overview.resourceDistribution.map((bar) => {
                    const max = overview.resourceDistribution[0]?.count ?? 1;
                    const width = Math.max(8, Math.round((bar.count / max) * 100));
                    return (
                      <li key={bar.resourceType}>
                        <span className="apps-operational-overview__bar-label">{bar.label}</span>
                        <span className="apps-operational-overview__bar-track" aria-hidden="true">
                          <span className="apps-operational-overview__bar-fill" style={{ width: `${width}%` }} />
                        </span>
                        <span className="apps-operational-overview__bar-value">{bar.count}</span>
                      </li>
                    );
                  })}
                </ul>
              </div>
            ) : null}
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
