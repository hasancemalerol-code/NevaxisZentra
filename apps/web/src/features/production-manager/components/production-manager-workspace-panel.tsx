import Link from "next/link";

import { EmptyPanel } from "@/components/ui/empty-panel";
import {
  getProductionManagerAutonomyLabel,
  getProductionManagerConfidenceLabel,
  getProductionManagerDecisionTypeLabel,
  getProductionManagerStatusLabel,
  PRODUCTION_MANAGER_TITLE,
} from "../domain/production-manager-labels";
import { readProductionManagerWorkspace } from "../server/production-manager-reader";
import { enumLabel } from "@/lib/presentation/enum-labels";
import "./production-manager-workspace.css";

export async function ProductionManagerWorkspacePanel() {
  const result = await readProductionManagerWorkspace();
  if (result.status === "failure") {
    return null;
  }

  const { workspace } = result;
  const attentionItems = workspace.predictions.slice(0, 4);
  const recommendations = workspace.pendingDecisions.slice(0, 4);
  const isWorkspaceEmpty =
    attentionItems.length === 0 &&
    recommendations.length === 0 &&
    workspace.bottlenecks.length === 0 &&
    workspace.recentDecisions.length === 0;
  const factoryStatusLabel = isWorkspaceEmpty ? "—" : workspace.executive.factoryStatusLabel;

  return (
    <section className="commint-panel" data-testid="production-manager-workspace-panel">
      <header className="commint-panel__header">
        <div>
          <h2 className="commint-panel__title">{PRODUCTION_MANAGER_TITLE}</h2>
        </div>
      </header>

      <div className="commint-kpi-grid" data-testid="production-manager-executive-header">
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Fabrika durumu</p>
          <p className="commint-kpi-card__value production-manager-workspace__status-text">{factoryStatusLabel}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Aktif riskler</p>
          <p className="commint-kpi-card__value">{workspace.executive.activeRiskCount}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Tahmin edilen sorunlar</p>
          <p className="commint-kpi-card__value">{workspace.executive.predictedIssueCount}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Karar bekleyenler</p>
          <p className="commint-kpi-card__value">{workspace.executive.pendingDecisionCount}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Otonom aksiyonlar</p>
          <p className="commint-kpi-card__value">{workspace.executive.autonomousActionCount}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Bugün doğrulanan kararlar</p>
          <p className="commint-kpi-card__value">{workspace.executive.verifiedTodayCount}</p>
        </article>
      </div>

      {isWorkspaceEmpty ? (
        <EmptyPanel
          data-testid="production-manager-empty-state"
          title="Henüz tahmin veya karar önerisi yok"
          description="Üretim, kalite ve bakım verisi aktıkça riskler, tahminler ve karar önerileri burada görünecek."
        />
      ) : (
        <div className="production-manager-workspace__columns">
          <section className="production-manager-workspace__section" data-testid="production-manager-attention-queue">
            <h3>Şimdi dikkat gerektirenler</h3>
            {attentionItems.length === 0 ? (
              <EmptyPanel compact title="Şu anda kritik tahmin yok" />
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

          <section className="production-manager-workspace__section" data-testid="production-manager-recommendations">
            <h3>Yapay zekâ üretim yöneticisi önerileri</h3>
            {recommendations.length === 0 ? (
              <EmptyPanel compact title="Bekleyen karar önerisi yok" />
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
      )}

      {workspace.bottlenecks.length > 0 ? (
        <section className="production-manager-workspace__section" data-testid="production-manager-bottlenecks">
          <h3>Darboğazlar</h3>
          <ul>
            {workspace.bottlenecks.map((bottleneck) => (
              <li key={`${bottleneck.domain}-${bottleneck.summary}`}>
                {bottleneck.summary} ({enumLabel(bottleneck.severity)})
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {workspace.recentDecisions.length > 0 ? (
        <section className="production-manager-workspace__section" data-testid="production-manager-autonomous-feed">
          <h3>Otonom aksiyon akışı</h3>
          <ul>
            {workspace.recentDecisions.slice(0, 5).map((item) => (
              <li key={item.id}>
                <Link href={`/operasyon/yonetim-merkezi/kararlar/${item.id}`}>
                  {item.title}
                </Link>
                <p>{getProductionManagerStatusLabel(item.status)}</p>
                {item.effectivenessScore != null ? (
                  <p>Etkinlik: {item.effectivenessScore}</p>
                ) : null}
              </li>
            ))}
          </ul>
        </section>
      ) : null}
    </section>
  );
}
