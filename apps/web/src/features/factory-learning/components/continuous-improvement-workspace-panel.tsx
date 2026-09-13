import {
  CONTINUOUS_IMPROVEMENT_SUBTITLE,
  CONTINUOUS_IMPROVEMENT_TITLE,
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
    <section className="commint-panel" data-testid="continuous-improvement-workspace-panel">
      <header className="commint-panel__header">
        <div>
          <p className="commint-executive__eyebrow">SÜREKLİ İYİLEŞTİRME</p>
          <h2 className="commint-panel__title">{CONTINUOUS_IMPROVEMENT_TITLE}</h2>
          <p className="commint-panel__subtitle">{CONTINUOUS_IMPROVEMENT_SUBTITLE}</p>
        </div>
      </header>

      <div className="commint-kpi-grid" data-testid="continuous-improvement-loss-command">
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Bugün Kayıp</p>
          <p className="commint-kpi-card__value">{formatLossMinutes(commandSummary.todayLossMinutes)}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">7 Gün Kayıp</p>
          <p className="commint-kpi-card__value">{formatLossMinutes(commandSummary.sevenDayLossMinutes)}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">30 Gün Kayıp</p>
          <p className="commint-kpi-card__value">{formatLossMinutes(commandSummary.thirtyDayLossMinutes)}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Tekrarlayan Sorun</p>
          <p className="commint-kpi-card__value">{commandSummary.recurringLossCount}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Aktif Deney</p>
          <p className="commint-kpi-card__value">{commandSummary.activeExperimentCount}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Doğrulanan İyileştirme</p>
          <p className="commint-kpi-card__value">{commandSummary.verifiedImprovementCount}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Gerileyen İyileştirme</p>
          <p className="commint-kpi-card__value">{commandSummary.regressingImprovementCount}</p>
        </article>
      </div>

      <section data-testid="continuous-improvement-losses">
        <h3>Fabrika Kayıpları</h3>
        {workspace.losses.length === 0 ? (
          <p className="commint-table__secondary">Son 30 günde kayıt bulunamadı.</p>
        ) : (
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
        )}
      </section>

      <section data-testid="continuous-improvement-pareto">
        <h3>En Büyük Kayıp Kaynakları (Pareto)</h3>
        {workspace.paretoRows.length === 0 ? (
          <p className="commint-table__secondary">Pareto analizi için yeterli kayıp yok.</p>
        ) : (
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
        )}
      </section>

      <div className="commint-two-column">
        <section data-testid="continuous-improvement-patterns">
          <h3>Tekrarlayan Sorunlar</h3>
          {workspace.patterns.length === 0 ? (
            <p className="commint-table__secondary">Tekrarlayan sorun tespit edilmedi.</p>
          ) : (
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
          )}
        </section>

        <section data-testid="continuous-improvement-opportunities">
          <h3>İyileştirme Fırsatları</h3>
          {workspace.opportunities.length === 0 ? (
            <p className="commint-table__secondary">Açık iyileştirme fırsatı yok.</p>
          ) : (
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
          )}
        </section>
      </div>

      <section data-testid="continuous-improvement-active-experiments">
        <h3>Aktif Deneyler</h3>
        {workspace.activeExperiments.length === 0 ? (
          <p className="commint-table__secondary">Devam eden deney yok.</p>
        ) : (
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
        )}
      </section>

      <div className="commint-two-column">
        <section data-testid="continuous-improvement-verified">
          <h3>Doğrulanan İyileştirmeler</h3>
          {sustainedImprovements.length === 0 ? (
            <p className="commint-table__secondary">Doğrulanmış iyileştirme yok.</p>
          ) : (
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
          )}
        </section>

        <section data-testid="continuous-improvement-regressing">
          <h3>Gerileyen İyileştirmeler</h3>
          {regressingImprovements.length === 0 ? (
            <p className="commint-table__secondary">Gerileyen iyileştirme yok.</p>
          ) : (
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
          )}
        </section>
      </div>

      <section data-testid="continuous-improvement-learning">
        <h3>FactOS Ne Öğrendi?</h3>
        {workspace.insights.length === 0 && workspace.learningMemories.length === 0 ? (
          <p className="commint-table__secondary">Henüz doğrulanmış öğrenme kaydı yok.</p>
        ) : (
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
        )}
      </section>

      <p className="commint-table__secondary">
        Veri tazeliği: {new Date(workspace.dataFreshnessUtc).toLocaleString("tr-TR")}
      </p>
    </section>
  );
}
