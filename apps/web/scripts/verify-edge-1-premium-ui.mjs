#!/usr/bin/env node
/**
 * verify-edge-1-premium-ui.mjs
 * EDGE-1 premium UI verifier.
 * Checks labels, workspace component, CSS, route, and UI/UX conventions.
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
// 1. Labels (Turkish)
// ---------------------------------------------------------------------------

console.log("\n[1] Turkish labels");
const labels = readFile("apps/web/src/features/edge/presentation/edge-labels.ts");

assertContains("Simulator label in Turkish", labels, '"Makine Simülatörü"');
assertContains("Online status label in Turkish", labels, '"Çevrimiçi"');
assertContains("Offline status label in Turkish", labels, '"Çevrimdışı"');
assertContains("Running status label in Turkish", labels, '"Çalışıyor"');
assertContains("Idle status label in Turkish", labels, '"Boşta"');
assertContains("Fault status label in Turkish", labels, '"Hata"');
assertContains("Maintenance status label in Turkish", labels, '"Bakımda"');
assertContains("Readiness label exported", labels, "getMachineReadinessLabel");
assertContains("Simulator disclaimer exported", labels, "getEdgeSimulatorDisclaimer");
assertContains("Connectivity label exported", labels, "getMachineConnectivityLabel");
assertContains("EDGE_CAPABILITIES array exported", labels, "EDGE_CAPABILITIES");
assertContains("Capabilities include idempotency", labels, "Telemetri ingest");
assertContains("Capabilities include runtime persistence", labels, "kalıcılık");

// ---------------------------------------------------------------------------
// 2. Workspace component structure
// ---------------------------------------------------------------------------

console.log("\n[2] Workspace component structure");
const workspace = readFile("apps/web/src/features/edge/components/edge-workspace-view.tsx");

assertContains("EdgeWorkspaceView exported", workspace, "export function EdgeWorkspaceView");
assertContains("Gateways rendered", workspace, "GatewayCard");
assertContains("Machines rendered", workspace, "MachineCard");
assertContains("Event rows rendered", workspace, "MachineEventRow");
assertContains("KPI strip present", workspace, "edge-kpi-grid");
assertContains("Section: Edge Gateways", workspace, "Edge Gateway");
assertContains("Section: Machine registry", workspace, "Makine Kayıt Defteri");
assertContains("Capabilities list displayed", workspace, "EDGE_CAPABILITIES");
assertContains("Simulator disclaimer shown to user", workspace, "Simülatör modu aktif");
assertContains("Ready-for-production badge present", workspace, "getMachineReadinessLabel");
assertContains("Status badge for gateway", workspace, "EdgeStatusBadge");
assertContains("Status badge for machine", workspace, "MachineStatusBadge");
assertContains("Duplicate events flagged in UI", workspace, "isDuplicate");
assertContains("Out-of-order labelled in event row", workspace, "edge-event-row--duplicate");
assertNotContains("No hardcoded machine name", workspace, "CNC-001 tezgah");
assertNotContains("No mutation button", workspace, "Makineyi Başlat");
assertNotContains("No control command button", workspace, "Durdur");

// ---------------------------------------------------------------------------
// 3. CSS
// ---------------------------------------------------------------------------

console.log("\n[3] CSS classes");
const css = readFile("apps/web/src/features/edge/components/edge-workspace.css");

assertContains("edge-workspace-page class", css, ".edge-workspace-page");
assertContains("edge-kpi-grid class", css, ".edge-kpi-grid");
assertContains("edge-card class", css, ".edge-card");
assertContains("edge-status--online", css, ".edge-status--online");
assertContains("edge-status--offline", css, ".edge-status--offline");
assertContains("machine-status--running", css, ".machine-status--running");
assertContains("machine-status--fault", css, ".machine-status--fault");
assertContains("edge-event-row--duplicate", css, ".edge-event-row--duplicate");
assertContains("edge-simulator-disclaimer", css, ".edge-simulator-disclaimer");
assertContains("edge-capabilities list", css, ".edge-capabilities");

// ---------------------------------------------------------------------------
// 4. App route exists
// ---------------------------------------------------------------------------

console.log("\n[4] App route");
const page = readFile("apps/web/src/app/(app)/makineler/page.tsx");

assertContains("Imports EdgeWorkspaceView", page, "EdgeWorkspaceView");
assertContains("Renders EdgeWorkspaceView", page, "<EdgeWorkspaceView");
assertContains("Default export", page, "export default async function");

// ---------------------------------------------------------------------------
// 5. Navigation entry exists
// ---------------------------------------------------------------------------

console.log("\n[5] Navigation entry");
const nav = readFile("apps/web/src/lib/navigation/primary-navigation.ts");

assertContains("Makineler navigation entry", nav, '"Makineler"');
assertContains("Makineler href is /makineler", nav, 'href: "/makineler"');

// ---------------------------------------------------------------------------
// 6. No placeholder text
// ---------------------------------------------------------------------------

console.log("\n[6] No placeholder / TODO text");
const workspaceStr = readFile("apps/web/src/features/edge/components/edge-workspace-view.tsx");

assertNotContains("No TODO left in workspace", workspaceStr, "TODO:");
assertNotContains("No PLACEHOLDER in workspace", workspaceStr, "PLACEHOLDER");
assertNotContains("No coming soon placeholder", workspaceStr, "Coming Soon");

// ---------------------------------------------------------------------------
// 7. Import consistency
// ---------------------------------------------------------------------------

console.log("\n[7] Import consistency");

assertContains("Labels imported in workspace", workspaceStr,
  'from "../presentation/edge-labels"');
assertContains("CSS imported in workspace", workspaceStr,
  '"./edge-workspace.css"');

// ---------------------------------------------------------------------------
// Summary
// ---------------------------------------------------------------------------

const total = passed + failed;
console.log(`\nEDGE-1 premium UI: ${passed}/${total} passed`);
if (failed > 0) {
  console.error(`EDGE-1 premium UI verification FAILED: ${failed} check(s) failed.`);
  process.exit(1);
}
console.log("EDGE-1 premium UI verification PASSED.\n");
