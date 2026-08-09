import { Warehouse } from "lucide-react";

import { ErpInventoryView } from "@/features/erp/components/erp-inventory-view";
import { readErpInventoryWorkspace } from "@/features/erp/server/erp-inventory-reader";

export default async function ErpStokPage() {
  const result = await readErpInventoryWorkspace();

  if (result.status === "failure") {
    return (
      <main className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Stok & Depolar</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <Warehouse aria-hidden="true" />
          <div>
            <h3>ERP stok verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
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
