

import { ErpProductionView } from "@/features/erp/components/erp-production-view";
import { readErpProductionOrders } from "@/features/erp/server/erp-production-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function ErpUretimPage() {
  const result = await readErpProductionOrders();

  if (result.status === "failure") {
    return (
      <div className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Üretim</h1>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="ERP üretim verisi yüklenemedi" inline />
      </div>
    );
  }

  return (
    <ErpProductionView snapshot={result.snapshot} productionOrders={result.productionOrders} />
  );
}
