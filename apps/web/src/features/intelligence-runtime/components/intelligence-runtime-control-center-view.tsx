import Link from "next/link";

import type { IntelligenceRuntimeControlCenterApi } from "../server/intelligence-runtime-api-client";

import "../intelligence-runtime.css";

type IntelligenceRuntimeControlCenterViewProps = Readonly<{
  controlCenter: IntelligenceRuntimeControlCenterApi;
}>;

function formatRate(rate: number): string {
  return `${(rate * 100).toFixed(1)}%`;
}

function formatCost(cost: number): string {
  return `$${cost.toFixed(2)}`;
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
          <p className="fact-section__eyebrow">ULTRA H</p>
          <h1>Intelligence Runtime</h1>
          <p className="fact-section__subtitle">
            Deterministik-first routing, model maliyeti ve yaşam döngüsü kontrol yüzeyi
          </p>
        </div>
        <Link className="shift-ops-cta" href="/fabrika">
          Fabrika
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
              Promosyon engellendi: {item.modelCode} · {item.evaluationMode} · {item.outcome}
            </div>
          ))}
          {rollbackEvaluations.map((item) => (
            <div
              key={item.evaluationRunId}
              className="intelligence-runtime-alert intelligence-runtime-alert--rollback"
              data-testid="intelligence-runtime-rollback"
            >
              Geri alma tetiklendi: {item.modelCode} · {item.outcome}
            </div>
          ))}
          {blockedModels.map((model) => (
            <div
              key={model.modelId}
              className="intelligence-runtime-alert intelligence-runtime-alert--blocked"
              data-testid="intelligence-runtime-promotion-blocked"
            >
              Model promosyonu engelli: {model.modelCode} · {model.evaluationStatus} / {model.safetyStatus}
            </div>
          ))}
        </div>
      )}

      <section className="intelligence-runtime-kpis" data-testid="intelligence-runtime-kpis">
        <div className="intelligence-runtime-kpi">
          <div className="intelligence-runtime-kpi__label">Deterministik</div>
          <div className="intelligence-runtime-kpi__value">{formatRate(metrics.deterministicRouteRate)}</div>
        </div>
        <div className="intelligence-runtime-kpi">
          <div className="intelligence-runtime-kpi__label">Analytics</div>
          <div className="intelligence-runtime-kpi__value">{formatRate(metrics.analyticsRouteRate)}</div>
        </div>
        <div className="intelligence-runtime-kpi">
          <div className="intelligence-runtime-kpi__label">Solver</div>
          <div className="intelligence-runtime-kpi__value">{formatRate(metrics.solverRouteRate)}</div>
        </div>
        <div className="intelligence-runtime-kpi">
          <div className="intelligence-runtime-kpi__label">Retrieval</div>
          <div className="intelligence-runtime-kpi__value">{formatRate(metrics.retrievalRouteRate)}</div>
        </div>
        <div className="intelligence-runtime-kpi">
          <div className="intelligence-runtime-kpi__label">Nevaxis model</div>
          <div className="intelligence-runtime-kpi__value">{formatRate(metrics.nevaxisModelCallRate)}</div>
        </div>
        <div className="intelligence-runtime-kpi">
          <div className="intelligence-runtime-kpi__label">Frontier model</div>
          <div className="intelligence-runtime-kpi__value">{formatRate(metrics.frontierModelCallRate)}</div>
        </div>
        <div className="intelligence-runtime-kpi">
          <div className="intelligence-runtime-kpi__label">Frontier fallback</div>
          <div className="intelligence-runtime-kpi__value">{formatRate(metrics.frontierFallbackRate)}</div>
        </div>
        <div className="intelligence-runtime-kpi intelligence-runtime-kpi--cost">
          <div className="intelligence-runtime-kpi__label">Toplam model maliyeti</div>
          <div className="intelligence-runtime-kpi__value">{formatCost(metrics.totalEstimatedCost)}</div>
        </div>
        <div className="intelligence-runtime-kpi intelligence-runtime-kpi--cost">
          <div className="intelligence-runtime-kpi__label">Doğrulanmış sonuç maliyeti</div>
          <div className="intelligence-runtime-kpi__value">
            {metrics.frontierCostPerVerifiedOutcome != null
              ? formatCost(metrics.frontierCostPerVerifiedOutcome)
              : "—"}
          </div>
        </div>
      </section>

      <section className="intelligence-runtime-section">
        <div className="intelligence-runtime-section__header">
          <h2>Son routing kararları</h2>
        </div>
        <div className="intelligence-runtime-section__body" data-testid="intelligence-runtime-routes">
          {controlCenter.recentRoutes.length === 0 ? (
            <p className="intelligence-runtime-item__meta">Henüz routing kaydı yok.</p>
          ) : (
            controlCenter.recentRoutes.map((route) => (
              <div key={route.routeId} className="intelligence-runtime-item">
                <div className="intelligence-runtime-item__row">
                  <div className="intelligence-runtime-item__title">{route.taskType}</div>
                  <span className="intelligence-runtime-badge">{route.routeType}</span>
                  {route.modelInvoked ? (
                    <span className="intelligence-runtime-badge intelligence-runtime-badge--limited">MODEL</span>
                  ) : null}
                </div>
                <div className="intelligence-runtime-item__meta">
                  {route.routingReason} · {formatCost(route.estimatedCost)} · {route.latencyMs}ms
                </div>
                <div className="intelligence-runtime-item__meta">{route.whyModelCalledOrSkipped}</div>
              </div>
            ))
          )}
        </div>
      </section>

      <section className="intelligence-runtime-section">
        <div className="intelligence-runtime-section__header">
          <h2>Dataset sürümleri</h2>
        </div>
        <div className="intelligence-runtime-section__body" data-testid="intelligence-runtime-datasets">
          {controlCenter.datasetVersions.length === 0 ? (
            <p className="intelligence-runtime-item__meta">Dataset sürümü bulunamadı.</p>
          ) : (
            controlCenter.datasetVersions.map((dataset) => (
              <div key={dataset.datasetVersionId} className="intelligence-runtime-item">
                <div className="intelligence-runtime-item__row">
                  <div className="intelligence-runtime-item__title">{dataset.versionCode}</div>
                  <span className="intelligence-runtime-badge">{dataset.readinessState}</span>
                </div>
                <div className="intelligence-runtime-item__meta">
                  {dataset.includedCaseCount} dahil · {dataset.excludedCaseCount} hariç · {dataset.schemaVersion}
                </div>
                <div className="intelligence-runtime-item__meta">Politika: {dataset.tenantPolicy}</div>
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
            <p className="intelligence-runtime-item__meta">Kayıtlı model yok.</p>
          ) : (
            controlCenter.models.map((model) => (
              <div key={model.modelId} className="intelligence-runtime-item">
                <div className="intelligence-runtime-item__row">
                  <div className="intelligence-runtime-item__title">{model.modelCode}</div>
                  <span className={`intelligence-runtime-badge ${modelStatusClass(model.status)}`}>
                    {model.status}
                  </span>
                  <span className="intelligence-runtime-badge">{model.evaluationStatus}</span>
                  <span className="intelligence-runtime-badge">{model.safetyStatus}</span>
                </div>
                <div className="intelligence-runtime-item__meta">
                  {model.version} · {model.baseModel}
                  {model.trainingDatasetVersion ? ` · dataset ${model.trainingDatasetVersion}` : ""}
                </div>
                <div className="intelligence-runtime-item__meta">
                  {model.taskCapabilities.join(", ")}
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
            <p className="intelligence-runtime-item__meta">Değerlendirme koşusu yok.</p>
          ) : (
            controlCenter.evaluations.map((evaluation) => (
              <div key={evaluation.evaluationRunId} className="intelligence-runtime-item">
                <div className="intelligence-runtime-item__row">
                  <div className="intelligence-runtime-item__title">{evaluation.modelCode}</div>
                  <span className={`intelligence-runtime-badge ${evaluationOutcomeClass(evaluation.outcome)}`}>
                    {evaluation.outcome}
                  </span>
                  <span className="intelligence-runtime-badge">{evaluation.evaluationMode}</span>
                </div>
                <div className="intelligence-runtime-gate-grid">
                  <div className="intelligence-runtime-gate">
                    <div className="intelligence-runtime-gate__label">Structured output</div>
                    <div className="intelligence-runtime-gate__value">
                      {formatRate(evaluation.structuredOutputValidityRate)}
                    </div>
                  </div>
                  <div className="intelligence-runtime-gate">
                    <div className="intelligence-runtime-gate__label">Safety pass</div>
                    <div className="intelligence-runtime-gate__value">{formatRate(evaluation.safetyPassRate)}</div>
                  </div>
                  <div className="intelligence-runtime-gate">
                    <div className="intelligence-runtime-gate__label">Hallucination rate</div>
                    <div className="intelligence-runtime-gate__value">
                      {formatRate(evaluation.hallucinatedEntityRate)}
                    </div>
                  </div>
                  <div className="intelligence-runtime-gate">
                    <div className="intelligence-runtime-gate__label">Maliyet / gecikme</div>
                    <div className="intelligence-runtime-gate__value">
                      {formatCost(evaluation.estimatedCost)} · {evaluation.latencyMs}ms
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
