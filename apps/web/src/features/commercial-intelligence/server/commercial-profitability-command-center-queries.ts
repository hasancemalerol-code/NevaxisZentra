import "./commercial-intelligence-server-boundary";

import type { CommercialProfitabilityCommandCenterFilters } from "../application/commercial-profitability-command-center-filter";
import {
  buildCommercialProfitabilityCommandCenterTimeWindowLabel,
  toCommercialProfitabilityCommandCenterComparisonFilters,
} from "../application/commercial-profitability-command-center-filter";
import { formatCount, formatDateTime } from "../domain/commercial-intelligence-formatters";
import { getCommercialIntelligenceEvidenceStatusLabel } from "../domain/commercial-intelligence-labels";
import {
  COMMERCIAL_PROFITABILITY_COMMAND_CENTER_AI_TASK_PARTIAL_NOTE,
  COMMERCIAL_PROFITABILITY_COMMAND_CENTER_PARTIAL_NOTE,
  COMMERCIAL_PROFITABILITY_COMMAND_CENTER_ROUTE_PATH,
  getCommercialProfitabilityCommandCenterSourceTypeLabel,
} from "../domain/commercial-profitability-command-center-labels";
import { getCommercialPricingScenarioLifecycleLabel } from "../domain/commercial-pricing-scenario-labels";
import {
  getCommercialProfitabilityAlertLifecycleLabel,
  getCommercialProfitabilityAlertSeverityLabel,
} from "../domain/commercial-profitability-alert-labels";
import { localizeCommercialProfitabilityProvenance } from "../domain/commercial-profitability-labels";
import type {
  CommercialProfitabilityCommandCenterDataConfidence,
  CommercialProfitabilityCommandCenterKpiCard,
  CommercialProfitabilityCommandCenterLensLink,
  CommercialProfitabilityCommandCenterLifecycleSnapshot,
  CommercialProfitabilityCommandCenterQueueItem,
  CommercialProfitabilityCommandCenterScopeState,
  CommercialProfitabilityCommandCenterWorkspacePayload,
} from "../types/commercial-profitability-command-center-types";
import { isAiTaskFinalStatus } from "../../ai-task-center/domain/ai-task";
import type { AiTask } from "../../ai-task-center/domain/ai-task";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "../../admin-access/server/admin-access-effective-access";
import {
  getCommercialPricingScenarioWorkspace,
  listCommercialPricingScenarios,
  type RawCommercialPricingScenarioSummary,
} from "./commercial-pricing-scenario-api-client";
import {
  getCommercialProfitabilityAlertWorkspace,
  listCommercialProfitabilityAlerts,
  type RawCommercialProfitabilityAlertSummary,
} from "./commercial-profitability-alert-api-client";
import {
  listCommercialProfitabilityComparisons,
} from "./commercial-profitability-api-client";
import {
  PROFITABILITY_COST_COMPONENT_MIX_TYPE,
  PROFITABILITY_CUSTOMER_CONTRIBUTION_TYPE,
  PROFITABILITY_MATERIAL_CONTRIBUTION_TYPE,
} from "../components/commercial-profitability-presentation";
import { resolveCommercialIntelligenceRequestContext } from "./commercial-intelligence-request-context";
import { listAiTasks } from "../../ai-task-center/server/ai-task-api-client";
import { mapRawAiTaskSummary } from "../../ai-task-center/server/ai-task-mapper";
import { resolveAiTaskRequestContext } from "../../ai-task-center/server/ai-task-request-context";

const COMMAND_CENTER_LIST_PAGE_SIZE = 100;
const LENS_PREVIEW_PAGE_SIZE = 5;

const ALERT_OPEN_LIFECYCLES = new Set(["active", "acknowledged", "task-created"]);
const SCENARIO_REVIEW_LIFECYCLES = new Set([
  "needs-review",
  "submitted-for-approval",
  "blocked",
  "eligible-for-approval",
]);

const SEVERITY_RANK: Record<string, number> = {
  critical: 4,
  high: 3,
  medium: 2,
  low: 1,
};

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type CommercialProfitabilityCommandCenterResult =
  | Readonly<{ status: "success"; workspace: CommercialProfitabilityCommandCenterWorkspacePayload }>
  | ReadFailure;

