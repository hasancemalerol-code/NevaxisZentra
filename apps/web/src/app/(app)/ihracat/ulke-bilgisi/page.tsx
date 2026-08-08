import Link from "next/link";
import { ArrowLeft } from "lucide-react";

import { ExportCountryKnowledgePanel } from "@/features/export/components/export-country-knowledge-panel";
import { readCountryKnowledgeEntries } from "@/features/export/server/export-case-reader";
import {
  createKnowledgeEntryAction,
  verifyKnowledgeEntryAction,
} from "@/features/export/server/export-case-writer";

export default async function CountryKnowledgePage() {
  const result = await readCountryKnowledgeEntries();

  if (result.status === "failure") {
    return (
      <main className="export-page">
        <header className="export-header">
          <p>ÜLKE BİLGİSİ</p>
          <h1>Ülke Bilgisi</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <p>Ülke bilgisi kayıtları yüklenemedi.</p>
        </div>
      </main>
    );
  }

  return (
    <main className="export-page">
      <div className="export-detail-page__back">
        <Link href="/ihracat">
          <ArrowLeft aria-hidden="true" />
          İhracat ana sayfasına dön
        </Link>
      </div>

      <header className="export-header">
        <p>ÜLKE BİLGİ BANKASI</p>
        <h1>Ülke Bilgisi</h1>
        <span>Doğrulanmış ülke gereksinimleri ve operasyonel bilgi kayıtları</span>
      </header>

      <ExportCountryKnowledgePanel
        entries={result.entries}
        onCreate={createKnowledgeEntryAction}
        onVerify={verifyKnowledgeEntryAction}
      />
    </main>
  );
}
