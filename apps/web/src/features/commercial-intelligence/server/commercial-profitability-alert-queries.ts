import "./commercial-intelligence-server-boundary";

import type { CommercialProfitabilityAlertApiFilters } from "../application/commercial-profitability-alert-filter";
import { formatCount, formatDateTime } from "../domain/commercial-intelligence-formatters";
import { getCommercialIntelligenceEvidenceStatusLabel } from "../domain/commercial-intelligence-labels";
import {
  getCommercialProfitabilityAlertAuditEventLabel,
  getCommercialProfitabilityAlertCategoryLabel,
  getCommercialProfitabilityAlertLifecycleLabel,
  getCommercialProfitabilityAlertSeverityLabel,
} from "../domain/commercial-profitability-alert-labels";
import {
  getCommercialProfitabilityComparisonTypeLabel,
  getCommercialProfitabilityDriverReasonLabel,
  getCommercialProfitabilityOutcomeLabel,
  localizeCommercialProfitabilityProvenance,
} from "../domain/commercial-profitability-labels";
import type {
  CommercialProfitabilityAlertAuditEvent,
  CommercialProfitabilityAlertDetail,
  CommercialProfitabilityAlertSummary,
  CommercialProfitabilityAlertTableRow,
  CommercialProfitabilityAlertWorkspacePayload,
} from "../types/commercial-profitability-alert-types";
import type { CommercialProfitabilityKpiCard } from "../types/commercial-profitability-types";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import {
  getCommercialProfitabilityAlert,
  getCommercialProfitabilityAlertWorkspace,
  listCommercialProfitabilityAlerts,
  type RawCommercialProfitabilityAlertAuditEvent,
  type RawCommercialProfitabilityAlertDetail,
  type RawCommercialProfitabilityAlertSummary,
} from "./commercial-profitability-alert-api-client";
import { mapComparisonDetail } from "./commercial-profitability-queries";
import { resolveCommercialIntelligenceRequestContext } from "./commercial-intelligence-request-context";

export type { CommercialProfitabilityAlertApiFilters };

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type CommercialProfitabilityAlertWorkspaceResult =
  | Readonly<{ status: "success"; workspace: CommercialProfitabilityAlertWorkspacePayload }>
  | ReadFailure;

export type CommercialProfitabilityAlertDetailResult =
  | Readonly<{ status: "success"; detail: CommercialProfitabilityAlertDetail }>
  | ReadFailure;

function buildAlertKpiCards(kpis: Readonly<{
  activeCount: number;
  highOrCriticalCount: number;
  acknowledgedCount: number;
  taskCreatedCount: number;
  evidenceReviewCount: number;
}>): readonly CommercialProfitabilityKpiCard[] {
  return [
    {
      id: "alert-active",
      label: "Aktif uyarı",
      value: formatCount(kpis.activeCount),
      hint: "Açık yaşam döngüsü",
      tone: kpis.activeCount > 0 ? "warning" : "neutral",
    },
    {
      id: "alert-high-critical",
      label: "Yüksek / kritik",
      value: formatCount(kpis.highOrCriticalCount),
      hint: "Öncelikli inceleme",
      tone: kpis.highOrCriticalCount > 0 ? "negative" : "neutral",
    },
    {
      id: "alert-acknowledged",
      label: "Onaylanmış",
      value: formatCount(kpis.acknowledgedCount),
      hint: "İnsan onayı alındı",
      tone: "neutral",
    },
    {
      id: "alert-task-created",
      label: "Görev bağlı",
      value: formatCount(kpis.taskCreatedCount),
      hint: "AI görev taslağı",
      tone: kpis.taskCreatedCount > 0 ? "positive" : "neutral",
    },
    {
      id: "alert-evidence-review",
      label: "Kanıt incelemesi",
      value: formatCount(kpis.evidenceReviewCount),
      hint: "Veri inceleme kategorisi",
      tone: kpis.evidenceReviewCount > 0 ? "warning" : "neutral",
    },
  ];
}

function mapAuditEvent(event: RawCommercialProfitabilityAlertAuditEvent): CommercialProfitabilityAlertAuditEvent {
  return {
    id: event.id,
    eventType: event.eventType,
    eventTypeLabel: getCommercialProfitabilityAlertAuditEventLabel(event.eventType),
    payloadJson: event.payloadJson,
    actorSubjectId: event.actorSubjectId,
    occurredAtUtc: event.occurredAtUtc,
    occurredLabel: formatDateTime(event.occurredAtUtc),
  };
}

function mapAlertSummary(item: RawCommercialProfitabilityAlertSummary): CommercialProfitabilityAlertSummary {
  return {
    id: item.id,
    alertReference: item.alertReference,
    alertCategory: item.alertCategory,
    alertCategoryLabel: getCommercialProfitabilityAlertCategoryLabel(item.alertCategory),
    severity: item.severity,
    severityLabel: getCommercialProfitabilityAlertSeverityLabel(item.severity),
    lifecycleStatus: item.lifecycleStatus,
    lifecycleStatusLabel: getCommercialProfitabilityAlertLifecycleLabel(item.lifecycleStatus),
    sourceComparisonId: item.sourceComparisonId,
    sourceComparisonType: item.sourceComparisonType,
    sourceComparisonTypeLabel: getCommercialProfitabilityComparisonTypeLabel(item.sourceComparisonType),
    title: item.title,
    evidenceSummary: item.evidenceSummary,
    evidenceStatus: item.evidenceStatus,
    evidenceStatusLabel: getCommercialIntelligenceEvidenceStatusLabel(item.evidenceStatus),
    comparisonOutcome: item.comparisonOutcome,
    comparisonOutcomeLabel: getCommercialProfitabilityOutcomeLabel(item.comparisonOutcome),
    leadingDriverCode: item.leadingDriverCode,
    leadingDriverLabel: getCommercialProfitabilityDriverReasonLabel(item.leadingDriverCode),
    linkedAiTaskId: item.linkedAiTaskId,
    materializedAtUtc: item.materializedAtUtc,
    materializedLabel: formatDateTime(item.materializedAtUtc),
    updatedAtUtc: item.updatedAtUtc,
    updatedLabel: formatDateTime(item.updatedAtUtc),
    version: item.version,
  };
}