function scopeFromPage(itemsLength: number, pageSize: number): CommercialProfitabilityCommandCenterScopeState {
  if (itemsLength === 0) return "complete";
  if (itemsLength >= pageSize) return "partial";
  return "complete";
}

function buildLensHref(
  basePath: string,
  filters: CommercialProfitabilityCommandCenterFilters,
  extra?: Record<string, string>,
): string {
  const params = new URLSearchParams();
  if (filters.windowFrom) params.set("windowFrom", filters.windowFrom);
  if (filters.windowTo) params.set("windowTo", filters.windowTo);
  if (extra) {
    for (const [key, value] of Object.entries(extra)) {
      if (value) params.set(key, value);
    }
  }
  const query = params.toString();
  return query ? `${basePath}?${query}` : basePath;
}

function alertDeepLink(alertId: string): string {
  return `/ticari-zeka/karlilik-uyarilari?alert=${alertId}`;
}

function scenarioDeepLink(scenarioId: string): string {
  return `/ticari-zeka/fiyat-koruma-ve-onay?scenario=${scenarioId}`;
}

function taskDeepLink(taskId: string): string {
  return `/ai-gorevleri/${taskId}`;
}

function alertPriorityTier(alert: RawCommercialProfitabilityAlertSummary): number {
  if (alert.alertCategory === "data-review") return 600;
  if (alert.severity === "critical") return 100;
  if (alert.severity === "high") return 200;
  if (alert.evidenceStatus === "insufficient-data" || alert.evidenceStatus === "source-conflict") {
    return 600;
  }
  if (alert.severity === "medium") return 800;
  return 900;
}

function scenarioPriorityTier(scenario: RawCommercialPricingScenarioSummary): number {
  if (scenario.lifecycleStatus === "blocked") return 300;
  if (scenario.lifecycleStatus === "submitted-for-approval") return 400;
  if (scenario.lifecycleStatus === "needs-review") return 500;
  if (scenario.lifecycleStatus === "eligible-for-approval") return 550;
  return 900;
}

function taskPriorityTier(task: AiTask): number {
  if (task.status === "awaiting-approval") return 350;
  if (task.status === "awaiting-review") return 450;
  if (task.riskLevel === "critical") return 250;
  return 700;
}

function buildQueueItemFromAlert(alert: RawCommercialProfitabilityAlertSummary): CommercialProfitabilityCommandCenterQueueItem {
  const isEvidenceReview =
    alert.alertCategory === "data-review"
    || alert.evidenceStatus === "insufficient-data"
    || alert.evidenceStatus === "source-conflict";
  const sourceType = isEvidenceReview ? "evidence-review" as const : "alert" as const;
  const humanDecisionRequired =
    ALERT_OPEN_LIFECYCLES.has(alert.lifecycleStatus)
    && (alert.severity === "critical" || alert.severity === "high" || isEvidenceReview);

  return {
    id: `alert:${alert.id}`,
    sourceType,
    sourceTypeLabel: getCommercialProfitabilityCommandCenterSourceTypeLabel(sourceType),
    title: alert.title,
    severityOrStatusLabel: `${getCommercialProfitabilityAlertSeverityLabel(alert.severity)} · ${getCommercialProfitabilityAlertLifecycleLabel(alert.lifecycleStatus)}`,
    evidenceStateLabel: getCommercialIntelligenceEvidenceStatusLabel(alert.evidenceStatus),
    ownerActionLabel: alert.linkedAiTaskId ? "Görev bağlı" : "İnsan kararı bekleniyor",
    humanDecisionRequired,
    deepLinkHref: alertDeepLink(alert.id),
    linkedTaskHref: alert.linkedAiTaskId ? taskDeepLink(alert.linkedAiTaskId) : null,
    priorityTier: alertPriorityTier(alert),
    severityRank: SEVERITY_RANK[alert.severity] ?? 0,
    sortTimestampUtc: alert.updatedAtUtc,
    sortTimestampLabel: formatDateTime(alert.updatedAtUtc),
  };
}

