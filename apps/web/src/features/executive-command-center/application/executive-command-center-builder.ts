import { FACILITY_TIME_ZONE } from "@/lib/presentation/format";
import type { DigitalWorkActivityApiSummary, DigitalWorkApprovalApiSummary, DigitalWorkforceControlCenterSnapshot } from "@/features/digital-workforce/server/digital-workforce-api-client";
import type { FactoryDecisionCaseQueueItemApi, FactoryDecisionCenterWorkspaceApi, FactoryDecisionEarlyWarningCardApi } from "@/features/factory-decision-center/server/factory-decision-center-api-client";
import type { RawAiTaskSummary } from "@/features/ai-task-center/server/ai-task-api-client";
import { isAiTaskFinalStatus, type AiTaskStatus } from "@/features/ai-task-center/domain/ai-task";
import { TASK_CENTER_DRILL_DOWN_LABEL } from "@/features/ai-task-center/domain/ai-task-product-labels";
import type { ThroughputOptimizerExecutiveSummaryApi } from "@/features/throughput-optimizer/server/throughput-optimizer-api-client";

import {
  EXECUTIVE_COMMAND_CENTER_EMPTY_APPROVALS,
  EXECUTIVE_COMMAND_CENTER_EMPTY_RISKS,
  EXECUTIVE_COMMAND_CENTER_EMPTY_VERIFIED,
} from "../domain/executive-command-center-labels";
import type {
  ExecutiveApprovalItem,
  ExecutiveCommandCenterFilters,
  ExecutiveCommandCenterWorkspacePayload,
  ExecutiveDigitalWorkforceStatus,
  ExecutiveOperationalExposure,
  ExecutiveOutcomeCard,
  ExecutivePredictiveRisk,
  ExecutivePriorityItem,
  ExecutiveScopeState,
  ExecutiveThroughputCaseItem,
  ExecutiveThroughputSummary,
  ExecutiveTimelineEntry,
  ExecutiveVerifiedValueItem,
} from "../types/executive-command-center-types";

export type ExecutiveCommandCenterSourceData = Readonly<{
  decisionCenter?: FactoryDecisionCenterWorkspaceApi;
  digitalWorkforce?: DigitalWorkforceControlCenterSnapshot;
  workforceApprovals?: readonly DigitalWorkApprovalApiSummary[];
  workforceActivity?: readonly DigitalWorkActivityApiSummary[];
  aiTasks?: readonly RawAiTaskSummary[];
  throughputSummary?: ThroughputOptimizerExecutiveSummaryApi;
  generatedAtUtc: string;
  filters: ExecutiveCommandCenterFilters;
  unavailableSources: readonly string[];
}>;

const DECISION_CASE_PATH = "/fabrika-karar-merkezi/vakalar";
const DECISION_CENTER_PATH = "/fabrika-karar-merkezi";
const INTELLIGENCE_PATH = "/fabrika-zekasi";
const WORKFORCE_PATH = "/dijital-is-gucu";
const WORKFORCE_APPROVALS_PATH = "/dijital-is-gucu/onaylar";
const AI_TASKS_PATH = "/";
const PRODUCTION_PATH = "/uretim";

function throughputLineDrillDown(lineId: string): string {
  return `${PRODUCTION_PATH}/hatlar/${lineId}`;
}

function formatTimeLabel(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleTimeString("tr-TR", { hour: "2-digit", minute: "2-digit", timeZone: FACILITY_TIME_ZONE });
}

function caseDrillDown(caseId: string): string {
  return `${DECISION_CASE_PATH}/${caseId}`;
}

function matchesSeverityFilter(item: FactoryDecisionCaseQueueItemApi, severity?: string): boolean {
  if (!severity) return true;
  return item.severity.toLowerCase() === severity.toLowerCase();
}

function isVerifiedCase(item: FactoryDecisionCaseQueueItemApi): boolean {
  return item.verificationState.toLowerCase() === "passed";
}

