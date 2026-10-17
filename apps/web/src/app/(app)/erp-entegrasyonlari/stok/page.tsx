

import { ErpInventoryView } from "@/features/erp/components/erp-inventory-view";
import { readErpInventoryWorkspace } from "@/features/erp/server/erp-inventory-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function ErpStokPage() {
  const result = await readErpInventoryWorkspace();

  if (result.status === "failure") {
    return (
      <div className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Stok ve Depolar</h1>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="ERP stok verisi yüklenemedi" inline />
      </div>
    );
  }

  return (
    <ErpInventoryView
      snapshot={result.snapshot}
      warehouses={result.warehouses}
      inventoryBalances={result.inventoryBalances}
      syncReliability={result.syncReliability}
    />
  );
}