function buildQueueItemFromScenario(
  scenario: RawCommercialPricingScenarioSummary,
): CommercialProfitabilityCommandCenterQueueItem {
  const humanDecisionRequired = SCENARIO_REVIEW_LIFECYCLES.has(scenario.lifecycleStatus);

  return {
    id: `scenario:${scenario.id}`,
    sourceType: "pricing-scenario",
    sourceTypeLabel: getCommercialProfitabilityCommandCenterSourceTypeLabel("pricing-scenario"),
    title: scenario.scenarioReference,
    severityOrStatusLabel: getCommercialPricingScenarioLifecycleLabel(scenario.lifecycleStatus),
    evidenceStateLabel: scenario.guardrailResult
      ? getCommercialPricingScenarioLifecycleLabel(scenario.guardrailResult)
      : "—",
    ownerActionLabel: scenario.linkedAiTaskId ? "Görev bağlı" : "Onay / inceleme bekleniyor",
    humanDecisionRequired,
    deepLinkHref: scenarioDeepLink(scenario.id),
    linkedTaskHref: scenario.linkedAiTaskId ? taskDeepLink(scenario.linkedAiTaskId) : null,
    priorityTier: scenarioPriorityTier(scenario),
    severityRank: scenario.lifecycleStatus === "blocked" ? 4 : 2,
    sortTimestampUtc: scenario.updatedAtUtc,
    sortTimestampLabel: formatDateTime(scenario.updatedAtUtc),
  };
}

function buildQueueItemFromTask(task: AiTask): CommercialProfitabilityCommandCenterQueueItem {
  const humanDecisionRequired =
    !isAiTaskFinalStatus(task.status)
    && (task.status === "awaiting-review" || task.status === "awaiting-approval" || task.approval.state === "pending");

  return {
    id: `task:${task.id}`,
    sourceType: "ai-task",
    sourceTypeLabel: getCommercialProfitabilityCommandCenterSourceTypeLabel("ai-task"),
    title: task.title,
    severityOrStatusLabel: `${task.priority} · ${task.status}`,
    evidenceStateLabel: task.riskLevel,
    ownerActionLabel: task.approval.state === "pending" ? "Onay bekleniyor" : "Görev aksiyonu bekleniyor",
    humanDecisionRequired,
    deepLinkHref: taskDeepLink(task.id),
    linkedTaskHref: null,
    priorityTier: taskPriorityTier(task),
    severityRank: task.riskLevel === "critical" ? 4 : task.riskLevel === "high" ? 3 : 1,
    sortTimestampUtc: task.updatedAt,
    sortTimestampLabel: formatDateTime(task.updatedAt),
  };
}

function sortQueueItems(
  items: readonly CommercialProfitabilityCommandCenterQueueItem[],
): readonly CommercialProfitabilityCommandCenterQueueItem[] {
  return [...items].sort((left, right) => {
    if (left.priorityTier !== right.priorityTier) {
      return left.priorityTier - right.priorityTier;
    }
    if (left.severityRank !== right.severityRank) {
      return right.severityRank - left.severityRank;
    }
    return Date.parse(right.sortTimestampUtc) - Date.parse(left.sortTimestampUtc);
  });
}

function countLifecycle(
  items: readonly { lifecycleStatus: string }[],
  status: string,
): number {
  return items.filter((item) => item.lifecycleStatus === status).length;
}

function buildAlertLifecycleSnapshots(
  alerts: readonly RawCommercialProfitabilityAlertSummary[],
  alertScope: CommercialProfitabilityCommandCenterScopeState,
): readonly CommercialProfitabilityCommandCenterLifecycleSnapshot[] {
  const statuses: ReadonlyArray<
    Readonly<{ key: string; label: string; category?: boolean }>
  > = [
    { key: "active", label: "Aktif" },
    { key: "acknowledged", label: "Onaylanmış" },
    { key: "task-created", label: "Görev oluşturuldu" },
    { key: "resolved", label: "Çözüldü" },
    { key: "dismissed", label: "Kapatıldı" },
    { key: "data-review", label: "Kanıt incelemesi", category: true },
  ];

  return statuses.map(({ key, label, category }) => ({
    label,
    count: category
      ? alerts.filter((item) => item.alertCategory === "data-review").length
      : countLifecycle(alerts, key),
    scopeState: alertScope,
    deepLinkHref: category
      ? "/ticari-zeka/karlilik-uyarilari?category=data-review"
      : `/ticari-zeka/karlilik-uyarilari?lifecycleStatus=${key}`,
  }));
}

