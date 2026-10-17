import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import path from "node:path";
import test from "node:test";
import { renderToStaticMarkup } from "react-dom/server";

import { ContextPanel } from "../src/components/workspace/context-panel";
import { LineTopology } from "../src/components/workspace/line-topology";
import { OperationalBadge, mapOperationalStatus } from "../src/components/workspace/operational-status";
import { uniqueTopologyStations, getProductionResourceTypeLabel } from "../src/features/production-resources/domain/production-resources-labels";
import { knownStateLabel } from "../src/lib/presentation/enum-labels";

// ── A. status labels: canonical values stay English in the API, the customer never sees them raw ────────────────────

test("raw machine and line states are shown in Turkish, canonical values are unchanged", () => {
  const expected: Record<string, string> = { IDLE: "Boşta", STOPPED: "Durdu", idle: "Boşta", stopped: "Durdu", RUNNING: "Çalışıyor", FAULTED: "Arıza", STARVED: "Aç" };
  for (const [raw, label] of Object.entries(expected)) {
    assert.equal(knownStateLabel(raw), label, raw);
    const html = renderToStaticMarkup(<OperationalBadge status={mapOperationalStatus(raw)} label={raw} />);
    assert.ok(html.includes(label), `${raw} renders as ${label}`);
    assert.ok(!new RegExp(`>\s*${raw}\s*<`).test(html), `${raw} must not render raw`);
  }
  // The mapping key (canonical state) is untouched.
  assert.equal(mapOperationalStatus("STOPPED"), "idle");
});

test("operational badge keeps text that is not a raw state", () => {
  assert.ok(renderToStaticMarkup(<OperationalBadge status="running" label="Çalışıyor" />).includes("Çalışıyor"));
  assert.ok(renderToStaticMarkup(<OperationalBadge status="idle" label="Hazırlanıyor" />).includes("Hazırlanıyor"));
});

test("imported resource types never render raw", () => {
  for (const raw of ["mold", "quality", "buffer", "tool", "machine"]) {
    const label = getProductionResourceTypeLabel(raw);
    assert.notEqual(label, raw, raw);
  }
});

// ── B. topology: one canonical station appears once, order is stable ────────────────────────────────────────────────

test("a station shared by several resources is drawn once and keeps its order", () => {
  const rows = [
    { stationId: "s1", stationCode: "ST-001", resourceId: "r1" },
    { stationId: "s1", stationCode: "ST-001", resourceId: "r5" },
    { stationId: "s1", stationCode: "ST-001", resourceId: "r7" },
    { stationId: "s3", stationCode: "ST-003", resourceId: "r3" },
    { stationId: "s1", stationCode: "ST-001", resourceId: "r9" },
  ];
  const once = uniqueTopologyStations(rows);
  assert.deepEqual(once.map((row) => row.stationId), ["s1", "s3"]);
  // Idempotent: reloading / re-importing the same rows cannot add nodes.
  assert.deepEqual(uniqueTopologyStations([...rows, ...rows]).map((row) => row.stationId), ["s1", "s3"]);
  assert.deepEqual(uniqueTopologyStations(once), once);

  const html = renderToStaticMarkup(
    <LineTopology stations={once.map((row) => ({ id: row.stationId, code: row.stationCode, name: row.stationCode, state: "idle" }))} />,
  );
  assert.equal((html.match(/fact-station-row"/g) ?? []).length, 2);
  assert.equal((html.match(/ST-001/g) ?? []).length, 2); // code + name of the single node
});

test("interactive topology rows are list items wrapping a button (a button cannot carry the listitem role)", () => {
  const html = renderToStaticMarkup(
    <LineTopology stations={[{ id: "s1", code: "ST-001", name: "Station", state: "IDLE" }]} onSelectStation={() => undefined} />,
  );
  assert.match(html, /<div class="fact-station-cell" role="listitem"><button[^>]*class="fact-station-row"/);
  assert.doesNotMatch(html, /<button[^>]*role="listitem"/);
});

// ── C. context panel layout ─────────────────────────────────────────────────────────────────────────────────────────

test("context panel renders one label/value row per entry inside a single-column body", () => {
  const html = renderToStaticMarkup(<ContextPanel title="Kaynak kimliği" rows={[{ label: "Kaynak", value: "RES-001" }, { label: "Tip", value: "Makine" }]} />);
  assert.equal((html.match(/fact-context-panel__row/g) ?? []).length, 2);

  const css = readFileSync(path.resolve(__dirname, "..", "src", "design-system", "zentra-premium.css"), "utf8");
  const body = css.match(/\.fact-context-panel__body\s*\{([^}]*)\}/g)?.at(-1) ?? "";
  assert.match(body, /grid-template-columns:\s*minmax\(0,\s*1fr\)/, "the generic dl auto-fit grid must not lay the rows side by side");
  const row = css.match(/\.fact-context-panel__row\s*\{([^}]*)\}/g)?.at(-1) ?? "";
  assert.match(row, /grid-template-columns:\s*minmax\(96px,\s*38%\)\s*minmax\(0,\s*1fr\)/);
});
