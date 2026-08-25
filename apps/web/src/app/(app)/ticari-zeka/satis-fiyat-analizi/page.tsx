import { ShieldAlert } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { CommercialSalesPriceWorkspaceView } from "@/features/commercial-intelligence/components/commercial-sales-price-workspace-view";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import { COMMERCIAL_SALES_PRICE_SUBTITLE } from "@/features/commercial-intelligence/domain/commercial-intelligence-labels";
import { readCommercialSalesPriceWorkspace } from "@/features/commercial-intelligence/server/commercial-sales-price-queries";

export default async function CommercialSalesPriceAnalysisPage() {
  await forbidUnlessGranted("commercial-intelligence.read");

  const result = await readCommercialSalesPriceWorkspace();

  if (result.status === "failure") {
    const isUnauthorized =
      result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Satış Fiyat Analizi</h1>
            <p className="commint-executive__subtitle">
              {COMMERCIAL_SALES_PRICE_SUBTITLE}
            </p>
          </div>
        </header>
        <div
          role="alert"
          className={isUnauthorized ? "commint-error-banner" : "commint-partial-error"}
        >
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>{isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "Satış fiyat verisi yüklenemedi"}</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <CommercialSalesPriceWorkspaceView
      workspace={result.workspace}
      canManage={result.canManage}
    />
  );
}
