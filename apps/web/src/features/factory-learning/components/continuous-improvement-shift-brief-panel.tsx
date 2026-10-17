import Link from "next/link";

import "@/features/shift-handover/components/shift-brief.css";

import {
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
  const title = mode === "start" ? "Sürekli iyileştirme" : "Açık kayıp ve deneyler";
  const hint = mode === "start"
    ? "Vardiyaya başlarken izlenecek kayıp ve iyileştirmeler."
    : "Vardiya teslimi öncesi açık kalan kayıp uyarıları ve deneyler.";
  const recurringPattern = workspace.patterns[0];
  const activeExperiment = workspace.activeExperiments[0];
  const verifiedImprovement = workspace.verifiedImprovements.find(
    (item) => item.sustainabilityStatus !== "regressing",
  );
  const isEmpty = !recurringPattern && !activeExperiment && !verifiedImprovement;

  return (
    <section className="shift-brief" data-testid={`continuous-improvement-shift-brief-${mode}`}>
      <div className="shift-brief__heading">
        <div className="shift-brief__heading-text">
          <h2 className="shift-brief__title">{title}</h2>
          {isEmpty ? null : <p className="shift-brief__hint">{hint}</p>}
        </div>
        <Link className="button button-secondary button-small" href="/operasyon/yonetim-merkezi/surekli-iyilestirme">
          İyileştirme merkezine git
        </Link>
      </div>
      {isEmpty ? (
        <div className="shift-brief__empty" data-testid="continuous-improvement-shift-empty">
          <strong>Henüz açık kayıp veya deney yok</strong>
          <p>Tekrarlayan kayıp uyarısı, aktif deney ya da doğrulanmış iyileştirme bulunmuyor.</p>
        </div>
      ) : (
        <ul className="shift-brief__list">
          {recurringPattern ? (
            <li className="shift-brief__item" data-testid="continuous-improvement-shift-recurring-warning">
              <strong>Tekrarlayan kayıp uyarısı: {recurringPattern.title}</strong>
              <p>{recurringPattern.summary}</p>
              <span className="shift-brief__meta">{getFactoryPatternTypeLabel(recurringPattern.patternType)}</span>
            </li>
          ) : null}
          {activeExperiment ? (
            <li className="shift-brief__item" data-testid="continuous-improvement-shift-active-experiment">
              <strong>Aktif deney: {activeExperiment.title}</strong>
              <p>{activeExperiment.hypothesis}</p>
              <span className="shift-brief__meta">{getFactoryExperimentStatusLabel(activeExperiment.status)}</span>
            </li>
          ) : null}
          {verifiedImprovement ? (
            <li className="shift-brief__item" data-testid="continuous-improvement-shift-verified-improvement">
              <strong>Doğrulanan iyileştirme: {verifiedImprovement.title}</strong>
              <p>{verifiedImprovement.outcomeSummary}</p>
              <span className="shift-brief__meta">{getSustainabilityStatusLabel(verifiedImprovement.sustainabilityStatus)}</span>
            </li>
          ) : null}
        </ul>
      )}
    </section>
  );
}
