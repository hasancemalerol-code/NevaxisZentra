#!/usr/bin/env node
/**
 * verify-edge-integration-gate-boundaries.mjs
 * EDGE Integration Gate — cross-domain boundary aggregation.
 */

import { readFileSync, existsSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const SCRIPTS = resolve(ROOT, "apps/web/scripts");

function readFile(rel) {
  const abs = resolve(ROOT, rel);
  if (!existsSync(abs)) throw new Error(`File not found: ${rel}`);
  return readFileSync(abs, "utf8");
}

function readAllEdgeUi() {
  const files = [
    "apps/web/src/features/edge/components/edge-workspace-view.tsx",
    "apps/web/src/features/edge/components/edge-machine-detail-panel.tsx",
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

function assertNotContains(label, content, text) {
  assert(label, !content.includes(text), `must not contain "${text}"`);
}

function runPackageVerifier(name) {
  console.log(`\n>>> ${name}`);
  const result = spawnSync("node", [resolve(SCRIPTS, name)], {
    cwd: ROOT,
    encoding: "utf8",
    shell: true,
    env: process.env,
  });
  if (result.stdout) process.stdout.write(result.stdout);
  if (result.stderr) process.stderr.write(result.stderr);
  assert(`${name} PASS`, result.status === 0, `exit=${result.status ?? "null"}`);
}

console.log("\n[1] Domain boundary verifiers");
runPackageVerifier("verify-edge-2-boundaries.mjs");
runPackageVerifier("verify-edge-3-scrap-boundaries.mjs");

console.log("\n[2] UI honesty + final landing");
runPackageVerifier("verify-edge-ui-1-honesty.mjs");
runPackageVerifier("verify-edge-ui-final-cleanup.mjs");

console.log("\n[3] No real transport adapters");
const adapter = readFile("packages/CrevaFactOS.Persistence/Edge/IEdgeGatewayAdapter.cs");
const sim = readFile("packages/CrevaFactOS.Persistence/Edge/EdgeSimulatorAdapter.cs");
for (const [label, content] of [
  ["adapter", adapter],
  ["simulator adapter", sim],
]) {
  assertNotContains(`${label}: no MQTT`, content, "MqttClient");
  assertNotContains(`${label}: no OPC UA`, content, "OpcUa");
  assertNotContains(`${label}: no Modbus`, content, "Modbus");
}

console.log("\n[4] No OEE / AI / predictive in Edge UI");
const ui = readAllEdgeUi();
assertNotContains("UI: no OEE", ui, "OEE");
assertNotContains("UI: no predictive maintenance", ui.toLowerCase(), "predictive maintenance");
assertNotContains("UI: no machine control", ui, "Makineyi Başlat");

console.log("\n[5] Downstream mutation boundary (Edge persistence)");
const edgeSources = [
  "packages/CrevaFactOS.Persistence/Edge/EdgeIngestionService.cs",
  "packages/CrevaFactOS.Persistence/Edge/EdgeTelemetryIngestionService.cs",
  "packages/CrevaFactOS.Persistence/Edge/EdgeDowntimeIngestionService.cs",
  "packages/CrevaFactOS.Persistence/Edge/EdgeAlarmIngestionService.cs",
  "packages/CrevaFactOS.Persistence/Edge/EdgeScrapIngestionService.cs",
].filter((f) => existsSync(resolve(ROOT, f)));

for (const rel of edgeSources) {
  const src = readFile(rel);
  const name = rel.split("/").pop();
  assertNotContains(`${name}: no ProductionOrders`, src, "ProductionOrders");
  assertNotContains(`${name}: no MaintenanceWorkOrders`, src, "MaintenanceWorkOrders");
  assertNotContains(`${name}: no Quality NCR`, src, "NonConformance");
  assertNotContains(`${name}: no ERP inventory`, src, "InventoryMovement");
}

console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE Integration Gate Boundaries: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
