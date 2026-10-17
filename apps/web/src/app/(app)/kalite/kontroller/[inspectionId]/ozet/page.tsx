import { notFound } from "next/navigation";
import { ClipboardCheck } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { QualityPageShell } from "@/features/quality-control/components/quality-page-shell";
import "@/features/quality-control/components/quality-workspace.css";
import {
  getQualityOverallResultLabel,
  getQualityReleaseStatusLabel,
} from "@/features/quality-control/presentation/quality-dashboard-labels";
import { formatQualityDateTime } from "@/features/quality-control/presentation/quality-ui-labels";
import { readInspectionSummary } from "@/features/quality-control/server/quality-control-reader";
import { FailureStatePage } from "@/components/ui/failure-state";

type PageProps = Readonly<{
  params: Promise<{ inspectionId: string }>;
}>;

function getResultTone(result: string): "success" | "danger" | "neutral" {
  if (result === "pass") {
    return "success";
  }
  if (result === "fail") {
    return "danger";
  }
  return "neutral";
}

export default async function QualityInspectionSummaryPage({ params }: PageProps) {
  const { inspectionId } = await params;
  const result = await readInspectionSummary(inspectionId);

  if (result.status === "failure") {
    return (
      <QualityPageShell
        eyebrow="KALİTE KONTROL"
        title="Kontrol Sonuç Özeti"
      >
        <FailureStatePage
          code={"code" in result ? result.code : undefined}
          message={result.message}
          failedTitle="Özet yüklenemedi"
          entityLabel="kalite kontrolü"
          backHref="/kalite"
          backLabel="Kalite ana sayfası"
          inline
        />
      </QualityPageShell>
    );
  }

  const { summary, releaseIntelligence } = result;
  if (!summary.inspectionId) {
    notFound();
  }

  return (
    <QualityPageShell
      backHref={`/kalite/kontroller/${inspectionId}`}
      backLabel="Kontrol detayı"
      eyebrow="KALİTE KONTROL"
      title="Kontrol Sonuç Özeti"
      subtitle={`${summary.inspectionNumber} · ${summary.controlPlanCode} rev. ${summary.controlPlanRevision}`}
      footer={
        <footer className="quality-footer-note">
          <ClipboardCheck aria-hidden="true" />
          Bu özet salt okunur raporlama görünümüdür; resmi sertifika değildir.
        </footer>
      }
    >
      <section className="quality-section" aria-label="Kalite kontrol özeti">
        <header className="quality-section__header">
          <h2>Kalite Kontrol Özeti</h2>
        </header>
        <dl className="quality-detail-dl">
          <div>
            <dt>Tesis Referansı</dt>
            <dd>{summary.facilityReference}</dd>
          </div>
          <div>
            <dt>Kontrol Tarihi</dt>
            <dd>{formatQualityDateTime(summary.inspectionDateUtc)}</dd>
          </div>
          <div>
            <dt>Denetçi</dt>
            <dd>{summary.inspectorSubjectId ?? "—"}</dd>
          </div>
        </dl>
      </section>

      <div className="quality-detail-grid">
        <section className="quality-section" aria-label="Üretim bilgileri">
          <header className="quality-section__header">
            <h2>Üretim Bilgileri</h2>
          </header>
          <dl className="quality-detail-dl">
            <div>
              <dt>Üretim Emri</dt>
              <dd>{summary.productionOrderReference ?? summary.productionOrderId}</dd>
            </div>
            <div>
              <dt>Ürün</dt>
              <dd>{summary.productReference ?? "—"}</dd>
            </div>
          </dl>
        </section>

        <section className="quality-section" aria-label="Kontrol planı">
          <header className="quality-section__header">
            <h2>Kontrol Planı</h2>
          </header>
          <dl className="quality-detail-dl">
            <div>
              <dt>Plan</dt>
              <dd>
                {summary.controlPlanCode} · {summary.controlPlanName}
              </dd>
            </div>
            <div>
              <dt>Revizyon</dt>
              <dd>{summary.controlPlanRevision}</dd>
            </div>
          </dl>
        </section>
      </div>

      <section className="quality-section" aria-label="Genel sonuç">
        <header className="quality-section__header">
          <h2>Genel Sonuç</h2>
        </header>
        <div className="quality-badge-row">
          <Badge tone={getResultTone(summary.overallResult)}>
            {getQualityOverallResultLabel(summary.overallResult)}
          </Badge>
          <Badge tone="neutral">{getQualityReleaseStatusLabel(summary.releaseStatus)}</Badge>
          {summary.hasBlockingHold ? <Badge tone="warning">Kalite Blokesi: Aktif</Badge> : null}
        </div>
        <p>Açık NCR: {summary.openNcrCount}</p>
        {summary.blockingSummary ? <p>{summary.blockingSummary}</p> : null}
      </section>

      <section className="quality-section quality-section--wide" aria-label="Serbest bırakma">
        <header className="quality-section__header">
          <h2>Release Durumu</h2>
        </header>
        {!releaseIntelligence.available ? (
          <p className="quality-empty-state">Serbest bırakma değerlendirmesi kullanılamıyor.</p>
        ) : (
          <>
            <div className="quality-badge-row">
              <Badge tone={releaseIntelligence.blocking ? "danger" : "success"}>
                {releaseIntelligence.blocking ? "Bloklayıcı" : "Blok yok"}
              </Badge>
              <Badge tone={releaseIntelligence.readyForRelease ? "success" : "warning"}>
                {releaseIntelligence.readyForRelease
                  ? "Serbest bırakmaya hazır"
                  : "İnceleme gerekli"}
              </Badge>
            </div>
            <dl className="quality-detail-dl">
              <div>
                <dt>Aktif Blokesi</dt>
                <dd>{releaseIntelligence.activeHoldCount}</dd>
              </div>
              <div>
                <dt>Açık NCR</dt>
                <dd>{releaseIntelligence.openNcrCount}</dd>
              </div>
              <div>
                <dt>Kritik NCR</dt>
                <dd>{releaseIntelligence.criticalNcrCount}</dd>
              </div>
              <div>
                <dt>Tamamlanmamış Aksiyon</dt>
                <dd>{releaseIntelligence.incompleteCorrectiveActionCount}</dd>
              </div>
            </dl>
            {releaseIntelligence.reasonSummary ? <p>{releaseIntelligence.reasonSummary}</p> : null}
          </>
        )}
      </section>

      <section className="quality-section quality-section--wide" aria-label="Ölçüm sonuçları">
        <header className="quality-section__header">
          <h2>Ölçüm Sonuçları</h2>
        </header>
        <div className="quality-table-wrap" tabIndex={0} role="region" aria-label="Ölçüm sonuçları tablosu">
          <table aria-label="Ölçüm Sonuçları" className="quality-table">
            <thead>
              <tr>
                <th>Kontrol Noktası</th>
                <th>Hedef / Tolerans</th>
                <th>Ölçülen</th>
                <th>Sonuç</th>
              </tr>
            </thead>
            <tbody>
              {summary.measurements.map((row) => (
                <tr key={`${row.definitionCode}-${row.definitionName}`}>
                  <td>
                    <div className="quality-table__identifier">
                      <strong>{row.definitionCode}</strong>
                      <small>{row.definitionName}</small>
                    </div>
                  </td>
                  <td className="quality-table__cell--wrap">
                    {row.nominalValue ?? "—"} {row.unit ?? ""}
                    <br />
                    {row.lowerLimit ?? "—"} – {row.upperLimit ?? "—"}
                  </td>
                  <td className="quality-table__cell">
                    {row.measuredNumericValue ??
                      row.measuredTextValue ??
                      (row.booleanResult === undefined
                        ? "—"
                        : row.booleanResult
                          ? "Evet"
                          : "Hayır")}
                  </td>
                  <td>
                    <Badge tone={getResultTone(row.result)}>
                      {getQualityOverallResultLabel(row.result)}
                    </Badge>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="quality-section" aria-label="NCR hold özeti">
        <header className="quality-section__header">
          <h2>NCR / Hold Özeti</h2>
        </header>
        <p>Açık NCR: {summary.openNcrCount}</p>
        {summary.hasBlockingHold ? (
          <p>Kalite Blokesi: Aktif</p>
        ) : (
          <p className="quality-empty-state">Aktif kalite blokesi bulunmuyor.</p>
        )}
      </section>
    </QualityPageShell>
  );
}
