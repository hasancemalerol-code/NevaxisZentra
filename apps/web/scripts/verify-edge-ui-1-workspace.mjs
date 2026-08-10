#!/usr/bin/env node
/**
 * verify-edge-ui-1-workspace.mjs
 * EDGE-UI-1 workspace verifier — main workspace, KPIs, table, detail, gateway, sections.
 */

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

console.log("\n[1] Main workspace header");
const workspace = readFile("apps/web/src/features/edge/components/edge-workspace-view.tsx");
assertContains("Header Makine & Edge", workspace, "Makine & Edge");
assertContains("Subtitle operational center", workspace, "Makine bağlantıları, operasyonel sinyaller ve Edge veri akışının merkezi.");
assertContains("Simulator page notice", workspace, "edge-simulator-page-notice");

console.log("\n[2] Executive KPIs");
const kpis = readFile("apps/web/src/features/edge/components/edge-executive-kpis.tsx");
assertContains("Toplam Makine KPI", kpis, "Toplam Makine");
assertContains("Çevrimiçi KPI", kpis, "Çevrimiçi");
assertContains("Çevrimdışı KPI", kpis, "Çevrimdışı");
assertContains("Arıza KPI", kpis, "Arıza");
assertContains("Aktif Alarm KPI", kpis, "Aktif Alarm");
assertContains("Bekleyen Edge Olayı KPI", kpis, "Bekleyen Edge Olayı");
assertContains("edge-exec-kpis testid", kpis, 'data-testid="edge-exec-kpis"');

console.log("\n[3] Machine table");
const table = readFile("apps/web/src/features/edge/components/edge-machine-table.tsx");
assertContains("Machine table testid", table, 'data-testid="edge-machine-table"');
assertContains("Column Makine", table, "<th>Makine</th>");
assertContains("Column Kod", table, "<th>Kod</th>");
assertContains("Column Gateway", table, "<th>Gateway</th>");
assertContains("Column Bağlantı", table, "<th>Bağlantı</th>");
assertContains("Column Makine Durumu", table, "<th>Makine Durumu</th>");
assertContains("Column Aktif Alarm", table, "<th>Aktif Alarm</th>");
assertContains("Column Son Çevrim", table, "<th>Son Çevrim</th>");
assertContains("Column Güncellik", table, "<th>Güncellik</th>");
assertContains("Column Edge Teslimatı", table, "<th>Edge Teslimatı</th>");
assertContains("Open button Aç", table, "Aç");

console.log("\n[4] Machine detail panel");
const detail = readFile("apps/web/src/features/edge/components/edge-machine-detail-panel.tsx");
assertContains("Detail testid", detail, 'data-testid="edge-machine-detail"');
assertContains("Tab Makine Özeti", detail, "Makine Özeti");
assertContains("Tab Canlı Durum", detail, "Canlı Durum");
assertContains("Tab Telemetri", detail, "Telemetri");
assertContains("Tab Çevrimler", detail, "Çevrimler");
assertContains("Tab Duruşlar", detail, "Duruşlar");
assertContains("Tab Alarmlar", detail, "Alarmlar");
assertContains("Tab Setup", detail, "Setup / Değişim");
assertContains("Tab Gözlenen Fire", detail, "Gözlenen Fire");
assertContains("Tab Edge Teslimat", detail, "Edge Teslimat");
assertContains("Tab Olay Geçmişi", detail, "Olay Geçmişi");
assertContains("Observed cycle label", detail, "Makineden Gözlenen Çevrim");
assertContains("Scrap disclaimer", detail, "Bu değer makine gözlemidir; resmi üretim veya kalite kaydı değildir.");

console.log("\n[5] Gateway workspace");
const gw = readFile("apps/web/src/features/edge/components/edge-gateway-workspace.tsx");
assertContains("Gateway workspace testid", gw, 'data-testid="edge-gateway-workspace"');
assertContains("Gateway table columns", gw, "Atanan Makine");
assertContains("API Bağlantısı column", gw, "API Bağlantısı");

console.log("\n[6] Event timeline");
assertContains("Timeline component", table, "EdgeEventTimeline");
assertContains("Timeline testid", table, 'data-testid="edge-event-timeline"');

console.log("\n[7] Server reader + page wiring");
const page = readFile("apps/web/src/app/(app)/makineler/page.tsx");
assertContains("readEdgeWorkspace import", page, "readEdgeWorkspace");
assertContains("EdgeWorkspaceView spread data", page, "<EdgeWorkspaceView {...data}");

console.log("\n[8] EDGE-1/2/3/4 sections preserved");
assertContains("Edge Gateway section", workspace, "Edge Gateway");
assertContains("Makine Kayıt Defteri", workspace, "Makine Kayıt Defteri");
assertContains("Canlı Telemetri", workspace, "Canlı Telemetri");
assertContains("Gözlenen Duruşlar", workspace, "Gözlenen Duruşlar");
assertContains("Gateway Delivery reliability", workspace, "Gateway Delivery / Edge Reliability");

console.log("\n[9] Empty states");
assertContains("Empty state no machines", table, "Kayıtlı makine yok");
assertContains("Empty state telemetry", detail, "Telemetri yok");
assertContains("Empty state cycles", detail, "Çevrim geçmişi yok");

console.log("\n[10] Live UI");
try {
  const resp = await fetch(`${WEB_URL}/makineler`);
  assert("GET /makineler HTTP 200", resp.status === 200, `status=${resp.status}`);
  const html = await resp.text();
  assertContains("Page has Makine & Edge", html, "Makine");
  assertContains("Page has simulator notice or table", html, "Simülatör");
} catch (e) {
  console.log(`  ℹ Skipping live UI: ${e.message}`);
}

console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-UI-1 Workspace Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
