

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { CommercialSalesPriceWorkspaceView } from "@/features/commercial-intelligence/components/commercial-sales-price-workspace-view";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import { COMMERCIAL_SALES_PRICE_SUBTITLE } from "@/features/commercial-intelligence/domain/commercial-intelligence-labels";
import { readCommercialSalesPriceWorkspace } from "@/features/commercial-intelligence/server/commercial-sales-price-queries";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function CommercialSalesPriceAnalysisPage() {
  await forbidUnlessGranted("commercial-intelligence.read");

  const result = await readCommercialSalesPriceWorkspace();

  if (result.status === "failure") {
    return (
      <div className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Satış Fiyat Analizi</h1>
            <p className="commint-executive__subtitle">
              {COMMERCIAL_SALES_PRICE_SUBTITLE}
            </p>
          </div>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Satış fiyat verisi yüklenemedi" inline />
      </div>
    );
  }

  return (
    <CommercialSalesPriceWorkspaceView
      workspace={result.workspace}
      canManage={result.canManage}
    />
  );
}
