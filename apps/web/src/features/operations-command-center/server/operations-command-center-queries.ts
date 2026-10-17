import { enumLabel } from "@/lib/presentation/enum-labels";
import "./operations-command-center-server-boundary";

import {
  buildOperationsCommandCenterTimeWindowLabel,
  filterOperationsCommandCenterQueueItems,
} from "../application/operations-command-center-filter";
import {
  COMMERCIAL_PROFITABILITY_COMMAND_CENTER_ROUTE_PATH,
  getOperationsCommandCenterModuleLabel,
  OPERATIONS_COMMAND_CENTER_AUTOMATION_DISCLAIMER,
  OPERATIONS_COMMAND_CENTER_PARTIAL_NOTE,
} from "../domain/operations-command-center-labels";
import type {
  OperationsCommandCenterFilters,
  OperationsCommandCenterKpiCard,
  OperationsCommandCenterModuleCode,
  OperationsCommandCenterModuleHealth,
  OperationsCommandCenterQueueItem,
  OperationsCommandCenterScopeState,
  OperationsCommandCenterSourceAvailability,
  OperationsCommandCenterWorkspacePayload,
} from "../types/operations-command-center-types";
import {
  fetchEffectiveAccessCapabilities,
  hasGrantedPermission,
  type AdminAccessEffectiveAccess,
} from "../../admin-access/server/admin-access-effective-access";
import { isAiTaskFinalStatus } from "../../ai-task-center/domain/ai-task";
import { mapRawAiTaskSummary } from "../../ai-task-center/server/ai-task-mapper";
import { listAiTasks } from "../../ai-task-center/server/ai-task-api-client";
import {
  listCommercialProfitabilityAlerts,
  type RawCommercialProfitabilityAlertSummary,
} from "../../commercial-intelligence/server/commercial-profitability-alert-api-client";
import { resolveCommercialIntelligenceRequestContext } from "../../commercial-intelligence/server/commercial-intelligence-request-context";
import { formatCount } from "../../commercial-intelligence/domain/commercial-intelligence-formatters";
import { formatDateTime } from "@/lib/presentation/format";
import {
  getCommercialProfitabilityAlertSeverityLabel,
  getCommercialProfitabilityAlertLifecycleLabel,
} from "../../commercial-intelligence/domain/commercial-profitability-alert-labels";
import { getCommercialIntelligenceEvidenceStatusLabel } from "../../commercial-intelligence/domain/commercial-intelligence-labels";
import { getMaterialAvailabilityRiskStatusLabel } from "../../material-availability-risk/presentation/material-availability-risk-labels";
import { getMaterialAvailabilityRiskWorkspace } from "../../material-availability-risk/server/material-availability-risk-api-client";
import { resolveMaterialRequestContext } from "../../materials/server/material-request-context";
import { getMaintenancePredictiveRiskWorkspace } from "../../maintenance/server/maintenance-predictive-risk-api-client";
import { getMoldFollowUpWorkspace } from "../../molds/server/mold-follow-up-api-client";
import { resolveMoldRequestContext } from "../../molds/server/mold-request-context";
import { getProcurementDeliveryRiskWorkspace } from "../../procurement-delivery-risk/server/procurement-delivery-risk-api-client";
import { getProcurementDeliveryRiskResultLabel } from "../../procurement-delivery-risk/presentation/procurement-delivery-risk-labels";
import type { ProcurementDeliveryRiskResult } from "../../procurement-delivery-risk/domain/procurement-delivery-risk";
import { getProductionFollowUpWorkspace } from "../../production-execution/server/production-follow-up-api-client";
import {
  parseProductionExecutionRuntimeConfig,
} from "../../production-execution/server/production-order-runtime-config";
import {
  resolveProductionOrderRequestContextForIncomingRequest,
  type ProductionOrderRequestContext,
} from "../../production-execution/server/production-order-request-context";
import { getQualityNcrFollowUpWorkspace } from "../../quality-control/server/quality-ncr-follow-up-api-client";
import { getProductionManagerWorkspace } from "../../production-manager/server/production-manager-api-client";
import type { ProductionManagerDecisionApi } from "../../production-manager/server/production-manager-api-client";
import {
  getProductionManagerConfidenceLabel,
  getProductionManagerDecisionTypeLabel,
  getProductionManagerStatusLabel,
} from "../../production-manager/domain/production-manager-labels";
import { getFactoryBlockerLabel } from "../../factory-orchestration/domain/factory-orchestration-labels";
import { listFactoryCases } from "../../factory-orchestration/server/factory-orchestration-api-client";
import { fetchCurrentTenantSubscription } from "../../saas-subscription/server/saas-subscription-queries";
import { isModuleEntitled } from "../../saas-subscription/domain/saas-subscription-labels";
import type { TenantSubscriptionCurrent } from "../../saas-subscription/domain/saas-subscription-types";

const LIST_PAGE_SIZE = 100;

/**
 * Deterministic priority tiers — lower number = higher priority.
 * 1. critical/high commercial alerts
 * 2. production exceptions needing review
 * 3. quality NCRs needing review
 * 4. predictive-maintenance high/critical risks
 * 5. mold maintenance/review follow-ups
 * 6. material shortages
 * 7. overdue/delivery-risk procurement assessments
 * 8. linked open AI Tasks
 * 9. insufficient-data/data-review items
 */
const PRIORITY_TIER = {
  FACTORY_CRITICAL: 50,
  COMMERCIAL_CRITICAL: 100,
  PRODUCTION_MANAGER: 150,
  PRODUCTION_REVIEW: 200,
  QUALITY_REVIEW: 300,
  MAINTENANCE_HIGH: 400,
  MOLD_REVIEW: 500,
  MATERIAL_SHORTAGE: 600,
  PROCUREMENT_RISK: 700,
  AI_TASK_OPEN: 800,
  DATA_REVIEW: 900,
} as const;

