

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { CommercialIntelligenceWorkspaceView } from "@/features/commercial-intelligence/components/commercial-intelligence-workspace-view";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import { readCommercialIntelligenceWorkspace } from "@/features/commercial-intelligence/server/commercial-intelligence-queries";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function CommercialIntelligencePurchasePricePage() {
  await forbidUnlessGranted("commercial-intelligence.read");

  const result = await readCommercialIntelligenceWorkspace();

  if (result.status === "failure") {
    return (
      <div className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Satın Alma Fiyat Analizi</h1>
            <p className="commint-executive__subtitle">
              Satın alma fiyat kanıtını, dönemsel sapmaları ve tedarikçi konumlarını tek çalışma alanında izleyin.
            </p>
          </div>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Ticari zekâ verisi yüklenemedi" inline />
      </div>
    );
  }

  return (
    <CommercialIntelligenceWorkspaceView
      workspace={result.workspace}
      canManage={result.canManage}
    />
  );
}
