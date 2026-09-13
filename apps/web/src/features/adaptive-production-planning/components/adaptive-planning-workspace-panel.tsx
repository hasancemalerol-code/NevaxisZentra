import {
  ADAPTIVE_PLANNING_SUBTITLE,
  ADAPTIVE_PLANNING_TITLE,
} from "../domain/adaptive-planning-labels";
import { readAdaptivePlanningWorkspaceDetail } from "../server/adaptive-planning-reader";
import { AdaptivePlanningWorkspaceClient } from "./adaptive-planning-workspace-client";

export async function AdaptivePlanningWorkspacePanel() {
  const result = await readAdaptivePlanningWorkspaceDetail();
  if (result.status === "failure") {
    return null;
  }

  return (
    <section className="commint-panel adaptive-planning-panel" data-testid="adaptive-planning-workspace-panel">
      <header className="commint-panel__header">
        <div>
          <p className="commint-executive__eyebrow">UYARLANABİLİR PLANLAMA</p>
          <h2 className="commint-panel__title">{ADAPTIVE_PLANNING_TITLE}</h2>
          <p className="commint-panel__subtitle">{ADAPTIVE_PLANNING_SUBTITLE}</p>
        </div>
      </header>

      <AdaptivePlanningWorkspaceClient
        workspace={result.workspace}
        replanEvaluation={result.replanEvaluation}
      />
    </section>
  );
}
