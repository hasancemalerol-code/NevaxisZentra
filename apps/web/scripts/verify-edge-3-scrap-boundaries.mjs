#!/usr/bin/env node
/**
 * verify-edge-3-scrap-boundaries.mjs
 * EDGE-3 scrap + ownership boundary verifier.
 * Proves: scrap observation model, Quantity>0/Unit validation, observed-only,
 * no Production/Quality/NCR/ERP/OEE mutation, no PLC packages/commands,
 * append-only migration grants, failure codes, dev/simulator, no history DELETE/PUT.
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

const EDGE3_SOURCES = [
  "packages/NevaxisZentra.Persistence/Edge/EdgeDowntimeIngestionService.cs",
  "packages/NevaxisZentra.Persistence/Edge/EdgeAlarmIngestionService.cs",
  "packages/NevaxisZentra.Persistence/Edge/EdgeSetupIngestionService.cs",
  "packages/NevaxisZentra.Persistence/Edge/EdgeScrapIngestionService.cs",
];

// ---------------------------------------------------------------------------
// 1. Scrap entity + validation
// ---------------------------------------------------------------------------
console.log("\n[1] MachineScrapObservation + Quantity/Unit validation");
const entities = readFile("packages/NevaxisZentra.Persistence/Edge/EdgeEntities.cs");
const scrapSvc = readFile("packages/NevaxisZentra.Persistence/Edge/EdgeScrapIngestionService.cs");

assertContains("MachineScrapObservation class", entities, "class MachineScrapObservation");
assertContains("Quantity field", entities, "double Quantity");
assertContains("Unit field", entities, "string Unit");
assertContains("EdgeScrapIngestionService class", scrapSvc, "class EdgeScrapIngestionService");
assertContains("IEdgeScrapIngestionService interface", scrapSvc, "interface IEdgeScrapIngestionService");
assertContains("Quantity > 0 validation", scrapSvc, "request.Quantity <= 0");
assertContains("Quantity must be > 0 message", scrapSvc, "Quantity must be > 0.");
assertContains("Unit required validation", scrapSvc, "Unit is required.");

// ---------------------------------------------------------------------------
// 2. Observed-only comments / no Production quantity mutation
// ---------------------------------------------------------------------------
console.log("\n[2] Observed-only scrap ownership");
assertContains("Observed terminology in scrap svc comment", scrapSvc, "Observed / machine-reported only");
assertContains("NOT official Production scrap note", scrapSvc, "NOT official Production scrap");
assertContains("entity NOT official Production scrap", entities, "NOT official Production scrap quantity");
assertContains("scrap svc denies NCR creation", scrapSvc, "create NCR");
assertNotContains("scrap svc: no QualityInspection", scrapSvc, "QualityInspection");
assertNotContains("scrap svc: no ErpSyncRun", scrapSvc, "ErpSyncRun");
assertNotContains("scrap svc: no ProductionOrder", scrapSvc, "ProductionOrder");
assertNotContains("scrap svc: no quantity.scrap mutation", scrapSvc, "quantity.scrap");
assertNotContains("scrap svc: no NcrDocument type", scrapSvc, "NcrDocument");
assertNotContains("scrap svc: no NonConformance type", scrapSvc, "NonConformance");

// ---------------------------------------------------------------------------
// 3. No OEE / Availability KPI in entities + EDGE-3 services
// ---------------------------------------------------------------------------
console.log("\n[3] No OEE / Availability KPI / CalculateOee");
assertNotContains("entities: no OEE", entities, "OEE");
assertNotContains("entities: no Availability KPI", entities, "Availability");
assertNotContains("entities: no CalculateOee", entities, "CalculateOee");

for (const srcPath of EDGE3_SOURCES) {
  const src = readFile(srcPath);
  const name = srcPath.split("/").pop();
  assertNotContains(`${name}: no OEE`, src, "OEE");
  assertNotContains(`${name}: no CalculateOee`, src, "CalculateOee");
  assertNotContains(`${name}: no Availability KPI`, src, "Availability");
}

// ---------------------------------------------------------------------------
// 4. No MQTT/OPC/Modbus packages
// ---------------------------------------------------------------------------
console.log("\n[4] No MQTT/OPC/Modbus packages in simulator/persistence csproj");
const simCsproj = readFileOpt("tools/NevaxisZentra.MachineSimulator/NevaxisZentra.MachineSimulator.csproj");
if (simCsproj) {
  assertNotContains("simulator csproj: no MQTTnet", simCsproj, "MQTTnet");
  assertNotContains("simulator csproj: no OPCFoundation", simCsproj, "OPCFoundation");
  assertNotContains("simulator csproj: no NModbus", simCsproj, "NModbus");
  assertNotContains("simulator csproj: no Modbus", simCsproj, "Modbus");
} else {
  assert("simulator csproj present", false, "file missing");
}

const persistCsproj = readFileOpt("packages/NevaxisZentra.Persistence/NevaxisZentra.Persistence.csproj");
if (persistCsproj) {
  assertNotContains("persistence csproj: no MQTTnet", persistCsproj, "MQTTnet");
  assertNotContains("persistence csproj: no OPCFoundation", persistCsproj, "OPCFoundation");
  assertNotContains("persistence csproj: no NModbus", persistCsproj, "NModbus");
  assertNotContains("persistence csproj: no Modbus", persistCsproj, "Modbus");
} else {
  assert("persistence csproj present", false, "file missing");
}

// ---------------------------------------------------------------------------
// 5. No MachineCommand / WriteTag / WriteCommand
// ---------------------------------------------------------------------------
console.log("\n[5] No MachineCommand / WriteTag / WriteCommand");
for (const srcPath of EDGE3_SOURCES) {
  const src = readFile(srcPath);
  const name = srcPath.split("/").pop();
  assertNotContains(`${name}: no MachineCommand`, src, "MachineCommand");
  assertNotContains(`${name}: no WriteTag`, src, "WriteTag");
  assertNotContains(`${name}: no WriteCommand`, src, "WriteCommand");
}

const api = readFile("apps/api/Edge/EdgeEndpointRouteBuilderExtensions.cs");
assertNotContains("API: no MachineCommand", api, "MachineCommand");
assertNotContains("API: no WriteTag", api, "WriteTag");
assertNotContains("API: no WriteCommand", api, "WriteCommand");

// ---------------------------------------------------------------------------
// 6. Migration append-only scrap
// ---------------------------------------------------------------------------
console.log("\n[6] Migration machine_scrap_observations append-only GRANT");
const migration = readFile(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260810200000_AddEdgeOperationalObservations.cs",
);

assertContains("machine_scrap_observations table", migration, "machine_scrap_observations");
assertContains("GRANT SELECT, INSERT on scrap", migration, "GRANT SELECT, INSERT ON TABLE machine_scrap_observations");
assertContains("RLS append-only scrap", migration, 'ApplyTenantRlsAppendOnly(migrationBuilder, "machine_scrap_observations"');
assertContains("quantity > 0 check", migration, "quantity > 0");

// ---------------------------------------------------------------------------
// 7. Failure codes + observed terminology
// ---------------------------------------------------------------------------
console.log("\n[7] Failure codes + observed terminology");
const models = readFile("packages/NevaxisZentra.Persistence/Edge/EdgeModels.cs");
assertContains("edge.scrap.invalid", models, '"edge.scrap.invalid"');
assertContains("edge.scrap.duplicate", models, '"edge.scrap.duplicate"');
assertContains("ScrapInvalid constant", models, "ScrapInvalid");
assertContains("ScrapDuplicate constant", models, "ScrapDuplicate");
assertContains("observed terminology in scrap svc", scrapSvc, "Observed");
assertContains("machine.scrap.observed event kind", entities, 'ScrapObserved = "machine.scrap.observed"');

// ---------------------------------------------------------------------------
// 8. Dev /scrap + simulator GetScrapScenario
// ---------------------------------------------------------------------------
console.log("\n[8] Dev /scrap endpoint + simulator GetScrapScenario");
assertContains("scrap endpoint", api, '"/scrap"');
assertContains("DevIngestScrapAsync", api, "DevIngestScrapAsync");
assertContains("dev guard (Development env only)", api, "env.IsDevelopment()");

const sim = readFile("tools/NevaxisZentra.MachineSimulator/MachineSimulatorScenarios.cs");
assertContains("GetScrapScenario", sim, "GetScrapScenario");

// ---------------------------------------------------------------------------
// 9. EDGE-3 services do not mutate ProductionExecutionEntry
// ---------------------------------------------------------------------------
console.log("\n[9] EDGE-3 services do not mutate ProductionExecutionEntry");
for (const srcPath of EDGE3_SOURCES) {
  const src = readFile(srcPath);
  const name = srcPath.split("/").pop();
  assertNotContains(`${name}: no ProductionExecutionEntry`, src, "ProductionExecutionEntry");
}

// ---------------------------------------------------------------------------
// 10. No history DELETE / PUT endpoints
// ---------------------------------------------------------------------------
console.log("\n[10] No MapDelete / MapPut for downtime/alarms/setups/scrap history");
assertNotContains("API: no MapDelete", api, "MapDelete");
assertNotContains("API: no MapPut", api, "MapPut");
assertContains("GET downtime history", api, '"/machines/{machineId:guid}/downtime"');
assertContains("GET alarms history", api, '"/machines/{machineId:guid}/alarms"');
assertContains("GET setups history", api, '"/machines/{machineId:guid}/setups"');
assertContains("GET scrap history", api, '"/machines/{machineId:guid}/scrap-observations"');

// ---------------------------------------------------------------------------
// Summary
// ---------------------------------------------------------------------------
console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-3 Scrap/Boundaries Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
