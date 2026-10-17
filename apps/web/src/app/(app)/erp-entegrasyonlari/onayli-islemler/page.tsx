

import { ErpWritebacksView } from "@/features/erp/components/erp-writebacks-view";
import { readErpWritebacks } from "@/features/erp/server/erp-writebacks-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function ErpOnayliIslemlerPage() {
  const result = await readErpWritebacks();

  if (result.status === "failure") {
    return (
      <div className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Onaylı İşlemler</h1>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Onaylı ERP işlemleri yüklenemedi" inline />
      </div>
    );
  }

  return <ErpWritebacksView writebacks={result.writebacks} />;
}
