#!/usr/bin/env node
/**
 * verify-edge-ui-1-density.mjs
 * EDGE-UI-1 density verifier — responsive breakpoints, compact KPIs, scrollable tables.
 */

import { readFileSync, existsSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function readFile(rel) {
  const abs = resolve(ROOT, rel);
  if (!existsSync(abs)) throw new Error(`File not found: ${rel}`);
  return readFileSync(abs, "utf8");
}

let passed = 0;
let failed = 0;

function assert(label, condition, detail = "") {
  if (condition) {
    console.log(`  ✓ ${label}`);
    passed++;
  } else {
    console.error(`  ✗ ${label}${detail ? `: ${detail}` : ""}`);
    failed++;
  }
}

function assertContains(label, content, text) {
  assert(label, content.includes(text), `expected "${text}"`);
}

function assertNotContains(label, content, text) {
  assert(label, !content.includes(text), `must not contain "${text}"`);
}

console.log("\n[1] Responsive breakpoints in CSS");
const css = readFile("apps/web/src/features/edge/components/edge-workspace.css");
assertContains("1600/1440 exec grid", css, ".edge-exec-kpi-grid");
assertContains("1280 breakpoint", css, "@media (max-width: 1280px)");
assertContains("1024 breakpoint", css, "@media (max-width: 1024px)");
assertContains("1440 breakpoint", css, "@media (max-width: 1440px)");

console.log("\n[2] Compact KPI height");
const kpiBlock = css.match(/\.edge-exec-kpi\s*\{[^}]+\}/)?.[0] ?? "";
assert("Exec KPI min-height <= 60px", /min-height:\s*56px/.test(kpiBlock));
assertNotContains("No giant KPI min-height 120", css, "min-height: 120px");
assertNotContains("No giant KPI min-height 200", css, "min-height: 200px");

console.log("\n[3] Header density");
const headerBlock = css.match(/\.edge-workspace-header h1\s*\{[^}]+\}/)?.[0] ?? "";
assertContains("Header uses clamp not huge px", headerBlock, "clamp(");
assertNotContains("No 3rem header", css, "font-size: 3rem");

console.log("\n[4] Table overflow handling");
assertContains("Machine table horizontal scroll", css, ".edge-machine-table-wrap");
assertContains("Machine table min-width", css, "min-width: 960px");
assertContains("Gateway table scroll", css, ".edge-gateway-table-wrap");
assertContains("Telemetry table scroll", css, ".edge-telemetry-table-wrap");

console.log("\n[5] No duplicate giant sections");
const workspace = readFile("apps/web/src/features/edge/components/edge-workspace-view.tsx");
const execKpiCount = (workspace.match(/<EdgeExecutiveKpis/g) ?? []).length;
assert("Single EdgeExecutiveKpis instance", execKpiCount === 1, `count=${execKpiCount}`);

console.log("\n[6] Detail tabs wrap");
assertContains("Detail tabs flex-wrap", css, ".edge-detail-tabs");
assertContains("Detail tab compact font", css, "font-size: 11px");

console.log("\n[7] globals.css untouched");
if (existsSync(resolve(ROOT, "apps/web/src/app/globals.css"))) {
  const globals = readFile("apps/web/src/app/globals.css");
  assertNotContains("No edge-exec-kpi in globals", globals, "edge-exec-kpi");
  assertNotContains("No edge-detail-panel in globals", globals, "edge-detail-panel");
}

console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-UI-1 Density Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