export function buildExecutiveNarrative(
  decisionCenter: FactoryDecisionCenterWorkspaceApi | undefined,
  digitalWorkforce: DigitalWorkforceControlCenterSnapshot | undefined,
  throughputSummary?: ThroughputOptimizerExecutiveSummaryApi,
): string {
  if (!decisionCenter) {
    return "Karar merkezi özeti şu anda alınamıyor.";
  }

  const { summary, liveQueues } = decisionCenter;
  const sentences: string[] = [];

  const activeCritical = liveQueues.criticalDecisions.length;
  if (activeCritical > 0) {
    sentences.push(`Bugün ${activeCritical} kritik sonuç vakası aktif.`);
  }

  const recovery = liveQueues.activeRecoveryOperations[0];
  if (recovery) {
    sentences.push(
      `${recovery.orderReference} teslimat riski için ${recovery.recommendedStrategy.toLowerCase()} yürütülüyor.`,
    );
  }

  const throughputActive = throughputSummary?.activeCases[0];
  if (throughputActive) {
    sentences.push(
      `${throughputActive.lineCode} hattında verim optimizasyonu (${throughputActive.observationState.toLowerCase()}) yürütülüyor.`,
    );
  }

  const verified = liveQueues.verifiedClosedToday[0];
  if (verified) {
    sentences.push(`${verified.orderReference} doğrulama sonrası kapatıldı.`);
  }

  const throughputVerified = throughputSummary?.verifiedToday.find(
    (item) => item.verificationOutcome === "VERIFIED_IMPROVEMENT",
  );
  if (throughputVerified?.verifiedThroughputGainPerHour) {
    sentences.push(
      `${throughputVerified.lineCode} hattında +${throughputVerified.verifiedThroughputGainPerHour.toFixed(0)} iyi/sa doğrulanmış iyileşme.`,
    );
  }

  if (summary.pendingHumanApproval > 0) {
    sentences.push(`${summary.pendingHumanApproval} insan kararı bekliyor.`);
  } else if (digitalWorkforce && digitalWorkforce.pendingApprovals > 0) {
    sentences.push(`${digitalWorkforce.pendingApprovals} dijital iş gücü onayı bekliyor.`);
  } else if (throughputSummary && throughputSummary.pendingApprovalCount > 0) {
    sentences.push(`${throughputSummary.pendingApprovalCount} verim müdahale onayı bekliyor.`);
  }

  if (sentences.length === 0) {
    return EXECUTIVE_COMMAND_CENTER_EMPTY_RISKS;
  }

  return sentences.join(" ");
}

