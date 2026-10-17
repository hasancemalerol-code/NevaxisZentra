import Link from "next/link";

import type { IntelligenceRuntimeControlCenterApi } from "../server/intelligence-runtime-api-client";

import { EmptyPanel } from "@/components/ui/empty-panel";
import { enumLabel } from "@/lib/presentation/enum-labels";
import { formatRatioAsPercent } from "@/lib/presentation/format";

import "../intelligence-runtime.css";

import "@/features/shift-operations/components/shift-operations-workspace.css";

type IntelligenceRuntimeControlCenterViewProps = Readonly<{
  controlCenter: IntelligenceRuntimeControlCenterApi;
}>;

function formatRate(rate: number): string {
  return formatRatioAsPercent(rate);
}

/** Routing ratios are only meaningful once at least one routing record exists; until then they read as no data. */
export function formatRoutingRate(rate: number, routeCount: number): string {
  return routeCount > 0 ? formatRate(rate) : "—";
}

function modelStatusClass(status: string): string {
  switch (status.toUpperCase()) {
    case "CANDIDATE":
      return "intelligence-runtime-badge--candidate";
    case "SHADOW":
      return "intelligence-runtime-badge--shadow";
    case "LIMITED":
      return "intelligence-runtime-badge--limited";
    case "PRODUCTION":
      return "intelligence-runtime-badge--production";
    case "RETIRED":
      return "intelligence-runtime-badge--retired";
    default:
      return "";
  }
}

function evaluationOutcomeClass(outcome: string): string {
  if (outcome.toUpperCase() === "PASS") {
    return "intelligence-runtime-badge--pass";
  }
  return "intelligence-runtime-badge--fail";
}

function isPromotionBlocked(outcome: string): boolean {
  const normalized = outcome.toUpperCase();
  return normalized === "PROMOTION_BLOCKED" || normalized === "EVALUATION_FAILED";
}

function isRollback(outcome: string): boolean {
  return outcome.toUpperCase() === "ROLLBACK_TRIGGERED";
}

