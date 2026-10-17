import type { CSSProperties } from "react";
import Link from "next/link";
import { notFound } from "next/navigation";
import { Factory } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { QualityCorrectiveActionControls } from "@/features/quality-control/components/quality-corrective-action-controls";
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
  getQualityCategoryLabel,
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
import { displayActor } from "@/lib/presentation/display-label";
import { enumLabel } from "@/lib/presentation/enum-labels";
import { FailureStatePage } from "@/components/ui/failure-state";

type PageProps = Readonly<{
  params: Promise<{ ncrId: string }>;
}>;

type NcrCols = CSSProperties & Record<"--ncr-cols" | "--ncr-cols-narrow", number>;

function ncrCols(wide: number, narrow: number): NcrCols {
  return { "--ncr-cols": wide, "--ncr-cols-narrow": narrow };
}

export default async function QualityNcrDetailPage({ params }: PageProps) {
  const { ncrId } = await params;
  const result = await readQualityNcrById(ncrId);

  if (result.status === "failure") {
    return (
      <QualityPageShell
        eyebrow="KALİTE KONTROL"
        title="Uygunsuzluk Detayı"
      >
        <FailureStatePage
          code={"code" in result ? result.code : undefined}
          message={result.message}
          failedTitle="NCR yüklenemedi"
          entityLabel="uygunsuzluk kaydı"
          backHref="/kalite/ncr"
          backLabel="NCR listesi"
          inline
        />
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
          <h2>NCR özeti</h2>
        </header>
        <div className="quality-badge-row">
          <span className="quality-labeled-chip">
            <span className="quality-labeled-chip__label">Kritiklik</span>
            <Badge tone={getQualityNcrSeverityTone(ncr.severity)}>
              {getQualityStatusLabel(ncr.severity)}
            </Badge>
          </span>
          <span className="quality-labeled-chip">
            <span className="quality-labeled-chip__label">Durum</span>
            <Badge tone="information">{getQualityStatusLabel(ncr.status)}</Badge>
          </span>
          <span className="quality-labeled-chip">
            <span className="quality-labeled-chip__label">Kalite blokesi</span>
            <Badge tone={holdActive ? "danger" : "neutral"}>
              {getQualityHoldLabel(ncr.holdStatus, holdActive)}
            </Badge>
          </span>
        </div>
        <p>{ncr.description}</p>
        <dl className="quality-ncr-dl" style={ncrCols(3, 2)}>
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
            <dd>{ncr.ownerSubjectId ? displayActor(ncr.ownerSubjectId) : "Atanmadı"}</dd>
          </div>
          <div>
            <dt>Tespit Eden</dt>
            <dd>{displayActor(ncr.detectedBySubjectId)}</dd>
          </div>
          <div>
            <dt>Kategori</dt>
            <dd>{ncr.category ? getQualityCategoryLabel(ncr.category) : "—"}</dd>
          </div>
        </dl>
      </section>

      <div className="quality-ncr-pair">
        <section
          className={`quality-section ${holdActive ? "quality-hold-panel" : ""}`}
          aria-label="Bağlantılar ve kalite blokesi"
        >
          <header className="quality-section__header">
            <h2 className={holdActive ? "quality-hold-panel__title" : undefined}>
              Bağlantılar ve kalite blokesi
            </h2>
          </header>
          <dl className="quality-ncr-dl" style={ncrCols(3, holdActive && ncr.activeHold ? 2 : 1)}>
            <div>
              <dt>Üretim Durumu</dt>
              <dd>
                {ncr.productionContext ? enumLabel(ncr.productionContext.status) : "Bağlı üretim emri yok"}
              </dd>
            </div>
            <div>
              <dt>Kalite Kontrolü</dt>
              <dd>
                {ncr.qualityInspectionNumber ? (
                  <Link href={`/kalite/kontroller/${ncr.qualityInspectionId}`}>
                    {ncr.qualityInspectionNumber}
                  </Link>
                ) : (
                  "Bağlı kalite kontrolü yok"
                )}
              </dd>
            </div>
            <div>
              <dt>Kalite Blokesi</dt>
              <dd>{holdActive && ncr.activeHold ? "Aktif" : "Aktif kalite blokesi bulunmuyor"}</dd>
            </div>
            {holdActive && ncr.activeHold ? (
              <>
                <div>
                  <dt>Neden</dt>
                  <dd>{ncr.activeHold.reason}</dd>
                </div>
                <div>
                  <dt>Koyan</dt>
                  <dd>{displayActor(ncr.activeHold.placedBySubjectId)}</dd>
                </div>
                <div>
                  <dt>Koyulma</dt>
                  <dd>{formatQualityDateTime(ncr.activeHold.placedAtUtc)}</dd>
                </div>
              </>
            ) : null}
          </dl>
        </section>

        <section className="quality-section" aria-label="Uygunsuzluk kararı">
          <header className="quality-section__header">
            <h2>Uygunsuzluk kararı</h2>
          </header>
          <dl className="quality-ncr-dl" style={ncrCols(2, 2)}>
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
          <p className="quality-detail-note">{ncr.dispositionNote ?? "Uygunsuzluk kararı notu yok."}</p>
        </section>
      </div>

      <section className="quality-section quality-section--wide" aria-label="Düzeltici aksiyonlar">
        <header className="quality-section__header">
          <h2>Düzeltici aksiyonlar</h2>
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
                    {action.ownerSubjectId ? <span>Sorumlu: {displayActor(action.ownerSubjectId)}</span> : null}
                    <span>Termin: {formatQualityDateTime(action.dueAtUtc)}</span>
                    {action.completionNote ? (
                      <span>Not: {action.completionNote}</span>
                    ) : null}
                  </div>
                </div>
                <div className="quality-ca-item__side">
                  <Badge tone={getCorrectiveActionStatusTone(action)}>
                    {getCorrectiveActionStatusLabel(action)}
                  </Badge>
                  <QualityCorrectiveActionControls
                    actionId={action.id}
                    actionTitle={action.title}
                    status={action.status}
                    version={action.version}
                    onStart={startQualityCorrectiveActionAction}
                    onComplete={completeQualityCorrectiveActionAction}
                  />
                </div>
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
        onResolve={resolveQualityNcrAction}
        onClose={closeQualityNcrAction}
      />
    </QualityPageShell>
  );
}
