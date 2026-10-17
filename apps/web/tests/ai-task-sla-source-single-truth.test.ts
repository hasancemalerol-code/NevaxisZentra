import assert from "node:assert/strict";
import test from "node:test";

import { deriveAiTaskCenterKpis } from "../src/features/ai-task-center/application/ai-task-center-kpis";
import {
  filterAiTaskCenterItems,
  mapTaskToCenterItem,
} from "../src/features/ai-task-center/application/ai-task-center-composition";
import { isAiTaskOverdue } from "../src/features/ai-task-center/application/ai-task-query";
import {
  mapRawAiTaskDetail,
  mapRawAiTaskSummary,
  resolveRawSlaState,
} from "../src/features/ai-task-center/server/ai-task-mapper";
import {
  getAiTaskResolutionDeadlineSummary,
  getAiTaskSlaDisplayText,
} from "../src/features/ai-task-center/presentation/ai-task-sla-labels";

// Resolution deadline in the fixtures is 2026-10-17T09:46:41Z; this "now" is ~14.5 h before it.
const NOW = new Date("2026-10-16T19:15:00Z");

const EMPTY_KPIS = { total: 0, open: 0, awaitingReview: 0, awaitingApproval: 0, inProgress: 0, criticalRisk: 0, overdue: 0, failed: 0, completed: 0 };

function rawSla(state: string) {
  return state === "not-applicable"
    ? { state, acknowledgmentDueAtUtc: null, responseDueAtUtc: null, resolutionDueAtUtc: null, isOverdue: false, breachType: null }
    : {
        state,
        acknowledgmentDueAtUtc: "2026-10-15T10:16:41Z",
        responseDueAtUtc: "2026-10-15T10:46:41Z",
        resolutionDueAtUtc: "2026-10-17T09:46:41Z",
        isOverdue: true,
        breachType: "acknowledgment",
      };
}

// Mirrors the live payloads: the list row carries `slaState`; the detail payload carries ONLY the `sla` object.
function rawSummary(slaState: string, listOriginKind: string, breachType: string | null = "acknowledgment") {
  return {
    id: "task-1",
    taskReference: "AT-1",
    title: "Enjeksiyon presi basınç trendini incele",
    summary: "özet",
    category: "maintenance",
    priority: "high",
    riskLevel: "medium",
    status: "draft",
    approval: { state: "required" },
    sourceModule: null,
    sourceEntityId: null,
    listOriginKind,
    dueAt: slaState === "not-applicable" ? null : "2026-10-17T09:46:41Z",
    assignee: null,
    assignedMembershipId: null,
    assignedTeamChannelId: null,
    acknowledgedAtUtc: null,
    escalationLevel: 0,
    escalationState: "none",
    slaState,
    slaBreachType: slaState === "breached" ? breachType : null,
    closureState: "none",
    evidenceCount: 0,
    closureApprovalRequired: false,
    createdAt: "2026-10-15T09:46:41Z",
    updatedAt: "2026-10-15T09:46:41Z",
    version: 1,
  };
}

function rawDetail(slaState: string, sourceCount: number) {
  const { slaState: _omitted, slaBreachType: _omittedBreach, ...base } = rawSummary(slaState, "operational-task");
  void _omitted;
  void _omittedBreach;
  return {
    ...base,
    detectedIssue: "x",
    reasoning: "x",
    recommendation: { title: "t", description: "d", actionType: "review", requiresApproval: false, reversible: true, targetSystem: null },
    confidence: { score: 0.7, explanation: "Kural", modelVersion: null },
    impact: { summary: "x" },
    sources: Array.from({ length: sourceCount }, () => ({
      sourceType: "maintenance",
      sourceId: null,
      sourceLabel: "Bakım talebi",
      observedAt: null,
      valueSummary: null,
    })),
    sla: rawSla(slaState),
    assignmentReason: null,
    assignmentSource: null,
    execution: null,
    auditTrail: [],
    closure: undefined,
    closurePolicy: undefined,
    closureValidation: undefined,
    evidence: [],
  };
}