const SEVERITY_RANK: Record<string, number> = {
  critical: 4,
  high: 3,
  medium: 2,
  low: 1,
};

const OPEN_ALERT_LIFECYCLES = new Set(["active", "acknowledged", "task-created"]);
const REVIEW_FOLLOW_UP = new Set(["requires-review", "acknowledged"]);

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type OperationsCommandCenterResult =
  | Readonly<{ status: "success"; workspace: OperationsCommandCenterWorkspacePayload }>
  | ReadFailure;

type ModuleAccess =
  | "granted"
  | "permission-denied"
  | "subscription-denied";

type SourceFetchResult<T> = Readonly<{
  access: ModuleAccess;
  availability: OperationsCommandCenterSourceAvailability;
  value?: T;
  message?: string;
  count?: number;
}>;

function scopeFromCount(count: number): OperationsCommandCenterScopeState {
  if (count === 0) return "complete";
  if (count >= LIST_PAGE_SIZE) return "partial";
  return "complete";
}

function evaluateModuleAccess(
  access: AdminAccessEffectiveAccess | null,
  subscription: TenantSubscriptionCurrent | null,
  permissionKey: string,
  entitlementCode: string,
): ModuleAccess {
  if (!hasGrantedPermission(access, permissionKey)) {
    return "permission-denied";
  }
  if (subscription && !isModuleEntitled(subscription, entitlementCode)) {
    return "subscription-denied";
  }
  return "granted";
}

function toSourceAvailability(
  access: ModuleAccess,
  fetchCode?: string,
): OperationsCommandCenterSourceAvailability {
  if (access === "permission-denied") return "unavailable-permission";
  if (access === "subscription-denied") return "unavailable-subscription";
  if (fetchCode === "permission-denied") return "unavailable-permission";
  if (fetchCode === "unauthenticated") return "unavailable-error";
  if (fetchCode) return "unavailable-error";
  return "available";
}

async function resolvePrimaryContext(): Promise<
  | Readonly<{ status: "success"; context: ProductionOrderRequestContext }>
  | Readonly<{ status: "failure"; message: string; code?: string }>> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status === "failure") {
    return { status: "failure", message: configResult.message, code: "configuration-error" };
  }
  if (configResult.config.mode === "development-preview") {
    return {
      status: "failure",
      message: "Operations Command Center requires API-backed runtime configuration.",
      code: "configuration-error",
    };
  }
  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }
  return { status: "success", context: contextResult.context };
}

function taskDeepLink(taskId: string): string {
  return `/ai-gorevleri/${taskId}`;
}

function sortQueueItems(
  items: readonly OperationsCommandCenterQueueItem[],
): readonly OperationsCommandCenterQueueItem[] {
  return [...items].sort((left, right) => {
    if (left.priorityTier !== right.priorityTier) {
      return left.priorityTier - right.priorityTier;
    }
    if (left.severityRank !== right.severityRank) {
      return right.severityRank - left.severityRank;
    }
    const timeDelta = Date.parse(right.sortTimestampUtc) - Date.parse(left.sortTimestampUtc);
    if (timeDelta !== 0) return timeDelta;
    return `${left.moduleCode}:${left.sourceReference}`.localeCompare(
      `${right.moduleCode}:${right.sourceReference}`,
      "tr",
    );
  });
}

function buildFactoryCaseItems(
  cases: readonly Readonly<{
    id: string;
    title: string;
    severity: string;
    status: string;
    primaryDomain: string;
    currentBlockerCode: string;
    updatedAtUtc: string;
    requiresApproval: boolean;
    verificationState: string;
  }>[],
): readonly OperationsCommandCenterQueueItem[] {
  return cases
    .filter((row) => row.status !== "closed")
    .map((row) => ({
      id: `factory:case:${row.id}`,
      moduleCode: "factory" as const,
      moduleLabel: getOperationsCommandCenterModuleLabel("factory"),
      sourceReference: row.id.slice(0, 8).toUpperCase(),
      title: row.title,
      statusLabel: `${enumLabel(row.severity)} · ${enumLabel(row.status)}`,
      evidenceLabel: getFactoryBlockerLabel(row.currentBlockerCode),
      freshnessLabel: formatDateTime(row.updatedAtUtc),
      deepLinkHref: `/operasyon/yonetim-merkezi/vakalar/${row.id}`,
      itemState: "available" as const,
      priorityTier:
        row.severity === "critical" || row.severity === "high"
          ? PRIORITY_TIER.FACTORY_CRITICAL
          : PRIORITY_TIER.PRODUCTION_REVIEW,
      severityRank: SEVERITY_RANK[row.severity] ?? 1,
      sortTimestampUtc: row.updatedAtUtc,
      sortTimestampLabel: formatDateTime(row.updatedAtUtc),
      humanReviewRequired:
        row.requiresApproval
        || row.currentBlockerCode !== "none"
        || row.verificationState === "pending",
    }));
}

