import { ShoppingCart } from "lucide-react";

import { ErpPurchaseOrderDetailView } from "@/features/erp/components/erp-purchase-order-detail-view";
import { readErpPurchaseOrderDetail } from "@/features/erp/server/erp-purchasing-reader";

type ErpPurchaseOrderDetailPageProps = Readonly<{
  params: Promise<{ purchaseOrderId: string }>;
}>;

export default async function ErpPurchaseOrderDetailPage({ params }: ErpPurchaseOrderDetailPageProps) {
  const { purchaseOrderId } = await params;
  const result = await readErpPurchaseOrderDetail(purchaseOrderId);

  if (result.status === "failure") {
    return (
      <main className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Satın Alma Detayı</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <ShoppingCart aria-hidden="true" />
          <div>
            <h3>ERP satın alma siparişi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return <ErpPurchaseOrderDetailView purchaseOrder={result.purchaseOrder} />;
}
