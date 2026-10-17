

import { ErpPurchasingView } from "@/features/erp/components/erp-purchasing-view";
import { readErpPurchaseOrders } from "@/features/erp/server/erp-purchasing-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function ErpSatinAlmaPage() {
  const result = await readErpPurchaseOrders();

  if (result.status === "failure") {
    return (
      <div className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Satın Alma</h1>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="ERP satın alma verisi yüklenemedi" inline />
      </div>
    );
  }

  return <ErpPurchasingView purchaseOrders={result.purchaseOrders} />;
}
