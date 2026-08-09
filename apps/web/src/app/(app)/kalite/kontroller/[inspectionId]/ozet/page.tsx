import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, ClipboardCheck, ShieldCheck } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { getQualityStatusLabel } from "@/features/quality-control/domain/quality-control";
import { readInspectionSummary } from "@/features/quality-control/server/quality-control-reader";

type PageProps = Readonly<{
  params: Promise<{ inspectionId: string }>;
}>;

export default async function QualityInspectionSummaryPage({ params }: PageProps) {
  const { inspectionId } = await params;
  const result = await readInspectionSummary(inspectionId);

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

  const { summary, releaseIntelligence } = result;
  if (!summary.inspectionId) {
    notFound();
  }

  return (
    <main className="production-execution-page">
      <header className="production-execution-header">
        <Link
          href={`/kalite/kontroller/${inspectionId}`}
          className="production-execution-header__link"
        >
          <ArrowLeft aria-hidden="true" />
          Kontrol detayı
        </Link>
        <p>KALİTE KONTROL</p>
        <h1>Kontrol Sonuç Özeti</h1>
        <span>
          {summary.inspectionNumber} · {summary.controlPlanCode} rev. {summary.controlPlanRevision}
        </span>
      </header>

      <section className="production-execution-grid">
        <Card className="production-execution-panel">
          <header>
            <h2>Özet bilgiler</h2>
          </header>
          <dl className="production-execution-details">
            <div>
              <dt>Tesis referansı</dt>
              <dd>{summary.facilityReference}</dd>
            </div>
            <div>
              <dt>Üretim emri</dt>
              <dd>{summary.productionOrderReference ?? summary.productionOrderId}</dd>
            </div>
            <div>
              <dt>Ürün</dt>
              <dd>{summary.productReference ?? "—"}</dd>
            </div>
            <div>
              <dt>Kontrol planı</dt>
              <dd>
                {summary.controlPlanCode} · {summary.controlPlanName}
              </dd>
            </div>
            <div>
              <dt>Kontrol tarihi</dt>
              <dd>
                {summary.inspectionDateUtc
                  ? new Date(summary.inspectionDateUtc).toLocaleString("tr-TR")
                  : "—"}
              </dd>
            </div>
            <div>
              <dt>Denetçi</dt>
              <dd>{summary.inspectorSubjectId ?? "—"}</dd>
            </div>
          </dl>
        </Card>

        <Card className="production-execution-panel">
          <header>
            <h2>Sonuç durumu</h2>
          </header>
          <div className="production-execution-badges">
            <Badge tone={summary.overallResult === "fail" ? "danger" : "success"}>
              {getQualityStatusLabel(summary.overallResult)}
            </Badge>
            <Badge tone="neutral">{getQualityStatusLabel(summary.releaseStatus)}</Badge>
            {summary.hasBlockingHold ? <Badge tone="warning">Bloklayıcı blokesi</Badge> : null}
          </div>
          <p>Açık NCR: {summary.openNcrCount}</p>
          {summary.blockingSummary ? <p>{summary.blockingSummary}</p> : null}
        </Card>

        <Card className="production-execution-panel production-execution-panel--wide">
          <header>
            <h2>Serbest bırakma zekâsı</h2>
          </header>
          {!releaseIntelligence.available ? (
            <p>Serbest bırakma değerlendirmesi kullanılamıyor.</p>
          ) : (
            <>
              <div className="production-execution-badges">
                <Badge tone={releaseIntelligence.blocking ? "danger" : "success"}>
                  {releaseIntelligence.blocking ? "Bloklayıcı" : "Blok yok"}
                </Badge>
                <Badge tone={releaseIntelligence.readyForRelease ? "success" : "warning"}>
                  {releaseIntelligence.readyForRelease
                    ? "Serbest bırakmaya hazır"
                    : "İnceleme gerekli"}
                </Badge>
              </div>
              <dl className="production-execution-details">
                <div>
                  <dt>Aktif blokesi</dt>
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
                  <dt>Tamamlanmamış aksiyon</dt>
                  <dd>{releaseIntelligence.incompleteCorrectiveActionCount}</dd>
                </div>
              </dl>
              {releaseIntelligence.reasonSummary ? (
                <p>{releaseIntelligence.reasonSummary}</p>
              ) : null}
              {releaseIntelligence.reasonCodes.length > 0 ? (
                <div className="production-execution-badges">
                  {releaseIntelligence.reasonCodes.map((code) => (
                    <Badge key={code} tone="information">
                      {code}
                    </Badge>
                  ))}
                </div>
              ) : null}
            </>
          )}
        </Card>

        <Card className="production-execution-panel production-execution-panel--wide">
          <header>
            <h2>Ölçüm özeti</h2>
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
              {summary.measurements.map((row) => (
                <tr key={`${row.definitionCode}-${row.definitionName}`}>
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
                    {row.measuredNumericValue ??
                      row.measuredTextValue ??
                      (row.booleanResult === undefined ? "—" : row.booleanResult ? "Evet" : "Hayır")}
                  </td>
                  <td>{getQualityStatusLabel(row.result)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      </section>

      <footer className="production-execution-footer-note">
        <ClipboardCheck aria-hidden="true" />
        Bu özet salt okunur raporlama görünümüdür; serbest bırakma kararı operasyon ekranından verilir.
      </footer>
    </main>
  );
}