test("acknowledgement breach with a future due date: list, detail and KPI agree, no delay wording, not counted overdue", () => {
  const list = mapRawAiTaskSummary(rawSummary("breached", "operational-task") as never);
  const detail = mapRawAiTaskDetail(rawDetail("breached", 1) as never);
  const item = mapTaskToCenterItem(list, null);
  const kpis = deriveAiTaskCenterKpis([item], EMPTY_KPIS, NOW);

  assert.equal(detail.slaState, "breached");
  assert.equal(detail.sla?.state, "breached");
  assert.equal(list.slaState, detail.slaState);
  assert.equal(item.slaLabel, "Onay hedefi aşıldı");
  assert.equal(list.sla?.breachType, "acknowledgment");
  assert.equal(detail.sla?.breachType, "acknowledgment");
  const badge = getAiTaskSlaDisplayText(item.slaState, item.slaDueAt, NOW, item.slaBreachType);
  const detailBadge = getAiTaskSlaDisplayText(detail.slaState, detail.sla?.resolutionDueAt, NOW, detail.sla?.breachType);
  assert.equal(badge, "Onay hedefi aşıldı");
  assert.equal(detailBadge, badge);
  assert.doesNotMatch(badge ?? "", /gecik|kaldı|SLA ihlali/i);
  const remaining = getAiTaskResolutionDeadlineSummary(detail.slaState, detail.sla?.resolutionDueAt, NOW);
  assert.equal(remaining?.label, "Çözüm hedefi");
  assert.equal(remaining?.value, "14 sa 32 dk");
  assert.equal(remaining?.passed, false);
  assert.equal(filterAiTaskCenterItems([item], "overdue", new Set(), "all", NOW).length, 0);
  assert.equal(filterAiTaskCenterItems([item], "sla-risk", new Set(), "all", NOW).length, 1);
  // Acknowledgement target missed, resolution deadline still ahead: SLA risk yes, "Geciken" no.
  assert.equal(kpis.criticalRisk, 1);
  assert.equal(kpis.overdue, 0);
  assert.equal(isAiTaskOverdue(detail, NOW), false);
  assert.equal(isAiTaskOverdue(list, NOW), false);
  // A source exists, so the list must not say it is missing.
  assert.notEqual(item.sourceLabel, "Kaynak belirtilmemiş");
  assert.equal(detail.sources.length, 1);
});

test("resolution deadline passed: counted overdue and worded as a delay", () => {
  const later = new Date("2026-10-17T12:00:00Z");
  const list = mapRawAiTaskSummary(rawSummary("breached", "operational-task", "resolution") as never);
  const item = mapTaskToCenterItem(list, null);

  assert.equal(item.slaLabel, "SLA ihlali · çözüm hedefi aşıldı");
  assert.equal(deriveAiTaskCenterKpis([item], EMPTY_KPIS, later).overdue, 1);
  const remaining = getAiTaskResolutionDeadlineSummary("breached", "2026-10-17T09:46:41Z", later);
  assert.equal(remaining?.label, "Çözüm hedefi");
  assert.equal(remaining?.passed, true);
  // Ack breach masking a resolution deadline that has also passed is still a delay.
  const ack = mapTaskToCenterItem(mapRawAiTaskSummary(rawSummary("breached", "operational-task") as never), null);
  assert.equal(deriveAiTaskCenterKpis([ack], EMPTY_KPIS, later).overdue, 1);
});

test("task with source but no SLA target: not applicable everywhere and never counted overdue", () => {
  const list = mapRawAiTaskSummary(rawSummary("not-applicable", "operational-task") as never);
  const detail = mapRawAiTaskDetail(rawDetail("not-applicable", 1) as never);
  const item = mapTaskToCenterItem(list, null);
  const kpis = deriveAiTaskCenterKpis([item], EMPTY_KPIS, NOW);

  assert.equal(detail.slaState, "not-applicable");
  assert.equal(list.slaState, "not-applicable");
  assert.equal(kpis.overdue, 0);
  assert.equal(kpis.criticalRisk, 0);
  assert.equal(filterAiTaskCenterItems([item], "overdue", new Set(), "all").length, 0);
  assert.equal(isAiTaskOverdue({ ...list, dueAt: "2020-01-01T00:00:00Z" }, NOW), false);
  assert.notEqual(item.sourceLabel, "Kaynak belirtilmemiş");
});

test("task with no source keeps the neutral missing-source label and its own SLA truth", () => {
  const list = mapRawAiTaskSummary(rawSummary("on-track", "unspecified") as never);
  const detail = mapRawAiTaskDetail(rawDetail("on-track", 0) as never);
  const item = mapTaskToCenterItem(list, null);

  assert.equal(item.sourceLabel, "Kaynak belirtilmemiş");
  assert.equal(detail.sources.length, 0);
  assert.equal(detail.slaState, list.slaState);
  assert.equal(deriveAiTaskCenterKpis([item], EMPTY_KPIS, NOW).overdue, 0);
});

test("detail SLA state is read from the sla object when the payload has no top-level slaState", () => {
  assert.equal(resolveRawSlaState({ sla: { state: "breached" } }), "breached");
  assert.equal(resolveRawSlaState({ slaState: "overdue", sla: { state: "breached" } }), "overdue");
  assert.equal(resolveRawSlaState({}), "not-applicable");
});