export function buildExecutiveOutcomeCards(
  decisionCenter: FactoryDecisionCenterWorkspaceApi | undefined,
  digitalWorkforce: DigitalWorkforceControlCenterSnapshot | undefined,
  openAiTasks: number,
): readonly ExecutiveOutcomeCard[] {
  if (!decisionCenter) return [];

  const { summary, liveQueues } = decisionCenter;

  return [
    {
      id: "outcomes-at-risk",
      label: "Risk altındaki sipariş",
      value: String(summary.outcomesAtRisk),
      detail: "Açık sonuç vakaları",
      tone: summary.outcomesAtRisk > 0 ? "negative" : "positive",
      drillDownHref: DECISION_CENTER_PATH,
      drillDownLabel: "Karar Merkezi",
    },
    {
      id: "production-gap",
      label: "Üretim hedef açığı",
      value: String(
        liveQueues.criticalDecisions.reduce((sum, item) => sum + Math.max(0, item.gapQuantity), 0),
      ),
      detail: "Kritik vakalardaki toplam fark (iyi parça)",
      tone: "warning",
      drillDownHref: DECISION_CENTER_PATH,
      drillDownLabel: "Karar Merkezi",
    },
    {
      id: "machine-risk",
      label: "Kritik makine riski",
      value: String(decisionCenter.earlyWarnings.criticalPredictedRisks.length),
      detail: "Öngörülen kritik riskler",
      tone: decisionCenter.earlyWarnings.criticalPredictedRisks.length > 0 ? "warning" : "neutral",
      drillDownHref: INTELLIGENCE_PATH,
      drillDownLabel: "Fabrika Zekâsı",
    },
    {
      id: "material-exposure",
      label: "Malzeme eksikliği riski",
      value: String(decisionCenter.earlyWarnings.materialDepletionRisks.length),
      detail: "Malzeme tükenme riskleri",
      tone: "warning",
      drillDownHref: INTELLIGENCE_PATH,
      drillDownLabel: "Fabrika Zekâsı",
    },
    {
      id: "quality-exposure",
      label: "Kalite riski",
      value: String(decisionCenter.earlyWarnings.qualityDeteriorationTrends.length),
      detail: "Kalite bozulma trendleri",
      tone: "warning",
      drillDownHref: INTELLIGENCE_PATH,
      drillDownLabel: "Fabrika Zekâsı",
    },
    {
      id: "active-recovery",
      label: "Aktif kurtarma operasyonları",
      value: String(summary.recoveryOperations),
      detail: "Yürütülen kurtarma operasyonları",
      tone: "neutral",
      drillDownHref: DECISION_CENTER_PATH,
      drillDownLabel: "Karar Merkezi",
    },
    {
      id: "awaiting-decision",
      label: "İnsan Onayı bekleyen",
      value: String(summary.pendingHumanApproval + (digitalWorkforce?.pendingApprovals ?? 0)),
      detail: digitalWorkforce ? "Karar merkezi + dijital iş gücü onayları" : "Karar merkezi onayları",
      tone: summary.pendingHumanApproval > 0 ? "warning" : "neutral",
      drillDownHref: digitalWorkforce ? WORKFORCE_APPROVALS_PATH : DECISION_CENTER_PATH,
      drillDownLabel: "Onaylar",
    },
    {
      id: "verified-today",
      label: "Bugün doğrulanan kurtarma",
      value: String(liveQueues.verifiedClosedToday.length),
      detail: "Bugün doğrulanarak kapanan vakalar",
      tone: liveQueues.verifiedClosedToday.length > 0 ? "positive" : "neutral",
      drillDownHref: DECISION_CENTER_PATH,
      drillDownLabel: "Karar Merkezi",
    },
    {
      id: "open-ai-tasks",
      label: "Açık yapay zekâ görevleri",
      value: String(openAiTasks),
      detail: "İnsan incelemesi gerektiren görevler",
      tone: openAiTasks > 0 ? "warning" : "neutral",
      drillDownHref: AI_TASKS_PATH,
      drillDownLabel: TASK_CENTER_DRILL_DOWN_LABEL,
    },
  ];
}

function mapCaseToPriority(
  item: FactoryDecisionCaseQueueItemApi,
  section: ExecutivePriorityItem["section"],
  timeToImpact?: string,
  showWorkers = false,
): ExecutivePriorityItem {
  return {
    id: `${section}-${item.caseId}`,
    section,
    outcome: item.orderReference,
    impact: item.riskSummary,
    status: item.status,
    owner: (showWorkers ? item.digitalWorkerNames.join(" · ") : "") || "Atanmadı",
    nextAction: item.decisionRequired ?? item.recommendedStrategy,
    timeToImpact,
    verificationState: item.verificationState,
    drillDownHref: caseDrillDown(item.caseId),
    drillDownModule: "decision",
  };
}

