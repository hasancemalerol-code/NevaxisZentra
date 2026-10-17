

import { ErpOrderDetailView } from "@/features/erp/components/erp-order-detail-view";
import { readErpOrderDetail } from "@/features/erp/server/erp-orders-reader";
import { ErpFailurePage } from "@/features/erp/components/erp-failure-page";

type ErpOrderDetailPageProps = Readonly<{
  params: Promise<{ orderId: string }>;
}>;

export default async function ErpOrderDetailPage({ params }: ErpOrderDetailPageProps) {
  const { orderId } = await params;
  const result = await readErpOrderDetail(orderId);

  if (result.status === "failure") {
    return (
      <ErpFailurePage
        title="Sipariş Detayı"
        description="ERP sipariş özeti ve operasyonel hazırlık."
        listHref="/erp-entegrasyonlari/siparisler"
        listLabel="Siparişlere Dön"
        failedTitle="ERP siparişi yüklenemedi"
        entityLabel="sipariş"
        code={"code" in result ? result.code : undefined}
        message={result.message}
      />
    );
  }

  return <ErpOrderDetailView order={result.order} materialAvailability={result.materialAvailability} />;
}
