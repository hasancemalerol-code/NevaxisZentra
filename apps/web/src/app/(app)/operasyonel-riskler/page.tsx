import { ShieldAlert } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { OperationalRiskWorkspaceView } from "@/features/operational-risk/components/operational-risk-workspace-view";
import "@/features/operational-risk/components/operational-risk-workspace.css";
import { readOperationalRiskWorkspace } from "@/features/operational-risk/server/operational-risk-queries";

export default async function OperationalRiskPage() {
  await forbidUnlessGranted("operational-risk.read");

  const result = await readOperationalRiskWorkspace();

  if (result.status === "failure") {
    const isUnauthorized =
      result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <main className="oprisk-page">
        <header className="oprisk-executive">
          <div className="oprisk-executive__intro">
            <p className="oprisk-executive__eyebrow">OPERASYONEL RİSK YÖNETİMİ</p>
            <h1>Operasyonel Riskler</h1>
            <p className="oprisk-executive__subtitle">
              Operasyonel risk vakalarını, kanıtları ve tespit çalıştırmalarını tek çalışma alanında izleyin.
            </p>
          </div>
        </header>
        <div
          role="alert"
          className={isUnauthorized ? "oprisk-error-banner" : "oprisk-partial-error"}
        >
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>{isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "Operasyonel risk verisi yüklenemedi"}</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <OperationalRiskWorkspaceView
      workspace={result.workspace}
      canManage={result.canManage}
    />
  );
}
