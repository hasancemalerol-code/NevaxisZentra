

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { OperationalRiskWorkspaceView } from "@/features/operational-risk/components/operational-risk-workspace-view";
import "@/features/operational-risk/components/operational-risk-workspace.css";
import { readOperationalRiskWorkspace } from "@/features/operational-risk/server/operational-risk-queries";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function OperationalRiskPage() {
  await forbidUnlessGranted("operational-risk.read");

  const result = await readOperationalRiskWorkspace();

  if (result.status === "failure") {
    return (
      <div className="oprisk-page">
        <header className="oprisk-executive">
          <div className="oprisk-executive__intro">
            <p className="oprisk-executive__eyebrow">OPERASYONEL RİSK YÖNETİMİ</p>
            <h1>Operasyonel Riskler</h1>
            <p className="oprisk-executive__subtitle">
              Operasyonel risk vakalarını, kanıtları ve kontrol çalışmalarını tek çalışma alanında izleyin.
            </p>
          </div>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Operasyonel risk verisi yüklenemedi" inline />
      </div>
    );
  }

  return (
    <OperationalRiskWorkspaceView
      workspace={result.workspace}
      canManage={result.canManage}
    />
  );
}
