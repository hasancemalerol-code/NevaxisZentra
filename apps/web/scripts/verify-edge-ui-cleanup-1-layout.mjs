#!/usr/bin/env node
/** verify-edge-ui-cleanup-1-layout.mjs */

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

console.log("\n[1] Tightened executive opening");
const css = readFile("apps/web/src/features/edge/components/edge-workspace.css");
assert("Landing page gap 12px", /\.edge-workspace-page\s*\{[^}]*gap:\s*12px/.test(css));
assertContains("Executive header compact margin", css, ".edge-workspace-header--executive");
assert("Header intro gap 4px", /\.edge-header__intro\s*\{[^}]*gap:\s*4px/.test(css));
assertContains("KPI directly below header", css, ".edge-workspace-header--executive + .edge-exec-kpi-grid");

console.log("\n[2] Gateway / timeline balance");
assertContains("62/38 gateway-timeline grid", css, "1.55fr 0.95fr");
assertContains("Timeline section min-width", css, "edge-landing-section--timeline");
assertContains("Timeline kind nowrap ellipsis", css, ".edge-timeline__kind");
assertNotContains("No oversized gateway panel min-height", css.match(/\.edge-gateway-panel\s*\{[^}]+\}/)?.[0] ?? "", "min-height:");

console.log("\n[3] Compact timeline");
assertContains("Timeline compact class", css, ".edge-timeline--compact");
assertContains("Timeline row min-height 28px", css, "min-height: 28px");
const table = readFile("apps/web/src/features/edge/components/edge-machine-table.tsx");
assertContains("Timeline limit 8", table, "TIMELINE_LIMIT = 8");
assertNotContains("No landing timeline max-height scroll", css.match(/\.edge-timeline--compact[\s\S]*?max-height:\s*320px/)?.[0] ?? "none", "320px");

console.log("\n[4] Responsive breakpoints");
assertContains("1600 breakpoint", css, "@media (max-width: 1600px)");
assertContains("1440 breakpoint", css, "@media (max-width: 1440px)");
assertContains("1280 breakpoint", css, "@media (max-width: 1280px)");
assertContains("1024 breakpoint", css, "@media (max-width: 1024px)");
assertContains("1280 stack landing secondary", css.match(/@media \(max-width: 1280px\)[\s\S]*?\.edge-landing-secondary[\s\S]*?grid-template-columns: 1fr/)?.[0] ?? "", "1fr");

console.log("\n[5] Overflow controls");
assertContains("Machine table scroll wrap", css, ".edge-machine-table-wrap");
assertNotContains("No page overflow-x hidden lock on body in edge css", css, "overflow-x: hidden");

console.log("\n[6] Landing hierarchy unchanged");
const view = readFile("apps/web/src/features/edge/components/edge-workspace-view.tsx");
const kpiPos = view.indexOf("<EdgeExecutiveKpis");
const attentionPos = view.indexOf("<EdgeOperationalAttention");
const tablePos = view.indexOf("<EdgeMachineTable");
const gatewayPos = view.indexOf("Edge Gateway Durumu");
assert("KPI before attention", kpiPos < attentionPos && kpiPos >= 0);
assert("Attention before machine table", attentionPos < tablePos);
assert("Machine table before gateway", tablePos < gatewayPos);

console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-UI-CLEANUP-1 Layout Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
