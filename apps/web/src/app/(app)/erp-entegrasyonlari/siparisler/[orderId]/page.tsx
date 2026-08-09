import { ClipboardList } from "lucide-react";

import { ErpOrderDetailView } from "@/features/erp/components/erp-order-detail-view";
import { readErpOrderDetail } from "@/features/erp/server/erp-orders-reader";

type ErpOrderDetailPageProps = Readonly<{
  params: Promise<{ orderId: string }>;
}>;

export default async function ErpOrderDetailPage({ params }: ErpOrderDetailPageProps) {
  const { orderId } = await params;
  const result = await readErpOrderDetail(orderId);

  if (result.status === "failure") {
    return (
      <main className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Sipariş Detayı</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <ClipboardList aria-hidden="true" />
          <div>
            <h3>ERP siparişi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return <ErpOrderDetailView order={result.order} materialAvailability={result.materialAvailability} />;
}
