import { ShieldCheck } from "lucide-react";

import { ErpWritebackDetailView } from "@/features/erp/components/erp-writeback-detail-view";
import { readErpWritebackDetail } from "@/features/erp/server/erp-writebacks-reader";

type ErpWritebackDetailPageProps = Readonly<{
  params: Promise<{ writebackId: string }>;
}>;

export default async function ErpWritebackDetailPage({ params }: ErpWritebackDetailPageProps) {
  const { writebackId } = await params;
  const result = await readErpWritebackDetail(writebackId);

  if (result.status === "failure") {
    return (
      <main className="production-execution-page erp-workspace-page">
        <header className="production-execution-header">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Onaylı İşlem Detayı</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <ShieldCheck aria-hidden="true" />
          <div>
            <h3>{result.code === "not-found" ? "Kayıt bulunamadı" : "ERP writeback isteği yüklenemedi"}</h3>
            <p>{result.code === "not-found" ? "İstenen ERP kaydı bulunamadı." : result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <ErpWritebackDetailView writeback={result.writeback} permissions={result.permissions} />
  );
}
