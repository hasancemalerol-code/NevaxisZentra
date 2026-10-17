import type { ReactNode } from "react";

import { EmptyPanel } from "@/components/ui/empty-panel";
import { formatDateTime } from "@/lib/presentation/format";
import {
  formatLossMinutes,
  getFactoryExperimentStatusLabel,
  getFactoryExperimentTypeLabel,
  getFactoryLearningImpactBadgeClass,
  getFactoryLearningImpactBadgeLabel,
  getFactoryLossCategoryLabel,
  getFactoryPatternTypeLabel,
  getImprovementBacklogStatusLabel,
  getSustainabilityStatusLabel,
} from "../domain/factory-learning-labels";
import { readFactoryLearningWorkspace } from "../server/factory-learning-reader";
import "./continuous-improvement-workspace.css";

function ImprovementSection({
  title,
  testId,
  isEmpty,
  emptyTitle,
  children,
}: Readonly<{ title: string; testId: string; isEmpty: boolean; emptyTitle: string; children: ReactNode }>) {
  return (
    <section className="continuous-improvement__section" data-testid={testId}>
      <h2>{title}</h2>
      {isEmpty ? <EmptyPanel compact title={emptyTitle} /> : children}
    </section>
  );
}

function ImpactBadge({ classification }: Readonly<{ classification: string }>) {
  return (
    <span className={getFactoryLearningImpactBadgeClass(classification)}>
      {getFactoryLearningImpactBadgeLabel(classification)}
    </span>
  );
}