export function buildExecutivePriorityStack(
  decisionCenter: FactoryDecisionCenterWorkspaceApi | undefined,
  filters: ExecutiveCommandCenterFilters,
  /** Digital Workforce is deferred: worker names are only attributed as owners when the module is enabled. */
  showWorkers = false,
): readonly ExecutivePriorityItem[] {
  if (!decisionCenter) return [];

  const { liveQueues, earlyWarnings } = decisionCenter;
  const items: ExecutivePriorityItem[] = [];

  for (const item of liveQueues.criticalDecisions.filter((row) => matchesSeverityFilter(row, filters.severity))) {
    const risk = earlyWarnings.criticalPredictedRisks.find(
      (card) => card.caseId === item.caseId || card.orderReference === item.orderReference,
    );
    items.push(mapCaseToPriority(item, "critical-now", risk?.timeToImpactLabel, showWorkers));
  }

  for (const item of liveQueues.pendingHumanApproval.filter((row) => matchesSeverityFilter(row, filters.severity))) {
    items.push(mapCaseToPriority(item, "decision-today", undefined, showWorkers));
  }

  for (const item of liveQueues.activeRecoveryOperations.filter((row) => matchesSeverityFilter(row, filters.severity))) {
    items.push(mapCaseToPriority(item, "active-recovery", undefined, showWorkers));
  }

  for (const item of liveQueues.pendingVerification.filter((row) => matchesSeverityFilter(row, filters.severity))) {
    items.push(mapCaseToPriority(item, "pending-verification", undefined, showWorkers));
  }

  for (const item of liveQueues.verifiedClosedToday.filter((row) => matchesSeverityFilter(row, filters.severity))) {
    if (!isVerifiedCase(item)) continue;
    items.push(mapCaseToPriority(item, "verified-today", undefined, showWorkers));
  }

  return items;
}

export function buildExecutivePredictiveRisks(
  decisionCenter: FactoryDecisionCenterWorkspaceApi | undefined,
): readonly ExecutivePredictiveRisk[] {
  if (!decisionCenter) return [];

  return decisionCenter.earlyWarnings.criticalPredictedRisks
    .filter((risk) => risk.riskState.toLowerCase() === "critical" || risk.riskState.toLowerCase() === "warning")
    .slice(0, 6)
    .map((risk: FactoryDecisionEarlyWarningCardApi) => ({
      id: risk.riskId,
      machineCode: risk.machineCode,
      orderReference: risk.orderReference,
      observedSummary: risk.observedSummary,
      predictedSummary: risk.predictedSummary,
      horizon: risk.horizon,
      confidenceBand: risk.confidenceBand,
      recommendedAction: risk.recommendedActions[0] ?? "Önleyici aksiyon planla",
      drillDownHref: risk.caseId ? caseDrillDown(risk.caseId) : INTELLIGENCE_PATH,
    }));
}

export function buildExecutiveApprovals(
  decisionCenter: FactoryDecisionCenterWorkspaceApi | undefined,
  workforceApprovals: readonly DigitalWorkApprovalApiSummary[] | undefined,
): readonly ExecutiveApprovalItem[] {
  const items: ExecutiveApprovalItem[] = [];

  if (decisionCenter) {
    for (const item of decisionCenter.liveQueues.pendingHumanApproval) {
      items.push({
        id: `fdc-${item.caseId}`,
        caseId: item.caseId,
        decision: item.decisionRequired ?? "Kurtarma planı onayı",
        businessOutcome: item.title,
        expectedBenefit: `${item.projectedQuantity}/${item.requiredQuantity} iyi parça öngörüsü`,
        tradeoff: item.recommendedStrategy,
        requester: (workforceApprovals ? item.digitalWorkerNames[0] : undefined) ?? "Fabrika Koordinatörü",
        urgency: item.severity,
        drillDownHref: caseDrillDown(item.caseId),
        recoveryContext: item.riskSummary,
      });
    }
  }

  if (workforceApprovals) {
    for (const approval of workforceApprovals.filter((row) => row.status.toLowerCase() === "pending")) {
      items.push({
        id: `dwf-${approval.id}`,
        caseId: approval.caseId,
        decision: approval.action,
        businessOutcome: approval.businessReason,
        expectedBenefit: approval.impact,
        tradeoff: `Risk seviyesi ${approval.riskLevel}`,
        requester: approval.workerCode,
        urgency: approval.riskLevel >= 4 ? "critical" : "high",
        drillDownHref: WORKFORCE_APPROVALS_PATH,
      });
    }
  }

  return items;
}