function buildCommercialItems(
  alerts: readonly RawCommercialProfitabilityAlertSummary[],
): readonly OperationsCommandCenterQueueItem[] {
  return alerts
    .filter((alert) =>
      OPEN_ALERT_LIFECYCLES.has(alert.lifecycleStatus)
      && (
        alert.severity === "critical"
        || alert.severity === "high"
        || alert.alertCategory === "data-review"
        || alert.evidenceStatus === "insufficient-data"
        || alert.evidenceStatus === "source-conflict"
      ))
    .map((alert) => {
      const isDataReview =
        alert.alertCategory === "data-review"
        || alert.evidenceStatus === "insufficient-data"
        || alert.evidenceStatus === "source-conflict";
      const priorityTier = isDataReview
        ? PRIORITY_TIER.DATA_REVIEW
        : alert.severity === "critical" || alert.severity === "high"
          ? PRIORITY_TIER.COMMERCIAL_CRITICAL
          : PRIORITY_TIER.DATA_REVIEW;

      return {
        id: `commercial:alert:${alert.id}`,
        moduleCode: "commercial" as const,
        moduleLabel: getOperationsCommandCenterModuleLabel("commercial"),
        sourceReference: alert.alertReference,
        title: alert.title,
        statusLabel: `${getCommercialProfitabilityAlertSeverityLabel(alert.severity)} · ${getCommercialProfitabilityAlertLifecycleLabel(alert.lifecycleStatus)}`,
        evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel(alert.evidenceStatus),
        freshnessLabel: formatDateTime(alert.updatedAtUtc),
        linkedTaskId: alert.linkedAiTaskId ?? undefined,
        linkedTaskHref: alert.linkedAiTaskId ? taskDeepLink(alert.linkedAiTaskId) : undefined,
        deepLinkHref: `/ticari-zeka/karlilik-uyarilari?alert=${encodeURIComponent(alert.id)}`,
        itemState: isDataReview ? "insufficient-data" as const : "available" as const,
        priorityTier,
        severityRank: SEVERITY_RANK[alert.severity] ?? 0,
        sortTimestampUtc: alert.updatedAtUtc,
        sortTimestampLabel: formatDateTime(alert.updatedAtUtc),
        humanReviewRequired: !isDataReview,
      };
    });
}

function buildProductionItems(
  exceptions: readonly Readonly<{
    id: string;
    productionOrderReference: string;
    exceptionType: string;
    severity: string;
    exceptionStatus: string;
    message: string;
    raisedAtUtc: string;
    followUpStatus: string;
    linkedAiTaskId?: string;
    hasInsufficientEvidence: boolean;
  }>[],
): readonly OperationsCommandCenterQueueItem[] {
  return exceptions
    .filter((row) =>
      REVIEW_FOLLOW_UP.has(row.followUpStatus)
      || row.hasInsufficientEvidence)
    .map((row) => {
      const isDataReview = row.hasInsufficientEvidence;
      return {
        id: `production:exception:${row.id}`,
        moduleCode: "production" as const,
        moduleLabel: getOperationsCommandCenterModuleLabel("production"),
        sourceReference: row.productionOrderReference,
        title: row.message || row.exceptionType,
        statusLabel: `${enumLabel(row.severity)} · ${enumLabel(row.exceptionStatus)}`,
        evidenceLabel: isDataReview ? "Yetersiz veri" : "Yetkili",
        freshnessLabel: formatDateTime(row.raisedAtUtc),
        linkedTaskId: row.linkedAiTaskId,
        linkedTaskHref: row.linkedAiTaskId ? taskDeepLink(row.linkedAiTaskId) : undefined,
        deepLinkHref: `/uretim/istisnalar-ve-gorevler?exception=${encodeURIComponent(row.id)}`,
        itemState: isDataReview ? "insufficient-data" as const : "available" as const,
        priorityTier: isDataReview ? PRIORITY_TIER.DATA_REVIEW : PRIORITY_TIER.PRODUCTION_REVIEW,
        severityRank: SEVERITY_RANK[row.severity] ?? 1,
        sortTimestampUtc: row.raisedAtUtc,
        sortTimestampLabel: formatDateTime(row.raisedAtUtc),
        humanReviewRequired: !isDataReview && REVIEW_FOLLOW_UP.has(row.followUpStatus),
      };
    });
}

function buildQualityItems(
  findings: readonly Readonly<{
    id: string;
    ncrNumber: string;
    title: string;
    severity: string;
    status: string;
    followUpStatus: string;
    detectedAtUtc: string;
    linkedAiTaskId?: string;
    hasInsufficientEvidence: boolean;
  }>[],
): readonly OperationsCommandCenterQueueItem[] {
  return findings
    .filter((row) =>
      REVIEW_FOLLOW_UP.has(row.followUpStatus)
      || row.hasInsufficientEvidence)
    .map((row) => {
      const isDataReview = row.hasInsufficientEvidence;
      return {
        id: `quality:ncr:${row.id}`,
        moduleCode: "quality" as const,
        moduleLabel: getOperationsCommandCenterModuleLabel("quality"),
        sourceReference: row.ncrNumber,
        title: row.title,
        statusLabel: `${enumLabel(row.severity)} · ${enumLabel(row.status)}`,
        evidenceLabel: isDataReview ? "Yetersiz veri" : "Yetkili",
        freshnessLabel: formatDateTime(row.detectedAtUtc),
        linkedTaskId: row.linkedAiTaskId,
        linkedTaskHref: row.linkedAiTaskId ? taskDeepLink(row.linkedAiTaskId) : undefined,
        deepLinkHref: `/kalite/takip-ve-gorevler?finding=${encodeURIComponent(row.id)}`,
        itemState: isDataReview ? "insufficient-data" as const : "available" as const,
        priorityTier: isDataReview ? PRIORITY_TIER.DATA_REVIEW : PRIORITY_TIER.QUALITY_REVIEW,
        severityRank: SEVERITY_RANK[row.severity] ?? 1,
        sortTimestampUtc: row.detectedAtUtc,
        sortTimestampLabel: formatDateTime(row.detectedAtUtc),
        humanReviewRequired: !isDataReview && REVIEW_FOLLOW_UP.has(row.followUpStatus),
      };
    });
}

