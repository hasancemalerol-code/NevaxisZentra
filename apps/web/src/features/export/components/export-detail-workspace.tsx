"use client";

import { useMemo, useState } from "react";

import type {
  ExportBrokerWorkspace,
  ExportPackingReconciliation,
  ExportPackingWorkspace,
  ExportShipment,
  ExportShipmentReadiness,
} from "../domain/export-2l";
import type {
  ExportComplianceSummary,
  ExportTraceability,
} from "../domain/export-3l";
import type { ExportAiRunDetail, AiUsageSummary, ExportAiRecommendation } from "../domain/export-4l";
import type {
  ExportActivity,
  ExportCaseDetail,
  ExportDocument,
  ExportReadiness,
} from "../domain/export-case";
import type {
  acknowledgeBrokerAction,
  addPackingItemAction,
  completePackingAction,
  createBrokerCorrectionAction,
  createBrokerWorkspaceAction,
  createPackingUnitAction,
  createShipmentAction,
  prepareBrokerPackageAction,
  registerExportDocumentAction,
  rejectExportDocumentAction,
  resolveBrokerCorrectionAction,
  sendBrokerPackageAction,
  transitionShipmentAction,
  updateShipmentAction,
  verifyExportDocumentAction,
  verifyPackingAction,
  evaluateComplianceAction,
  revalidateComplianceAction,
  satisfyComplianceRequirementAction,
} from "../server/export-case-writer";
import type {
  acceptExportAiRecommendationAction,
  analyzeExportCaseAction,
  dismissExportAiRecommendationAction,
  explainExportReadinessAction,
  rejectExportAiRecommendationAction,
  suggestExportNextActionsAction,
} from "../server/export-4l-writer";
import { ExportActivityTimeline } from "./export-activity-timeline";
import { ExportAiCopilotPanel } from "./export-ai-copilot-panel";
import { ExportBrokerPanel } from "./export-broker-panel";
import { ExportCompliancePanel } from "./export-compliance-panel";
import { ExportDocumentsPanel } from "./export-documents-panel";
import { ExportPackingPanel } from "./export-packing-panel";
import { ExportReadinessPanel } from "./export-readiness-panel";
import { ExportShipmentPanel } from "./export-shipment-panel";
import { ExportTraceabilityPanel } from "./export-traceability-panel";

export type ExportDetailTab =
  | "overview"
  | "ai-copilot"
  | "documents"
  | "packing"
  | "shipment"
  | "broker"
  | "compliance"
  | "traceability"
  | "activity";

const TABS: readonly { id: ExportDetailTab; label: string }[] = [
  { id: "overview", label: "Genel Bakış" },
  { id: "ai-copilot", label: "AI Copilot" },
  { id: "documents", label: "Belgeler" },
  { id: "packing", label: "Paketleme" },
  { id: "shipment", label: "Sevkiyat" },
  { id: "broker", label: "Gümrük Müşaviri" },
  { id: "compliance", label: "Uygunluk" },
  { id: "traceability", label: "İzlenebilirlik" },
  { id: "activity", label: "Aktivite" },
] as const;

type ExportDetailWorkspaceProps = Readonly<{
  exportCase: ExportCaseDetail;
  readiness: ExportReadiness | null;
  documents: readonly ExportDocument[];
  activities: readonly ExportActivity[];
  packing: ExportPackingWorkspace | null;
  reconciliation: ExportPackingReconciliation | null;
  shipments: readonly ExportShipment[];
  shipmentReadinessById: Readonly<Record<string, ExportShipmentReadiness>>;
  brokerWorkspace: ExportBrokerWorkspace | null;
  compliance: ExportComplianceSummary | null;
  traceability: ExportTraceability | null;
  aiCopilot: Readonly<{
    latestRunDetail: ExportAiRunDetail | null;
    recommendations: readonly ExportAiRecommendation[];
    usage: AiUsageSummary | null;
  }>;
  initialTab?: ExportDetailTab;
  actions: Readonly<{
    registerDocument: typeof registerExportDocumentAction;
    verifyDocument: typeof verifyExportDocumentAction;
    rejectDocument: typeof rejectExportDocumentAction;
    createPackingUnit: typeof createPackingUnitAction;
    addPackingItem: typeof addPackingItemAction;
    completePacking: typeof completePackingAction;
    verifyPacking: typeof verifyPackingAction;
    createShipment: typeof createShipmentAction;
    updateShipment: typeof updateShipmentAction;
    transitionShipment: typeof transitionShipmentAction;
    createBrokerWorkspace: typeof createBrokerWorkspaceAction;
    prepareBrokerPackage: typeof prepareBrokerPackageAction;
    sendBrokerPackage: typeof sendBrokerPackageAction;
    acknowledgeBroker: typeof acknowledgeBrokerAction;
    createBrokerCorrection: typeof createBrokerCorrectionAction;
    resolveBrokerCorrection: typeof resolveBrokerCorrectionAction;
    evaluateCompliance: typeof evaluateComplianceAction;
    revalidateCompliance: typeof revalidateComplianceAction;
    satisfyComplianceRequirement: typeof satisfyComplianceRequirementAction;
    analyzeExportCase: typeof analyzeExportCaseAction;
    explainExportReadiness: typeof explainExportReadinessAction;
    suggestExportNextActions: typeof suggestExportNextActionsAction;
    acceptAiRecommendation: typeof acceptExportAiRecommendationAction;
    rejectAiRecommendation: typeof rejectExportAiRecommendationAction;
    dismissAiRecommendation: typeof dismissExportAiRecommendationAction;
  }>;
}>;

