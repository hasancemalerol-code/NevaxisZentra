import assert from "node:assert/strict";
import test from "node:test";

import {
  buildExecutiveApprovals,
  buildExecutiveCommandCenterWorkspace,
  buildExecutiveNarrative,
  buildExecutivePredictiveRisks,
  buildExecutivePriorityStack,
  buildExecutiveThroughputSummary,
  buildExecutiveTimeline,
  buildExecutiveVerifiedValue,
  countOpenAiTasks,
  getExecutiveEmptyStateMessage,
} from "../src/features/executive-command-center/application/executive-command-center-builder";
import type { FactoryDecisionCenterWorkspaceApi } from "../src/features/factory-decision-center/server/factory-decision-center-api-client";
import type { ThroughputOptimizerExecutiveSummaryApi } from "../src/features/throughput-optimizer/server/throughput-optimizer-api-client";
import { EXECUTIVE_COMMAND_CENTER_ROUTE_PATH } from "../src/features/executive-command-center/domain/executive-command-center-labels";
import {
  buildExecutiveCommandCenterFilterParams,
  clearExecutiveCommandCenterFilterParams,
  hasActiveExecutiveCommandCenterFilters,
  readExecutiveCommandCenterFilters,
} from "../src/features/executive-command-center/application/executive-command-center-filter";
import { navigationItems } from "../src/lib/navigation/primary-navigation";

function createQueueItem(overrides: Partial<FactoryDecisionCenterWorkspaceApi["priorityQueue"][number]> = {}) {
  return {
    caseId: "case-1",
    title: "Teslimat riski",
    severity: "critical",
    status: "executing",
    orderReference: "ORD-8742",
    customerName: "Demo Müşteri",
    deliveryCommitmentLabel: "18 Eylül",
    riskSummary: "Üretim açığı",
    requiredQuantity: 1000,
    projectedQuantity: 820,
    gapQuantity: 180,
    recommendedStrategy: "Üretim kurtarma planı",
    digitalWorkerNames: ["Üretim Kurtarma"],
    decisionRequired: undefined,
    currentBlockerCode: undefined,
    verificationState: "pending",
    updatedAtUtc: "2026-09-16T09:12:00.000Z",
    concurrencyToken: 1,
    ...overrides,
  };
}

function createThroughputSummary(
  overrides: Partial<ThroughputOptimizerExecutiveSummaryApi> = {},
): ThroughputOptimizerExecutiveSummaryApi {
  return {
    activeCaseCount: 1,
    pendingApprovalCount: 0,
    verifiedImprovementTodayCount: 1,
    activeCases: [
      {
        caseId: "throughput-active",
        lineId: "line-1",
        lineCode: "MIX-01",
        currentPhase: "EXECUTE",
        caseStatus: "executing",
        currentConstraintStationCode: "ST-30",
        currentConstraintType: "operator",
        selectedInterventionTitle: "Add operator to constraint station",
        selectedInterventionType: "ADD_OPERATOR",
        hasPendingApproval: false,
        observationState: "Müdahale uygulanıyor",
        updatedAtUtc: "2026-09-16T09:30:00.000Z",
        isVerifiedClosed: false,
      },
    ],
    verifiedToday: [
      {
        caseId: "throughput-verified",
        lineId: "line-1",
        lineCode: "MIX-01",
        currentPhase: "CLOSED",
        caseStatus: "closed",
        currentConstraintStationCode: "ST-30",
        currentConstraintType: "operator",
        selectedInterventionTitle: "Add operator to constraint station",
        selectedInterventionType: "ADD_OPERATOR",
        hasPendingApproval: false,
        observationState: "Kapatıldı",
        verificationOutcome: "VERIFIED_IMPROVEMENT",
        verifiedThroughputGainPerHour: 42,
        updatedAtUtc: "2026-09-16T09:45:00.000Z",
        isVerifiedClosed: true,
      },
    ],
    ...overrides,
  };
}

