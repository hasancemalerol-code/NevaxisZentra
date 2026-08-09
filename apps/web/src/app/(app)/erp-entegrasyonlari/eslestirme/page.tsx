import { GitCompare } from "lucide-react";

import { ErpReconciliationView } from "@/features/erp/components/erp-reconciliation-view";
import { readErpReconciliation } from "@/features/erp/server/erp-reconciliation-reader";

export default async function ErpEslestirmePage() {
  const result = await readErpReconciliation();

  if (result.status === "failure") {
    return (
      <main className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Eşleştirme</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <GitCompare aria-hidden="true" />
          <div>
            <h3>Eşleştirme verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <ErpReconciliationView customers={result.customers} products={result.products} />
  );
}
