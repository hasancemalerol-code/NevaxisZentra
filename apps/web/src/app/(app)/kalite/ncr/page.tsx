import Link from "next/link";
import { FileWarning } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { QualityNcrFilters } from "@/features/quality-control/components/quality-ncr-filters";
import { QualityPageShell } from "@/features/quality-control/components/quality-page-shell";
import "@/features/quality-control/components/quality-workspace.css";
import {
  getQualityNcrSeverityTone,
  getQualityStatusLabel,
} from "@/features/quality-control/domain/quality-control";
import {
  formatQualityDateTime,
  getQualityHoldLabel,
} from "@/features/quality-control/presentation/quality-ui-labels";
import { displayActor } from "@/lib/presentation/display-label";
import { readQualityNcrList } from "@/features/quality-control/server/quality-control-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

type PageProps = Readonly<{
  searchParams: Promise<{
    status?: string;
    severity?: string;
    hold?: string;
    productionOrderId?: string;
  }>;
}>;

export default async function QualityNcrListPage({ searchParams }: PageProps) {
  const filters = await searchParams;
  const holdActive =
    filters.hold === "active" ? true : filters.hold === "none" ? false : undefined;

  const result = await readQualityNcrList({
    status: filters.status,
    severity: filters.severity,
    holdActive,
    productionOrderId: filters.productionOrderId,
  });

  if (result.status === "failure") {
    return (
      <QualityPageShell
        backHref="/kalite"
        backLabel="Kalite ana sayfası"
        eyebrow="KALİTE KONTROL"
        title="Uygunsuzluk Raporları"
        nav="ncr"
      >
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="NCR listesi yüklenemedi" inline />
      </QualityPageShell>
    );
  }

  const ncrs = [...result.ncrs].sort((a, b) => b.updatedAtUtc.localeCompare(a.updatedAtUtc));

  return (
    <QualityPageShell
      backHref="/kalite"
      backLabel="Kalite ana sayfası"
      eyebrow="KALİTE KONTROL"
      title="Uygunsuzluk Raporları"
      subtitle="Açık NCR, kalite blokesi ve düzeltici aksiyon durumlarını izleyin."
      nav="ncr"
      footer={
        <footer className="quality-footer-note">
          <FileWarning aria-hidden="true" />
          Kalite blokesi üretim emri durumundan ayrı izlenir; üretim durumu salt okunurdur.
        </footer>
      }
    >
      <QualityNcrFilters
        status={filters.status}
        severity={filters.severity}
        hold={filters.hold}
        productionOrderId={filters.productionOrderId}
      />

      <section className="quality-section quality-section--wide" aria-label="NCR listesi">
        <header className="quality-section__header">
          <h2>NCR Listesi</h2>
          <span>{ncrs.length} kayıt</span>
        </header>
        {ncrs.length === 0 ? (
          <p className="quality-empty-state">Açık NCR bulunmuyor.</p>
        ) : (
          <div className="quality-table-wrap" tabIndex={0} role="region" aria-label="NCR listesi tablosu">
            <table aria-label="NCR Listesi" className="quality-table quality-table--ncr">
              <thead>
                <tr>
                  <th>NCR No</th>
                  <th>Üretim Emri</th>
                  <th>Ürün</th>
                  <th>Kritiklik</th>
                  <th>Durum</th>
                  <th>Kalite Blokesi</th>
                  <th className="quality-col-wide-only">Uygunsuzluk kararı</th>
                  <th>Sorumlu</th>
                  <th>Oluşturulma</th>
                </tr>
              </thead>
              <tbody>
                {ncrs.map((ncr) => (
                  <tr key={ncr.id}>
                    <td>
                      <div className="quality-table__identifier">
                        <Link href={`/kalite/ncr/${ncr.id}`}>{ncr.ncrNumber}</Link>
                        <small title={ncr.title}>{ncr.title}</small>
                      </div>
                    </td>
                    <td className="quality-table__cell" title={ncr.productionOrderReference ?? ncr.productionOrderId ?? undefined}>
                      {ncr.productionOrderReference ?? ncr.productionOrderId ?? "—"}
                    </td>
                    <td className="quality-table__cell" title={ncr.productReference ?? undefined}>
                      {ncr.productReference ?? "—"}
                    </td>
                    <td>
                      <Badge tone={getQualityNcrSeverityTone(ncr.severity)}>
                        {getQualityStatusLabel(ncr.severity)}
                      </Badge>
                    </td>
                    <td className="quality-table__cell">{getQualityStatusLabel(ncr.status)}</td>
                    <td className="quality-table__cell">
                      {getQualityHoldLabel(ncr.holdStatus, ncr.holdStatus === "active")}
                    </td>
                    <td className="quality-table__cell--wrap quality-col-wide-only">
                      {ncr.dispositionType
                        ? getQualityStatusLabel(ncr.dispositionType)
                        : getQualityStatusLabel(ncr.dispositionStatus)}
                    </td>
                    <td className="quality-table__cell">{displayActor(ncr.ownerSubjectId)}</td>
                    <td className="quality-table__cell">{formatQualityDateTime(ncr.createdAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </QualityPageShell>
  );
}
