#!/usr/bin/env node
/** verify-edge-ui-polish-1-density.mjs */

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

console.log("\n[1] Responsive breakpoints");
const css = readFile("apps/web/src/features/edge/components/edge-workspace.css");
assertContains("1600 KPI grid", css, "repeat(6, minmax(0, 1fr))");
assertContains("1440 breakpoint", css, "@media (max-width: 1440px)");
assertContains("1280 breakpoint", css, "@media (max-width: 1280px)");
assertContains("1024 breakpoint", css, "@media (max-width: 1024px)");

console.log("\n[2] KPI compact height");
const kpiBlock = css.match(/\.edge-exec-kpi\s*\{[^}]+\}/)?.[0] ?? "";
assert("KPI min-height 72px", /min-height:\s*72px/.test(kpiBlock));
assert("KPI max-height 88px", /max-height:\s*88px/.test(kpiBlock));
assertNotContains("No 120px KPI", css, "min-height: 120px");

console.log("\n[3] Timeline row compact");
assertContains("Timeline row min-height 28px", css, "min-height: 28px");
assertNotContains("No 320px timeline on landing", css.match(/\.edge-timeline--compact[\s\S]*?max-height:\s*320px/)?.[0] ?? "none");

console.log("\n[4] No duplicate KPI blocks on landing");
const view = readFile("apps/web/src/features/edge/components/edge-workspace-view.tsx");
const execCount = (view.match(/<EdgeExecutiveKpis/g) ?? []).length;
assert("Single executive KPI component", execCount === 1, `count=${execCount}`);

console.log("\n[5] Table overflow");
assertContains("Machine table scroll", css, ".edge-machine-table-wrap");

console.log("\n[6] globals.css lock");
if (existsSync(resolve(ROOT, "apps/web/src/app/globals.css"))) {
  const globals = readFile("apps/web/src/app/globals.css");
  assertNotContains("No edge-attention in globals", globals, "edge-attention");
}

console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-UI-POLISH-1 Density Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
