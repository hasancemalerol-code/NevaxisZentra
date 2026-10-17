import Link from "next/link";
import { Activity, ArrowLeft, BadgeCheck, Inbox, Lightbulb, Percent } from "lucide-react";

import type {
  ContinuousOptimizationControlCenterApi,
  OptimizationOpportunityApi,
} from "../server/continuous-optimization-api-client";

import "../continuous-optimization.css";

import "@/features/shift-operations/components/shift-operations-workspace.css";
import { MetricCard, MetricGrid } from "@/components/ui/metric";
import { PageHeader } from "@/components/ui/page";
import { Panel } from "@/components/ui/panel";
import { enumLabel } from "@/lib/presentation/enum-labels";
import { formatPercent } from "@/lib/presentation/format";

type ContinuousOptimizationControlCenterViewProps = Readonly<{
  controlCenter: ContinuousOptimizationControlCenterApi;
}>;

function OptimizationEmpty({ eyebrow, title, description }: Readonly<{ eyebrow: string; title: string; description: string }>) {
  return (
    <div className="continuous-optimization-empty" role="status">
      <span className="continuous-optimization-empty__icon" aria-hidden="true">
        <Inbox size={22} />
      </span>
      <p className="continuous-optimization-empty__eyebrow">{eyebrow}</p>
      <strong className="continuous-optimization-empty__title">{title}</strong>
      <p className="continuous-optimization-empty__text">{description}</p>
    </div>
  );
}

function OpportunityList({ items, emptyLabel }: Readonly<{ items: readonly OptimizationOpportunityApi[]; emptyLabel: string }>) {
  if (items.length === 0) {
    return <OptimizationEmpty eyebrow="FIRSAT YOK" title={emptyLabel} description="Yeni bir fırsat nitelendiğinde burada listelenir." />;
  }

  return items.map((item) => (
    <div key={`${item.scopeEntityType}:${item.scopeEntityId}:${item.title}`} className="continuous-optimization-item">
      <div className="continuous-optimization-item__title">{item.title}</div>
      <div className="continuous-optimization-item__meta">{item.summary}</div>
      <div className="continuous-optimization-item__meta">
        +{item.estimatedBenefit.toFixed(1)} beklenen kazanç · {enumLabel(item.riskClass)} risk · {enumLabel(item.triggerType)}
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
      <PageHeader
        eyebrow="Uygulamalar"
        title="Sürekli Optimizasyon"
        description="Fabrikadaki iyileştirme fırsatlarını izleyin, onay bekleyenleri yönetin ve doğrulanan kazançları görün."
        actions={
          <Link className="button button-secondary button-medium" href="/fabrika">
            <ArrowLeft aria-hidden="true" size={16} />
            Fabrika&apos;ya dön
          </Link>
        }
      />

      <div className="continuous-optimization-kpis" data-testid="continuous-optimization-kpis">
        <MetricGrid label="Optimizasyon özeti">
          <MetricCard label="Aktif koşu" value={kpis.runsActive} hint="Yürütülen optimizasyon" icon={<Activity size={18} />} tone="info" />
          <MetricCard label="Nitelenen fırsat" value={kpis.opportunitiesQualified} hint="Değerlendirmeye uygun" icon={<Lightbulb size={18} />} />
          <MetricCard label="Doğrulanmış kazanç" value={kpis.verifiedGains} hint="Ölçülerek onaylanan" icon={<BadgeCheck size={18} />} tone="low" />
          <MetricCard
            label="Kazanç oranı"
            value={formatPercent(kpis.verifiedGainRate * 100, 0)}
            hint="Kazanca dönüşen koşular"
            icon={<Percent size={18} />}
            tone="low"
          />
        </MetricGrid>
      </div>

      <Panel title="Aktif fırsatlar" subtitle="Kural tabanlı tarama sonucu nitelenen iyileştirme fırsatları">
        <div className="continuous-optimization-section__body" data-testid="continuous-optimization-opportunities">
          <OpportunityList items={controlCenter.activeOpportunities} emptyLabel="Aktif optimizasyon fırsatı yok" />
        </div>
      </Panel>

      <div className="continuous-optimization-pair">
        <Panel className="continuous-optimization-pair__card" title="Onay bekleyen" subtitle="İnsan onayı gereken optimizasyon koşuları">
          <div className="continuous-optimization-section__body" data-testid="continuous-optimization-approval">
            {controlCenter.awaitingApproval.length === 0 ? (
              <OptimizationEmpty eyebrow="ONAY YOK" title="Onay bekleyen optimizasyon yok" description="Onay gerektiren bir koşu oluştuğunda burada listelenir." />
            ) : (
              controlCenter.awaitingApproval.map((run) => (
                <div key={run.runId} className="continuous-optimization-item">
                  <div className="continuous-optimization-item__title">{run.lineCode ?? enumLabel(run.optimizationType)}</div>
                  <div className="continuous-optimization-item__meta">{enumLabel(run.triggerType)} · {enumLabel(run.currentPhase)}</div>
                </div>
              ))
            )}
          </div>
        </Panel>

        <Panel className="continuous-optimization-pair__card" title="Doğrulanmış kazançlar" subtitle="Uygulandıktan sonra ölçülerek doğrulanan sonuçlar">
          <div className="continuous-optimization-section__body" data-testid="continuous-optimization-verified-gains">
            {controlCenter.verifiedGains.length === 0 ? (
              <OptimizationEmpty eyebrow="KAZANÇ YOK" title="Henüz doğrulanmış kazanç yok" description="Ölçüm tamamlandığında burada görünür." />
            ) : (
              controlCenter.verifiedGains.map((gain) => (
                <div key={gain.runId} className="continuous-optimization-item">
                  <div className="continuous-optimization-item__title">{gain.lineCode}</div>
                  <div className="continuous-optimization-item__meta">{gain.summary}</div>
                </div>
              ))
            )}
          </div>
        </Panel>
      </div>
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