export function buildExecutiveDigitalWorkforceStatus(
  snapshot: DigitalWorkforceControlCenterSnapshot | undefined,
): ExecutiveDigitalWorkforceStatus {
  if (!snapshot) {
    return {
      activeWorkers: 0,
      activeCases: 0,
      waitingApproval: 0,
      blocked: 0,
      failed: 0,
      verifiedOutcomes: 0,
      drillDownHref: WORKFORCE_PATH,
      available: false,
    };
  }

  const activeWorkers = snapshot.workers.filter(
    (worker) => worker.enabled && !worker.isPaused && !worker.isKilled,
  ).length;

  const verifiedOutcomes = snapshot.workers.reduce((sum, worker) => sum + worker.verifiedClosures, 0);

  return {
    activeWorkers,
    activeCases: snapshot.openCases,
    waitingApproval: snapshot.pendingApprovals,
    blocked: snapshot.blockedExecutions,
    failed: snapshot.failedExecutions,
    verifiedOutcomes,
    drillDownHref: WORKFORCE_PATH,
    available: true,
  };
}

export function buildExecutiveVerifiedValue(
  decisionCenter: FactoryDecisionCenterWorkspaceApi | undefined,
  throughputSummary?: ThroughputOptimizerExecutiveSummaryApi,
): readonly ExecutiveVerifiedValueItem[] {
  const items: ExecutiveVerifiedValueItem[] = [];

  if (decisionCenter) {
    for (const item of decisionCenter.liveQueues.verifiedClosedToday.filter(isVerifiedCase)) {
      items.push({
        id: item.caseId,
        outcomeLabel: item.gapQuantity <= 0 ? "Teslimat taahhüdü korundu" : "Üretim kurtarıldı",
        orderReference: item.orderReference,
        summary: `${item.title} · ${item.projectedQuantity}/${item.requiredQuantity} iyi parça`,
        verifiedAtLabel: formatTimeLabel(item.updatedAtUtc),
        drillDownHref: caseDrillDown(item.caseId),
      });
    }
  }

  if (throughputSummary) {
    for (const item of throughputSummary.verifiedToday) {
      if (item.verificationOutcome !== "VERIFIED_IMPROVEMENT" || !item.verifiedThroughputGainPerHour) {
        continue;
      }

      items.push({
        id: `throughput-${item.caseId}`,
        outcomeLabel: "Doğrulanmış verimlilik iyileşmesi",
        orderReference: item.lineCode,
        summary: `${item.selectedInterventionTitle ?? "Müdahale"} · +${item.verifiedThroughputGainPerHour.toFixed(0)} iyi parça/sa`,
        verifiedAtLabel: formatTimeLabel(item.updatedAtUtc),
        drillDownHref: throughputLineDrillDown(item.lineId),
      });
    }
  }

  return items;
}

function mapThroughputExecutiveCase(
  item: ThroughputOptimizerExecutiveSummaryApi["activeCases"][number],
): ExecutiveThroughputCaseItem {
  const constraintLabel = item.currentConstraintStationCode
    ? `${item.currentConstraintStationCode}${item.currentConstraintType ? ` · ${item.currentConstraintType}` : ""}`
    : "Kısıt tespit edilmedi";

  return {
    id: item.caseId,
    lineCode: item.lineCode,
    lineId: item.lineId,
    currentPhase: item.currentPhase,
    caseStatus: item.caseStatus,
    constraintLabel,
    selectedIntervention: item.selectedInterventionTitle,
    approvalState: item.hasPendingApproval ? "Onay bekliyor" : "Onay gerekmiyor",
    observationState: item.observationState,
    verifiedGainPerHour: item.verifiedThroughputGainPerHour,
    verificationOutcome: item.verificationOutcome,
    drillDownHref: throughputLineDrillDown(item.lineId),
  };
}

