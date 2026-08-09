import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, Factory, ShieldCheck } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { QualityNcrActions } from "@/features/quality-control/components/quality-ncr-actions";
import {
  getQualityNcrSeverityTone,
  getQualityStatusLabel,
} from "@/features/quality-control/domain/quality-control";
import { readQualityNcrById } from "@/features/quality-control/server/quality-control-reader";
import {
  closeQualityNcrAction,
  completeQualityCorrectiveActionAction,
  createQualityCorrectiveActionAction,
  decideQualityDispositionAction,
  placeQualityHoldAction,
  releaseQualityHoldAction,
  resolveQualityNcrAction,
  startQualityCorrectiveActionAction,
} from "@/features/quality-control/server/quality-control-writer";

type PageProps = Readonly<{
  params: Promise<{ ncrId: string }>;
}>;

export default async function QualityNcrDetailPage({ params }: PageProps) {
  const { ncrId } = await params;
  const result = await readQualityNcrById(ncrId);

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

  const ncr = result.ncr;
  if (!ncr.id) {
    notFound();
  }

  const holdActive = ncr.holdStatus === "active" || ncr.activeHold?.status === "active";

  return (
    <main className="production-execution-page">
      <header className="production-execution-header">
        <Link href="/kalite/ncr" className="production-execution-header__link">
          <ArrowLeft aria-hidden="true" />
          NCR listesi
        </Link>
        <p>KALİTE KONTROL</p>
        <h1>{ncr.ncrNumber}</h1>
        <span>{ncr.title}</span>
      </header>

      <section className="production-execution-grid">
        <Card className="production-execution-panel">
          <header>
            <h2>NCR özeti</h2>
          </header>
          <div className="production-execution-badges">
            <Badge tone={getQualityNcrSeverityTone(ncr.severity)}>
              {getQualityStatusLabel(ncr.severity)}
            </Badge>
            <Badge tone="information">{getQualityStatusLabel(ncr.status)}</Badge>
            <Badge tone={holdActive ? "danger" : "neutral"}>
              Kalite Blokesi: {holdActive ? "Aktif" : getQualityStatusLabel(ncr.holdStatus)}
            </Badge>
          </div>
          <p>{ncr.description}</p>
          <dl className="production-execution-details">
            <div>
              <dt>Kategori</dt>
              <dd>{ncr.category ?? "—"}</dd>
            </div>
            <div>
              <dt>Tespit eden</dt>
              <dd>{ncr.detectedBySubjectId}</dd>
            </div>
            <div>
              <dt>Sorumlu</dt>
              <dd>{ncr.ownerSubjectId ?? "—"}</dd>
            </div>
          </dl>
        </Card>

        <Card className="production-execution-panel">
          <header>
            <h2>Üretim bağlamı</h2>
          </header>
          {ncr.productionContext ? (
            <dl className="production-execution-details">
              <div>
                <dt>Emir</dt>
                <dd>{ncr.productionContext.referenceCode}</dd>
              </div>
              <div>
                <dt>Ürün</dt>
                <dd>{ncr.productionContext.productReference ?? "—"}</dd>
              </div>
              <div>
                <dt>Üretim durumu</dt>
                <dd>{ncr.productionContext.status}</dd>
              </div>
            </dl>
          ) : (
            <p>Bağlı üretim emri yok.</p>
          )}
          {ncr.qualityInspectionNumber ? (
            <p>
              Bağlı kontrol:{" "}
              <Link href={`/kalite/kontroller/${ncr.qualityInspectionId}`}>
                {ncr.qualityInspectionNumber}
              </Link>
            </p>
          ) : null}
        </Card>

        <Card className="production-execution-panel">
          <header>
            <h2>Disposition</h2>
          </header>
          <p>Durum: {getQualityStatusLabel(ncr.dispositionStatus)}</p>
          <p>
            Karar:{" "}
            {ncr.dispositionType ? getQualityStatusLabel(ncr.dispositionType) : "—"}
          </p>
          <p>{ncr.dispositionNote ?? "—"}</p>
        </Card>

        <Card className="production-execution-panel production-execution-panel--wide">
          <header>
            <h2>Düzeltici aksiyonlar</h2>
          </header>
          {ncr.correctiveActions.length === 0 ? (
            <p>Düzeltici aksiyon yok.</p>
          ) : (
            <ul className="production-execution-list">
              {ncr.correctiveActions.map((action) => (
                <li key={action.id}>
                  <strong>{action.title}</strong>
                  <span>{action.description}</span>
                  <Badge tone="neutral">{getQualityStatusLabel(action.status)}</Badge>
                </li>
              ))}
            </ul>
          )}
        </Card>

        <QualityNcrActions
          ncr={ncr}
          onPlaceHold={placeQualityHoldAction}
          onReleaseHold={releaseQualityHoldAction}
          onDecideDisposition={decideQualityDispositionAction}
          onCreateCorrectiveAction={createQualityCorrectiveActionAction}
          onStartCorrectiveAction={startQualityCorrectiveActionAction}
          onCompleteCorrectiveAction={completeQualityCorrectiveActionAction}
          onResolve={resolveQualityNcrAction}
          onClose={closeQualityNcrAction}
        />
      </section>

      <footer className="production-execution-footer-note">
        <Factory aria-hidden="true" />
        Kalite blokesi üretim emri durumunu doğrudan değiştirmez; sinyal salt okunur bağlamda sunulur.
      </footer>
    </main>
  );
}