export function IntelligenceRuntimeControlCenterView({
  controlCenter,
}: IntelligenceRuntimeControlCenterViewProps) {
  const { metrics } = controlCenter;

  const blockedEvaluations = controlCenter.evaluations.filter((item) => isPromotionBlocked(item.outcome));
  const rollbackEvaluations = controlCenter.evaluations.filter((item) => isRollback(item.outcome));
  const blockedModels = controlCenter.models.filter(
    (model) =>
      isPromotionBlocked(model.evaluationStatus)
      || model.safetyStatus.toUpperCase() === "BLOCKED",
  );

  return (
    <div className="intelligence-runtime-shell" data-testid="intelligence-runtime-control-center">
      <header className="fact-section__header">
        <div>
          <p className="fact-section__eyebrow">ZEKÂ ALTYAPISI</p>
          <h1>Zekâ Altyapısı Durumu</h1>
          <p className="fact-section__subtitle">
            Kural tabanlı yönlendirme, model kullanımı ve değerlendirme sonuçlarının kontrol yüzeyi
          </p>
        </div>
        <Link className="button button--secondary" href="/fabrika">
          Fabrika Zekâsı’na git
        </Link>
      </header>

      {(blockedEvaluations.length > 0 || rollbackEvaluations.length > 0 || blockedModels.length > 0) && (
        <div className="intelligence-runtime-alerts">
          {blockedEvaluations.map((item) => (
            <div
              key={item.evaluationRunId}
              className="intelligence-runtime-alert intelligence-runtime-alert--blocked"
              data-testid="intelligence-runtime-promotion-blocked"
            >
              Yayın engellendi: {item.modelCode} · {enumLabel(item.evaluationMode)} · {enumLabel(item.outcome)}
            </div>
          ))}
          {rollbackEvaluations.map((item) => (
            <div
              key={item.evaluationRunId}
              className="intelligence-runtime-alert intelligence-runtime-alert--rollback"
              data-testid="intelligence-runtime-rollback"
            >
              Geri alma tetiklendi: {item.modelCode} · {enumLabel(item.outcome)}
            </div>
          ))}
          {blockedModels.map((model) => (
            <div
              key={model.modelId}
              className="intelligence-runtime-alert intelligence-runtime-alert--blocked"
              data-testid="intelligence-runtime-promotion-blocked"
            >
              Model yayını engelli: {model.modelCode} · {enumLabel(model.evaluationStatus)} / {enumLabel(model.safetyStatus)}
            </div>
          ))}
        </div>
      )}

      <p className="intelligence-runtime-kpis-note">Oranlar, yönlendirme kararlarının hangi yöntemle işlendiğini gösterir.</p>

      <section className="intelligence-runtime-kpis" data-testid="intelligence-runtime-kpis">
        <div className="intelligence-runtime-kpi">
          <div className="intelligence-runtime-kpi__label">Kural tabanlı</div>
          <div className="intelligence-runtime-kpi__value">{formatRoutingRate(metrics.deterministicRouteRate, controlCenter.recentRoutes.length)}</div>
        </div>
        <div className="intelligence-runtime-kpi">
          <div className="intelligence-runtime-kpi__label">Analiz</div>
          <div className="intelligence-runtime-kpi__value">{formatRoutingRate(metrics.analyticsRouteRate, controlCenter.recentRoutes.length)}</div>
        </div>
        <div className="intelligence-runtime-kpi">
          <div className="intelligence-runtime-kpi__label">Optimizasyon</div>
          <div className="intelligence-runtime-kpi__value">{formatRoutingRate(metrics.solverRouteRate, controlCenter.recentRoutes.length)}</div>
        </div>
        <div className="intelligence-runtime-kpi">
          <div className="intelligence-runtime-kpi__label">Bilgi arama</div>
          <div className="intelligence-runtime-kpi__value">{formatRoutingRate(metrics.retrievalRouteRate, controlCenter.recentRoutes.length)}</div>
        </div>
        <div className="intelligence-runtime-kpi">
          <div className="intelligence-runtime-kpi__label">Zentra modeli</div>
          <div className="intelligence-runtime-kpi__value">{formatRoutingRate(metrics.nevaxisModelCallRate, controlCenter.recentRoutes.length)}</div>
        </div>
        <div className="intelligence-runtime-kpi">
          <div className="intelligence-runtime-kpi__label">Harici model</div>
          <div className="intelligence-runtime-kpi__value">{formatRoutingRate(metrics.frontierModelCallRate, controlCenter.recentRoutes.length)}</div>
        </div>
        <div className="intelligence-runtime-kpi">
          <div className="intelligence-runtime-kpi__label">Harici modele geçiş</div>
          <div className="intelligence-runtime-kpi__value">{formatRoutingRate(metrics.frontierFallbackRate, controlCenter.recentRoutes.length)}</div>
        </div>
      </section>

      <section className="intelligence-runtime-section">
        <div className="intelligence-runtime-section__header">
          <h2>Son yönlendirme kararları</h2>
        </div>
        <div className="intelligence-runtime-section__body" data-testid="intelligence-runtime-routes">
          {controlCenter.recentRoutes.length === 0 ? (
            <EmptyPanel compact title="Henüz yönlendirme kaydı yok" description="Zekâ altyapısı bir istek yönlendirdiğinde kararlar burada listelenir." />
          ) : (
            controlCenter.recentRoutes.map((route) => (
              <div key={route.routeId} className="intelligence-runtime-item">
                <div className="intelligence-runtime-item__row">
                  <div className="intelligence-runtime-item__title">{enumLabel(route.taskType)}</div>
                  <span className="intelligence-runtime-badge">{enumLabel(route.routeType)}</span>
                  {route.modelInvoked ? (
                    <span className="intelligence-runtime-badge intelligence-runtime-badge--limited">Model kullanıldı</span>
                  ) : null}
                </div>
                <div className="intelligence-runtime-item__meta">
                  {route.routingReason} · {route.latencyMs} ms
                </div>
                <div className="intelligence-runtime-item__meta">{route.whyModelCalledOrSkipped}</div>
              </div>
            ))
          )}
        </div>
      </section>

      <section className="intelligence-runtime-section">
        <div className="intelligence-runtime-section__header">
          <h2>Veri seti sürümleri</h2>
        </div>
        <div className="intelligence-runtime-section__body" data-testid="intelligence-runtime-datasets">
          {controlCenter.datasetVersions.length === 0 ? (
            <EmptyPanel compact title="Henüz veri seti sürümü yok" />
          ) : (
            controlCenter.datasetVersions.map((dataset) => (
              <div key={dataset.datasetVersionId} className="intelligence-runtime-item">
                <div className="intelligence-runtime-item__row">
                  <div className="intelligence-runtime-item__title">{dataset.versionCode}</div>
                  <span className="intelligence-runtime-badge">{enumLabel(dataset.readinessState)}</span>
                </div>
                <div className="intelligence-runtime-item__meta">
                  {dataset.includedCaseCount} dahil · {dataset.excludedCaseCount} hariç
                </div>
              </div>
            ))
          )}
        </div>
      </section>

      <section className="intelligence-runtime-section">
        <div className="intelligence-runtime-section__header">
          <h2>Model kaydı</h2>
        </div>
        <div className="intelligence-runtime-section__body" data-testid="intelligence-runtime-models">
          {controlCenter.models.length === 0 ? (
            <EmptyPanel compact title="Kayıtlı model yok" />
          ) : (
            controlCenter.models.map((model) => (
              <div key={model.modelId} className="intelligence-runtime-item">
                <div className="intelligence-runtime-item__row">
                  <div className="intelligence-runtime-item__title">{model.modelCode}</div>
                  <span className={`intelligence-runtime-badge ${modelStatusClass(model.status)}`}>
                    {enumLabel(model.status)}
                  </span>
                  <span className="intelligence-runtime-badge">{enumLabel(model.evaluationStatus)}</span>
                  <span className="intelligence-runtime-badge">{enumLabel(model.safetyStatus)}</span>
                </div>
                <div className="intelligence-runtime-item__meta">
                  Sürüm {model.version}
                  {model.trainingDatasetVersion ? ` · veri seti ${model.trainingDatasetVersion}` : ""}
                </div>
                <div className="intelligence-runtime-item__meta">
                  {model.taskCapabilities.map((capability) => enumLabel(capability)).join(", ")}
                </div>
              </div>
            ))
          )}
        </div>
      </section>

      <section className="intelligence-runtime-section">
        <div className="intelligence-runtime-section__header">
          <h2>Değerlendirme karşılaştırması</h2>
        </div>
        <div className="intelligence-runtime-section__body" data-testid="intelligence-runtime-evaluations">
          {controlCenter.evaluations.length === 0 ? (
            <EmptyPanel compact title="Değerlendirme koşusu yok" />
          ) : (
            controlCenter.evaluations.map((evaluation) => (
              <div key={evaluation.evaluationRunId} className="intelligence-runtime-item">
                <div className="intelligence-runtime-item__row">
                  <div className="intelligence-runtime-item__title">{evaluation.modelCode}</div>
                  <span className={`intelligence-runtime-badge ${evaluationOutcomeClass(evaluation.outcome)}`}>
                    {enumLabel(evaluation.outcome)}
                  </span>
                  <span className="intelligence-runtime-badge">{enumLabel(evaluation.evaluationMode)}</span>
                </div>
                <div className="intelligence-runtime-gate-grid">
                  <div className="intelligence-runtime-gate">
                    <div className="intelligence-runtime-gate__label">Yapılandırılmış çıktı</div>
                    <div className="intelligence-runtime-gate__value">
                      {formatRate(evaluation.structuredOutputValidityRate)}
                    </div>
                  </div>
                  <div className="intelligence-runtime-gate">
                    <div className="intelligence-runtime-gate__label">Güvenlik başarısı</div>
                    <div className="intelligence-runtime-gate__value">{formatRate(evaluation.safetyPassRate)}</div>
                  </div>
                  <div className="intelligence-runtime-gate">
                    <div className="intelligence-runtime-gate__label">Hatalı bilgi oranı</div>
                    <div className="intelligence-runtime-gate__value">
                      {formatRate(evaluation.hallucinatedEntityRate)}
                    </div>
                  </div>
                  <div className="intelligence-runtime-gate">
                    <div className="intelligence-runtime-gate__label">Gecikme</div>
                    <div className="intelligence-runtime-gate__value">
                      {evaluation.latencyMs} ms
                    </div>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      </section>
    </div>
  );
}
