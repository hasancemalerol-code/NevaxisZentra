#!/usr/bin/env node
/** verify-edge-ui-final-cleanup.mjs */

import { readFileSync, existsSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB_URL = process.env.CREVAFACTOS_WEB_URL ?? "http://localhost:3001";

function readFile(rel) {
  const abs = resolve(ROOT, rel);
  if (!existsSync(abs)) throw new Error(`File not found: ${rel}`);
  return readFileSync(abs, "utf8");
}

function readAllEdgeUi() {
  const files = [
    "apps/web/src/features/edge/components/edge-workspace-view.tsx",
    "apps/web/src/features/edge/components/edge-executive-kpis.tsx",
    "apps/web/src/features/edge/components/edge-machine-table.tsx",
    "apps/web/src/features/edge/components/edge-operational-attention.tsx",
    "apps/web/src/features/edge/components/edge-gateway-workspace.tsx",
    "apps/web/src/features/edge/components/edge-machine-detail-panel.tsx",
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

console.log("\n[1] Premium landing composition");
const view = readFile("apps/web/src/features/edge/components/edge-workspace-view.tsx");
assertContains("Makine & Edge header", view, "Makine & Edge");
assertContains("Executive KPI strip", view, "<EdgeExecutiveKpis");
assertContains("Operasyonel Dikkat", view, "<EdgeOperationalAttention");
assertContains("Makine Operasyonları", view, "Makine Operasyonları");
assertContains("Edge Gateway Durumu", view, "Edge Gateway Durumu");
assertContains("Son Operasyonel Olaylar", view, "Son Operasyonel Olaylar");

console.log("\n[2] Legacy capability block removed from landing");
assertNotContains("No Yetenekleri heading", view, "Yetenekleri");
assertNotContains("No edge-capabilities list on landing", view, 'className="edge-capabilities"');
assertNotContains("No EDGE_CAPABILITIES map on landing", view, "EDGE_CAPABILITIES.map");
assertNotContains("No landing footer capability section", view, "edge-workspace-landing-footer");

console.log("\n[3] No visible EDGE package labels on landing JSX");
const landingJsx = view.slice(view.indexOf("return ("), view.lastIndexOf(");"));
assertNotContains("No EDGE-1 on landing", landingJsx, ">EDGE-1");
assertNotContains("No EDGE-2 on landing", landingJsx, ">EDGE-2");
assertNotContains("No EDGE-3 on landing", landingJsx, ">EDGE-3");
assertNotContains("No EDGE-4 on landing", landingJsx, ">EDGE-4");

console.log("\n[4] Machine detail + honesty locks");
const detail = readFile("apps/web/src/features/edge/components/edge-machine-detail-panel.tsx");
const all = readAllEdgeUi();
assertContains("Detail Telemetri tab", detail, '"Telemetri"');
assertContains("Detail Edge Sağlığı tab", detail, "Edge Sağlığı");
assertContains("Detail Olaylar tab", detail, '"Olaylar"');
assertContains("Simulator disclaimer on landing", view, "Simülatör verisi — gerçek makine bağlantısı değildir.");
assertNotContains("No OEE in UI", all, "OEE");
assertNotContains("No Makineyi Başlat", all, "Makineyi Başlat");
assertNotContains("No visible >running<", all, ">running<");
assertNotContains("No visible >fault<", all, ">fault<");

console.log("\n[5] Capability data retained internally");
const labels = readFile("apps/web/src/features/edge/presentation/edge-labels.ts");
assertContains("EDGE_CAPABILITIES in labels", labels, "export const EDGE_CAPABILITIES");

console.log("\n[6] Live HTTP");
try {
  const resp = await fetch(`${WEB_URL}/makineler`);
  assert("GET /makineler HTTP 200", resp.status === 200, `status=${resp.status}`);
  const html = await resp.text();
  assertNotContains("No Yetenekleri in live HTML", html, "Yetenekleri");
  assertNotContains("No EDGE-1 package label in live HTML", html, "EDGE-1 + EDGE-2");
  assertContains("Live page has Makine & Edge", html, "Makine");
  assertContains("Live simulator notice", html, "Simülatör");
} catch (e) {
  console.log(`  ℹ Skipping live HTTP: ${e.message}`);
}

console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-UI-FINAL-CLEANUP Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
