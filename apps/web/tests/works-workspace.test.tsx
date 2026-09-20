import assert from "node:assert/strict";
import test from "node:test";
import { renderToStaticMarkup } from "react-dom/server";

import {
  buildWorksWorkspaceHref,
  getWorksQueueCount,
  getWorksQueueLabel,
  resolveWorksQueueId,
  WORKS_QUEUE_BLOCKED,
  WORKS_QUEUE_CRITICAL,
  WORKS_QUEUE_PENDING_APPROVAL,
  WORKS_QUEUE_TEAM,
} from "../src/features/works/domain/works-queues";
import {
  formatWorksApprovalStatus,
  resolveWorksDisplayBadgeClass,
  resolveWorksPriorityBadgeClass,
} from "../src/features/works/domain/works-state-projection";
import { WorksWorkspaceView } from "../src/features/works/components/works-workspace-view";
import type {
  WorksCaseDetailSnapshot,
  WorksListItem,
  WorksWorkspacePagePayload,
} from "../src/features/works/domain/works-types";

const listItem: WorksListItem = {
  id: "case-001",
  title: "Assembly bottleneck recovery in observation",
  problemSummary: "MIX-LINE-01 throughput below target with Order 418 risk",
  displayState: "GÖZLEMLENİYOR",
  priorityLabel: "Kritik",
  workTypeLabel: "Üretim Kurtarma",
  factoryContextLabel: "Pilot tesis",
  ownerLabel: "Operasyon ekibi",
  actingSystemLabel: "Üretim Kurtarma Dijital Çalışanı",
  slaLabel: null,
  hasPendingApproval: false,
  isBlocked: false,
  blockerLabel: null,
  expectedImpact: "Order 418 delivery risk",
  lastUpdateLabel: "20.09.2026 09:00",
  updatedAtUtc: "2026-09-20T06:00:00.000Z",
  status: "executing",
  verificationState: "pending",
  outcomeLabel: "",
  severity: "high",
  primaryDomain: "scenario-recovery",
  concurrencyToken: 1,
};

const caseDetail: WorksCaseDetailSnapshot = {
  summary: listItem,
  description: listItem.problemSummary,
  impactSummary: "Order 418 delivery risk",
  nextStepLabel: "Uygulama ilerlemesini izleyin.",
  activeActionLabel: null,
  pendingApproval: null,
  blocker: null,
  dependencies: [],
  relatedWork: [],
  timeline: [
    {
      code: "created",
      label: "İş oluşturuldu",
      occurredAtUtc: "2026-09-20T05:00:00.000Z",
      detail: listItem.title,
    },
  ],
  verification: {
    outcomeLabel: "DOĞRULANMIŞ İYİLEŞME",
    beforeValue: 185,
    afterValue: 804,
    verificationState: "passed",
    verifiedAtUtc: "2026-09-20T06:30:00.000Z",
    verifiedByLabel: "Üretim Kurtarma Dijital Çalışanı",
  },
  scenarioContext: {
    runId: "run-001",
    lineCode: "MIX-LINE-01",
    currentPhase: "CLOSED",
    selectedOptionLabel: "OPTION_B",
    verificationOutcome: "VERIFIED_SUCCESS",
    baselineThroughput: 185,
    predictedThroughput: 613.34,
    actualThroughput: 804,
    scenarioHref: "/senaryo-kurtarma?entryPoint=ISLER&runId=run-001",
  },
  evidence: [
    {
      kind: "measurement",
      label: "Operasyon ölçümü",
      detail: "185 → 804 GOOD/saat",
      occurredAtUtc: "2026-09-20T06:30:00.000Z",
    },
  ],
  permittedActions: ["open-factory-context", "open-scenario"],
  factoryHref: "/fabrika",
  planHref: null,
  scenarioHref: "/senaryo-kurtarma?entryPoint=ISLER&runId=run-001",
  digitalWorkforceHref: "/dijital-is-gucu/worker?case=case-001",
};

const page: WorksWorkspacePagePayload = {
  workspace: {
    queueCounts: {
      assigned: 1,
      team: 4,
      pendingApproval: 1,
      critical: 2,
      blocked: 1,
      inProgress: 3,
      observing: 1,
      verificationPending: 1,
      completedToday: 2,
    },
    items: [listItem],
    activeQueue: WORKS_QUEUE_TEAM,
    totalCount: 4,
  },
  caseDetail,
  canDecideApproval: true,
  initialQueue: WORKS_QUEUE_TEAM,
  selectedCaseId: listItem.id,
  search: "",
};

test("works queue helpers resolve Turkish labels and hrefs", () => {
  assert.equal(resolveWorksQueueId("blocked"), WORKS_QUEUE_BLOCKED);
  assert.equal(resolveWorksQueueId(undefined), WORKS_QUEUE_TEAM);
  assert.equal(getWorksQueueLabel(WORKS_QUEUE_PENDING_APPROVAL), "Onay Bekleyen");
  assert.equal(
    buildWorksWorkspaceHref({ queue: WORKS_QUEUE_BLOCKED, caseId: "abc", search: "mix" }),
    "/isler?kuyruk=blocked&is=abc&ara=mix",
  );
  assert.equal(getWorksQueueCount(page.workspace.queueCounts, WORKS_QUEUE_CRITICAL), 2);
});

test("works state projection exposes badge classes", () => {
  assert.match(resolveWorksDisplayBadgeClass("GÖZLEMLENİYOR"), /info/);
  assert.match(resolveWorksDisplayBadgeClass("ENGELLENDİ"), /critical/);
  assert.match(resolveWorksPriorityBadgeClass("Kritik"), /critical/);
  assert.equal(formatWorksApprovalStatus(true), "Onay bekliyor");
});

test("works workspace renders 3-panel layout and inspector sections", () => {
  const html = renderToStaticMarkup(<WorksWorkspaceView page={page} />);

  assert.match(html, /data-testid="works-workspace"/);
  assert.match(html, /data-testid="works-queue-nav"/);
  assert.match(html, /data-testid="works-list-panel"/);
  assert.match(html, /data-testid="works-inspector"/);
  assert.match(html, /Assembly bottleneck recovery in observation/);
  assert.match(html, /Sorun \/ Sonuç/);
  assert.match(html, /Doğrulama/);
  assert.match(html, /185/);
  assert.match(html, /804/);
  assert.match(html, /Senaryo bağlamı/);
});

test("works workspace list row exposes compact operational fields", () => {
  const html = renderToStaticMarkup(<WorksWorkspaceView page={page} />);
  assert.match(html, /GÖZLEMLENİYOR/);
  assert.match(html, /Üretim Kurtarma/);
  assert.match(html, /Pilot tesis/);
  assert.match(html, /data-testid="works-row-case-001"/);
});

test("works workspace shows blocked queue option", () => {
  const html = renderToStaticMarkup(<WorksWorkspaceView page={page} />);
  assert.match(html, /Engelli/);
  assert.match(html, /Doğrulama Bekleyen/);
});
