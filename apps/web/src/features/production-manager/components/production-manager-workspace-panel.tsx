import Link from "next/link";

import {
  getProductionManagerAutonomyLabel,
  getProductionManagerConfidenceLabel,
  getProductionManagerDecisionTypeLabel,
  getProductionManagerStatusLabel,
  PRODUCTION_MANAGER_TITLE,
} from "../domain/production-manager-labels";
import { readProductionManagerWorkspace } from "../server/production-manager-reader";

export async function ProductionManagerWorkspacePanel() {
  const result = await readProductionManagerWorkspace();
  if (result.status === "failure") {
    return null;
  }

  const { workspace } = result;
  const attentionItems = workspace.predictions.slice(0, 4);
  const recommendations = workspace.pendingDecisions.slice(0, 4);

  return (
    <section className="commint-panel" data-testid="production-manager-workspace-panel">
      <header className="commint-panel__header">
        <div>
          <p className="commint-executive__eyebrow">AI ÜRETİM YÖNETİCİSİ</p>
          <h2 className="commint-panel__title">{PRODUCTION_MANAGER_TITLE}</h2>
        </div>
      </header>

      <div className="commint-kpi-grid" data-testid="production-manager-executive-header">
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Fabrika Durumu</p>
          <p className="commint-kpi-card__value">{workspace.executive.factoryStatusLabel}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Aktif Riskler</p>
          <p className="commint-kpi-card__value">{workspace.executive.activeRiskCount}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Tahmin Edilen Sorunlar</p>
          <p className="commint-kpi-card__value">{workspace.executive.predictedIssueCount}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Karar Bekleyenler</p>
          <p className="commint-kpi-card__value">{workspace.executive.pendingDecisionCount}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Otonom Aksiyonlar</p>
          <p className="commint-kpi-card__value">{workspace.executive.autonomousActionCount}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Bugün Doğrulanan Kararlar</p>
          <p className="commint-kpi-card__value">{workspace.executive.verifiedTodayCount}</p>
        </article>
      </div>

      <div className="commint-two-column">
        <section data-testid="production-manager-attention-queue">
          <h3>Şimdi Dikkat Gerektirenler</h3>
          {attentionItems.length === 0 ? (
            <p className="commint-table__secondary">Şu anda kritik tahmin yok.</p>
          ) : (
            <ul>
              {attentionItems.map((item) => (
                <li key={item.id}>
                  <strong>{item.title}</strong>
                  <p>{item.summary}</p>
                  <span>{getProductionManagerConfidenceLabel(item.confidenceBand)}</span>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section data-testid="production-manager-recommendations">
          <h3>AI Üretim Yöneticisi Önerileri</h3>
          {recommendations.length === 0 ? (
            <p className="commint-table__secondary">Bekleyen karar önerisi yok.</p>
          ) : (
            <ul>
              {recommendations.map((decision) => (
                <li key={decision.id}>
                  <Link href={`/operasyon/yonetim-merkezi/kararlar/${decision.id}`}>
                    {decision.title}
                  </Link>
                  <p>{getProductionManagerDecisionTypeLabel(decision.decisionType)}</p>
                  <p>{getProductionManagerStatusLabel(decision.status)}</p>
                  <p>{getProductionManagerConfidenceLabel(decision.confidenceBand)}</p>
                  <p>{getProductionManagerAutonomyLabel(decision.autonomyMode)}</p>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>

      {workspace.bottlenecks.length > 0 ? (
        <section data-testid="production-manager-bottlenecks">
          <h3>Darboğazlar</h3>
          <ul>
            {workspace.bottlenecks.map((bottleneck) => (
              <li key={`${bottleneck.domain}-${bottleneck.summary}`}>
                {bottleneck.summary} ({bottleneck.severity})
              </li>
            ))}
          </ul>
        </section>
      ) : null}
    </section>
  );
}
