import { FileText } from "lucide-react";

import { ErpDocumentsView } from "@/features/erp/components/erp-documents-view";
import { readErpDocuments } from "@/features/erp/server/erp-documents-reader";

export default async function ErpBelgelerPage() {
  const result = await readErpDocuments();

  if (result.status === "failure") {
    return (
      <main className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Belgeler</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <FileText aria-hidden="true" />
          <div>
            <h3>ERP belge verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return <ErpDocumentsView snapshot={result.snapshot} documents={result.documents} />;
}
