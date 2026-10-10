import { ShieldAlert } from "lucide-react";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { ContinuousImprovementWorkspacePanel } from "@/features/factory-learning/components/continuous-improvement-workspace-panel";
import { CONTINUOUS_IMPROVEMENT_SUBTITLE, CONTINUOUS_IMPROVEMENT_TITLE } from "@/features/factory-learning/domain/factory-learning-labels";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";

export default async function ContinuousImprovementPage() {
  const authorization = await denyUnlessGranted("continuous-improvement.read");
  if (authorization.status === "failure") {
    return (
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">OPERASYON</p>
            <h1>{CONTINUOUS_IMPROVEMENT_TITLE}</h1>
            <p className="commint-executive__subtitle">{CONTINUOUS_IMPROVEMENT_SUBTITLE}</p>
          </div>
        </header>
        <div
          role="alert"
          className="commint-error-banner"
          data-testid="continuous-improvement-forbidden"
        >
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>{authorization.code === "authorization-denied" ? "Bu merkeze erişim yetkiniz yok" : "Yetki doğrulanamadı"}</h3>
            <p>{authorization.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className="commint-page">
      <header className="commint-executive">
        <div className="commint-executive__intro">
          <p className="commint-executive__eyebrow">OPERASYON</p>
          <h1>{CONTINUOUS_IMPROVEMENT_TITLE}</h1>
          <p className="commint-executive__subtitle">{CONTINUOUS_IMPROVEMENT_SUBTITLE}</p>
        </div>
      </header>
      <ContinuousImprovementWorkspacePanel />
    </main>
  );
}
