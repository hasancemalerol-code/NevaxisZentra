import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, Factory, ShieldCheck } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { QualityInspectionActions } from "@/features/quality-control/components/quality-inspection-actions";
import { QualityInspectionNcrLink } from "@/features/quality-control/components/quality-inspection-ncr-link";
import { getQualityStatusLabel } from "@/features/quality-control/domain/quality-control";
import { readQualityInspectionById } from "@/features/quality-control/server/quality-control-reader";
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

export default async function QualityInspectionDetailPage({ params }: PageProps) {
  const { inspectionId } = await params;
  const result = await readQualityInspectionById(inspectionId);

  if (result.status === "failure") {
    return (
      <main className="production-execution-page">
        <div role="alert" className="state-panel error-state">
          <ShieldCheck aria-hidden="true" />
          <p>{result.message}</p>
        </div>
      </main>
    );
  }

  const inspection = result.inspection;
  if (!inspection.id) {
    notFound();
  }

  return (
    <main className="production-execution-page">
      <header className="production-execution-header">
        <Link href="/kalite" className="production-execution-header__link">
          <ArrowLeft aria-hidden="true" />
          Kalite ana sayfası
        </Link>
        <p>KALİTE KONTROL</p>
        <h1>{inspection.inspectionNumber}</h1>
        <span>
          {inspection.planCode} · {inspection.planName}
        </span>
      </header>

      <section className="production-execution-grid">
        <Card className="production-execution-panel">
          <header>
            <h2>Üretim bağlamı</h2>
          </header>
          <dl className="production-execution-details">
            <div>
              <dt>Emir</dt>
              <dd>{inspection.productionContext.referenceCode}</dd>
            </div>
            <div>
              <dt>Ürün</dt>
              <dd>{inspection.productionContext.productReference ?? "—"}</dd>
            </div>
            <div>
              <dt>Üretim durumu</dt>
              <dd>{inspection.productionContext.status}</dd>
            </div>
            <div>
              <dt>İyi miktar</dt>
              <dd>{inspection.productionContext.goodQuantity}</dd>
            </div>
          </dl>
        </Card>

        <Card className="production-execution-panel">
          <header>
            <h2>Kontrol durumu</h2>
          </header>
          <div className="production-execution-badges">
            <Badge tone="information">{getQualityStatusLabel(inspection.status)}</Badge>
            <Badge tone={inspection.overallResult === "fail" ? "danger" : "success"}>
              {getQualityStatusLabel(inspection.overallResult)}
            </Badge>
            <Badge tone="neutral">{getQualityStatusLabel(inspection.releaseStatus)}</Badge>
          </div>
          <p>Denetçi: {inspection.inspectorSubjectId ?? "—"}</p>
        </Card>

        <Card className="production-execution-panel production-execution-panel--wide">
          <header>
            <h2>Ölçüm / kontrol listesi</h2>
          </header>
          <table className="production-execution-table">
            <thead>
              <tr>
                <th>Kontrol</th>
                <th>Hedef / tolerans</th>
                <th>Ölçüm</th>
                <th>Sonuç</th>
              </tr>
            </thead>
            <tbody>
              {inspection.results.map((row) => (
                <tr key={row.inspectionDefinitionId}>
                  <td>
                    <strong>{row.definitionCode}</strong>
                    <span>{row.definitionName}</span>
                  </td>
                  <td>
                    {row.nominalValue ?? "—"} {row.unit ?? ""}
                    <br />
                    {row.lowerLimit ?? "—"} – {row.upperLimit ?? "—"}
                  </td>
                  <td>
                    {row.measuredNumericValue ?? row.measuredTextValue ?? "—"}
                  </td>
                  <td>{getQualityStatusLabel(row.result)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>

        <Card className="production-execution-panel">
          <header>
            <h2>Uygunsuzluk</h2>
          </header>
          <QualityInspectionNcrLink
            inspection={inspection}
            linkedNonconformances={inspection.linkedNonconformances}
            onCreateNcr={createQualityNcrAction}
          />
        </Card>

        <QualityInspectionActions
          inspection={inspection}
          onStart={startQualityInspectionAction}
          onRecordResult={recordQualityResultAction}
          onComplete={completeQualityInspectionAction}
          onRelease={releaseQualityInspectionAction}
          onReject={rejectQualityInspectionAction}
        />
      </section>

      <footer className="production-execution-footer-note">
        <Factory aria-hidden="true" />
        Üretim emri verileri salt okunur; kalite serbest bırakma üretim tamamlama değildir.
      </footer>
    </main>
  );
}