function buildMaintenanceItems(
  assets: readonly Readonly<{
    assessmentId?: string;
    assetReference: string;
    assetName?: string;
    riskBand: string;
    reviewPriority: string;
    dataFreshness: string;
    assessedAtUtc?: string;
    followUpStatus?: string;
    linkedAiTaskId?: string;
  }>[],
): readonly OperationsCommandCenterQueueItem[] {
  return assets
    .filter((row) =>
      row.riskBand === "high"
      || row.riskBand === "critical"
      || row.riskBand === "insufficient-data")
    .map((row) => {
      const isDataReview = row.riskBand === "insufficient-data";
      return {
        id: `maintenance:asset:${row.assetReference}`,
        moduleCode: "maintenance" as const,
        moduleLabel: getOperationsCommandCenterModuleLabel("maintenance"),
        sourceReference: row.assetReference,
        title: row.assetName ?? row.assetReference,
        statusLabel: `${enumLabel(row.riskBand)} · ${enumLabel(row.reviewPriority)}`,
        evidenceLabel: isDataReview ? "Yetersiz veri" : enumLabel(row.dataFreshness),
        freshnessLabel: formatDateTime(row.assessedAtUtc),
        linkedTaskId: row.linkedAiTaskId,
        linkedTaskHref: row.linkedAiTaskId ? taskDeepLink(row.linkedAiTaskId) : undefined,
        deepLinkHref: `/bakim/kestirimci-riskler?asset=${encodeURIComponent(row.assetReference)}`,
        itemState: isDataReview ? "insufficient-data" as const : "available" as const,
        priorityTier: isDataReview ? PRIORITY_TIER.DATA_REVIEW : PRIORITY_TIER.MAINTENANCE_HIGH,
        severityRank: row.riskBand === "critical" ? 4 : row.riskBand === "high" ? 3 : 0,
        sortTimestampUtc: row.assessedAtUtc ?? "",
        sortTimestampLabel: formatDateTime(row.assessedAtUtc),
        humanReviewRequired: !isDataReview,
      };
    });
}

function buildMoldItems(
  molds: readonly Readonly<{
    id: string;
    code: string;
    name: string;
    lifecycleState: string;
    followUpStatus: string;
    updatedAtUtc: string;
    linkedAiTaskId?: string;
    hasInsufficientEvidence: boolean;
  }>[],
): readonly OperationsCommandCenterQueueItem[] {
  return molds
    .filter((row) =>
      REVIEW_FOLLOW_UP.has(row.followUpStatus)
      || row.hasInsufficientEvidence)
    .map((row) => {
      const isDataReview = row.hasInsufficientEvidence;
      return {
        id: `mold:${row.id}`,
        moduleCode: "mold" as const,
        moduleLabel: getOperationsCommandCenterModuleLabel("mold"),
        sourceReference: row.code,
        title: row.name,
        statusLabel: `${enumLabel(row.lifecycleState)} · ${enumLabel(row.followUpStatus)}`,
        evidenceLabel: isDataReview ? "Yetersiz veri" : "Yetkili",
        freshnessLabel: formatDateTime(row.updatedAtUtc),
        linkedTaskId: row.linkedAiTaskId,
        linkedTaskHref: row.linkedAiTaskId ? taskDeepLink(row.linkedAiTaskId) : undefined,
        deepLinkHref: `/kaliplar/takip-ve-gorevler?mold=${encodeURIComponent(row.id)}`,
        itemState: isDataReview ? "insufficient-data" as const : "available" as const,
        priorityTier: isDataReview ? PRIORITY_TIER.DATA_REVIEW : PRIORITY_TIER.MOLD_REVIEW,
        severityRank: 2,
        sortTimestampUtc: row.updatedAtUtc,
        sortTimestampLabel: formatDateTime(row.updatedAtUtc),
        humanReviewRequired: !isDataReview && REVIEW_FOLLOW_UP.has(row.followUpStatus),
      };
    });
}

function buildMaterialItems(
  assessments: readonly Readonly<{
    id: string;
    materialCode: string;
    materialName: string;
    status: string;
    sourceFreshness?: string;
    evaluatedAtUtc: string;
    followUpStatus: string;
    linkedAiTaskId?: string;
  }>[],
): readonly OperationsCommandCenterQueueItem[] {
  return assessments
    .filter((row) =>
      row.status === "shortage"
      || row.status === "insufficient-data"
      || row.status === "source-conflict")
    .map((row) => {
      const isDataReview = row.status === "insufficient-data" || row.status === "source-conflict";
      return {
        id: `material:${row.id}`,
        moduleCode: "material" as const,
        moduleLabel: getOperationsCommandCenterModuleLabel("material"),
        sourceReference: row.materialCode,
        title: row.materialName,
        statusLabel: getMaterialAvailabilityRiskStatusLabel(row.status),
        evidenceLabel: isDataReview ? "Yetersiz veri" : row.sourceFreshness ? enumLabel(row.sourceFreshness) : "—",
        freshnessLabel: formatDateTime(row.evaluatedAtUtc),
        linkedTaskId: row.linkedAiTaskId,
        linkedTaskHref: row.linkedAiTaskId ? taskDeepLink(row.linkedAiTaskId) : undefined,
        deepLinkHref: `/stok-ve-depo/malzeme-riskleri?assessment=${encodeURIComponent(row.id)}`,
        itemState: isDataReview ? "insufficient-data" as const : "available" as const,
        priorityTier: isDataReview ? PRIORITY_TIER.DATA_REVIEW : PRIORITY_TIER.MATERIAL_SHORTAGE,
        severityRank: row.status === "shortage" ? 3 : 0,
        sortTimestampUtc: row.evaluatedAtUtc,
        sortTimestampLabel: formatDateTime(row.evaluatedAtUtc),
        humanReviewRequired: !isDataReview,
      };
    });
}

