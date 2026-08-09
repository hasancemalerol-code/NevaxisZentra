import { Factory } from "lucide-react";

import { ErpProductionView } from "@/features/erp/components/erp-production-view";
import { readErpProductionOrders } from "@/features/erp/server/erp-production-reader";

export default async function ErpUretimPage() {
  const result = await readErpProductionOrders();

  if (result.status === "failure") {
    return (
      <main className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Üretim</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <Factory aria-hidden="true" />
          <div>
            <h3>ERP üretim verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <ErpProductionView snapshot={result.snapshot} productionOrders={result.productionOrders} />
  );
}