function buildPricingLifecycleSnapshots(
  scenarios: readonly RawCommercialPricingScenarioSummary[],
  workspaceKpis: Readonly<{
    draftCount: number;
    needsReviewCount: number;
    eligibleCount: number;
    blockedCount: number;
    approvedCount: number;
  }>,
  scenarioScope: CommercialProfitabilityCommandCenterScopeState,
): readonly CommercialProfitabilityCommandCenterLifecycleSnapshot[] {
  const submittedCount = countLifecycle(scenarios, "submitted-for-approval");
  const rejectedCount = countLifecycle(scenarios, "rejected");

  return [
    {
      label: "Taslak",
      count: workspaceKpis.draftCount,
      scopeState: "complete",
      deepLinkHref: "/ticari-zeka/fiyat-koruma-ve-onay?lifecycleStatus=draft",
    },
    {
      label: "İnceleme gerekli",
      count: workspaceKpis.needsReviewCount,
      scopeState: "complete",
      deepLinkHref: "/ticari-zeka/fiyat-koruma-ve-onay?lifecycleStatus=needs-review",
    },
    {
      label: "Onaya gönderildi",
      count: submittedCount,
      scopeState: scenarioScope,
      deepLinkHref: "/ticari-zeka/fiyat-koruma-ve-onay?lifecycleStatus=submitted-for-approval",
    },
    {
      label: "Engellendi",
      count: workspaceKpis.blockedCount,
      scopeState: "complete",
      deepLinkHref: "/ticari-zeka/fiyat-koruma-ve-onay?lifecycleStatus=blocked",
    },
    {
      label: "Onaylandı",
      count: workspaceKpis.approvedCount,
      scopeState: "complete",
      deepLinkHref: "/ticari-zeka/fiyat-koruma-ve-onay?lifecycleStatus=approved",
    },
    {
      label: "Reddedildi",
      count: rejectedCount,
      scopeState: scenarioScope,
      deepLinkHref: "/ticari-zeka/fiyat-koruma-ve-onay?lifecycleStatus=rejected",
    },
  ];
}

function buildKpiCards(input: Readonly<{
  alertKpis: Readonly<{
    activeCount: number;
    highOrCriticalCount: number;
    evidenceReviewCount: number;
  }>;
  scenarioKpis: Readonly<{
    needsReviewCount: number;
    blockedCount: number;
  }>;
  openLinkedTaskCount: number | null;
  aiTaskScopeState: CommercialProfitabilityCommandCenterScopeState;
  timeWindowLabel: string;
  facilityLabel: string;
}>): readonly CommercialProfitabilityCommandCenterKpiCard[] {
  return [
    {
      id: "cc-alert-active",
      label: "Aktif kârlılık uyarıları",
      value: formatCount(input.alertKpis.activeCount),
      hint: "Açık uyarı yaşam döngüsü",
      tone: input.alertKpis.activeCount > 0 ? "warning" : "neutral",
      scopeState: "complete",
    },
    {
      id: "cc-alert-high-critical",
      label: "Kritik / yüksek uyarı",
      value: formatCount(input.alertKpis.highOrCriticalCount),
      hint: "Öncelikli inceleme",
      tone: input.alertKpis.highOrCriticalCount > 0 ? "negative" : "neutral",
      scopeState: "complete",
    },
    {
      id: "cc-open-linked-tasks",
      label: "Açık bağlı AI görevleri",
      value: input.openLinkedTaskCount === null ? "—" : formatCount(input.openLinkedTaskCount),
      hint: input.aiTaskScopeState === "insufficient"
        ? "ai-task.read gerekli"
        : "commercial-intelligence kaynaklı",
      tone: input.openLinkedTaskCount && input.openLinkedTaskCount > 0 ? "warning" : "neutral",
      scopeState: input.aiTaskScopeState,
    },
    {
      id: "cc-scenario-review",
      label: "İnceleme bekleyen senaryo",
      value: formatCount(input.scenarioKpis.needsReviewCount),
      hint: "Fiyat koruma incelemesi",
      tone: input.scenarioKpis.needsReviewCount > 0 ? "warning" : "neutral",
      scopeState: "complete",
    },
    {
      id: "cc-scenario-blocked",
      label: "Engellenen senaryo",
      value: formatCount(input.scenarioKpis.blockedCount),
      hint: "Koruma engeli",
      tone: input.scenarioKpis.blockedCount > 0 ? "negative" : "neutral",
      scopeState: "complete",
    },
    {
      id: "cc-evidence-review",
      label: "Kanıt / veri incelemesi",
      value: formatCount(input.alertKpis.evidenceReviewCount),
      hint: "Veri kalitesi incelemesi",
      tone: input.alertKpis.evidenceReviewCount > 0 ? "warning" : "neutral",
      scopeState: "complete",
    },
    {
      id: "cc-scope",
      label: "Kapsam",
      value: input.facilityLabel,
      hint: input.timeWindowLabel,
      tone: "neutral",
      scopeState: "complete",
    },
  ];
}

