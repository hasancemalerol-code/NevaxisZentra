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
import {
  ExportDetailWorkspace,
  type ExportDetailTab,
} from "@/features/export/components/export-detail-workspace";
import type {
  ExportCaseStatus,
  ExportReadinessStatus,
} from "@/features/export/domain/export-case";
import {
  getExportCaseStatusLabel,
  getExportReadinessStatusLabel,
} from "@/features/export/domain/export-case";
import type { ExportShipmentReadiness } from "@/features/export/domain/export-2l";
import {
  readBrokerWorkspace,
  readComplianceSummary,
  readExportActivity,
  readExportCaseById,
  readExportDocuments,
  readExportReadiness,
  readPacking,
  readPackingReconciliation,
  readShipmentReadiness,
  readShipments,
  readTraceability,
} from "@/features/export/server/export-case-reader";
import {
  acknowledgeBrokerAction,
  addPackingItemAction,
  completePackingAction,
  createBrokerCorrectionAction,
  createBrokerWorkspaceAction,
  createPackingUnitAction,
  createShipmentAction,
  evaluateComplianceAction,
  prepareBrokerPackageAction,
  registerExportDocumentAction,
  rejectExportDocumentAction,
  revalidateComplianceAction,
  resolveBrokerCorrectionAction,
  satisfyComplianceRequirementAction,
  sendBrokerPackageAction,
  transitionShipmentAction,
  updateShipmentAction,
  verifyExportDocumentAction,
  verifyPackingAction,
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

function normalizeTab(value: string | string[] | undefined): ExportDetailTab {
  const raw = Array.isArray(value) ? value[0] : value;
  switch (raw) {
    case "documents":
    case "packing":
    case "shipment":
    case "broker":
    case "compliance":
    case "traceability":
    case "activity":
    case "overview":
      return raw;
    default:
      return "overview";
  }
}

type ExportCaseDetailPageProps = Readonly<{
  params: Promise<{
    exportCaseId: string;
  }>;
  searchParams: Promise<{
    tab?: string | string[];
  }>;
}>;

export default async function ExportCaseDetailPage({
  params,
  searchParams,
}: ExportCaseDetailPageProps) {
  const { exportCaseId } = await params;
  const resolvedSearchParams = await searchParams;
  const initialTab = normalizeTab(resolvedSearchParams.tab);

  const [
    caseResult,
    readinessResult,
    documentsResult,
    activityResult,
    packingResult,
    reconciliationResult,
    shipmentsResult,
    brokerResult,
    complianceResult,
    traceabilityResult,
  ] = await Promise.all([
    readExportCaseById(exportCaseId),
    readExportReadiness(exportCaseId),
    readExportDocuments(exportCaseId),
    readExportActivity(exportCaseId),
    readPacking(exportCaseId),
    readPackingReconciliation(exportCaseId),
    readShipments(exportCaseId),
    readBrokerWorkspace(exportCaseId),
    readComplianceSummary(exportCaseId),
    readTraceability(exportCaseId),
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
  const packing =
    packingResult.status === "success" ? packingResult.packing : null;
  const reconciliation =
    reconciliationResult.status === "success"
      ? reconciliationResult.reconciliation
      : null;
  const shipments =
    shipmentsResult.status === "success" ? shipmentsResult.shipments : [];
  const brokerWorkspace =
    brokerResult.status === "success" ? brokerResult.workspace : null;
  const compliance =
    complianceResult.status === "success" ? complianceResult.compliance : null;
  const traceability =
    traceabilityResult.status === "success"
      ? traceabilityResult.traceability
      : null;

  const readinessEntries = await Promise.all(
    shipments.map(async (shipment) => {
      const result = await readShipmentReadiness(exportCaseId, shipment.id);
      return result.status === "success"
        ? ([shipment.id, result.readiness] as const)
        : null;
    }),
  );

  const shipmentReadinessById: Record<string, ExportShipmentReadiness> = {};
  for (const entry of readinessEntries) {
    if (entry) {
      shipmentReadinessById[entry[0]] = entry[1];
    }
  }

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

      <ExportDetailWorkspace
        exportCase={exportCase}
        readiness={readiness}
        documents={documents}
        activities={activities}
        packing={packing}
        reconciliation={reconciliation}
        shipments={shipments}
        shipmentReadinessById={shipmentReadinessById}
        brokerWorkspace={brokerWorkspace}
        compliance={compliance}
        traceability={traceability}
        initialTab={initialTab}
        actions={{
          registerDocument: registerExportDocumentAction,
          verifyDocument: verifyExportDocumentAction,
          rejectDocument: rejectExportDocumentAction,
          createPackingUnit: createPackingUnitAction,
          addPackingItem: addPackingItemAction,
          completePacking: completePackingAction,
          verifyPacking: verifyPackingAction,
          createShipment: createShipmentAction,
          updateShipment: updateShipmentAction,
          transitionShipment: transitionShipmentAction,
          createBrokerWorkspace: createBrokerWorkspaceAction,
          prepareBrokerPackage: prepareBrokerPackageAction,
          sendBrokerPackage: sendBrokerPackageAction,
          acknowledgeBroker: acknowledgeBrokerAction,
          createBrokerCorrection: createBrokerCorrectionAction,
          resolveBrokerCorrection: resolveBrokerCorrectionAction,
          evaluateCompliance: evaluateComplianceAction,
          revalidateCompliance: revalidateComplianceAction,
          satisfyComplianceRequirement: satisfyComplianceRequirementAction,
        }}
      />
    </main>
  );
}