function buildProcurementItems(
  assessments: readonly Readonly<{
    id: string;
    purchaseOrderNumber: string;
    supplierName?: string;
    result: string;
    sourceFreshness?: string;
    evaluatedAtUtc: string;
    followUpStatus?: string;
    linkedAiTaskId?: string;
  }>[],
): readonly OperationsCommandCenterQueueItem[] {
  return assessments
    .filter((row) =>
      row.result === "overdue"
      || row.result === "delivery-risk"
      || row.result === "insufficient-data"
      || row.result === "source-conflict")
    .map((row) => {
      const isDataReview = row.result === "insufficient-data" || row.result === "source-conflict";
      return {
        id: `procurement:${row.id}`,
        moduleCode: "procurement" as const,
        moduleLabel: getOperationsCommandCenterModuleLabel("procurement"),
        sourceReference: row.purchaseOrderNumber,
        title: row.supplierName ?? row.purchaseOrderNumber,
        statusLabel: getProcurementDeliveryRiskResultLabel(row.result as ProcurementDeliveryRiskResult),
        evidenceLabel: isDataReview ? "Yetersiz veri" : row.sourceFreshness ? enumLabel(row.sourceFreshness) : "—",
        freshnessLabel: formatDateTime(row.evaluatedAtUtc),
        linkedTaskId: row.linkedAiTaskId,
        linkedTaskHref: row.linkedAiTaskId ? taskDeepLink(row.linkedAiTaskId) : undefined,
        deepLinkHref: `/satin-alma/tedarik-riskleri?assessment=${encodeURIComponent(row.id)}`,
        itemState: isDataReview ? "insufficient-data" as const : "available" as const,
        priorityTier: isDataReview ? PRIORITY_TIER.DATA_REVIEW : PRIORITY_TIER.PROCUREMENT_RISK,
        severityRank: row.result === "overdue" ? 4 : row.result === "delivery-risk" ? 3 : 0,
        sortTimestampUtc: row.evaluatedAtUtc,
        sortTimestampLabel: formatDateTime(row.evaluatedAtUtc),
        humanReviewRequired: !isDataReview,
      };
    });
}

function buildProductionManagerDecisionItems(
  decisions: readonly ProductionManagerDecisionApi[],
  stateCapturedAtUtc: string,
): readonly OperationsCommandCenterQueueItem[] {
  return decisions
    .filter((row) =>
      row.status === "pending-approval"
      || row.status === "proposed"
      || row.status === "approved")
    .map((row) => {
      const isDataReview = row.confidenceBand === "insufficient-data";
      const severityRank =
        row.confidenceBand === "low" ? 3
        : row.confidenceBand === "medium" ? 2
        : row.confidenceBand === "high" ? 1
        : 0;

      return {
        id: `production-manager:decision:${row.id}`,
        moduleCode: "production-manager" as const,
        moduleLabel: getOperationsCommandCenterModuleLabel("production-manager"),
        sourceReference: row.id.slice(0, 8).toUpperCase(),
        title: row.title,
        statusLabel: `${getProductionManagerDecisionTypeLabel(row.decisionType)} · ${getProductionManagerStatusLabel(row.status)}`,
        evidenceLabel: getProductionManagerConfidenceLabel(row.confidenceBand),
        freshnessLabel: formatDateTime(stateCapturedAtUtc),
        deepLinkHref: `/operasyon/yonetim-merkezi/kararlar/${row.id}`,
        itemState: isDataReview ? "insufficient-data" as const : "available" as const,
        priorityTier: isDataReview ? PRIORITY_TIER.DATA_REVIEW : PRIORITY_TIER.PRODUCTION_MANAGER,
        severityRank,
        sortTimestampUtc: stateCapturedAtUtc,
        sortTimestampLabel: formatDateTime(stateCapturedAtUtc),
        humanReviewRequired:
          row.status === "pending-approval"
          || row.status === "proposed"
          || (row.status === "approved" && row.approvalState === "approved"),
      };
    });
}

function buildAiTaskItems(
  tasks: readonly ReturnType<typeof mapRawAiTaskSummary>[],
): readonly OperationsCommandCenterQueueItem[] {
  return tasks
    .filter((task) => !isAiTaskFinalStatus(task.status))
    .map((task) => ({
      id: `ai-task:${task.id}`,
      moduleCode: "ai-task" as const,
      moduleLabel: getOperationsCommandCenterModuleLabel("ai-task"),
      sourceReference: task.id.slice(0, 8).toUpperCase(),
      title: task.title,
      statusLabel: `${enumLabel(task.priority)} · ${enumLabel(task.status)}`,
      evidenceLabel: `Risk: ${enumLabel(task.riskLevel)}`,
      freshnessLabel: formatDateTime(task.updatedAt),
      linkedTaskId: task.id,
      linkedTaskHref: taskDeepLink(task.id),
      deepLinkHref: taskDeepLink(task.id),
      itemState: "available" as const,
      priorityTier: PRIORITY_TIER.AI_TASK_OPEN,
      severityRank: SEVERITY_RANK[task.riskLevel] ?? SEVERITY_RANK[task.priority] ?? 1,
      sortTimestampUtc: task.updatedAt,
      sortTimestampLabel: formatDateTime(task.updatedAt),
      humanReviewRequired:
        task.status === "awaiting-review"
        || task.status === "awaiting-approval"
        || task.approval.state === "pending",
    }));
}

function buildModuleHealthEntry(
  moduleCode: OperationsCommandCenterModuleCode,
  source: SourceFetchResult<unknown>,
): OperationsCommandCenterModuleHealth {
  const availability = source.availability === "available" && source.count !== undefined && source.count >= LIST_PAGE_SIZE
    ? "partial"
    : source.availability;

  let hint = "Kaynak erişilebilir";
  if (availability === "unavailable-permission") hint = "Bu modül için okuma yetkisi yok";
  if (availability === "unavailable-subscription") hint = "Abonelik kapsamı dışında";
  if (availability === "unavailable-error") hint = source.message ?? "Kaynak okunamadı";
  if (availability === "partial") hint = "Liste üst sınırına ulaşıldı";
  if (availability === "insufficient-data") hint = "Kaynak yetersiz veri bildirdi";

  return {
    moduleCode,
    moduleLabel: getOperationsCommandCenterModuleLabel(moduleCode),
    availability,
    hint,
    countLabel:
      availability === "available" || availability === "partial"
        ? formatCount(source.count ?? 0)
        : null,
  };
}

