#!/usr/bin/env node
/**
 * verify-edge-3-premium-ui.mjs
 * EDGE-3 premium UI verifier.
 * Proves: EDGE-1/2 sections preserved, observed downtime/alarms/setup/scrap UI,
 * simulator disclaimer, observation terminology, no OEE, no raw UUID primary display,
 * globals.css untouched for edge-ops, sidebar unchanged, responsive breakpoints,
 * edge-labels alarm severity + EDGE-3 capabilities.
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

// ---------------------------------------------------------------------------
// 1. EDGE-1 / EDGE-2 preserved
// ---------------------------------------------------------------------------
console.log("\n[1] EDGE-1/2 sections preserved");
const workspace = readFile("apps/web/src/features/edge/components/edge-workspace-view.tsx");

assertContains("Gateways section", workspace, "Edge Gateway");
assertContains("Makine Kayıt Defteri", workspace, "Makine Kayıt Defteri");
assertContains("Canlı Telemetri", workspace, "Canlı Telemetri");
assertContains("Çevrim Bilgileri", workspace, "Çevrim Bilgileri");
assertContains("Makine Sinyalleri", workspace, "Makine Sinyalleri");

// ---------------------------------------------------------------------------
// 2. EDGE-3 operational sections
// ---------------------------------------------------------------------------
console.log("\n[2] EDGE-3 observed operational sections");
const detail = readFile("apps/web/src/features/edge/components/edge-machine-detail-panel.tsx");
assertContains("Gözlenen Duruşlar", detail, "Gözlenen Duruşlar");
assertContains("Aktif Alarmlar", detail, "Aktif Alarmlar");
assertContains("Alarm Geçmişi", detail, "Alarm Geçmişi");
assertContains("Gözlenen Setup / Değişimler", detail, "Gözlenen Setup / Değişimler");
assertContains("Makineden Gözlenen Fire", detail, "Makineden Gözlenen Fire");

// ---------------------------------------------------------------------------
// 3. Simulator disclaimer
// ---------------------------------------------------------------------------
console.log("\n[3] Simulator disclaimer");
assertContains(
  "simulator disclaimer gerçek makine bağlantısı değildir",
  workspace,
  "gerçek makine bağlantısı değildir",
);

// ---------------------------------------------------------------------------
// 4. Observation terminology
// ---------------------------------------------------------------------------
console.log("\n[4] Observation terminology");
assertContains("Gözlenen terminology", workspace, "Gözlenen");
assertContains("makine gözlemidir", detail, "makine gözlemidir");
assertContains("resmi üretim/kalite", detail, "resmi üretim veya kalite kaydı değildir");

// ---------------------------------------------------------------------------
// 5. No OEE + no raw UUID primary display
// ---------------------------------------------------------------------------
console.log("\n[5] No OEE / no raw {episode.id} or {scrap.id}");
assertNotContains("workspace: no OEE string", workspace, "OEE");
assertNotContains("no raw {episode.id} primary display", workspace, "{episode.id}");
assertNotContains("no raw {scrap.id} primary display", workspace, "{scrap.id}");

// ---------------------------------------------------------------------------
// 6. globals.css untouched for edge-ops / edge-downtime
// ---------------------------------------------------------------------------
console.log("\n[6] globals.css has no edge-ops / edge-downtime");
const globalsPath = "apps/web/src/app/globals.css";
if (existsSync(resolve(ROOT, globalsPath))) {
  const globals = readFile(globalsPath);
  assertNotContains("globals.css: no edge-ops", globals, "edge-ops");
  assertNotContains("globals.css: no edge-downtime", globals, "edge-downtime");
} else {
  assert("globals.css present", false, "file missing");
}

const css = readFile("apps/web/src/features/edge/components/edge-workspace.css");
assertContains("scoped edge-ops styles in workspace CSS", css, ".edge-ops-summary-grid");
assertContains("scoped edge-ops table styles", css, ".edge-ops-table");

// ---------------------------------------------------------------------------
// 7. Sidebar unchanged
// ---------------------------------------------------------------------------
console.log("\n[7] primary-navigation still has makineler");
const primaryNav = readFile("apps/web/src/lib/navigation/primary-navigation.ts");
assert(
  "primary-navigation.ts: uses existing makineler route",
  primaryNav.includes("makineler"),
  "expected existing 'makineler' route",
);

// ---------------------------------------------------------------------------
// 8. Responsive breakpoints
// ---------------------------------------------------------------------------
console.log("\n[8] CSS 1024 and 1280 breakpoints");
assertContains("1024 breakpoint", css, "@media (max-width: 1024px)");
assertContains("1280 breakpoint", css, "@media (max-width: 1280px)");

// ---------------------------------------------------------------------------
// 9. edge-labels alarm severity + EDGE-3 capabilities
// ---------------------------------------------------------------------------
console.log("\n[9] edge-labels alarm severity + EDGE-3 capabilities");
const labelsFile = readFile("apps/web/src/features/edge/presentation/edge-labels.ts");
assertContains("ALARM_SEVERITY_LABELS", labelsFile, "ALARM_SEVERITY_LABELS");
assertContains("info severity label", labelsFile, "info:");
assertContains("warning severity label", labelsFile, "warning:");
assertContains("error severity label", labelsFile, "error:");
assertContains("critical severity label", labelsFile, "critical:");
assertContains("EDGE_CAPABILITIES", labelsFile, "EDGE_CAPABILITIES");
assertContains("EDGE-3 downtime capability", labelsFile, "Gözlenen duruş (machine.downtime.started / ended)");
assertContains("EDGE-3 alarm capability", labelsFile, "Alarm gözlemi (machine.alarm.raised / cleared)");
assertContains("EDGE-3 setup capability", labelsFile, "Gözlenen setup / değişim");
assertContains("EDGE-3 scrap capability", labelsFile, "Makineden gözlenen fire (machine.scrap.observed)");
assertContains("getAlarmSeverityLabel export", labelsFile, "getAlarmSeverityLabel");
assertContains("getAlarmSeverityClass export", labelsFile, "getAlarmSeverityClass");

// ---------------------------------------------------------------------------
// Summary
// ---------------------------------------------------------------------------
console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-3 Premium UI Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
