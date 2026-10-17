import assert from "node:assert/strict";
import test from "node:test";
import { renderToStaticMarkup } from "react-dom/server";

import { ShiftHandoverListView } from "../src/features/shift-handover/components/shift-handover-list-view";
import { ShiftHandoverPageShell } from "../src/features/shift-handover/components/shift-handover-page-shell";
import {
  SHIFT_OPERATIONS_PAGE_HEADERS,
  SHIFT_OPERATIONS_SUBNAV,
} from "../src/features/shift-operations/domain/shift-operations-labels";
import { CONTEXTUAL_NAVIGATION_SECTIONS, WORKFLOW_PRIMARY_HOMES } from "../src/lib/navigation/workflow-primary-navigation";

const CANONICAL_HANDOVER_ROUTE = "/vardiya-devir";
const READINESS_ROUTE = "/vardiyalar/teslim";

test("one label maps to one destination across the Vardiyalar tabs", () => {
  const byLabel = new Map<string, Set<string>>();
  for (const item of SHIFT_OPERATIONS_SUBNAV) {
    byLabel.set(item.label, (byLabel.get(item.label) ?? new Set()).add(item.href));
  }
  for (const [label, hrefs] of byLabel) {
    assert.equal(hrefs.size, 1, `tab label '${label}' points to more than one route`);
  }
  const labels = SHIFT_OPERATIONS_SUBNAV.map((item) => item.label);
  assert.equal(new Set(labels).size, labels.length, "tab labels must be unique");
});

test("the Vardiya Devri tab and the sidebar item resolve to the same canonical route", () => {
  const tab = SHIFT_OPERATIONS_SUBNAV.find((item) => item.label === "Vardiya Devri");
  assert.equal(tab?.href, CANONICAL_HANDOVER_ROUTE);

  const sidebarItems = JSON.stringify([WORKFLOW_PRIMARY_HOMES, CONTEXTUAL_NAVIGATION_SECTIONS]);
  assert.match(sidebarItems, /"label":"Vardiya Devri","href":"\/vardiya-devir"/);
  assert.doesNotMatch(sidebarItems, /"label":"Vardiya Devri","href":"\/vardiyalar\/teslim"/);

  const readiness = SHIFT_OPERATIONS_SUBNAV.find((item) => item.href === READINESS_ROUTE);
  assert.equal(readiness?.label, "Teslim Hazırlığı", "the readiness page must not reuse the 'Vardiya Devri' name");
  const readinessHeader = SHIFT_OPERATIONS_PAGE_HEADERS.find((entry) => entry.href === READINESS_ROUTE);
  assert.equal(readinessHeader?.title, "Teslim Hazırlığı");
});

test("on the handover page the active 'Vardiya Devri' tab points at the page itself, never at a different route", () => {
  const html = renderToStaticMarkup(
    <ShiftHandoverPageShell>
      <p>içerik</p>
    </ShiftHandoverPageShell>,
  );
  assert.match(html, /class="zx-subnav__link"/, "sub navigation must render");

  const activeAnchors = [...html.matchAll(/<a [^>]*aria-current="page"[^>]*>[^<]*<\/a>/g)].map((m) => m[0]);
  assert.equal(activeAnchors.length, 1, "exactly one active tab");
  assert.match(activeAnchors[0], /href="\/vardiya-devir"/);
  assert.match(activeAnchors[0], />Vardiya Devri</);
  assert.doesNotMatch(html, /href="\/vardiyalar\/teslim"[^>]*aria-current/);
});

test("a handover record is discoverable from the canonical route and links to its own detail page", () => {
  const html = renderToStaticMarkup(
    <ShiftHandoverListView
      list={{
        items: [
          {
            id: "11111111-1111-4111-8111-111111111111",
            handoverReference: "VD-20261015-0001",
            status: "draft",
            shiftDate: "2026-10-15",
            shiftPeriod: "day",
            department: "production",
            outgoingLabel: "Üretim amiri",
            unresolvedItemCount: 0,
          },
        ],
        capabilities: { canManage: true },
      } as never}
    />,
  );
  assert.match(html, /VD-20261015-0001/);
  assert.match(html, /href="\/vardiya-devir\/11111111-1111-4111-8111-111111111111"/);
  assert.match(html, /Taslak/);
});