function createWorkspace(overrides: Partial<FactoryDecisionCenterWorkspaceApi> = {}): FactoryDecisionCenterWorkspaceApi {
  const queueItem = createQueueItem();
  const verifiedItem = createQueueItem({
    caseId: "case-verified",
    orderReference: "ORD-9001",
    verificationState: "passed",
    status: "closed",
    gapQuantity: 0,
    projectedQuantity: 1000,
    updatedAtUtc: "2026-09-16T09:45:00.000Z",
  });

  return {
    summary: {
      criticalDecisions: 1,
      outcomesAtRisk: 1,
      pendingHumanApproval: 1,
      recoveryOperations: 1,
      pendingVerification: 1,
    },
    priorityQueue: [queueItem],
    liveQueues: {
      criticalDecisions: [queueItem],
      activeRecoveryOperations: [queueItem],
      pendingHumanApproval: [createQueueItem({ caseId: "case-approval", decisionRequired: "Plan onayı" })],
      blockedOrEscalated: [],
      pendingVerification: [createQueueItem({ caseId: "case-verify", status: "verifying" })],
      verifiedClosedToday: [verifiedItem],
    },
    earlyWarnings: {
      criticalPredictedRisks: [
        {
          riskId: "risk-1",
          caseId: "case-1",
          riskType: "machine-degradation",
          riskState: "critical",
          machineCode: "INJ-006",
          orderReference: "ORD-8742",
          observedSummary: "Cycle time drift",
          predictedSummary: "Delivery slip in 6h",
          timeToImpactLabel: "6 saat",
          confidenceBand: "high",
          horizon: "short",
          evidence: ["OEE drop"],
          recommendedActions: ["Early intervention"],
          dataQuality: "good",
          generatedAtUtc: "2026-09-16T09:10:00.000Z",
        },
        {
          riskId: "risk-watch",
          riskType: "watch",
          riskState: "watch",
          machineCode: "INJ-001",
          observedSummary: "Minor drift",
          predictedSummary: "Monitor",
          timeToImpactLabel: "24 saat",
          confidenceBand: "low",
          horizon: "long",
          evidence: [],
          recommendedActions: [],
          dataQuality: "good",
          generatedAtUtc: "2026-09-16T08:00:00.000Z",
        },
      ],
      deliveryRisks: [],
      machineDegradationTrends: [],
      materialDepletionRisks: [],
      qualityDeteriorationTrends: [],
    },
    generatedAtUtc: "2026-09-16T10:00:00.000Z",
    ...overrides,
  };
}

test("executive navigation entry is registered", () => {
  const item = navigationItems.find((candidate) => candidate.href === EXECUTIVE_COMMAND_CENTER_ROUTE_PATH);
  assert.ok(item);
  assert.equal(item?.label, "Yönetici Komuta Merkezi");
  assert.equal(item?.permissionKey, "factory-case.read");
});

test("executive narrative is grounded in case data", () => {
  const narrative = buildExecutiveNarrative(createWorkspace(), undefined);
  assert.match(narrative, /ORD-8742/);
  assert.match(narrative, /insan kararı bekliyor/i);
});

test("verified value counts only passed verification state", () => {
  const verified = buildExecutiveVerifiedValue(
    createWorkspace({
      liveQueues: {
        ...createWorkspace().liveQueues,
        verifiedClosedToday: [
          createQueueItem({ caseId: "v1", verificationState: "passed", orderReference: "ORD-A" }),
          createQueueItem({ caseId: "v2", verificationState: "pending", orderReference: "ORD-B" }),
        ],
      },
    }),
  );

  assert.equal(verified.length, 1);
  assert.equal(verified[0]?.orderReference, "ORD-A");
});

test("executive narrative includes throughput optimizer context", () => {
  const narrative = buildExecutiveNarrative(createWorkspace(), undefined, createThroughputSummary());
  assert.match(narrative, /MIX-01/);
  assert.match(narrative, /VERIFIED iyileşme/i);
});

