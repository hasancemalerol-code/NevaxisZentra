

import { ErpPurchaseOrderDetailView } from "@/features/erp/components/erp-purchase-order-detail-view";
import { readErpPurchaseOrderDetail } from "@/features/erp/server/erp-purchasing-reader";
import { ErpFailurePage } from "@/features/erp/components/erp-failure-page";

type ErpPurchaseOrderDetailPageProps = Readonly<{
  params: Promise<{ purchaseOrderId: string }>;
}>;

export default async function ErpPurchaseOrderDetailPage({ params }: ErpPurchaseOrderDetailPageProps) {
  const { purchaseOrderId } = await params;
  const result = await readErpPurchaseOrderDetail(purchaseOrderId);

  if (result.status === "failure") {
    return (
      <ErpFailurePage
        title="Satın Alma Detayı"
        description="ERP satın alma siparişi ve satır özeti."
        listHref="/erp-entegrasyonlari/satin-alma"
        listLabel="Satın Alma Listesine Dön"
        failedTitle="ERP satın alma siparişi yüklenemedi"
        entityLabel="satın alma siparişi"
        code={"code" in result ? result.code : undefined}
        message={result.message}
      />
    );
  }

  return <ErpPurchaseOrderDetailView purchaseOrder={result.purchaseOrder} />;
}