function buildKpiCards(input: Readonly<{
  reviewCount: number;
  queueCount: number;
  openLinkedTaskCount: number | null;
  aiTaskScope: OperationsCommandCenterScopeState;
  productionCount: number | null;
  qualityCount: number | null;
  maintenanceCount: number | null;
  materialCount: number | null;
  procurementCount: number | null;
  facilityLabel: string;
  timeWindowLabel: string;
  partialModules: number;
}>): readonly OperationsCommandCenterKpiCard[] {
  const partialHint = input.partialModules > 0 ? `${input.partialModules} kaynak kısmi` : "Tam kapsam";

  return [
    {
      id: "occ-priority-review",
      label: "İnsan incelemesi gereken",
      value: formatCount(input.reviewCount),
      hint: `Kuyruktaki ${formatCount(input.queueCount)} kayıttan`,
      tone: input.reviewCount > 0 ? "warning" : "neutral",
      scopeState: "complete",
    },
    {
      id: "occ-open-linked-tasks",
      label: "Yapay zekâ görevleri",
      value: input.openLinkedTaskCount === null ? "—" : formatCount(input.openLinkedTaskCount),
      hint: input.aiTaskScope === "insufficient" ? "ai-task.read gerekli" : "Açık, bağlantılı görevler",
      tone: input.openLinkedTaskCount && input.openLinkedTaskCount > 0 ? "warning" : "neutral",
      scopeState: input.aiTaskScope,
    },
    {
      id: "occ-production",
      label: "Üretim istisnası",
      value: input.productionCount === null ? "—" : formatCount(input.productionCount),
      hint: input.productionCount === null ? "Erişim yok" : "İnceleme kuyruğu",
      tone: "neutral",
      scopeState: input.productionCount === null ? "insufficient" : "complete",
    },
    {
      id: "occ-quality",
      label: "Kalite NCR",
      value: input.qualityCount === null ? "—" : formatCount(input.qualityCount),
      hint: input.qualityCount === null ? "Erişim yok" : "Takip kuyruğu",
      tone: "neutral",
      scopeState: input.qualityCount === null ? "insufficient" : "complete",
    },
    {
      id: "occ-maintenance",
      label: "Bakım riski",
      value: input.maintenanceCount === null ? "—" : formatCount(input.maintenanceCount),
      hint: input.maintenanceCount === null ? "Erişim yok" : "Yüksek/kritik",
      tone: "neutral",
      scopeState: input.maintenanceCount === null ? "insufficient" : "complete",
    },
    {
      id: "occ-material",
      label: "Malzeme riski",
      value: input.materialCount === null ? "—" : formatCount(input.materialCount),
      hint: input.materialCount === null ? "Erişim yok" : "Eksiklik / veri",
      tone: "neutral",
      scopeState: input.materialCount === null ? "insufficient" : "complete",
    },
    {
      id: "occ-procurement",
      label: "Tedarik riski",
      value: input.procurementCount === null ? "—" : formatCount(input.procurementCount),
      hint: input.procurementCount === null ? "Erişim yok" : "Gecikme / risk",
      tone: "neutral",
      scopeState: input.procurementCount === null ? "insufficient" : "complete",
    },
    {
      id: "occ-scope",
      label: "Kapsam",
      value: input.facilityLabel,
      hint: `${input.timeWindowLabel} · ${partialHint}`,
      tone: "neutral",
      scopeState: input.partialModules > 0 ? "partial" : "complete",
    },
  ];
}

