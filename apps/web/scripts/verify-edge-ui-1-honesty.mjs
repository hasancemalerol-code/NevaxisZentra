#!/usr/bin/env node
/**
 * verify-edge-ui-1-honesty.mjs
 * EDGE-UI-1 honesty verifier — observed terminology, no fake claims, no secrets.
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

function readAllEdgeUiFiles() {
  const files = [
    "apps/web/src/features/edge/components/edge-workspace-view.tsx",
    "apps/web/src/features/edge/components/edge-executive-kpis.tsx",
    "apps/web/src/features/edge/components/edge-machine-table.tsx",
    "apps/web/src/features/edge/components/edge-machine-detail-panel.tsx",
    "apps/web/src/features/edge/components/edge-gateway-workspace.tsx",
    "apps/web/src/features/edge/presentation/edge-labels.ts",
  ];
  return files.map((f) => readFile(f)).join("\n");
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

const all = readAllEdgeUiFiles();
const workspace = readFile("apps/web/src/features/edge/components/edge-workspace-view.tsx");
const detail = readFile("apps/web/src/features/edge/components/edge-machine-detail-panel.tsx");

console.log("\n[1] Simulator disclaimer");
assertContains("Page-level simulator notice", workspace, "Simülatör verisi — gerçek makine bağlantısı değildir.");
assertContains("Simulator disclaimer exported", readFile("apps/web/src/features/edge/presentation/edge-labels.ts"), "getEdgeSimulatorDisclaimer");
assertContains("Simülatör badge", all, "Simülatör");

console.log("\n[2] Observed terminology");
assertContains("Gözlenen çevrim", all, "Gözlenen");
assertContains("Makineden Gözlenen Çevrim", all, "Makineden Gözlenen Çevrim");
assertContains("Gözlenen Duruşlar", detail, "Gözlenen Duruşlar");
assertContains("Makineden Gözlenen Fire", all, "Makineden Gözlenen Fire");

console.log("\n[3] No official Production/Quality claims");
assertNotContains("No Üretim Adedi label", all, "Üretim Adedi");
assertNotContains("No resmi üretim miktarı as positive claim", detail, "resmi üretim miktarıdır");
assertNotContains("No Gerçek Makine Doğrulandı", all, "Gerçek Makine Doğrulandı");
assertNotContains("No gerçek makine doğrulandı", all, "gerçek makine doğrulandı");

console.log("\n[4] No machine control");
assertNotContains("No Makineyi Başlat", all, "Makineyi Başlat");
assertNotContains("No PLC write", all, "PLC yaz");
assertNotContains("No Durdur command button", workspace, ">Durdur<");

console.log("\n[5] No OEE / predictive");
assertNotContains("No OEE", all, "OEE");
assertNotContains("No predictive maintenance", all, "predictive");
assertNotContains("No kök neden root-cause claim", all.replace(/kök neden analizi YOK/gi, ""), "kök neden analizi");

console.log("\n[6] No secrets / credentials");
assertNotContains("No NEVAXIS_FACTOS_DEV_TOKEN in UI", all, "NEVAXIS_FACTOS_DEV_TOKEN");
assertNotContains("No password field", all, "password");
assertNotContains("No apiKey display", all, "apiKey");

console.log("\n[7] No raw UUID primary display");
assertNotContains("No bb000001 in table component", readFile("apps/web/src/features/edge/components/edge-machine-table.tsx"), "bb000001");
assertNotContains("No machine.id rendered in table", readFile("apps/web/src/features/edge/components/edge-machine-table.tsx"), "{m.id}");

console.log("\n[8] Scrap disclaimer");
assertContains("Scrap observation disclaimer", detail, "Bu değer makine gözlemidir; resmi üretim veya kalite kaydı değildir.");

console.log("\n[9] Sidebar unchanged");
const nav = readFile("apps/web/src/lib/navigation/primary-navigation.ts");
assertContains("Makineler nav preserved", nav, 'href: "/makineler"');

console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-UI-1 Honesty Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