test("verified value includes only passed decision and throughput improvements", () => {
  const verified = buildExecutiveVerifiedValue(createWorkspace(), createThroughputSummary());
  assert.equal(verified.length, 2);
  assert.ok(verified.some((item) => item.id === "case-verified"));
  assert.ok(verified.some((item) => item.id === "throughput-throughput-verified"));
});

test("throughput summary maps drill-down to production line detail", () => {
  const summary = buildExecutiveThroughputSummary(createThroughputSummary());
  assert.equal(summary.activeCaseCount, 1);
  assert.equal(summary.verifiedImprovementTodayCount, 1);
  assert.equal(summary.activeCases[0]?.drillDownHref, "/uretim/hatlar/line-1");
});

test("timeline orders entries chronologically by occurredAtUtc", () => {
  const timeline = buildExecutiveTimeline(
    createWorkspace(),
    [
      {
        id: "a1",
        caseId: "case-1",
        workerId: "w1",
        workerCode: "worker",
        actionType: "Worker action started",
        description: "Late activity",
        result: "ok",
        sourceClassification: "real",
        createdAtUtc: "2026-09-16T09:44:00.000Z",
      },
    ],
    createThroughputSummary(),
  );

  assert.ok(timeline.length >= 2);
  for (let index = 1; index < timeline.length; index += 1) {
    const previous = new Date(timeline[index - 1]!.occurredAtUtc).getTime();
    const current = new Date(timeline[index]!.occurredAtUtc).getTime();
    assert.ok(current >= previous);
  }
});

test("workspace builder exposes throughput optimizer section", () => {
  const workspace = buildExecutiveCommandCenterWorkspace({
    decisionCenter: createWorkspace(),
    throughputSummary: createThroughputSummary(),
    generatedAtUtc: "2026-09-16T10:00:00.000Z",
    filters: { timeScope: "today" },
    unavailableSources: [],
  });

  assert.equal(workspace.throughputOptimizer.activeCaseCount, 1);
  assert.equal(workspace.throughputOptimizer.verifiedImprovementTodayCount, 1);
});

test("predictive risks filter out non-material watch signals", () => {
  const risks = buildExecutivePredictiveRisks(createWorkspace());
  assert.equal(risks.length, 1);
  assert.equal(risks[0]?.machineCode, "INJ-006");
});

test("priority stack includes decision center drill-down links", () => {
  const stack = buildExecutivePriorityStack(createWorkspace(), { timeScope: "today" });
  assert.ok(stack.some((item) => item.drillDownHref.includes("/fabrika-karar-merkezi/vakalar/case-approval")));
  assert.ok(stack.some((item) => item.section === "verified-today"));
});

test("pending approvals aggregate decision center and workforce approvals", () => {
  const approvals = buildExecutiveApprovals(createWorkspace(), [
    {
      id: "approval-1",
      caseId: "case-dwf",
      workerId: "worker-1",
      workerCode: "production-recovery",
      action: "Rezervasyon onayı",
      businessReason: "Malzeme rezervasyonu",
      impact: "Teslimat korunur",
      riskLevel: 4,
      amount: null,
      status: "pending",
      createdAtUtc: "2026-09-16T09:00:00.000Z",
      expiresAtUtc: null,
      concurrencyToken: 1,
    },
  ]);

  assert.equal(approvals.length, 2);
  assert.ok(approvals.some((item) => item.id.startsWith("fdc-")));
  assert.ok(approvals.some((item) => item.id.startsWith("dwf-")));
});

test("workspace builder exposes empty states", () => {
  const workspace = buildExecutiveCommandCenterWorkspace({
    decisionCenter: createWorkspace({
      liveQueues: {
        criticalDecisions: [],
        activeRecoveryOperations: [],
        pendingHumanApproval: [],
        blockedOrEscalated: [],
        pendingVerification: [],
        verifiedClosedToday: [],
      },
      earlyWarnings: {
        criticalPredictedRisks: [],
        deliveryRisks: [],
        machineDegradationTrends: [],
        materialDepletionRisks: [],
        qualityDeteriorationTrends: [],
      },
      summary: {
        criticalDecisions: 0,
        outcomesAtRisk: 0,
        pendingHumanApproval: 0,
        recoveryOperations: 0,
        pendingVerification: 0,
      },
    }),
    generatedAtUtc: "2026-09-16T10:00:00.000Z",
    filters: { timeScope: "today" },
    unavailableSources: [],
  });

  assert.equal(getExecutiveEmptyStateMessage(workspace, "risks").length > 0, true);
  assert.equal(getExecutiveEmptyStateMessage(workspace, "approvals").length > 0, true);
});