export async function ContinuousImprovementWorkspacePanel() {
  const result = await readFactoryLearningWorkspace();
  if (result.status === "failure") {
    return null;
  }

  const { workspace } = result;
  const { commandSummary } = workspace;
  const regressingImprovements = workspace.verifiedImprovements.filter(
    (item) => item.sustainabilityStatus === "regressing",
  );
  const sustainedImprovements = workspace.verifiedImprovements.filter(
    (item) => item.sustainabilityStatus !== "regressing",
  );

  return (
    <section className="commint-panel continuous-improvement" data-testid="continuous-improvement-workspace-panel">
      <div className="commint-kpi-grid" data-testid="continuous-improvement-loss-command">
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Bugün kayıp</p>
          <p className="commint-kpi-card__value">{formatLossMinutes(commandSummary.todayLossMinutes)}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">7 gün kayıp</p>
          <p className="commint-kpi-card__value">{formatLossMinutes(commandSummary.sevenDayLossMinutes)}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">30 gün kayıp</p>
          <p className="commint-kpi-card__value">{formatLossMinutes(commandSummary.thirtyDayLossMinutes)}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Tekrarlayan sorun</p>
          <p className="commint-kpi-card__value">{commandSummary.recurringLossCount}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Aktif deney</p>
          <p className="commint-kpi-card__value">{commandSummary.activeExperimentCount}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Doğrulanan</p>
          <p className="commint-kpi-card__value">{commandSummary.verifiedImprovementCount}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Gerileyen</p>
          <p className="commint-kpi-card__value">{commandSummary.regressingImprovementCount}</p>
        </article>
      </div>

      <div className="continuous-improvement__grid">
      <ImprovementSection title="Fabrika kayıpları" testId="continuous-improvement-losses" isEmpty={workspace.losses.length === 0} emptyTitle="Son 30 günde kayıt yok">
        <ul>
            {workspace.losses.slice(0, 8).map((loss) => (
              <li key={loss.id}>
                <strong>{loss.title}</strong>
                <p>{loss.summary}</p>
                <p>
                  {getFactoryLossCategoryLabel(loss.category)} · {formatLossMinutes(loss.durationMinutes)} ·
                  Tekrar: {loss.recurrenceCount}
                </p>
                <ImpactBadge classification={loss.dataClassification} />
              </li>
            ))}
          </ul>
      </ImprovementSection>

      <ImprovementSection title="En büyük kayıp kaynakları (Pareto analizi)" testId="continuous-improvement-pareto" isEmpty={workspace.paretoRows.length === 0} emptyTitle="Pareto analizi için yeterli kayıp kaydı yok">
        <ul>
            {workspace.paretoRows.slice(0, 6).map((row) => (
              <li key={`${row.category}-${row.label}`}>
                <strong>{row.label}</strong>
                <p>
                  {getFactoryLossCategoryLabel(row.category)} · {formatLossMinutes(row.totalMinutes)} · %
                  {row.contributionPercent.toFixed(1)} · {row.occurrenceCount} olay
                </p>
                <ImpactBadge classification={row.dataClassification} />
              </li>
            ))}
          </ul>
      </ImprovementSection>

        <ImprovementSection title="Tekrarlayan sorunlar" testId="continuous-improvement-patterns" isEmpty={workspace.patterns.length === 0} emptyTitle="Tekrarlayan sorun tespit edilmedi">
          <ul>
              {workspace.patterns.slice(0, 4).map((pattern) => (
                <li key={pattern.id}>
                  <strong>{pattern.title}</strong>
                  <p>{pattern.summary}</p>
                  <p>
                    {getFactoryPatternTypeLabel(pattern.patternType)} · {pattern.occurrenceCount} kez
                  </p>
                  <ImpactBadge classification={pattern.dataClassification} />
                </li>
              ))}
            </ul>
        </ImprovementSection>

        <ImprovementSection title="İyileştirme fırsatları" testId="continuous-improvement-opportunities" isEmpty={workspace.opportunities.length === 0} emptyTitle="Açık iyileştirme fırsatı yok">
          <ul>
              {workspace.opportunities.slice(0, 4).map((opportunity) => (
                <li key={opportunity.id}>
                  <strong>{opportunity.title}</strong>
                  <p>{opportunity.problemSummary}</p>
                  <p>{getImprovementBacklogStatusLabel(opportunity.status)} · {opportunity.domain}</p>
                  <ImpactBadge classification={opportunity.impactClassification} />
                </li>
              ))}
            </ul>
        </ImprovementSection>

      <ImprovementSection title="Aktif deneyler" testId="continuous-improvement-active-experiments" isEmpty={workspace.activeExperiments.length === 0} emptyTitle="Devam eden deney yok">
        <ul>
            {workspace.activeExperiments.slice(0, 4).map((experiment) => (
              <li key={experiment.id}>
                <strong>{experiment.title}</strong>
                <p>{experiment.hypothesis}</p>
                <p>
                  {getFactoryExperimentTypeLabel(experiment.experimentType)} ·{" "}
                  {getFactoryExperimentStatusLabel(experiment.status)}
                </p>
              </li>
            ))}
          </ul>
      </ImprovementSection>

        <ImprovementSection title="Doğrulanan iyileştirmeler" testId="continuous-improvement-verified" isEmpty={sustainedImprovements.length === 0} emptyTitle="Doğrulanmış iyileştirme yok">
          <ul>
              {sustainedImprovements.slice(0, 4).map((item) => (
                <li key={item.id}>
                  <strong>{item.title}</strong>
                  <p>{item.outcomeSummary}</p>
                  <p>
                    Etkinlik: {item.effectivenessScore} ·{" "}
                    {getSustainabilityStatusLabel(item.sustainabilityStatus)}
                  </p>
                  <ImpactBadge classification={item.impactClassification} />
                </li>
              ))}
            </ul>
        </ImprovementSection>

        <ImprovementSection title="Gerileyen iyileştirmeler" testId="continuous-improvement-regressing" isEmpty={regressingImprovements.length === 0} emptyTitle="Gerileyen iyileştirme yok">
          <ul>
              {regressingImprovements.slice(0, 4).map((item) => (
                <li key={item.id}>
                  <strong>{item.title}</strong>
                  <p>{item.outcomeSummary}</p>
                  <p>{getSustainabilityStatusLabel(item.sustainabilityStatus)}</p>
                  <ImpactBadge classification={item.impactClassification} />
                </li>
              ))}
            </ul>
        </ImprovementSection>

      <ImprovementSection title="Zentra neler öğrendi?" testId="continuous-improvement-learning" isEmpty={workspace.insights.length === 0 && workspace.learningMemories.length === 0} emptyTitle="Henüz doğrulanmış öğrenme kaydı yok">
        <ul>
            {workspace.insights.slice(0, 4).map((insight, index) => (
              <li key={`${insight.insightType}-${index}`}>
                <strong>{insight.title}</strong>
                <p>{insight.summary}</p>
                <p>{insight.evidenceSummary}</p>
              </li>
            ))}
            {workspace.learningMemories.slice(0, 4).map((memory) => (
              <li key={memory.id}>
                <strong>{memory.problemPattern}</strong>
                <p>{memory.resultSummary}</p>
                <p>Etkinlik: {memory.effectivenessScore}</p>
              </li>
            ))}
          </ul>
      </ImprovementSection>

      </div>

      <p className="commint-table__secondary">
        Veri güncelliği: {formatDateTime(workspace.dataFreshnessUtc)}
      </p>
    </section>
  );
}
