import { ShieldCheck } from "lucide-react";

import { ErpWritebacksView } from "@/features/erp/components/erp-writebacks-view";
import { readErpWritebacks } from "@/features/erp/server/erp-writebacks-reader";

export default async function ErpOnayliIslemlerPage() {
  const result = await readErpWritebacks();

  if (result.status === "failure") {
    return (
      <main className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Onaylı İşlemler</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <ShieldCheck aria-hidden="true" />
          <div>
            <h3>ERP writeback verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return <ErpWritebacksView writebacks={result.writebacks} />;
}