test("open ai task count excludes final statuses", () => {
  const count = countOpenAiTasks([
    {
      id: "1",
      taskReference: "T-1",
      title: "Open",
      summary: "",
      category: "production-planning",
      priority: "high",
      riskLevel: "high",
      status: "in-progress",
      approval: { state: "pending" },
      sourceModule: null,
      sourceEntityId: null,
      listOriginKind: "manual",
      assignee: null,
      assignedMembershipId: null,
      assignedTeamChannelId: null,
      acknowledgedAtUtc: null,
      escalationLevel: 0,
      escalationState: "none",
      slaState: "on-track",
      dueAt: null,
      createdAt: "2026-09-16T08:00:00.000Z",
      updatedAt: "2026-09-16T08:00:00.000Z",
      version: 1,
      closureState: null,
      evidenceCount: 0,
      closureApprovalRequired: false,
    },
    {
      id: "2",
      taskReference: "T-2",
      title: "Done",
      summary: "",
      category: "production-planning",
      priority: "low",
      riskLevel: "low",
      status: "completed",
      approval: { state: "approved" },
      sourceModule: null,
      sourceEntityId: null,
      listOriginKind: "manual",
      assignee: null,
      assignedMembershipId: null,
      assignedTeamChannelId: null,
      acknowledgedAtUtc: null,
      escalationLevel: 0,
      escalationState: "none",
      slaState: "on-track",
      dueAt: null,
      createdAt: "2026-09-16T08:00:00.000Z",
      updatedAt: "2026-09-16T08:00:00.000Z",
      version: 1,
      closureState: null,
      evidenceCount: 0,
      closureApprovalRequired: false,
    },
  ]);

  assert.equal(count, 1);
});

test("executive filter params preserve existing query keys and deep-link behavior", () => {
  const base = new URLSearchParams("outcomeType=delivery&severity=high&timeScope=shift");
  const updated = buildExecutiveCommandCenterFilterParams(base, { severity: "critical", timeScope: "today" });

  assert.equal(updated.get("severity"), "critical");
  assert.equal(updated.get("timeScope"), null);
  assert.equal(updated.get("outcomeType"), "delivery");
});

test("executive clear filters resets severity and timeScope only", () => {
  const cleared = clearExecutiveCommandCenterFilterParams(
    new URLSearchParams("severity=critical&timeScope=shift&outcomeType=delivery"),
  );

  assert.equal(cleared.get("severity"), null);
  assert.equal(cleared.get("timeScope"), null);
  assert.equal(cleared.get("outcomeType"), "delivery");
});

test("executive active filter detection uses defaults", () => {
  assert.equal(hasActiveExecutiveCommandCenterFilters(readExecutiveCommandCenterFilters(new URLSearchParams())), false);
  assert.equal(
    hasActiveExecutiveCommandCenterFilters(readExecutiveCommandCenterFilters(new URLSearchParams("severity=critical"))),
    true,
  );
  assert.equal(
    hasActiveExecutiveCommandCenterFilters(readExecutiveCommandCenterFilters(new URLSearchParams("timeScope=shift"))),
    true,
  );
});

test("priority stack respects severity filter from URL params", () => {
  const workspace = createWorkspace({
    liveQueues: {
      ...createWorkspace().liveQueues,
      pendingHumanApproval: [createQueueItem({ caseId: "case-approval", severity: "low" })],
    },
  });
  const stack = buildExecutivePriorityStack(workspace, {
    timeScope: "today",
    severity: "critical",
  });
  assert.ok(!stack.some((item) => item.id.includes("case-approval")));
});
