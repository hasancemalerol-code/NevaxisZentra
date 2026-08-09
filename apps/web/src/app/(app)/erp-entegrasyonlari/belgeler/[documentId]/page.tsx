import { FileText } from "lucide-react";

import { ErpDocumentDetailView } from "@/features/erp/components/erp-document-detail-view";
import { readErpDocumentDetail } from "@/features/erp/server/erp-documents-reader";

type ErpDocumentDetailPageProps = Readonly<{
  params: Promise<{ documentId: string }>;
}>;

export default async function ErpDocumentDetailPage({ params }: ErpDocumentDetailPageProps) {
  const { documentId } = await params;
  const result = await readErpDocumentDetail(documentId);

  if (result.status === "failure") {
    return (
      <main className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Belge Detayı</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <FileText aria-hidden="true" />
          <div>
            <h3>ERP belgesi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return <ErpDocumentDetailView document={result.document} />;
}
