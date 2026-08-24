import { ShieldAlert } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { CommercialIntelligenceWorkspaceView } from "@/features/commercial-intelligence/components/commercial-intelligence-workspace-view";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import { readCommercialIntelligenceWorkspace } from "@/features/commercial-intelligence/server/commercial-intelligence-queries";

export default async function CommercialIntelligencePurchasePricePage() {
  await forbidUnlessGranted("commercial-intelligence.read");

  const result = await readCommercialIntelligenceWorkspace();

  if (result.status === "failure") {
    const isUnauthorized =
      result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Satın Alma Fiyat Analizi</h1>
            <p className="commint-executive__subtitle">
              Satın alma fiyat kanıtını, dönemsel sapmaları ve tedarikçi konumlarını tek çalışma alanında izleyin.
            </p>
          </div>
        </header>
        <div
          role="alert"
          className={isUnauthorized ? "commint-error-banner" : "commint-partial-error"}
        >
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>{isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "Ticari zekâ verisi yüklenemedi"}</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <CommercialIntelligenceWorkspaceView
      workspace={result.workspace}
      canManage={result.canManage}
    />
  );
}