function mapAlertTableRow(item: CommercialProfitabilityAlertSummary): CommercialProfitabilityAlertTableRow {
  return {
    id: item.id,
    alertReference: item.alertReference,
    categoryLabel: item.alertCategoryLabel,
    severityLabel: item.severityLabel,
    lifecycleLabel: item.lifecycleStatusLabel,
    title: item.title,
    comparisonTypeLabel: item.sourceComparisonTypeLabel,
    outcomeLabel: item.comparisonOutcomeLabel,
    evidenceLabel: item.evidenceStatusLabel,
    updatedLabel: item.updatedLabel,
    severity: item.severity,
    lifecycleStatus: item.lifecycleStatus,
    alertCategory: item.alertCategory,
    comparisonOutcome: item.comparisonOutcome,
    evidenceStatus: item.evidenceStatus,
  };
}

function mapAlertDetail(item: RawCommercialProfitabilityAlertDetail): CommercialProfitabilityAlertDetail {
  const summary = mapAlertSummary(item);
  const currencyCode = item.sourceComparison?.profileCode?.includes("USD") ? "USD" : "TRY";

  return {
    ...summary,
    sourceSnapshotJson: item.sourceSnapshotJson,
    ruleSetVersion: item.ruleSetVersion,
    sourceProvenance: localizeCommercialProfitabilityProvenance(item.sourceProvenance),
    automationDisclaimer: item.automationDisclaimer ?? "Otomatik ticari işlem yapılmaz.",
    sourceComparison: mapComparisonDetail(item.sourceComparison, currencyCode),
    auditEvents: (item.auditEvents ?? [])
      .slice()
      .sort((left, right) => Date.parse(right.occurredAtUtc) - Date.parse(left.occurredAtUtc))
      .map(mapAuditEvent),
  };
}

function freshnessLabel(alerts: readonly CommercialProfitabilityAlertSummary[]): string {
  const timestamps = alerts
    .map((item) => Date.parse(item.updatedAtUtc))
    .filter((value) => Number.isFinite(value));
  if (timestamps.length === 0) return "—";
  return formatDateTime(new Date(Math.max(...timestamps)).toISOString());
}

export async function readCommercialProfitabilityAlertWorkspace(
  requestedAlertId?: string | null,
  filters: CommercialProfitabilityAlertApiFilters = { page: 1, pageSize: 25 },
): Promise<CommercialProfitabilityAlertWorkspaceResult> {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const [workspaceResult, alertsResult] = await Promise.all([
    getCommercialProfitabilityAlertWorkspace(contextResult.context),
    listCommercialProfitabilityAlerts(contextResult.context, filters),
  ]);

  if (workspaceResult.status === "failure") {
    return { status: "failure", message: workspaceResult.message, code: workspaceResult.code };
  }
  if (alertsResult.status === "failure") {
    return { status: "failure", message: alertsResult.message, code: alertsResult.code };
  }

  const alerts = alertsResult.value.items.map(mapAlertSummary);
  const selected = requestedAlertId
    ? alerts.find((item) => item.id === requestedAlertId) ?? null
    : null;

  return {
    status: "success",
    workspace: {
      facilityLabel: "Yetkili tesis",
      ruleSetVersion: workspaceResult.value.ruleSetVersion,
      provenanceDisclaimer: localizeCommercialProfitabilityProvenance(workspaceResult.value.provenanceDisclaimer),
      automationDisclaimer: workspaceResult.value.automationDisclaimer,
      reportingPeriodLabel: freshnessLabel(alerts),
      freshnessLabel: freshnessLabel(alerts),
      alerts,
      selectedAlertId: selected?.id ?? requestedAlertId ?? null,
      kpiCards: buildAlertKpiCards(workspaceResult.value.kpis),
      tableRows: alerts.map(mapAlertTableRow),
      page: filters.page,
      pageSize: filters.pageSize,
      returnedCount: alerts.length,
      canRead: workspaceResult.value.capabilities.canRead,
      canManage: workspaceResult.value.capabilities.canManage,
      canCreateTask: workspaceResult.value.capabilities.canCreateTask,
    },
  };
}

export async function readCommercialProfitabilityAlertDetail(
  alertId: string,
): Promise<CommercialProfitabilityAlertDetailResult> {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const detailResult = await getCommercialProfitabilityAlert(contextResult.context, alertId);
  if (detailResult.status === "failure") {
    return { status: "failure", message: detailResult.message, code: detailResult.code };
  }

  return {
    status: "success",
    detail: mapAlertDetail(detailResult.value),
  };
}

export { mapAlertDetail };
