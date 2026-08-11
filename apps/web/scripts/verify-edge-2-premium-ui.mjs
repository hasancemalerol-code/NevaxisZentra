#!/usr/bin/env node
/**
 * verify-edge-2-premium-ui.mjs
 * EDGE-2 premium UI verifier.
 * Proves: EDGE-1 layout preserved, telemetry section, cycle section, signals,
 * state history, quality/freshness, "observed" terminology, simulator disclaimer,
 * no raw UUID, no false real-machine validation, globals.css untouched, sidebar unchanged.
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

function readFileOpt(rel) {
  const abs = resolve(ROOT, rel);
  if (!existsSync(abs)) return null;
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
// 1. EDGE-1 layout preserved
// ---------------------------------------------------------------------------
console.log("\n[1] EDGE-1 layout preserved");
const workspace = readFile("apps/web/src/features/edge/components/edge-workspace-view.tsx");
const detail = readFile("apps/web/src/features/edge/components/edge-machine-detail-panel.tsx");
const css = readFile("apps/web/src/features/edge/components/edge-workspace.css");

assertContains("Gateways section", workspace, "Edge Gateway");
assertContains("Machines section", workspace, "Makine Kayıt Defteri");
assertContains("Recent events section", workspace, "Son Olaylar");
assertContains("GatewayCard component", workspace, "GatewayCard");
assertContains("MachineCard component", workspace, "MachineCard");
assertContains("MachineEventRow component", workspace, "MachineEventRow");
assertContains("KPI grid", workspace, "<EdgeExecutiveKpis");

// ---------------------------------------------------------------------------
// 2. Canlı Telemetri section
// ---------------------------------------------------------------------------
console.log("\n[2] Canlı Telemetri section");
assertContains("telemetry section heading", workspace, "Canlı Telemetri");
assertContains("TelemetryCard component", workspace, "TelemetryCard");
assertContains("edge-telemetry-grid class", css, ".edge-telemetry-grid");
assertContains("latestTelemetry prop", workspace, "latestTelemetry");

// ---------------------------------------------------------------------------
// 3. Çevrim Bilgileri section
// ---------------------------------------------------------------------------
console.log("\n[3] Çevrim Bilgileri section");
assertContains("cycle section heading", workspace, "Çevrim Bilgileri");
assertContains("cycle projection display", workspace, "cycleProjection");
assertContains("CycleRow component", workspace, "CycleRow");
assertContains("cycle history table", workspace, "edge-cycle-table");

// ---------------------------------------------------------------------------
// 4. Makine Sinyalleri section
// ---------------------------------------------------------------------------
console.log("\n[4] Makine Sinyalleri section");
assertContains("signals section heading", workspace, "Makine Sinyalleri");
assertContains("signalProjection prop", workspace, "signalProjection");
assertContains("edge-signal-card class", css, ".edge-signal-card");

// ---------------------------------------------------------------------------
// 5. Durum Geçmişi section
// ---------------------------------------------------------------------------
console.log("\n[5] Durum Geçmişi section");
assertContains("state history heading", workspace, "Durum Geçmişi");
assertContains("StateTransitionRow component", workspace, "StateTransitionRow");
assertContains("stateTransitions prop", workspace, "stateTransitions");

// ---------------------------------------------------------------------------
// 6. Quality presentation
// ---------------------------------------------------------------------------
console.log("\n[6] Quality/freshness presentation");
assertContains("TelemetryQualityBadge component", workspace, "TelemetryQualityBadge");
assertContains("quality class applied", workspace, "getTelemetryQualityClass");
assertContains("quality label shown", workspace, "getTelemetryQualityLabel");

// ---------------------------------------------------------------------------
// 7. Observed terminology — NOT official production quantity
// ---------------------------------------------------------------------------
console.log("\n[7] Observed terminology");
assertContains("Gözlenen label used", workspace, "Gözlenen");
assertContains("Makineden Gözlenen Çevrim label", detail, "Makineden Gözlenen Çevrim");
assertContains("observed-not-production disclaimer", detail, "resmi üretim veya kalite kaydı değildir");
assertNotContains("must not use simple Üretim Adedi", workspace, '"Üretim Adedi"');

// ---------------------------------------------------------------------------
// 8. Simulator disclaimer visible
// ---------------------------------------------------------------------------
console.log("\n[8] Simulator disclaimer");
assertContains("simulator disclaimer in telemetry section", workspace, "gerçek makine bağlantısı değildir");
assertContains("simulator disclaimer at bottom", workspace, "Simülatör modu aktif");
assertNotContains("must not claim Gerçek makine doğrulandı", workspace, "Gerçek makine doğrulandı");

// ---------------------------------------------------------------------------
// 9. No raw UUID as primary display
// ---------------------------------------------------------------------------
console.log("\n[9] No raw UUID as primary UI element");
// CycleRow renders cycle identity string, not raw UUID
assertContains("cycle identity shown not raw id", workspace, "edge-cycle-identity");
assertNotContains("raw id as primary display in cycle row", workspace, "{cycle.id}");

// ---------------------------------------------------------------------------
// 10. CSS — no globals.css modification, scoped CSS used
// ---------------------------------------------------------------------------
console.log("\n[10] CSS scoped, no globals.css");

assertContains("telemetry grid class defined", css, ".edge-telemetry-grid");
assertContains("telemetry card class defined", css, ".edge-telemetry-card");
assertContains("telemetry quality good class", css, ".telemetry-quality--good");
assertContains("telemetry quality uncertain class", css, ".telemetry-quality--uncertain");
assertContains("telemetry quality bad class", css, ".telemetry-quality--bad");
assertContains("cycle grid class defined", css, ".edge-cycle-grid");
assertContains("cycle kpi class defined", css, ".edge-cycle-kpi");
assertContains("cycle table class defined", css, ".edge-cycle-table");
assertContains("signal card class defined", css, ".edge-signal-card");
assertContains("state transition row class", css, ".edge-state-row");
assertContains("1024px responsive breakpoint", css, "1024px");

// Check globals.css not modified (it should not contain edge-specific styles)
const globalsPath = "apps/web/src/app/globals.css";
if (existsSync(resolve(ROOT, globalsPath))) {
  const globals = readFile(globalsPath);
  assertNotContains("globals.css: no edge-telemetry class", globals, ".edge-telemetry");
  assertNotContains("globals.css: no telemetry-quality class", globals, ".telemetry-quality");
  assertNotContains("globals.css: no edge-cycle class", globals, ".edge-cycle");
  assertNotContains("globals.css: no edge-signal class", globals, ".edge-signal");
}

// ---------------------------------------------------------------------------
// 11. Sidebar unchanged
// ---------------------------------------------------------------------------
console.log("\n[11] Sidebar unchanged");

// Edge workspace uses existing Edge route from EDGE-1, no new sidebar entry
const labelsFile = readFile("apps/web/src/features/edge/presentation/edge-labels.ts");
assertContains("EDGE_CAPABILITIES updated for EDGE-2", labelsFile, "Yazılı telemetri örnekleri");

// Verify makineler route still exists from EDGE-1 (no new sidebar entry needed)
const primaryNav = readFile("apps/web/src/lib/navigation/primary-navigation.ts");
assert("primary-navigation.ts: uses existing Edge route", primaryNav.includes("makineler"), "expected existing 'makineler' route");

// ---------------------------------------------------------------------------
// 12. Labels defined
// ---------------------------------------------------------------------------
console.log("\n[12] Labels defined");
assertContains("telemetry quality labels", labelsFile, "TELEMETRY_QUALITY_LABELS");
assertContains("metric key labels", labelsFile, "TELEMETRY_METRIC_LABELS");
assertContains("connectivity labels", labelsFile, "CONNECTIVITY_LABELS");
assertContains("getTelemetryQualityLabel export", labelsFile, "getTelemetryQualityLabel");
assertContains("getTelemetryQualityClass export", labelsFile, "getTelemetryQualityClass");
assertContains("getMetricLabel export", labelsFile, "getMetricLabel");
assertContains("getConnectivityLabel export", labelsFile, "getConnectivityLabel");
assertContains("formatDurationMs export", labelsFile, "formatDurationMs");
assertContains("cycle-completed label", labelsFile, '"cycle-completed"');
assertContains("counter-observed label", labelsFile, '"counter-observed"');

// ---------------------------------------------------------------------------
// 13. Responsive design
// ---------------------------------------------------------------------------
console.log("\n[13] Responsive design");
assertContains("1024 breakpoint", css, "@media (max-width: 1024px)");
assertContains("1280 breakpoint", css, "@media (max-width: 1280px)");

// ---------------------------------------------------------------------------
// Summary
// ---------------------------------------------------------------------------
console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-2 Premium UI Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