export function buildExecutiveThroughputSummary(
  throughputSummary: ThroughputOptimizerExecutiveSummaryApi | undefined,
): ExecutiveThroughputSummary {
  if (!throughputSummary) {
    return {
      activeCaseCount: 0,
      pendingApprovalCount: 0,
      verifiedImprovementTodayCount: 0,
      activeCases: [],
      verifiedToday: [],
      drillDownHref: PRODUCTION_PATH,
    };
  }

  return {
    activeCaseCount: throughputSummary.activeCaseCount,
    pendingApprovalCount: throughputSummary.pendingApprovalCount,
    verifiedImprovementTodayCount: throughputSummary.verifiedImprovementTodayCount,
    activeCases: throughputSummary.activeCases.map(mapThroughputExecutiveCase),
    verifiedToday: throughputSummary.verifiedToday.map(mapThroughputExecutiveCase),
    drillDownHref: PRODUCTION_PATH,
  };
}

export function buildExecutiveOperationalExposure(
  decisionCenter: FactoryDecisionCenterWorkspaceApi | undefined,
): readonly ExecutiveOperationalExposure[] {
  if (!decisionCenter) return [];

  const gapTotal = decisionCenter.liveQueues.criticalDecisions.reduce(
    (sum, item) => sum + Math.max(0, item.gapQuantity),
    0,
  );
  const deliveryRisks = decisionCenter.earlyWarnings.deliveryRisks.length;
  const downtimeSignals = decisionCenter.earlyWarnings.machineDegradationTrends.length;

  return [
    {
      id: "output-gap",
      label: "Kurtarılabilir üretim fırsatı",
      value: `${gapTotal} iyi parça`,
      detail: "Kritik vakalardaki üretim açığı",
      drillDownHref: DECISION_CENTER_PATH,
    },
    {
      id: "delivery-risk",
      label: "Teslimat riski",
      value: String(deliveryRisks),
      detail: "Teslimat riski sinyalleri",
      drillDownHref: INTELLIGENCE_PATH,
    },
    {
      id: "downtime-exposure",
      label: "Duruş riski",
      value: String(downtimeSignals),
      detail: "Makine bozulma trendleri",
      drillDownHref: INTELLIGENCE_PATH,
    },
    {
      id: "pending-verification",
      label: "Doğrulama bekleyenler",
      value: String(decisionCenter.summary.pendingVerification),
      detail: "Doğrulama bekleyen sonuç vakaları",
      drillDownHref: DECISION_CENTER_PATH,
    },
  ];
}

export function buildExecutiveTimeline(
  decisionCenter: FactoryDecisionCenterWorkspaceApi | undefined,
  activity: readonly DigitalWorkActivityApiSummary[] | undefined,
  throughputSummary?: ThroughputOptimizerExecutiveSummaryApi,
): readonly ExecutiveTimelineEntry[] {
  const entries: ExecutiveTimelineEntry[] = [];

  if (activity) {
    for (const row of activity.slice(0, 8)) {
      entries.push({
        id: `activity-${row.id}`,
        timeLabel: formatTimeLabel(row.createdAtUtc),
        occurredAtUtc: row.createdAtUtc,
        label: row.actionType,
        description: row.description,
        drillDownHref: `${WORKFORCE_PATH}?caseId=${row.caseId}`,
      });
    }
  }

  if (decisionCenter) {
    for (const item of decisionCenter.priorityQueue.slice(0, 5)) {
      entries.push({
        id: `case-${item.caseId}`,
        timeLabel: formatTimeLabel(item.updatedAtUtc),
        occurredAtUtc: item.updatedAtUtc,
        label: item.status,
        description: `${item.orderReference} · ${item.title}`,
        drillDownHref: caseDrillDown(item.caseId),
      });
    }
  }

  if (throughputSummary) {
    for (const item of [...throughputSummary.activeCases, ...throughputSummary.verifiedToday].slice(0, 4)) {
      entries.push({
        id: `throughput-${item.caseId}-${item.currentPhase}`,
        timeLabel: formatTimeLabel(item.updatedAtUtc),
        occurredAtUtc: item.updatedAtUtc,
        label: item.currentPhase,
        description: `${item.lineCode} · ${item.observationState}${item.selectedInterventionTitle ? ` · ${item.selectedInterventionTitle}` : ""}`,
        drillDownHref: throughputLineDrillDown(item.lineId),
      });
    }
  }

  return entries
    .sort(
      (left, right) =>
        new Date(left.occurredAtUtc).getTime() - new Date(right.occurredAtUtc).getTime(),
    )
    .slice(0, 12);
}

