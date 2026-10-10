import Link from "next/link";
import { notFound } from "next/navigation";
import { Factory, GitBranch, ShieldCheck } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { QualityInspectionActions } from "@/features/quality-control/components/quality-inspection-actions";
import { QualityInspectionNcrLink } from "@/features/quality-control/components/quality-inspection-ncr-link";
import { QualityPageShell } from "@/features/quality-control/components/quality-page-shell";
import { QualityTraceabilityPanel } from "@/features/quality-control/components/quality-traceability-panel";
import "@/features/quality-control/components/quality-workspace.css";
import { getQualityStatusLabel } from "@/features/quality-control/domain/quality-control";
import {
  getQualityOverallResultLabel,
  getQualityReleaseStatusLabel,
} from "@/features/quality-control/presentation/quality-dashboard-labels";
import {
  readProductionOrderTraceability,
  readQualityInspectionById,
} from "@/features/quality-control/server/quality-control-reader";
import {
  completeQualityInspectionAction,
  createQualityNcrAction,
  recordQualityResultAction,
  releaseQualityInspectionAction,
  rejectQualityInspectionAction,
  startQualityInspectionAction,
} from "@/features/quality-control/server/quality-control-writer";

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

export default async function QualityInspectionDetailPage({ params }: PageProps) {
  const { inspectionId } = await params;
  const result = await readQualityInspectionById(inspectionId);

  if (result.status === "failure") {
    return (
      <QualityPageShell
        backHref="/kalite"
        backLabel="Kalite ana sayfası"
        eyebrow="KALİTE KONTROL"
        title="Kalite Kontrol Detayı"
      >
        <div role="alert" className="quality-error-panel">
          <ShieldCheck aria-hidden="true" />
          <div>
            <h3>{result.code === "not-found" ? "Kayıt bulunamadı" : "Kontrol yüklenemedi"}</h3>
            <p>{result.code === "not-found" ? "İstenen kalite kontrolü bulunamadı." : result.message}</p>
          </div>
        </div>
      </QualityPageShell>
    );
  }

  const inspection = result.inspection;
  if (!inspection.id) {
    notFound();
  }

  const traceabilityResult = await readProductionOrderTraceability(inspection.productionOrderId);
  const traceability =
    traceabilityResult.status === "success" ? traceabilityResult.traceability : null;

  return (
    <QualityPageShell
      backHref="/kalite"
      backLabel="Kalite ana sayfası"
      eyebrow="KALİTE KONTROL"
      title={inspection.inspectionNumber}
      subtitle={`${inspection.planCode} · ${inspection.planName}`}
      actions={
        <Link href={`/kalite/kontroller/${inspectionId}/ozet`} className="quality-dashboard-action quality-dashboard-action--secondary">
          <GitBranch aria-hidden="true" />
          Kontrol Sonuç Özeti
        </Link>
      }
      footer={
        <footer className="quality-footer-note">
          <Factory aria-hidden="true" />
          Üretim emri verileri salt okunur; kalite serbest bırakma üretim tamamlama değildir.
        </footer>
      }
    >
      <section className="quality-section" aria-label="Kontrol durumu">
        <header className="quality-section__header">
          <h2>Kontrol Durumu</h2>
        </header>
        <div className="quality-badge-row">
          <Badge tone="information">{getQualityStatusLabel(inspection.status)}</Badge>
          <Badge tone={getResultTone(inspection.overallResult)}>
            {getQualityOverallResultLabel(inspection.overallResult)}
          </Badge>
          <Badge tone="neutral">{getQualityReleaseStatusLabel(inspection.releaseStatus)}</Badge>
        </div>
        <dl className="quality-detail-dl">
          <div>
            <dt>Denetçi</dt>
            <dd>{inspection.inspectorSubjectId ?? "—"}</dd>
          </div>
        </dl>
      </section>

      <div className="quality-detail-grid">
        <section className="quality-section" aria-label="Üretim bağlamı">
          <header className="quality-section__header">
            <h2>Üretim Bağlamı</h2>
          </header>
          <dl className="quality-detail-dl">
            <div>
              <dt>Emir</dt>
              <dd>{inspection.productionContext.referenceCode}</dd>
            </div>
            <div>
              <dt>Ürün</dt>
              <dd>{inspection.productionContext.productReference ?? "—"}</dd>
            </div>
            <div>
              <dt>Üretim Durumu</dt>
              <dd>{inspection.productionContext.status}</dd>
            </div>
            <div>
              <dt>İyi Miktar</dt>
              <dd>{inspection.productionContext.goodQuantity}</dd>
            </div>
          </dl>
        </section>

        <section className="quality-section" aria-label="Kontrol planı">
          <header className="quality-section__header">
            <h2>Kontrol Planı</h2>
          </header>
          <dl className="quality-detail-dl">
            <div>
              <dt>Plan Kodu</dt>
              <dd>{inspection.planCode}</dd>
            </div>
            <div>
              <dt>Plan Adı</dt>
              <dd>{inspection.planName}</dd>
            </div>
          </dl>
        </section>
      </div>

      <section className="quality-section quality-section--wide" aria-label="Ölçüm kontrol listesi">
        <header className="quality-section__header">
          <h2>Ölçüm / Kontrol Listesi</h2>
        </header>
        {inspection.results.length === 0 ? (
          <p className="quality-empty-state">Kontrol sonucu bulunmuyor.</p>
        ) : (
          <div className="quality-table-wrap">
            <table className="quality-table">
              <thead>
                <tr>
                  <th>Kontrol Noktası</th>
                  <th>Hedef / Tolerans</th>
                  <th>Ölçülen</th>
                  <th>Sonuç</th>
                  <th>Yorum</th>
                </tr>
              </thead>
              <tbody>
                {inspection.results.map((row) => (
                  <tr key={row.inspectionDefinitionId}>
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
                      {row.measuredNumericValue ?? row.measuredTextValue ?? "—"}
                    </td>
                    <td>
                      <Badge tone={getResultTone(row.result)}>
                        {getQualityOverallResultLabel(row.result)}
                      </Badge>
                    </td>
                    <td className="quality-table__cell--wrap">{row.comment ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="quality-section" aria-label="Uygunsuzluk">
        <header className="quality-section__header">
          <h2>NCR / Kalite Blokesi</h2>
        </header>
        <QualityInspectionNcrLink
          inspection={inspection}
          linkedNonconformances={inspection.linkedNonconformances}
          onCreateNcr={createQualityNcrAction}
        />
      </section>

      <QualityInspectionActions
        inspection={inspection}
        onStart={startQualityInspectionAction}
        onRecordResult={recordQualityResultAction}
        onComplete={completeQualityInspectionAction}
        onRelease={releaseQualityInspectionAction}
        onReject={rejectQualityInspectionAction}
      />

      <QualityTraceabilityPanel traceability={traceability} />
    </QualityPageShell>
  );
}
