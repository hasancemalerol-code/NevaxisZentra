import Link from "next/link";

import {
  CONTINUOUS_IMPROVEMENT_TITLE,
  getFactoryExperimentStatusLabel,
  getFactoryPatternTypeLabel,
  getSustainabilityStatusLabel,
} from "../domain/factory-learning-labels";
import { readFactoryLearningWorkspace } from "../server/factory-learning-reader";

type ContinuousImprovementShiftBriefPanelProps = Readonly<{
  mode: "start" | "handover";
}>;

export async function ContinuousImprovementShiftBriefPanel({
  mode,
}: ContinuousImprovementShiftBriefPanelProps) {
  const result = await readFactoryLearningWorkspace();
  if (result.status === "failure") {
    return null;
  }

  const { workspace } = result;
  const title =
    mode === "start"
      ? "Vardiyaya Başlarken — Sürekli İyileştirme"
      : "Vardiya Teslimi — Açık Kayıp ve Deneyler";
  const recurringPattern = workspace.patterns[0];
  const activeExperiment = workspace.activeExperiments[0];
  const verifiedImprovement = workspace.verifiedImprovements.find(
    (item) => item.sustainabilityStatus !== "regressing",
  );

  return (
    <section className="shift-ops-card" data-testid={`continuous-improvement-shift-brief-${mode}`}>
      <h3>{title}</h3>
      <p>{CONTINUOUS_IMPROVEMENT_TITLE}</p>
      <ul className="shift-ops-item-list">
        {recurringPattern ? (
          <li data-testid="continuous-improvement-shift-recurring-warning">
            <strong>Tekrarlayan kayıp uyarısı: {recurringPattern.title}</strong>
            <p>{recurringPattern.summary}</p>
            <span>{getFactoryPatternTypeLabel(recurringPattern.patternType)}</span>
          </li>
        ) : (
          <li>Tekrarlayan kayıp uyarısı yok.</li>
        )}
        {activeExperiment ? (
          <li data-testid="continuous-improvement-shift-active-experiment">
            <strong>Aktif deney: {activeExperiment.title}</strong>
            <p>{activeExperiment.hypothesis}</p>
            <span>{getFactoryExperimentStatusLabel(activeExperiment.status)}</span>
          </li>
        ) : (
          <li>Aktif deney yok.</li>
        )}
        {verifiedImprovement ? (
          <li data-testid="continuous-improvement-shift-verified-improvement">
            <strong>Doğrulanan iyileştirme: {verifiedImprovement.title}</strong>
            <p>{verifiedImprovement.outcomeSummary}</p>
            <span>{getSustainabilityStatusLabel(verifiedImprovement.sustainabilityStatus)}</span>
          </li>
        ) : (
          <li>Doğrulanmış iyileştirme yok.</li>
        )}
      </ul>
      <Link className="shift-ops-cta" href="/operasyon/yonetim-merkezi/surekli-iyilestirme">
        Sürekli İyileştirme Merkezi
      </Link>
    </section>
  );
}
