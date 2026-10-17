import Link from "next/link";
import {
  AlertTriangle,
  CheckCircle2,
  FileStack,
  Plus,
  ShieldAlert,
} from "lucide-react";

import { Card } from "@/components/ui/card";
import { ExportCaseCard } from "@/features/export/components/export-case-card";
import type { ExportCaseSummary } from "@/features/export/domain/export-case";
import type { ComplianceEvaluationResult } from "@/features/export/domain/export-3l";
import {
  readComplianceSummary,
  readExportCases,
} from "@/features/export/server/export-case-reader";
import { FailureStatePage } from "@/components/ui/failure-state";

function countActive(cases: ExportCaseSummary[]): number {
  return cases.filter((c) => c.status !== "closed").length;
}

function countReady(cases: ExportCaseSummary[]): number {
  return cases.filter((c) => c.readinessStatus === "ready").length;
}

function countMissingDocs(cases: ExportCaseSummary[]): number {
  return cases.filter(
    (c) =>
      c.missingRequirementCount > 0 || c.readinessStatus === "attention",
  ).length;
}

function countBlocked(cases: ExportCaseSummary[]): number {
  return cases.filter(
    (c) => c.status === "blocked" || c.blockingIssueCount > 0,
  ).length;
}

export default async function ExportCasesPage() {
  const result = await readExportCases();

  if (result.status === "failure") {
    return (
      <div className="export-page">
        <header className="export-header">
          <p>İHRACAT</p>
          <h1>İhracat</h1>
        </header>

        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="İhracat dosyaları yüklenemedi" inline />
      </div>
    );
  }

  const cases = result.cases;
  const complianceByCaseId: Record<
    string,
    { status: ComplianceEvaluationResult; percent: number }
  > = {};

  if (cases.length > 0 && cases.length <= 24) {
    const complianceResults = await Promise.all(
      cases.map(async (exportCase) => {
        const complianceResult = await readComplianceSummary(exportCase.id);
        return complianceResult.status === "success"
          ? ([
              exportCase.id,
              {
                status: complianceResult.compliance.complianceStatus,
                percent: complianceResult.compliance.complianceReadinessPercent,
              },
            ] as const)
          : null;
      }),
    );

    for (const entry of complianceResults) {
      if (entry) {
        complianceByCaseId[entry[0]] = {
          status: entry[1].status,
          percent: entry[1].percent,
        };
      }
    }
  }

  const activeCount = countActive(cases);
  const readyCount = countReady(cases);
  const missingCount = countMissingDocs(cases);
  const blockedCount = countBlocked(cases);

  return (
    <div className="export-page">
      <header className="export-header">
        <div className="export-header__top">
          <div className="export-header__intro">
            <p>İHRACAT</p>
            <h1>İhracat</h1>
          </div>
          <div className="export-header__actions">
            <Link
              href="/ihracat/ulke-arastirma"
              className="button button-primary button-medium"
            >
              <Plus aria-hidden="true" />
              Yeni Ülke Araştır
            </Link>
          </div>
        </div>
        <span>
          İhracat siparişleri, belge hazırlığı ve sevkiyat öncesi uygunluk takibi
        </span>
      </header>

      <section className="export-kpis" aria-label="Özet istatistikler">
        <Card className="export-kpis__item--active">
          <FileStack aria-hidden="true" />
          <div>
            <span>Aktif</span>
            <strong>{activeCount}</strong>
            <small>Açık ihracat dosyaları</small>
          </div>
        </Card>

        <Card className="export-kpis__item--ready">
          <CheckCircle2 aria-hidden="true" />
          <div>
            <span>Hazır</span>
            <strong>{readyCount}</strong>
            <small>Belge hazırlığı tamam</small>
          </div>
        </Card>

        <Card className="export-kpis__item--missing">
          <AlertTriangle aria-hidden="true" />
          <div>
            <span>Eksik Belgeli</span>
            <strong>{missingCount}</strong>
            <small>Eksik gereksinim</small>
          </div>
        </Card>

        <Card className={blockedCount === 0 ? "export-kpis__item--calm" : "export-kpis__item--blocked"}>
          <ShieldAlert aria-hidden="true" />
          <div>
            <span>Blokeli</span>
            <strong>{blockedCount}</strong>
            <small>Bloke dosyalar</small>
          </div>
        </Card>
      </section>

      <section className="export-workspace" aria-label="İhracat dosyaları listesi">
        <div className="export-workspace__heading">
          <div>
            <h2>Aktif ihracat dosyaları</h2>
            <span>
              Hazırlık yüzdesini, eksik belgeleri ve bloke eden gereksinimleri
              takip edin.
            </span>
          </div>
        </div>

        {cases.length === 0 ? (
          <div className="export-empty" role="status" aria-label="Boş liste">
            <div className="export-empty__icon">
              <FileStack aria-hidden="true" />
            </div>
            <div>
              <p>İHRACAT DOSYASI YOK</p>
              <h2>Henüz oluşturulmuş bir ihracat dosyası bulunmuyor</h2>
            </div>
            <p className="export-empty__desc">
              İhracat siparişlerinden oluşturulan dosyalar burada
              listelenir. Sipariş detayından ihracat dosyası açabilirsiniz.
            </p>
            <Link href="/siparisler" className="button button-secondary button-medium">
              Siparişlere git
            </Link>
          </div>
        ) : (
          <div className="export-workspace__list">
            {cases.map((exportCase) => (
              <ExportCaseCard
                key={exportCase.id}
                exportCase={exportCase}
                complianceStatus={complianceByCaseId[exportCase.id]?.status}
                complianceReadinessPercent={
                  complianceByCaseId[exportCase.id]?.percent
                }
              />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
