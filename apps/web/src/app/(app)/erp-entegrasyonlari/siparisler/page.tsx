import { ClipboardList } from "lucide-react";

import { ErpOrdersView } from "@/features/erp/components/erp-orders-view";
import { readErpOrders } from "@/features/erp/server/erp-orders-reader";

export default async function ErpSiparislerPage() {
  const result = await readErpOrders();

  if (result.status === "failure") {
    return (
      <main className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>ERP Siparişleri</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <ClipboardList aria-hidden="true" />
          <div>
            <h3>ERP siparişleri yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return <ErpOrdersView orders={result.orders} />;
}