export function countOpenAiTasks(tasks: readonly RawAiTaskSummary[] | undefined): number {
  if (!tasks) return 0;
  return tasks.filter((task) => !isAiTaskFinalStatus(task.status as AiTaskStatus)).length;
}

export function buildExecutiveCommandCenterWorkspace(
  source: ExecutiveCommandCenterSourceData,
): ExecutiveCommandCenterWorkspacePayload {
  const openAiTasks = countOpenAiTasks(source.aiTasks);
  const scopeState: ExecutiveScopeState =
    source.unavailableSources.length === 0 ? "full" : "partial";

  const throughputOptimizer = buildExecutiveThroughputSummary(source.throughputSummary);
  const narrative = buildExecutiveNarrative(
    source.decisionCenter,
    source.digitalWorkforce,
    source.throughputSummary,
  );
  const verifiedValue = buildExecutiveVerifiedValue(source.decisionCenter, source.throughputSummary);
  const approvals = buildExecutiveApprovals(source.decisionCenter, source.workforceApprovals);

  return {
    generatedAtUtc: source.generatedAtUtc,
    narrative:
      narrative === EXECUTIVE_COMMAND_CENTER_EMPTY_RISKS &&
      approvals.length === 0 &&
      verifiedValue.length === 0 &&
      throughputOptimizer.activeCaseCount === 0
        ? EXECUTIVE_COMMAND_CENTER_EMPTY_RISKS
        : narrative,
    outcomeCards: buildExecutiveOutcomeCards(
      source.decisionCenter,
      source.digitalWorkforce,
      openAiTasks,
    ),
    priorityStack: buildExecutivePriorityStack(source.decisionCenter, source.filters, Boolean(source.digitalWorkforce)),
    predictiveRisks: buildExecutivePredictiveRisks(source.decisionCenter),
    approvals,
    digitalWorkforce: buildExecutiveDigitalWorkforceStatus(source.digitalWorkforce),
    verifiedValue,
    throughputOptimizer,
    operationalExposure: buildExecutiveOperationalExposure(source.decisionCenter),
    timeline: buildExecutiveTimeline(
      source.decisionCenter,
      source.workforceActivity,
      source.throughputSummary,
    ),
    scopeState,
    unavailableSources: source.unavailableSources,
    filters: source.filters,
  };
}

export function getExecutiveEmptyStateMessage(
  workspace: ExecutiveCommandCenterWorkspacePayload,
  section: "risks" | "approvals" | "verified",
): string {
  if (section === "risks" && workspace.predictiveRisks.length === 0) {
    return EXECUTIVE_COMMAND_CENTER_EMPTY_RISKS;
  }
  if (section === "approvals" && workspace.approvals.length === 0) {
    return EXECUTIVE_COMMAND_CENTER_EMPTY_APPROVALS;
  }
  if (section === "verified" && workspace.verifiedValue.length === 0) {
    return EXECUTIVE_COMMAND_CENTER_EMPTY_VERIFIED;
  }
  return "";
}
