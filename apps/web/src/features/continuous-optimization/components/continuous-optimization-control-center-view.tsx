import Link from "next/link";

import type {
  ContinuousOptimizationControlCenterApi,
  OptimizationOpportunityApi,
} from "../server/continuous-optimization-api-client";

import "../continuous-optimization.css";

type ContinuousOptimizationControlCenterViewProps = Readonly<{
  controlCenter: ContinuousOptimizationControlCenterApi;
}>;

function OpportunityList({ items, emptyLabel }: Readonly<{ items: readonly OptimizationOpportunityApi[]; emptyLabel: string }>) {
  if (items.length === 0) {
    return <p className="continuous-optimization-item__meta">{emptyLabel}</p>;
  }

  return items.map((item) => (
    <div key={`${item.scopeEntityType}:${item.scopeEntityId}:${item.title}`} className="continuous-optimization-item">
      <div className="continuous-optimization-item__title">{item.title}</div>
      <div className="continuous-optimization-item__meta">{item.summary}</div>
      <div className="continuous-optimization-item__meta">
        +{item.estimatedBenefit.toFixed(1)} beklenen kazanç · {item.riskClass} risk · {item.triggerType}
      </div>
    </div>
  ));
}

export function ContinuousOptimizationControlCenterView({
  controlCenter,
}: ContinuousOptimizationControlCenterViewProps) {
  const { kpis } = controlCenter;

  return (
    <div className="continuous-optimization-shell" data-testid="continuous-optimization-control-center">
      <header className="fact-section__header">
        <div>
          <p className="fact-section__eyebrow">ULTRA G</p>
          <h1>Sürekli Optimizasyon</h1>
          <p className="fact-section__subtitle">
            Olay güdümlü, deterministik-first optimizasyon kontrol yüzeyi
          </p>
        </div>
        <Link className="shift-ops-cta" href="/fabrika">
          Fabrika
        </Link>
      </header>

      <section className="continuous-optimization-kpis" data-testid="continuous-optimization-kpis">
        <div className="continuous-optimization-kpi">
          <div className="continuous-optimization-kpi__label">Aktif koşu</div>
          <div className="continuous-optimization-kpi__value">{kpis.runsActive}</div>
        </div>
        <div className="continuous-optimization-kpi">
          <div className="continuous-optimization-kpi__label">Nitelenen fırsat</div>
          <div className="continuous-optimization-kpi__value">{kpis.opportunitiesQualified}</div>
        </div>
        <div className="continuous-optimization-kpi">
          <div className="continuous-optimization-kpi__label">Doğrulanmış kazanç</div>
          <div className="continuous-optimization-kpi__value">{kpis.verifiedGains}</div>
        </div>
        <div className="continuous-optimization-kpi">
          <div className="continuous-optimization-kpi__label">Verified gain rate</div>
          <div className="continuous-optimization-kpi__value">{(kpis.verifiedGainRate * 100).toFixed(0)}%</div>
        </div>
      </section>

      <section className="continuous-optimization-section">
        <div className="continuous-optimization-section__header">
          <h2>Aktif fırsatlar</h2>
        </div>
        <div className="continuous-optimization-section__body" data-testid="continuous-optimization-opportunities">
          <OpportunityList items={controlCenter.activeOpportunities} emptyLabel="Aktif optimizasyon fırsatı yok." />
        </div>
      </section>

      <section className="continuous-optimization-section">
        <div className="continuous-optimization-section__header">
          <h2>Onay bekleyen</h2>
        </div>
        <div className="continuous-optimization-section__body" data-testid="continuous-optimization-approval">
          {controlCenter.awaitingApproval.length === 0 ? (
            <p className="continuous-optimization-item__meta">Onay bekleyen optimizasyon yok.</p>
          ) : (
            controlCenter.awaitingApproval.map((run) => (
              <div key={run.runId} className="continuous-optimization-item">
                <div className="continuous-optimization-item__title">{run.lineCode ?? run.optimizationType}</div>
                <div className="continuous-optimization-item__meta">{run.triggerType} · {run.currentPhase}</div>
              </div>
            ))
          )}
        </div>
      </section>

      <section className="continuous-optimization-section">
        <div className="continuous-optimization-section__header">
          <h2>Doğrulanmış kazançlar</h2>
        </div>
        <div className="continuous-optimization-section__body" data-testid="continuous-optimization-verified-gains">
          {controlCenter.verifiedGains.length === 0 ? (
            <p className="continuous-optimization-item__meta">Henüz doğrulanmış kazanç yok.</p>
          ) : (
            controlCenter.verifiedGains.map((gain) => (
              <div key={gain.runId} className="continuous-optimization-item">
                <div className="continuous-optimization-item__title">{gain.lineCode}</div>
                <div className="continuous-optimization-item__meta">{gain.summary}</div>
              </div>
            ))
          )}
        </div>
      </section>
    </div>
  );
}

type ContinuousOptimizationOpportunityStripProps = Readonly<{
  opportunities: readonly OptimizationOpportunityApi[];
}>;

export function ContinuousOptimizationOpportunityStrip({
  opportunities,
}: ContinuousOptimizationOpportunityStripProps) {
  if (opportunities.length === 0) {
    return null;
  }

  return (
    <section className="fact-section" data-testid="continuous-optimization-factory-strip">
      <div className="fact-section__header">
        <div>
          <p className="fact-section__eyebrow">Sürekli optimizasyon</p>
          <h3>Anlamlı fırsatlar</h3>
        </div>
        <Link className="shift-ops-cta" href="/surekli-optimizasyon">
          Tümünü gör
        </Link>
      </div>
      <div className="continuous-optimization-opportunity-strip fact-section__body">
        {opportunities.slice(0, 3).map((item) => (
          <div key={`${item.title}:${item.scopeEntityId}`} className="continuous-optimization-opportunity-strip__item">
            <div>
              <div className="continuous-optimization-item__title">{item.title}</div>
              <div className="continuous-optimization-item__meta">{item.summary}</div>
            </div>
            <div className="continuous-optimization-opportunity-strip__benefit">+{item.estimatedBenefit.toFixed(1)}</div>
          </div>
        ))}
      </div>
    </section>
  );
}
