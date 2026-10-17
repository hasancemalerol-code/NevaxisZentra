

import { ErpDocumentDetailView } from "@/features/erp/components/erp-document-detail-view";
import { readErpDocumentDetail } from "@/features/erp/server/erp-documents-reader";
import { ErpFailurePage } from "@/features/erp/components/erp-failure-page";

type ErpDocumentDetailPageProps = Readonly<{
  params: Promise<{ documentId: string }>;
}>;

export default async function ErpDocumentDetailPage({ params }: ErpDocumentDetailPageProps) {
  const { documentId } = await params;
  const result = await readErpDocumentDetail(documentId);

  if (result.status === "failure") {
    return (
      <ErpFailurePage
        title="Belge Detayı"
        description="ERP belge özeti ve ilişkili kayıt bilgisi."
        listHref="/erp-entegrasyonlari/belgeler"
        listLabel="Belgelere Dön"
        failedTitle="ERP belgesi yüklenemedi"
        entityLabel="belge"
        code={"code" in result ? result.code : undefined}
        message={result.message}
      />
    );
  }

  return <ErpDocumentDetailView document={result.document} />;
}
