import { AuthorizationGateState } from "@/components/ui/failure-state";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { ContinuousImprovementWorkspacePanel } from "@/features/factory-learning/components/continuous-improvement-workspace-panel";
import { CONTINUOUS_IMPROVEMENT_SUBTITLE, CONTINUOUS_IMPROVEMENT_TITLE } from "@/features/factory-learning/domain/factory-learning-labels";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";

export default async function ContinuousImprovementPage() {
  const authorization = await denyUnlessGranted("continuous-improvement.read");
  if (authorization.status === "failure") {
    return (
      <div className="commint-page continuous-improvement-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">OPERASYON</p>
            <h1>{CONTINUOUS_IMPROVEMENT_TITLE}</h1>
            <p className="commint-executive__subtitle">{CONTINUOUS_IMPROVEMENT_SUBTITLE}</p>
          </div>
        </header>
        <AuthorizationGateState code={authorization.code} />
      </div>
    );
  }

  return (
    <div className="commint-page continuous-improvement-page">
      <header className="commint-executive">
        <div className="commint-executive__intro">
          <p className="commint-executive__eyebrow">OPERASYON</p>
          <h1>{CONTINUOUS_IMPROVEMENT_TITLE}</h1>
          <p className="commint-executive__subtitle">{CONTINUOUS_IMPROVEMENT_SUBTITLE}</p>
        </div>
      </header>
      <ContinuousImprovementWorkspacePanel />
    </div>
  );
}
