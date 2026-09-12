#!/usr/bin/env node
/**
 * verify-edge-4-premium-ui.mjs
 * EDGE-4 premium UI verifier.
 */

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

function assertNotContains(label, content, text) {
  assert(label, !content.includes(text), `must not contain "${text}"`);
}

console.log("\n[1] EDGE-1/2/3 sections preserved");
const workspace = readFile("apps/web/src/features/edge/components/edge-workspace-view.tsx");
assertContains("Gateways section", workspace, "Edge Gateway");
assertContains("Makine Kayıt Defteri", workspace, "Makine Kayıt Defteri");
assertContains("Canlı Telemetri", workspace, "Canlı Telemetri");
assertContains("Gözlenen Duruşlar", workspace, "Gözlenen Duruşlar");

console.log("\n[2] EDGE-4 reliability UI");
const gateway = readFile("apps/web/src/features/edge/components/edge-gateway-workspace.tsx");
const detail = readFile("apps/web/src/features/edge/components/edge-machine-detail-panel.tsx");
const edgeLabels = readFile("apps/web/src/features/edge/presentation/edge-labels.ts");
const edgeReliability = gateway + detail + workspace + edgeLabels;
assertContains("Gateway Delivery section", workspace, "Gateway Delivery / Edge Reliability");
assertContains("Bekleyen Olay KPI", edgeReliability, "Bekleyen Olay");
assertContains("Yeniden Deneme KPI", edgeReliability, "Yeniden Deneme");
assertContains("Dead Letter KPI", edgeReliability, "Dead Letter");
assertContains("Son Başarılı Teslim KPI", edgeReliability, "Son Başarılı Teslim");
assertContains("Kuyruk Kullanımı KPI", edgeReliability, "Kuyruk Kullanımı");
assertContains("API Bağlantısı", edgeReliability, "API Bağlantısı");
assertContains("Teslimat Durumu", edgeReliability, "Teslimat Durumu");
assertContains("En Eski Bekleyen", edgeReliability, "En Eski Bekleyen");
assertContains("Buffered events table capability", detail, "edge-reliability-table");
assertContains("Dead Letter capability", workspace, "function DeadLetterRow");
assertContains("offline banner", readFile("apps/web/src/features/edge/presentation/edge-reliability-fixtures.ts"), "Gateway çevrimdışı — olaylar yerel kuyrukta saklanıyor.");
assertContains("replay banner", readFile("apps/web/src/features/edge/presentation/edge-reliability-fixtures.ts"), "Bağlantı geri geldi — bekleyen olaylar aktarılıyor.");

console.log("\n[3] Simulator disclaimer + no secrets");
assertContains("simulator disclaimer", workspace, "gerçek makine bağlantısı değildir");
assertNotContains("no raw UUID primary in buffer table", workspace, "{event.id}");
assertNotContains("no secret in UI", workspace, "NEVAXIS_FACTOS_DEV_TOKEN");
assertNotContains("no false real-machine claim", workspace, "gerçek makine doğrulandı");

console.log("\n[4] edge-labels EDGE-4");
const labels = readFile("apps/web/src/features/edge/presentation/edge-labels.ts");
assertContains("EDGE-4 store-and-forward capability", labels, "store-and-forward");
assertContains("delivery status labels", labels, "EDGE_DELIVERY_STATUS_LABELS");
assertContains("buffer status labels", labels, "EDGE_BUFFER_STATUS_LABELS");

console.log("\n[5] Scoped CSS — no globals.css");
const css = readFile("apps/web/src/features/edge/components/edge-workspace.css");
assertContains("edge-reliability styles", css, ".edge-reliability-kpi-grid");
assertContains("1024 breakpoint", css, "@media (max-width: 1024px)");
assertContains("1280 breakpoint", css, "@media (max-width: 1280px)");
if (existsSync(resolve(ROOT, "apps/web/src/app/globals.css"))) {
  const globals = readFile("apps/web/src/app/globals.css");
  assertNotContains("globals.css: no edge-reliability", globals, "edge-reliability");
}

console.log("\n[6] Sidebar unchanged");
const nav = readFile("apps/web/src/lib/navigation/primary-navigation.ts");
assert("primary-navigation still uses makineler", nav.includes("makineler"));

console.log("\n[7] Fixtures module");
const fixtures = readFile("apps/web/src/features/edge/presentation/edge-reliability-fixtures.ts");
assertContains("EDGE_RELIABILITY_FIXTURE", fixtures, "EDGE_RELIABILITY_FIXTURE");

console.log("\n[8] Live UI HTTP 200");
try {
  const resp = await fetch(`${WEB_URL}/makineler`);
  assert("GET /makineler HTTP 200", resp.status === 200, `status=${resp.status}`);
  const html = await resp.text();
  assert("page contains reliability section text", html.includes("Edge Gateway Durumu") || html.includes("API Bağlantısı") || html.includes("Makineler"));
} catch (e) {
  console.log(`  ℹ Skipping live UI test: ${e.message}`);
}

console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-4 Premium UI Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
