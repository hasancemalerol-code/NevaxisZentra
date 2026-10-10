import { Factory } from "lucide-react";

import { ErpProductionOrderDetailView } from "@/features/erp/components/erp-production-order-detail-view";
import { readErpProductionOrderDetail } from "@/features/erp/server/erp-production-reader";

type ErpProductionOrderDetailPageProps = Readonly<{
  params: Promise<{ productionOrderId: string }>;
}>;

export default async function ErpProductionOrderDetailPage({
  params,
}: ErpProductionOrderDetailPageProps) {
  const { productionOrderId } = await params;
  const result = await readErpProductionOrderDetail(productionOrderId);

  if (result.status === "failure") {
    return (
      <main className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Üretim Detayı</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <Factory aria-hidden="true" />
          <div>
            <h3>{result.code === "not-found" ? "Kayıt bulunamadı" : "ERP üretim emri yüklenemedi"}</h3>
            <p>{result.code === "not-found" ? "İstenen ERP kaydı bulunamadı." : result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return <ErpProductionOrderDetailView productionOrder={result.productionOrder} />;
}
