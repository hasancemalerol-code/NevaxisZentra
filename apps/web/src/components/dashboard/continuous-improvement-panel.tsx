import Link from "next/link";

import {
  CONTINUOUS_IMPROVEMENT_TITLE,
  formatLossMinutes,
  getFactoryExperimentStatusLabel,
  getFactoryPatternTypeLabel,
} from "@/features/factory-learning/domain/factory-learning-labels";
import { readFactoryLearningDashboardPanel } from "@/features/factory-learning/server/factory-learning-reader";

export async function ContinuousImprovementDashboardPanel() {
  const panel = await readFactoryLearningDashboardPanel();
  if (panel.status === "failure") {
    return null;
  }

  const items = [
    panel.topPattern
      ? `Tekrarlayan: ${panel.topPattern.title} · ${getFactoryPatternTypeLabel(panel.topPattern.patternType)}`
      : "Tekrarlayan sorun yok",
    panel.topOpportunity ? `Fırsat: ${panel.topOpportunity.title}` : "Açık fırsat yok",
    panel.activeExperiment
      ? `Deney: ${panel.activeExperiment.title} · ${getFactoryExperimentStatusLabel(panel.activeExperiment.status)}`
      : "Aktif deney yok",
    panel.topInsight ? `Öğrenme: ${panel.topInsight.summary}` : "Doğrulanmış öğrenme yok",
  ].slice(0, 4);

  return (
    <section className="dashboard-panel" data-testid="dashboard-continuous-improvement-panel">
      <p className="shift-ops-hero__eyebrow">{CONTINUOUS_IMPROVEMENT_TITLE}</p>
      <p>Bugün kayıp: {formatLossMinutes(panel.commandSummary.todayLossMinutes)}</p>
      <p>7 gün kayıp: {formatLossMinutes(panel.commandSummary.sevenDayLossMinutes)}</p>
      <ul>
        {items.map((item) => (
          <li key={item}>{item}</li>
        ))}
      </ul>
      <Link className="shift-ops-cta" href="/operasyon/yonetim-merkezi/surekli-iyilestirme">
        Sürekli İyileştirme Merkezi
      </Link>
    </section>
  );
}