function maxTimestamp(...values: readonly string[]): string {
  const parsed = values
    .map((value) => Date.parse(value))
    .filter((value) => Number.isFinite(value));
  if (parsed.length === 0) return "—";
  return formatDateTime(new Date(Math.max(...parsed)).toISOString());
}

export async function readCommercialProfitabilityCommandCenterWorkspace(
  filters: CommercialProfitabilityCommandCenterFilters = { windowFrom: "", windowTo: "" },
): Promise<CommercialProfitabilityCommandCenterResult> {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const access = await fetchCurrentEffectiveAccess();
  const canReadAiTasks = hasGrantedPermission(access, "ai-task.read");
  const comparisonWindow = toCommercialProfitabilityCommandCenterComparisonFilters(filters);
  const timeWindowLabel = buildCommercialProfitabilityCommandCenterTimeWindowLabel(filters);

  const [
    alertWorkspaceResult,
    alertsListResult,
    scenarioWorkspaceResult,
    scenariosListResult,
    productComparisonsResult,
    customerComparisonsResult,
    materialComparisonsResult,
    costDriverComparisonsResult,
  ] = await Promise.all([
    getCommercialProfitabilityAlertWorkspace(contextResult.context),
    listCommercialProfitabilityAlerts(contextResult.context, { page: 1, pageSize: COMMAND_CENTER_LIST_PAGE_SIZE }),
    getCommercialPricingScenarioWorkspace(contextResult.context),
    listCommercialPricingScenarios(contextResult.context, { page: 1, pageSize: COMMAND_CENTER_LIST_PAGE_SIZE }),
    listCommercialProfitabilityComparisons(contextResult.context, {
      page: 1,
      pageSize: LENS_PREVIEW_PAGE_SIZE,
      comparisonType: "temporal-contribution-trend",
      outcome: "below-threshold",
      ...comparisonWindow,
    }),
    listCommercialProfitabilityComparisons(contextResult.context, {
      page: 1,
      pageSize: LENS_PREVIEW_PAGE_SIZE,
      comparisonType: PROFITABILITY_CUSTOMER_CONTRIBUTION_TYPE,
      outcome: "below-threshold",
      ...comparisonWindow,
    }),
    listCommercialProfitabilityComparisons(contextResult.context, {
      page: 1,
      pageSize: LENS_PREVIEW_PAGE_SIZE,
      comparisonType: PROFITABILITY_MATERIAL_CONTRIBUTION_TYPE,
      outcome: "below-threshold",
      ...comparisonWindow,
    }),
    listCommercialProfitabilityComparisons(contextResult.context, {
      page: 1,
      pageSize: LENS_PREVIEW_PAGE_SIZE,
      comparisonType: PROFITABILITY_COST_COMPONENT_MIX_TYPE,
      outcome: "below-threshold",
      ...comparisonWindow,
    }),
  ]);

  if (alertWorkspaceResult.status === "failure") {
    return { status: "failure", message: alertWorkspaceResult.message, code: alertWorkspaceResult.code };
  }
  if (alertsListResult.status === "failure") {
    return { status: "failure", message: alertsListResult.message, code: alertsListResult.code };
  }
  if (scenarioWorkspaceResult.status === "failure") {
    return { status: "failure", message: scenarioWorkspaceResult.message, code: scenarioWorkspaceResult.code };
  }
  if (scenariosListResult.status === "failure") {
    return { status: "failure", message: scenariosListResult.message, code: scenariosListResult.code };
  }

  const alerts = alertsListResult.value.items;
  const scenarios = scenariosListResult.value.items;
  const alertScope = scopeFromPage(alerts.length, COMMAND_CENTER_LIST_PAGE_SIZE);
  const scenarioScope = scopeFromPage(scenarios.length, COMMAND_CENTER_LIST_PAGE_SIZE);

  let openLinkedTaskCount: number | null = null;
  let aiTaskScopeState: CommercialProfitabilityCommandCenterScopeState = "insufficient";
  const queueTasks: AiTask[] = [];

  if (canReadAiTasks) {
    const aiContextResult = resolveAiTaskRequestContext();
    if (aiContextResult.status === "success") {
      const tasksResult = await listAiTasks(aiContextResult.context, {
        sourceModule: "commercial-intelligence",
        page: 1,
        pageSize: COMMAND_CENTER_LIST_PAGE_SIZE,
      });
      if (tasksResult.status === "success") {
        const tasks = tasksResult.value.items.map(mapRawAiTaskSummary);
        openLinkedTaskCount = tasks.filter((task) => !isAiTaskFinalStatus(task.status)).length;
        aiTaskScopeState = scopeFromPage(tasks.length, COMMAND_CENTER_LIST_PAGE_SIZE);
        for (const task of tasks) {
          if (!isAiTaskFinalStatus(task.status)) {
            queueTasks.push(task);
          }
        }
      }
    }
  }

  const queueAlerts = alerts.filter((alert) =>
    ALERT_OPEN_LIFECYCLES.has(alert.lifecycleStatus)
    && (
      alert.severity === "critical"
      || alert.severity === "high"
      || alert.alertCategory === "data-review"
      || alert.evidenceStatus === "insufficient-data"
      || alert.evidenceStatus === "source-conflict"
    ),
  );

  const queueScenarios = scenarios.filter((scenario) =>
    SCENARIO_REVIEW_LIFECYCLES.has(scenario.lifecycleStatus),
  );

  const priorityQueue = sortQueueItems([
    ...queueAlerts.map(buildQueueItemFromAlert),
    ...queueScenarios.map(buildQueueItemFromScenario),
    ...queueTasks.map(buildQueueItemFromTask),
  ]).slice(0, 25);

  const partialNotes: string[] = [];
  if (alertScope === "partial" || scenarioScope === "partial") {
    partialNotes.push(COMMERCIAL_PROFITABILITY_COMMAND_CENTER_PARTIAL_NOTE);
  }
  if (!canReadAiTasks) {
    partialNotes.push(COMMERCIAL_PROFITABILITY_COMMAND_CENTER_AI_TASK_PARTIAL_NOTE);
  }

  const evidenceNotes: string[] = [];
  const conflictNotes: string[] = [];
  for (const alert of alerts) {
    if (alert.evidenceStatus === "insufficient-data") {
      evidenceNotes.push(`${alert.alertReference}: yetersiz kanıt`);
    }
    if (alert.evidenceStatus === "source-conflict") {
      conflictNotes.push(`${alert.alertReference}: kaynak çakışması`);
    }
  }

  const dataConfidence: CommercialProfitabilityCommandCenterDataConfidence = {
    scopeLabel: "Yetkili tesis kapsamı",
    timeWindowLabel,
    facilityLabel: "Yetkili tesis",
    lastRefreshLabel: maxTimestamp(
      ...alerts.map((item) => item.updatedAtUtc),
      ...scenarios.map((item) => item.updatedAtUtc),
    ),
    partialAggregationNotes: partialNotes,
    evidenceInsufficiencyNotes: evidenceNotes.slice(0, 5),
    sourceConflictNotes: conflictNotes.slice(0, 5),
  };

  const profitabilityLenses: readonly CommercialProfitabilityCommandCenterLensLink[] = [
    {
      id: "lens-products",
      label: "Ürün kârlılığı",
      hint: "Eşik altı ürün karşılaştırmaları",
      deepLinkHref: buildLensHref("/ticari-zeka/urun-karliligi", filters, { outcome: "below-threshold" }),
      previewCount: productComparisonsResult.status === "success"
        ? productComparisonsResult.value.items.length
        : null,
      scopeState: productComparisonsResult.status === "success"
        ? scopeFromPage(productComparisonsResult.value.items.length, LENS_PREVIEW_PAGE_SIZE)
        : "insufficient",
    },
    {
      id: "lens-customers",
      label: "Müşteri kârlılığı",
      hint: "Eşik altı müşteri katkı pozisyonları",
      deepLinkHref: buildLensHref("/ticari-zeka/musteri-karliligi", filters, { outcome: "below-threshold" }),
      previewCount: customerComparisonsResult.status === "success"
        ? customerComparisonsResult.value.items.length
        : null,
      scopeState: customerComparisonsResult.status === "success"
        ? scopeFromPage(customerComparisonsResult.value.items.length, LENS_PREVIEW_PAGE_SIZE)
        : "insufficient",
    },
    {
      id: "lens-materials",
      label: "Malzeme kârlılığı",
      hint: "Eşik altı malzeme katkı pozisyonları",
      deepLinkHref: buildLensHref("/ticari-zeka/malzeme-ve-maliyet-karliligi", filters, {
        lens: "material",
        outcome: "below-threshold",
      }),
      previewCount: materialComparisonsResult.status === "success"
        ? materialComparisonsResult.value.items.length
        : null,
      scopeState: materialComparisonsResult.status === "success"
        ? scopeFromPage(materialComparisonsResult.value.items.length, LENS_PREVIEW_PAGE_SIZE)
        : "insufficient",
    },
    {
      id: "lens-cost-drivers",
      label: "Maliyet sürücüleri",
      hint: "Eşik altı maliyet karışımı değişimleri",
      deepLinkHref: buildLensHref("/ticari-zeka/malzeme-ve-maliyet-karliligi", filters, {
        lens: "cost-driver",
        outcome: "below-threshold",
      }),
      previewCount: costDriverComparisonsResult.status === "success"
        ? costDriverComparisonsResult.value.items.length
        : null,
      scopeState: costDriverComparisonsResult.status === "success"
        ? scopeFromPage(costDriverComparisonsResult.value.items.length, LENS_PREVIEW_PAGE_SIZE)
        : "insufficient",
    },
  ];

  return {
    status: "success",
    workspace: {
      facilityLabel: "Yetkili tesis",
      facilityId: contextResult.context.facilityId,
      timeWindowLabel,
      windowFrom: filters.windowFrom,
      windowTo: filters.windowTo,
      ruleSetVersion: alertWorkspaceResult.value.ruleSetVersion,
      provenanceDisclaimer: localizeCommercialProfitabilityProvenance(
        alertWorkspaceResult.value.provenanceDisclaimer,
      ),
      automationDisclaimer: alertWorkspaceResult.value.automationDisclaimer,
      kpiCards: buildKpiCards({
        alertKpis: alertWorkspaceResult.value.kpis,
        scenarioKpis: scenarioWorkspaceResult.value.kpis,
        openLinkedTaskCount,
        aiTaskScopeState,
        timeWindowLabel,
        facilityLabel: "Yetkili tesis",
      }),
      priorityQueue,
      profitabilityLenses,
      pricingLifecycleSnapshots: buildPricingLifecycleSnapshots(
        scenarios,
        scenarioWorkspaceResult.value.kpis,
        scenarioScope,
      ),
      alertLifecycleSnapshots: buildAlertLifecycleSnapshots(alerts, alertScope),
      dataConfidence,
      canReadAiTasks,
      aiTaskScopeState,
    },
  };
}

export { COMMERCIAL_PROFITABILITY_COMMAND_CENTER_ROUTE_PATH } from "../domain/commercial-profitability-command-center-labels";
