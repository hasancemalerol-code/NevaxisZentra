import assert from "node:assert/strict";
import test from "node:test";

import { deriveAiTaskCenterKpis } from "../src/features/ai-task-center/application/ai-task-center-kpis";
import { filterAiTaskCenterItems } from "../src/features/ai-task-center/application/ai-task-center-composition";
import type { AiTaskCenterItem } from "../src/features/ai-task-center/domain/ai-task-center-item";

test("summary cards use the same filters as the tab counts", () => {
  const items = [{
    kind: "task",
    statusCode: "in-progress",
    slaState: "overdue",
    assignedMembershipId: null,
    assignedTeamChannelId: null,
  } as unknown as AiTaskCenterItem];
  const fallback = { total: 0, open: 0, awaitingReview: 0, awaitingApproval: 0, inProgress: 0, criticalRisk: 0, overdue: 0, failed: 0, completed: 0 };
  const kpis = deriveAiTaskCenterKpis(items, fallback);
  const none = new Set<string>();

  assert.equal(kpis.open, filterAiTaskCenterItems(items, "all-tasks", none, "all").length);
  assert.equal(kpis.overdue, filterAiTaskCenterItems(items, "overdue", none, "all").length);
  assert.equal(kpis.criticalRisk, filterAiTaskCenterItems(items, "sla-risk", none, "all").length);
  assert.equal(kpis.overdue, 1);
  assert.equal(kpis.total, 1);
});
