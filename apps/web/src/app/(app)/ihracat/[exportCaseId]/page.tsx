import {
  ArrowLeft,
  CalendarClock,
  Globe2,
  Package,
  ShieldCheck,
  UserRound,
} from "lucide-react";
import Link from "next/link";
import { notFound } from "next/navigation";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { ExportActivityTimeline } from "@/features/export/components/export-activity-timeline";
import { ExportDocumentsPanel } from "@/features/export/components/export-documents-panel";
import { ExportReadinessPanel } from "@/features/export/components/export-readiness-panel";
import type {
  ExportCaseStatus,
  ExportReadinessStatus,
} from "@/features/export/domain/export-case";
import {
  getExportCaseStatusLabel,
  getExportReadinessStatusLabel,
} from "@/features/export/domain/export-case";
import {
  readExportActivity,
  readExportCaseById,
  readExportDocuments,
  readExportReadiness,
} from "@/features/export/server/export-case-reader";
import {
  registerExportDocumentAction,
  rejectExportDocumentAction,
  verifyExportDocumentAction,
} from "@/features/export/server/export-case-writer";

type BadgeTone =
  | "neutral"
  | "information"
  | "success"
  | "warning"
  | "danger"
  | "ai";

function getCaseStatusTone(status: ExportCaseStatus): BadgeTone {
  switch (status) {
    case "ready":
      return "success";
    case "blocked":
      return "danger";
    case "preparing":
      return "information";
    case "closed":
      return "neutral";
    case "open":
    default:
      return "warning";
  }
}

function getReadinessTone(status: ExportReadinessStatus): BadgeTone {
  switch (status) {
    case "ready":
      return "success";
    case "attention":
      return "warning";
    case "not-ready":
    default:
      return "danger";
  }
}

function formatUtcDate(iso: string | undefined): string {
  if (!iso) return "—";

  try {
    return new Date(iso).toLocaleDateString("tr-TR", {
      day: "2-digit",
      month: "long",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return iso;
  }
}

type ExportCaseDetailPageProps = Readonly<{
  params: Promise<{
    exportCaseId: string;
  }>;
}>;

export default async function ExportCaseDetailPage({
  params,
}: ExportCaseDetailPageProps) {
  const { exportCaseId } = await params;

  const [caseResult, readinessResult, documentsResult, activityResult] =
    await Promise.all([
      readExportCaseById(exportCaseId),
      readExportReadiness(exportCaseId),
      readExportDocuments(exportCaseId),
      readExportActivity(exportCaseId),
    ]);

  if (caseResult.status === "failure") {
    if (caseResult.code === "not-found") {
      notFound();
    }

    return (
      <main className="export-detail-page">
        <div className="export-detail-page__back">
          <Link href="/ihracat">
            <ArrowLeft aria-hidden="true" />
            İhracat dosyalarına dön
          </Link>
        </div>

        <div role="alert" className="state-panel error-state">
          <div className="state-icon">
            <ShieldCheck aria-hidden="true" />
          </div>
          <div>
            <h3>İhracat dosyası yüklenemedi</h3>
            <p>Veriler alınırken bir hata oluştu. Lütfen sayfayı yenileyin.</p>
          </div>
        </div>
      </main>
    );
  }

  const { exportCase } = caseResult;
  const readiness =
    readinessResult.status === "success" ? readinessResult.readiness : null;
  const documents =
    documentsResult.status === "success" ? documentsResult.documents : [];
  const activities =
    activityResult.status === "success" ? activityResult.activities : [];

  return (
    <main className="export-detail-page">
      <div className="export-detail-page__back">
        <Link href="/ihracat">
          <ArrowLeft aria-hidden="true" />
          İhracat dosyalarına dön
        </Link>
      </div>

      <header className="export-detail-page__header">
        <p className="export-detail-page__header-eyebrow">İHRACAT DOSYASI</p>
        <h1>{exportCase.caseNumber}</h1>
        <p className="export-detail-page__header-ref">
          {exportCase.customerName} · {exportCase.destinationCountryCode}
        </p>

        <div className="export-detail-page__badges">
          <Badge tone={getCaseStatusTone(exportCase.status)}>
            {getExportCaseStatusLabel(exportCase.status)}
          </Badge>
          <Badge tone={getReadinessTone(exportCase.readinessStatus)}>
            {getExportReadinessStatusLabel(exportCase.readinessStatus)} · %
            {exportCase.readinessPercent}
          </Badge>
        </div>
      </header>

      <section
        className="export-detail-page__summary"
        aria-label="Dosya özeti"
      >
        <Card>
          <UserRound aria-hidden="true" />
          <div>
            <span>Müşteri</span>
            <strong>{exportCase.customerName}</strong>
          </div>
        </Card>

        <Card>
          <Globe2 aria-hidden="true" />
          <div>
            <span>Hedef / Menşe</span>
            <strong>{exportCase.destinationCountryCode}</strong>
            <small>
              {exportCase.originCountryCode
                ? `Menşe: ${exportCase.originCountryCode}`
                : "Menşe belirtilmedi"}
              {exportCase.incotermCode
                ? ` · ${exportCase.incotermCode}`
                : ""}
            </small>
          </div>
        </Card>

        <Card>
          <Package aria-hidden="true" />
          <div>
            <span>Kaynak sipariş</span>
            <strong>{exportCase.sourceOrderReference}</strong>
            <small>
              <Link href={`/siparisler/${exportCase.sourceOrderId}`}>
                Siparişe git
              </Link>
            </small>
          </div>
        </Card>

        <Card>
          <CalendarClock aria-hidden="true" />
          <div>
            <span>Güncellendi</span>
            <strong>{formatUtcDate(exportCase.updatedAtUtc)}</strong>
            <small>Oluşturulma: {formatUtcDate(exportCase.createdAtUtc)}</small>
          </div>
        </Card>
      </section>

      <section className="export-detail-page__content">
        {readiness ? (
          <ExportReadinessPanel readiness={readiness} />
        ) : (
          <div className="export-detail-page__context-card" role="status">
            <p>HAZIRLIK</p>
            <h2>Hazırlık verisi alınamadı</h2>
            <span>
              Dosya özeti yüklendi ancak hazırlık paneli şu an
              görüntülenemiyor.
            </span>
          </div>
        )}

        <ExportDocumentsPanel
          exportCaseId={exportCase.id}
          documents={documents}
          onRegister={registerExportDocumentAction}
          onVerify={verifyExportDocumentAction}
          onReject={rejectExportDocumentAction}
        />
      </section>

      <ExportActivityTimeline activities={activities} />
    </main>
  );
}
