import { ShoppingCart } from "lucide-react";

import { ErpPurchasingView } from "@/features/erp/components/erp-purchasing-view";
import { readErpPurchaseOrders } from "@/features/erp/server/erp-purchasing-reader";

export default async function ErpSatinAlmaPage() {
  const result = await readErpPurchaseOrders();

  if (result.status === "failure") {
    return (
      <main className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Satın Alma</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <ShoppingCart aria-hidden="true" />
          <div>
            <h3>ERP satın alma verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return <ErpPurchasingView purchaseOrders={result.purchaseOrders} />;
}
