

import { ErpProductionOrderDetailView } from "@/features/erp/components/erp-production-order-detail-view";
import { readErpProductionOrderDetail } from "@/features/erp/server/erp-production-reader";
import { ErpFailurePage } from "@/features/erp/components/erp-failure-page";

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
      <ErpFailurePage
        title="Üretim Detayı"
        description="ERP üretim emri ve ilerleme özeti."
        listHref="/erp-entegrasyonlari/uretim"
        listLabel="Üretim Emirlerine Dön"
        failedTitle="ERP üretim emri yüklenemedi"
        entityLabel="üretim emri"
        code={"code" in result ? result.code : undefined}
        message={result.message}
      />
    );
  }

  return <ErpProductionOrderDetailView productionOrder={result.productionOrder} />;
}
