#!/usr/bin/env node
/** verify-edge-ui-polish-1-hierarchy.mjs */

import { readFileSync, existsSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB_URL = process.env.NEVAXIS_FACTOS_WEB_URL ?? "http://localhost:3001";

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

function indexOfOrEnd(content, needle) {
  const i = content.indexOf(needle);
  return i === -1 ? content.length : i;
}

console.log("\n[1] Landing hierarchy order");
const view = readFile("apps/web/src/features/edge/components/edge-workspace-view.tsx");
const kpiPos = view.indexOf("<EdgeExecutiveKpis");
const attentionPos = view.indexOf("<EdgeOperationalAttention");
const tablePos = view.indexOf("<EdgeMachineTable");
const gatewayPos = view.indexOf("Edge Gateway Durumu");
const timelinePos = view.indexOf("<EdgeEventTimeline");

assert("KPI before attention", kpiPos < attentionPos && kpiPos >= 0);
assert("Attention before machine table", attentionPos < tablePos && attentionPos >= 0);
assert("Machine table before gateway section", tablePos < gatewayPos);
assert("Gateway before timeline", gatewayPos < timelinePos);

console.log("\n[2] Operational attention");
const attention = readFile("apps/web/src/features/edge/components/edge-operational-attention.tsx");
assertContains("Operasyonel Dikkat title", attention, "Operasyonel Dikkat");
assertContains("Empty attention message", attention, "Şu anda kritik operasyonel uyarı bulunmuyor.");
assertContains("Attention testid", attention, "edge-operational-attention");

console.log("\n[3] Gateway reliability panel");
const gw = readFile("apps/web/src/features/edge/components/edge-gateway-workspace.tsx");
assertContains("Gateway panel", gw, "edge-gateway-panel");
assertContains("Gateway summary", gw, "edge-gateway-summary");
assertContains("Structured delivery meta", gw, "edge-gateway-reliability-meta");

console.log("\n[4] Compact timeline");
const table = readFile("apps/web/src/features/edge/components/edge-machine-table.tsx");
assertContains("Timeline compact class", table, "edge-timeline--compact");
assertContains("Timeline limit 8", table, "TIMELINE_LIMIT = 8");

console.log("\n[5] Machine detail tabs");
const detail = readFile("apps/web/src/features/edge/components/edge-machine-detail-panel.tsx");
assertContains("Özet tab", detail, '"Özet"');
assertContains("Duruş & Alarm tab", detail, "Duruş & Alarm");
assertContains("Edge Sağlığı tab", detail, "Edge Sağlığı");

console.log("\n[6] Live HTTP");
try {
  const resp = await fetch(`${WEB_URL}/makineler`);
  assert("GET /makineler HTTP 200", resp.status === 200, `status=${resp.status}`);
} catch (e) {
  console.log(`  ℹ Skipping live HTTP: ${e.message}`);
}

console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-UI-POLISH-1 Hierarchy Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
