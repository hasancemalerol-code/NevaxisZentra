

import { ErpDocumentsView } from "@/features/erp/components/erp-documents-view";
import { readErpDocuments } from "@/features/erp/server/erp-documents-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function ErpBelgelerPage() {
  const result = await readErpDocuments();

  if (result.status === "failure") {
    return (
      <div className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Belgeler</h1>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="ERP belge verisi yüklenemedi" inline />
      </div>
    );
  }

  return <ErpDocumentsView snapshot={result.snapshot} documents={result.documents} />;
}
