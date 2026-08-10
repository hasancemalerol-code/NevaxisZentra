#!/usr/bin/env node
/** verify-edge-ui-polish-1-language.mjs */

import { readFileSync, existsSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function readFile(rel) {
  const abs = resolve(ROOT, rel);
  if (!existsSync(abs)) throw new Error(`File not found: ${rel}`);
  return readFileSync(abs, "utf8");
}

const UI_FILES = [
  "apps/web/src/features/edge/components/edge-workspace-view.tsx",
  "apps/web/src/features/edge/components/edge-executive-kpis.tsx",
  "apps/web/src/features/edge/components/edge-operational-attention.tsx",
  "apps/web/src/features/edge/components/edge-machine-table.tsx",
  "apps/web/src/features/edge/components/edge-gateway-workspace.tsx",
  "apps/web/src/features/edge/components/edge-machine-detail-panel.tsx",
  "apps/web/src/features/edge/presentation/edge-labels.ts",
  "apps/web/src/features/edge/presentation/edge-freshness.ts",
];

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

const all = UI_FILES.map(readFile).join("\n");
const labels = readFile("apps/web/src/features/edge/presentation/edge-labels.ts");

console.log("\n[1] Required Turkish labels");
assertContains("Bilinmiyor", labels, '"Bilinmiyor"');
assertContains("Çalışıyor", labels, '"Çalışıyor"');
assertContains("Boşta", labels, '"Boşta"');
assertContains("Durdu", labels, '"Durdu"');
assertContains("Arıza in observed labels", labels, 'fault: "Arıza"');
assertContains("getObservedMachineStateLabel", labels, "getObservedMachineStateLabel");

console.log("\n[2] Forbidden raw enum display in UI components");
const components = UI_FILES.filter((f) => f.endsWith(".tsx")).map(readFile).join("\n");
const forbidden = [
  ">unknown<",
  ">running<",
  ">idle<",
  ">stopped<",
  ">fault<",
  ">fresh<",
  ">stale<",
  '"retry-wait"',
  "retry-wait",
];
for (const word of forbidden) {
  assertNotContains(`No visible ${word}`, components, word);
}

console.log("\n[3] Simulator disclaimer");
assertContains("Simulator notice", all, "Simülatör verisi — gerçek makine bağlantısı değildir.");

console.log("\n[4] Observed terminology");
assertContains("Gözlenen fire", all, "Makineden Gözlenen Fire");
assertContains("Scrap disclaimer", all, "Bu değer makine gözlemidir");

console.log("\n[5] No forbidden claims");
assertNotContains("No OEE", all, "OEE");
assertNotContains("No predictive", all.toLowerCase(), "predictive maintenance");
assertNotContains("No real-machine validated", all.toLowerCase(), "gerçek makine doğrulandı");
assertNotContains("No Üretim Adedi", all, "Üretim Adedi");

console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-UI-POLISH-1 Language Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
