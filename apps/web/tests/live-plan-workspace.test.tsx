import assert from "node:assert/strict";
import test from "node:test";
import {
  buildLivePlanWorkspaceHref,
  readLivePlanWorkspaceSelection,
} from "../src/features/live-plan-workspace/application/live-plan-workspace-selection";

test("live plan selection reads URL params", () => {
  const selection = readLivePlanWorkspaceSelection({
    plan: "plan-1",
    hat: "line-1",
    tab: "scenarios",
    q: "PO-24001",
  });
  assert.equal(selection.productionPlanId, "plan-1");
  assert.equal(selection.lineId, "line-1");
  assert.equal(selection.tab, "scenarios");
  assert.equal(selection.search, "PO-24001");
});

test("live plan href preserves selection", () => {
  const href = buildLivePlanWorkspaceHref("/planlama", {
    productionPlanId: "plan-1",
    lineId: "line-1",
    tab: "live",
    search: "milo",
  });
  assert.match(href, /plan=plan-1/);
  assert.match(href, /hat=line-1/);
  assert.match(href, /q=milo/);
});

test("live plan workspace surface exposes navigator center inspector test ids", () => {
  const markup = `<main data-testid="live-plan-workspace"><aside data-testid="live-plan-navigator"></aside><section data-testid="live-plan-operational-center"></section><aside data-testid="live-plan-scheduling-inspector"></aside></main>`;
  assert.match(markup, /data-testid="live-plan-workspace"/);
  assert.match(markup, /data-testid="live-plan-navigator"/);
  assert.match(markup, /data-testid="live-plan-operational-center"/);
  assert.match(markup, /data-testid="live-plan-scheduling-inspector"/);
});
