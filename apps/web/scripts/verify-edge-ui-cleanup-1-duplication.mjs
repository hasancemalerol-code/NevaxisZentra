#!/usr/bin/env node
/** verify-edge-ui-cleanup-1-duplication.mjs */

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

console.log("\n[1] Single primary KPI strip");
const view = readFile("apps/web/src/features/edge/components/edge-workspace-view.tsx");
const kpis = readFile("apps/web/src/features/edge/components/edge-executive-kpis.tsx");
const execCount = (view.match(/<EdgeExecutiveKpis/g) ?? []).length;
assert("One EdgeExecutiveKpis on landing", execCount === 1, `count=${execCount}`);
assertContains("Executive KPI testid", kpis, 'data-testid="edge-exec-kpis"');
assertNotContains("No legacy edge-kpi-grid on landing", view, '<div className="edge-kpi-grid">');

console.log("\n[2] Legacy duplicate KPI labels removed from landing");
assertNotContains("No Kayıtlı Makine legacy KPI", view, 'edge-kpi__label">Kayıtlı Makine');
assertNotContains("No Üretime Hazır legacy KPI", view, 'edge-kpi__label">Üretime Hazır');
assertNotContains("No legacy Edge Gateway KPI row", view, 'edge-kpi__label">Edge Gateway');
assertNotContains("No legacy Son Olaylar KPI row", view, 'edge-kpi__label">Son Olaylar');

console.log("\n[3] Legacy gateway / event blocks removed");
assertNotContains("No Edge Gateway Kayıtları landing block", view, "Edge Gateway Kayıtları");
assertNotContains("No Makine Kayıt Defteri card grid on landing", view, "<MachineCard");
assertNotContains("No legacy MachineEventRow list on landing", view, "<MachineEventRow");
assertNotContains("No legacy edge-events-list on landing", view, 'className="edge-events-list"');

console.log("\n[4] Premium landing sections preserved");
assertContains("Premium gateway section title", view, "Edge Gateway Durumu");
assertContains("Premium recent events title", view, "Son Operasyonel Olaylar");
assertContains("Premium machine table", view, "<EdgeMachineTable");
assertContains("Operational attention", view, "<EdgeOperationalAttention");

console.log("\n[5] Machine detail capability intact");
const detail = readFile("apps/web/src/features/edge/components/edge-machine-detail-panel.tsx");
assertContains("Detail Telemetri tab", detail, '"Telemetri"');
assertContains("Detail Çevrimler tab", detail, '"Çevrimler"');
assertContains("Detail Duruş & Alarm tab", detail, "Duruş & Alarm");
assertContains("Detail Setup & Fire tab", detail, "Setup & Fire");
assertContains("Detail Edge Sağlığı tab", detail, "Edge Sağlığı");
assertContains("Detail Olaylar tab", detail, '"Olaylar"');
assertContains("Gözlenen Duruşlar in detail", detail, "Gözlenen Duruşlar");
assertContains("Scrap disclaimer in detail", detail, "Bu değer makine gözlemidir");

console.log("\n[6] EDGE capability anchors retained in code");
assertContains("GatewayCard component retained", view, "function GatewayCard");
assertContains("TelemetryCard component retained", view, "function TelemetryCard");
assertContains("Capability anchors comment", view, "Makine Kayıt Defteri");

console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-UI-CLEANUP-1 Duplication Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