function normalizeTab(value: string | undefined): ExportDetailTab {
  switch (value) {
    case "documents":
    case "packing":
    case "shipment":
    case "broker":
    case "compliance":
    case "traceability":
    case "activity":
    case "overview":
    case "ai-copilot":
      return value;
    default:
      return "overview";
  }
}

export function ExportDetailWorkspace({
  exportCase,
  readiness,
  documents,
  activities,
  packing,
  reconciliation,
  shipments,
  shipmentReadinessById,
  brokerWorkspace,
  compliance,
  traceability,
  aiCopilot,
  initialTab,
  actions,
}: ExportDetailWorkspaceProps) {
  const [tab, setTab] = useState<ExportDetailTab>(normalizeTab(initialTab));

  const shipmentIds = useMemo(
    () => shipments.map((shipment) => shipment.id),
    [shipments],
  );

  return (
    <div className="export-detail-workspace">
      <nav className="export-detail-workspace__tabs" aria-label="Dosya bölümleri">
        {TABS.map((item) => (
          <button
            key={item.id}
            type="button"
            className={
              tab === item.id
                ? "export-detail-workspace__tab export-detail-workspace__tab--active"
                : "export-detail-workspace__tab"
            }
            onClick={() => setTab(item.id)}
          >
            {item.label}
          </button>
        ))}
      </nav>

      <div className="export-detail-workspace__panel">
        {tab === "overview" ? (
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

            <div className="export-detail-page__context-card">
              <p>ÖZET</p>
              <h2>Operasyon durumu</h2>
              <span>
                Paketleme: {packing?.packingStatus ?? "—"} · Sevkiyat:{" "}
                {shipments.length} · Broker:{" "}
                {brokerWorkspace?.status ?? "yok"}
                {compliance
                  ? ` · Uygunluk: ${compliance.complianceStatus}`
                  : ""}
              </span>
            </div>
          </section>
        ) : null}

        {tab === "ai-copilot" ? (
          <ExportAiCopilotPanel
            exportCaseId={exportCase.id}
            latestRunDetail={aiCopilot.latestRunDetail}
            recommendations={aiCopilot.recommendations}
            usage={aiCopilot.usage}
            onAnalyze={actions.analyzeExportCase}
            onExplainReadiness={actions.explainExportReadiness}
            onSuggestNextActions={actions.suggestExportNextActions}
            onAcceptRecommendation={actions.acceptAiRecommendation}
            onRejectRecommendation={actions.rejectAiRecommendation}
            onDismissRecommendation={actions.dismissAiRecommendation}
          />
        ) : null}

        {tab === "documents" ? (
          <ExportDocumentsPanel
            exportCaseId={exportCase.id}
            documents={documents}
            onRegister={actions.registerDocument}
            onVerify={actions.verifyDocument}
            onReject={actions.rejectDocument}
          />
        ) : null}

        {tab === "packing" ? (
          <ExportPackingPanel
            exportCaseId={exportCase.id}
            caseVersion={exportCase.version}
            packing={packing}
            reconciliation={reconciliation}
            onCreateUnit={actions.createPackingUnit}
            onAddItem={actions.addPackingItem}
            onComplete={actions.completePacking}
            onVerify={actions.verifyPacking}
          />
        ) : null}

        {tab === "shipment" ? (
          <ExportShipmentPanel
            exportCaseId={exportCase.id}
            defaultDestinationCountryCode={exportCase.destinationCountryCode}
            defaultIncotermCode={exportCase.incotermCode}
            shipments={shipments}
            shipmentReadinessById={shipmentReadinessById}
            onCreate={actions.createShipment}
            onUpdate={actions.updateShipment}
            onTransition={actions.transitionShipment}
          />
        ) : null}

        {tab === "broker" ? (
          <ExportBrokerPanel
            exportCaseId={exportCase.id}
            workspace={brokerWorkspace}
            documents={documents}
            shipmentIds={shipmentIds}
            onCreateWorkspace={actions.createBrokerWorkspace}
            onPreparePackage={actions.prepareBrokerPackage}
            onSendPackage={actions.sendBrokerPackage}
            onAcknowledge={actions.acknowledgeBroker}
            onCreateCorrection={actions.createBrokerCorrection}
            onResolveCorrection={actions.resolveBrokerCorrection}
          />
        ) : null}

        {tab === "compliance" ? (
          <ExportCompliancePanel
            exportCaseId={exportCase.id}
            caseVersion={exportCase.version}
            compliance={compliance}
            onEvaluate={actions.evaluateCompliance}
            onRevalidate={actions.revalidateCompliance}
            onSatisfyRequirement={actions.satisfyComplianceRequirement}
          />
        ) : null}

        {tab === "traceability" ? (
          <ExportTraceabilityPanel traceability={traceability} />
        ) : null}

        {tab === "activity" ? (
          <ExportActivityTimeline activities={activities} />
        ) : null}
      </div>
    </div>
  );
}
