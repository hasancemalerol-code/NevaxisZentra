import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { notFound } from "next/navigation";

import { ExportCountryResearchWorkspace } from "@/features/export/components/export-country-research-workspace";
import {
  addResearchFindingAction,
  completeResearchCaseAction,
  reviewResearchCaseAction,
} from "@/features/export/server/export-case-writer";
import { readCountryResearchCase } from "@/features/export/server/export-case-reader";

type CountryResearchDetailPageProps = Readonly<{
  params: Promise<{ researchCaseId: string }>;
}>;

export default async function CountryResearchDetailPage({
  params,
}: CountryResearchDetailPageProps) {
  const { researchCaseId } = await params;
  const result = await readCountryResearchCase(researchCaseId);

  if (result.status === "failure") {
    if (result.code === "not-found") {
      notFound();
    }

    return (
      <main className="export-detail-page">
        <div role="alert" className="state-panel error-state">
          <p>Araştırma dosyası yüklenemedi.</p>
        </div>
      </main>
    );
  }

  return (
    <main className="export-detail-page">
      <div className="export-detail-page__back">
        <Link href="/ihracat/ulke-arastirma">
          <ArrowLeft aria-hidden="true" />
          Ülke araştırmalarına dön
        </Link>
      </div>

      <ExportCountryResearchWorkspace
        detail={result.detail}
        onAddFinding={addResearchFindingAction}
        onReview={reviewResearchCaseAction}
        onComplete={completeResearchCaseAction}
      />
    </main>
  );
}
