import Link from "next/link";
import { ArrowLeft } from "lucide-react";

import { ExportCountryKnowledgePanel } from "@/features/export/components/export-country-knowledge-panel";
import { readCountryKnowledgeEntries } from "@/features/export/server/export-case-reader";
import {
  createKnowledgeEntryAction,
  verifyKnowledgeEntryAction,
} from "@/features/export/server/export-case-writer";
import { FailureStatePage } from "@/components/ui/failure-state";

export default async function CountryKnowledgePage() {
  const result = await readCountryKnowledgeEntries();

  if (result.status === "failure") {
    return (
      <div className="export-page">
        <header className="export-header">
          <p>İHRACAT</p>
          <h1>Ülke Bilgisi</h1>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Ülke bilgisi kayıtları yüklenemedi" inline />
      </div>
    );
  }

  return (
    <div className="export-page">
      <header className="export-header">
        <div className="export-header__top">
          <div className="export-header__intro">
            <p>İHRACAT</p>
            <h1>Ülke Bilgisi</h1>
          </div>
          <div className="export-header__actions">
            <Link href="/ihracat" className="button button-secondary button-medium">
              <ArrowLeft aria-hidden="true" />
              İhracat ana sayfası
            </Link>
          </div>
        </div>
        <span>Doğrulanmış ülke gereksinimleri ve operasyonel bilgi kayıtları</span>
      </header>

      <ExportCountryKnowledgePanel
        entries={result.entries}
        onCreate={createKnowledgeEntryAction}
        onVerify={verifyKnowledgeEntryAction}
      />
    </div>
  );
}
