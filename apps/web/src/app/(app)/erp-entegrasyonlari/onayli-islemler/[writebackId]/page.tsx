

import { ErpWritebackDetailView } from "@/features/erp/components/erp-writeback-detail-view";
import { readErpWritebackDetail } from "@/features/erp/server/erp-writebacks-reader";
import { ErpFailurePage } from "@/features/erp/components/erp-failure-page";

type ErpWritebackDetailPageProps = Readonly<{
  params: Promise<{ writebackId: string }>;
}>;

export default async function ErpWritebackDetailPage({ params }: ErpWritebackDetailPageProps) {
  const { writebackId } = await params;
  const result = await readErpWritebackDetail(writebackId);

  if (result.status === "failure") {
    return (
      <ErpFailurePage
        title="Onaylı İşlem Detayı"
        description="ERP’ye yazılacak işlem isteği ve onay geçmişi."
        listHref="/erp-entegrasyonlari/onayli-islemler"
        listLabel="Onaylı İşlemlere Dön"
        failedTitle="Onaylı ERP işlemi yüklenemedi"
        entityLabel="işlem isteği"
        code={"code" in result ? result.code : undefined}
        message={result.message}
      />
    );
  }

  return (
    <ErpWritebackDetailView writeback={result.writeback} permissions={result.permissions} />
  );
}
