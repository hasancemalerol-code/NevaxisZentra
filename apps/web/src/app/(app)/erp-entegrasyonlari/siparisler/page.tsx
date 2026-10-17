

import { ErpOrdersView } from "@/features/erp/components/erp-orders-view";
import { readErpOrders } from "@/features/erp/server/erp-orders-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function ErpSiparislerPage() {
  const result = await readErpOrders();

  if (result.status === "failure") {
    return (
      <div className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>ERP Siparişleri</h1>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="ERP siparişleri yüklenemedi" inline />
      </div>
    );
  }

  return <ErpOrdersView orders={result.orders} />;
}
