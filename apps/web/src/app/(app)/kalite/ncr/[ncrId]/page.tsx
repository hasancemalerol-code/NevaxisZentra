import Link from "next/link";
import { notFound } from "next/navigation";
import { Factory, ShieldCheck } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { QualityNcrActions } from "@/features/quality-control/components/quality-ncr-actions";
import { QualityPageShell } from "@/features/quality-control/components/quality-page-shell";
import "@/features/quality-control/components/quality-workspace.css";
import {
  getQualityNcrSeverityTone,
  getQualityStatusLabel,
} from "@/features/quality-control/domain/quality-control";
import {
  formatQualityDateTime,
  getCorrectiveActionStatusLabel,
  getCorrectiveActionStatusTone,
  getQualityHoldLabel,
} from "@/features/quality-control/presentation/quality-ui-labels";
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
      <QualityPageShell
        backHref="/kalite/ncr"
        backLabel="NCR listesi"
        eyebrow="KALİTE KONTROL"
        title="Uygunsuzluk Detayı"
      >
        <div role="alert" className="quality-error-panel">
          <ShieldCheck aria-hidden="true" />
          <div>
            <h3>{result.code === "not-found" ? "Kayıt bulunamadı" : "NCR yüklenemedi"}</h3>
            <p>{result.code === "not-found" ? "İstenen uygunsuzluk kaydı bulunamadı." : result.message}</p>
          </div>
        </div>
      </QualityPageShell>
    );
  }

  const ncr = result.ncr;
  if (!ncr.id) {
    notFound();
  }

  const holdActive = ncr.holdStatus === "active" || ncr.activeHold?.status === "active";

  return (
    <QualityPageShell
      backHref="/kalite/ncr"
      backLabel="NCR listesi"
      eyebrow="KALİTE KONTROL"
      title={ncr.ncrNumber}
      subtitle={ncr.title}
      footer={
        <footer className="quality-footer-note">
          <Factory aria-hidden="true" />
          Kalite blokesi üretim emri durumunu doğrudan değiştirmez; sinyal salt okunur bağlamda sunulur.
        </footer>
      }
    >
      <section className="quality-section" aria-label="NCR özeti">
        <header className="quality-section__header">
          <h2>NCR Özeti</h2>
        </header>
        <div className="quality-badge-row">
          <Badge tone={getQualityNcrSeverityTone(ncr.severity)}>
            {getQualityStatusLabel(ncr.severity)}
          </Badge>
          <Badge tone="information">{getQualityStatusLabel(ncr.status)}</Badge>
          <Badge tone={holdActive ? "danger" : "neutral"}>
            Kalite Blokesi: {getQualityHoldLabel(ncr.holdStatus, holdActive)}
          </Badge>
        </div>
        <p>{ncr.description}</p>
        <dl className="quality-detail-dl">
          <div>
            <dt>Üretim Emri</dt>
            <dd>{ncr.productionContext?.referenceCode ?? ncr.productionOrderId ?? "—"}</dd>
          </div>
          <div>
            <dt>Ürün</dt>
            <dd>{ncr.productReference ?? ncr.productionContext?.productReference ?? "—"}</dd>
          </div>
          <div>
            <dt>Oluşturulma</dt>
            <dd>{formatQualityDateTime(ncr.createdAtUtc)}</dd>
          </div>
          <div>
            <dt>Sorumlu</dt>
            <dd>{ncr.ownerSubjectId ?? "—"}</dd>
          </div>
          <div>
            <dt>Tespit Eden</dt>
            <dd>{ncr.detectedBySubjectId}</dd>
          </div>
          <div>
            <dt>Kategori</dt>
            <dd>{ncr.category ?? "—"}</dd>
          </div>
        </dl>
      </section>

      <div className="quality-detail-grid">
        <section className="quality-section" aria-label="Bağlı üretim">
          <header className="quality-section__header">
            <h2>Bağlı Üretim</h2>
          </header>
          {ncr.productionContext ? (
            <dl className="quality-detail-dl">
              <div>
                <dt>Emir</dt>
                <dd>{ncr.productionContext.referenceCode}</dd>
              </div>
              <div>
                <dt>Ürün</dt>
                <dd>{ncr.productionContext.productReference ?? "—"}</dd>
              </div>
              <div>
                <dt>Üretim Durumu</dt>
                <dd>{ncr.productionContext.status}</dd>
              </div>
            </dl>
          ) : (
            <p className="quality-empty-state">Bağlı üretim emri yok.</p>
          )}
        </section>

        <section className="quality-section" aria-label="Bağlı kalite kontrolü">
          <header className="quality-section__header">
            <h2>Bağlı Kalite Kontrolü</h2>
          </header>
          {ncr.qualityInspectionNumber ? (
            <p>
              <Link href={`/kalite/kontroller/${ncr.qualityInspectionId}`}>
                {ncr.qualityInspectionNumber}
              </Link>
            </p>
          ) : (
            <p className="quality-empty-state">Bağlı kalite kontrolü yok.</p>
          )}
        </section>

        <section
          className={`quality-section ${holdActive ? "quality-hold-panel" : ""}`}
          aria-label="Kalite blokesi"
        >
          <header className="quality-section__header">
            <h2 className={holdActive ? "quality-hold-panel__title" : undefined}>
              Kalite Blokesi
            </h2>
          </header>
          {holdActive && ncr.activeHold ? (
            <dl className="quality-detail-dl">
              <div>
                <dt>Durum</dt>
                <dd>Kalite Blokesi: Aktif</dd>
              </div>
              <div>
                <dt>Neden</dt>
                <dd>{ncr.activeHold.reason}</dd>
              </div>
              <div>
                <dt>Koyan</dt>
                <dd>{ncr.activeHold.placedBySubjectId}</dd>
              </div>
              <div>
                <dt>Koyulma</dt>
                <dd>{formatQualityDateTime(ncr.activeHold.placedAtUtc)}</dd>
              </div>
            </dl>
          ) : (
            <p className="quality-empty-state">Aktif kalite blokesi bulunmuyor.</p>
          )}
        </section>

        <section className="quality-section" aria-label="Disposition">
          <header className="quality-section__header">
            <h2>Disposition</h2>
          </header>
          <dl className="quality-detail-dl">
            <div>
              <dt>Durum</dt>
              <dd>{getQualityStatusLabel(ncr.dispositionStatus)}</dd>
            </div>
            <div>
              <dt>Karar</dt>
              <dd>
                {ncr.dispositionType ? getQualityStatusLabel(ncr.dispositionType) : "Bekliyor"}
              </dd>
            </div>
          </dl>
          <p>{ncr.dispositionNote ?? "Disposition notu yok."}</p>
        </section>
      </div>

      <section className="quality-section quality-section--wide" aria-label="Düzeltici aksiyonlar">
        <header className="quality-section__header">
          <h2>Düzeltici Aksiyonlar</h2>
        </header>
        {ncr.correctiveActions.length === 0 ? (
          <p className="quality-empty-state">Düzeltici aksiyon bulunmuyor.</p>
        ) : (
          <ul className="quality-ca-list">
            {ncr.correctiveActions.map((action) => (
              <li key={action.id} className="quality-ca-item">
                <div className="quality-ca-item__body">
                  <strong>{action.title}</strong>
                  <span>{action.description}</span>
                  <div className="quality-ca-item__meta">
                    <span>Sorumlu: {action.ownerSubjectId ?? "—"}</span>
                    <span>Termin: {formatQualityDateTime(action.dueAtUtc)}</span>
                    {action.completionNote ? (
                      <span>Not: {action.completionNote}</span>
                    ) : null}
                  </div>
                </div>
                <Badge tone={getCorrectiveActionStatusTone(action)}>
                  {getCorrectiveActionStatusLabel(action)}
                </Badge>
              </li>
            ))}
          </ul>
        )}
      </section>

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
    </QualityPageShell>
  );
}