export async function readOperationsCommandCenterWorkspace(
  filters: OperationsCommandCenterFilters = {
    windowFrom: "",
    windowTo: "",
    module: "",
    state: "",
  },
): Promise<OperationsCommandCenterResult> {
  const access = await fetchEffectiveAccessCapabilities();
  const subscriptionResult = await fetchCurrentTenantSubscription();
  const subscription = subscriptionResult.status === "success" ? subscriptionResult.value : null;

  const primaryContextResult = await resolvePrimaryContext();
  if (primaryContextResult.status === "failure") {
    return {
      status: "failure",
      message: primaryContextResult.message,
      code: primaryContextResult.code,
    };
  }

  const productionContext = primaryContextResult.context;
  const timeWindowLabel = buildOperationsCommandCenterTimeWindowLabel(filters);

  const commercialAccess = evaluateModuleAccess(
    access,
    subscription,
    "commercial-intelligence.read",
    "module.commercial-intelligence",
  );
  const productionAccess = evaluateModuleAccess(
    access,
    subscription,
    "production.execution.read",
    "module.production",
  );
  const qualityAccess = evaluateModuleAccess(
    access,
    subscription,
    "quality.read",
    "module.quality",
  );
  const maintenanceAccess = evaluateModuleAccess(
    access,
    subscription,
    "maintenance.read",
    "module.maintenance",
  );
  const moldAccess = evaluateModuleAccess(
    access,
    subscription,
    "mold.read",
    "module.mold",
  );
  const materialAccess = evaluateModuleAccess(
    access,
    subscription,
    "material.read",
    "module.material",
  );
  const procurementAccess = evaluateModuleAccess(
    access,
    subscription,
    "erp.read",
    "module.erp-integration",
  );
  const aiTaskAccess = evaluateModuleAccess(
    access,
    subscription,
    "ai-task.read",
    "module.ai-task-center",
  );
  const factoryAccess = evaluateModuleAccess(
    access,
    subscription,
    "factory-case.read",
    "module.production",
  );
  const productionManagerAccess = evaluateModuleAccess(
    access,
    subscription,
    "production-manager.read",
    "module.production",
  );

  const [commercialContextResult, materialContextResult, moldContextResult] = await Promise.all([
    commercialAccess === "granted" ? resolveCommercialIntelligenceRequestContext() : Promise.resolve(null),
    materialAccess === "granted" ? resolveMaterialRequestContext() : Promise.resolve(null),
    moldAccess === "granted" ? resolveMoldRequestContext() : Promise.resolve(null),
  ]);

  const [
    commercialResult,
    productionResult,
    qualityResult,
    maintenanceResult,
    moldResult,
    materialResult,
    procurementResult,
    factoryResult,
    productionManagerResult,
    aiTaskResult,
  ] = await Promise.all([
    commercialAccess === "granted" && commercialContextResult?.status === "success"
      ? listCommercialProfitabilityAlerts(commercialContextResult.context, {
          page: 1,
          pageSize: LIST_PAGE_SIZE,
        }).then((result) => ({
          access: commercialAccess,
          availability: result.status === "success"
            ? scopeFromCount(result.value.items.length) === "partial" ? "partial" as const : "available" as const
            : toSourceAvailability(commercialAccess, result.code),
          value: result.status === "success" ? result.value.items : undefined,
          message: result.status === "failure" ? result.message : undefined,
          count: result.status === "success" ? result.value.items.length : undefined,
        }))
      : Promise.resolve({
          access: commercialAccess,
          availability: toSourceAvailability(commercialAccess),
          value: undefined,
        }),

    productionAccess === "granted"
      ? getProductionFollowUpWorkspace(productionContext).then((result) => ({
          access: productionAccess,
          availability: result.status === "success"
            ? scopeFromCount(result.value.exceptions.length) === "partial" ? "partial" as const : "available" as const
            : toSourceAvailability(productionAccess, result.code),
          value: result.status === "success" ? result.value.exceptions : undefined,
          message: result.status === "failure" ? result.message : undefined,
          count: result.status === "success" ? result.value.exceptions.length : undefined,
        }))
      : Promise.resolve({
          access: productionAccess,
          availability: toSourceAvailability(productionAccess),
          value: undefined,
        }),

    qualityAccess === "granted"
      ? getQualityNcrFollowUpWorkspace(productionContext).then((result) => ({
          access: qualityAccess,
          availability: result.status === "success"
            ? scopeFromCount(result.value.findings.length) === "partial" ? "partial" as const : "available" as const
            : toSourceAvailability(qualityAccess, result.code),
          value: result.status === "success" ? result.value.findings : undefined,
          message: result.status === "failure" ? result.message : undefined,
          count: result.status === "success" ? result.value.findings.length : undefined,
        }))
      : Promise.resolve({
          access: qualityAccess,
          availability: toSourceAvailability(qualityAccess),
          value: undefined,
        }),

    maintenanceAccess === "granted"
      ? getMaintenancePredictiveRiskWorkspace(productionContext).then((result) => ({
          access: maintenanceAccess,
          availability: result.status === "success"
            ? scopeFromCount(result.value.assets.length) === "partial" ? "partial" as const : "available" as const
            : toSourceAvailability(maintenanceAccess, result.code),
          value: result.status === "success" ? result.value.assets : undefined,
          message: result.status === "failure" ? result.message : undefined,
          count: result.status === "success" ? result.value.assets.length : undefined,
        }))
      : Promise.resolve({
          access: maintenanceAccess,
          availability: toSourceAvailability(maintenanceAccess),
          value: undefined,
        }),

    moldAccess === "granted" && moldContextResult?.status === "success"
      ? getMoldFollowUpWorkspace(moldContextResult.context).then((result) => ({
          access: moldAccess,
          availability: result.status === "success"
            ? scopeFromCount(result.value.molds.length) === "partial" ? "partial" as const : "available" as const
            : toSourceAvailability(moldAccess, result.code),
          value: result.status === "success" ? result.value.molds : undefined,
          message: result.status === "failure" ? result.message : undefined,
          count: result.status === "success" ? result.value.molds.length : undefined,
        }))
      : Promise.resolve({
          access: moldAccess,
          availability: toSourceAvailability(moldAccess),
          value: undefined,
        }),

    materialAccess === "granted" && materialContextResult?.status === "success"
      ? getMaterialAvailabilityRiskWorkspace(materialContextResult.context, {
          page: 1,
          pageSize: LIST_PAGE_SIZE,
        }).then((result) => ({
          access: materialAccess,
          availability: result.status === "success"
            ? scopeFromCount(result.value.assessments.length) === "partial" ? "partial" as const : "available" as const
            : toSourceAvailability(materialAccess, result.code),
          value: result.status === "success" ? result.value.assessments : undefined,
          message: result.status === "failure" ? result.message : undefined,
          count: result.status === "success" ? result.value.assessments.length : undefined,
        }))
      : Promise.resolve({
          access: materialAccess,
          availability: toSourceAvailability(materialAccess),
          value: undefined,
        }),

    procurementAccess === "granted"
      ? getProcurementDeliveryRiskWorkspace(productionContext).then((result) => ({
          access: procurementAccess,
          availability: result.status === "success"
            ? scopeFromCount(result.value.assessments.length) === "partial" ? "partial" as const : "available" as const
            : toSourceAvailability(procurementAccess, result.code),
          value: result.status === "success" ? result.value.assessments : undefined,
          message: result.status === "failure" ? result.message : undefined,
          count: result.status === "success" ? result.value.assessments.length : undefined,
        }))
      : Promise.resolve({
          access: procurementAccess,
          availability: toSourceAvailability(procurementAccess),
          value: undefined,
        }),

    factoryAccess === "granted"
      ? listFactoryCases(productionContext, LIST_PAGE_SIZE).then((result) => ({
          access: factoryAccess,
          availability: result.status === "success"
            ? scopeFromCount(result.value.items.length) === "partial" ? "partial" as const : "available" as const
            : toSourceAvailability(factoryAccess, result.code),
          value: result.status === "success" ? result.value.items : undefined,
          message: result.status === "failure" ? result.message : undefined,
          count: result.status === "success" ? result.value.items.length : undefined,
        }))
      : Promise.resolve({
          access: factoryAccess,
          availability: toSourceAvailability(factoryAccess),
          value: undefined,
        }),

    productionManagerAccess === "granted"
      ? getProductionManagerWorkspace(productionContext).then((result) => ({
          access: productionManagerAccess,
          availability: result.status === "success" ? "available" as const : toSourceAvailability(productionManagerAccess, result.code),
          value: result.status === "success" ? result.value : undefined,
          message: result.status === "failure" ? result.message : undefined,
          count: result.status === "success" ? result.value.pendingDecisions.length : undefined,
        }))
      : Promise.resolve({
          access: productionManagerAccess,
          availability: toSourceAvailability(productionManagerAccess),
          value: undefined,
        }),

    aiTaskAccess === "granted"
      ? listAiTasks(productionContext, { page: 1, pageSize: LIST_PAGE_SIZE }).then((result) => ({
          access: aiTaskAccess,
          availability: result.status === "success"
            ? scopeFromCount(result.value.items.length) === "partial" ? "partial" as const : "available" as const
            : toSourceAvailability(aiTaskAccess, result.code),
          value: result.status === "success"
            ? result.value.items.map(mapRawAiTaskSummary)
            : undefined,
          message: result.status === "failure" ? result.message : undefined,
          count: result.status === "success" ? result.value.items.length : undefined,
        }))
      : Promise.resolve({
          access: aiTaskAccess,
          availability: toSourceAvailability(aiTaskAccess),
          value: undefined,
        }),
  ]);

  const queueItems = sortQueueItems([
    ...buildFactoryCaseItems(factoryResult.value ?? []),
    ...buildCommercialItems(commercialResult.value ?? []),
    ...buildProductionManagerDecisionItems(
      productionManagerResult.value?.pendingDecisions ?? [],
      productionManagerResult.value?.stateCapturedAtUtc ?? "",
    ),
    ...buildProductionItems(productionResult.value ?? []),
    ...buildQualityItems(qualityResult.value ?? []),
    ...buildMaintenanceItems(maintenanceResult.value ?? []),
    ...buildMoldItems(moldResult.value ?? []),
    ...buildMaterialItems(materialResult.value ?? []),
    ...buildProcurementItems(procurementResult.value ?? []),
    ...buildAiTaskItems(aiTaskResult.value ?? []),
  ]);

  const filteredQueue = filterOperationsCommandCenterQueueItems(queueItems, filters);

  const moduleHealth: OperationsCommandCenterModuleHealth[] = [
    buildModuleHealthEntry("factory", factoryResult),
    buildModuleHealthEntry("production-manager", productionManagerResult),
    buildModuleHealthEntry("commercial", commercialResult),
    buildModuleHealthEntry("production", productionResult),
    buildModuleHealthEntry("quality", qualityResult),
    buildModuleHealthEntry("maintenance", maintenanceResult),
    buildModuleHealthEntry("mold", moldResult),
    buildModuleHealthEntry("material", materialResult),
    buildModuleHealthEntry("procurement", procurementResult),
    buildModuleHealthEntry("ai-task", aiTaskResult),
  ];

  const accessibleModules = moduleHealth.filter(
    (entry) => entry.availability === "available" || entry.availability === "partial",
  ).length;
  const partialModules = moduleHealth.filter((entry) => entry.availability === "partial").length;
  const scopeState: OperationsCommandCenterScopeState =
    accessibleModules === 0 ? "insufficient" : partialModules > 0 ? "partial" : "complete";

  const reviewCount = filteredQueue.filter((item) => item.humanReviewRequired).length;
  const openLinkedTaskCount = aiTaskAccess === "granted"
    ? filteredQueue.filter((item) => item.linkedTaskId).length
    : null;

  const kpis = buildKpiCards({
    reviewCount,
    queueCount: filteredQueue.length,
    openLinkedTaskCount,
    aiTaskScope: aiTaskAccess === "granted" ? scopeState : "insufficient",
    productionCount: productionAccess === "granted" && productionResult.availability !== "unavailable-error"
      ? buildProductionItems(productionResult.value ?? []).length
      : null,
    qualityCount: qualityAccess === "granted" && qualityResult.availability !== "unavailable-error"
      ? buildQualityItems(qualityResult.value ?? []).length
      : null,
    maintenanceCount: maintenanceAccess === "granted" && maintenanceResult.availability !== "unavailable-error"
      ? buildMaintenanceItems(maintenanceResult.value ?? []).length
      : null,
    materialCount: materialAccess === "granted" && materialResult.availability !== "unavailable-error"
      ? buildMaterialItems(materialResult.value ?? []).length
      : null,
    procurementCount: procurementAccess === "granted" && procurementResult.availability !== "unavailable-error"
      ? buildProcurementItems(procurementResult.value ?? []).length
      : null,
    facilityLabel: "Yetkili tesis",
    timeWindowLabel,
    partialModules,
  });

  return {
    status: "success",
    workspace: {
      automationDisclaimer: OPERATIONS_COMMAND_CENTER_AUTOMATION_DISCLAIMER,
      facilityLabel: "Yetkili tesis",
      dataConfidenceLabel: scopeState === "partial"
        ? "Kısmi veri güveni"
        : scopeState === "insufficient"
          ? "Erişilebilir kaynak yok"
          : "Yetkili kaynak verisi",
      scopeState,
      partialScopeNote: partialModules > 0 ? OPERATIONS_COMMAND_CENTER_PARTIAL_NOTE : null,
      noAccessibleSources: accessibleModules === 0,
      commercialDeepLinkHref: COMMERCIAL_PROFITABILITY_COMMAND_CENTER_ROUTE_PATH,
      kpis,
      queueItems: filteredQueue,
      moduleHealth,
      filters,
    },
  };
}
