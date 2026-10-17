

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { CommercialQuotationRiskWorkspaceView } from "@/features/commercial-intelligence/components/commercial-quotation-risk-workspace-view";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import { COMMERCIAL_QUOTATION_RISK_SUBTITLE } from "@/features/commercial-intelligence/domain/commercial-quotation-risk-labels";
import { readCommercialQuotationRiskWorkspace } from "@/features/commercial-intelligence/server/commercial-quotation-risk-queries";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function CommercialQuotationRiskPage() {
  await forbidUnlessGranted("commercial-intelligence.read");

  const result = await readCommercialQuotationRiskWorkspace();

  if (result.status === "failure") {
    return (
      <div className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Teklif Riskleri</h1>
            <p className="commint-executive__subtitle">
              {COMMERCIAL_QUOTATION_RISK_SUBTITLE}
            </p>
          </div>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Teklif risk verisi yüklenemedi" inline />
      </div>
    );
  }

  return (
    <CommercialQuotationRiskWorkspaceView
      workspace={result.workspace}
      canManage={result.canManage}
    />
  );
}
