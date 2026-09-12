#!/usr/bin/env node
/**
 * verify-edge-2-boundaries.mjs
 * EDGE-2 boundary verifier.
 * Proves: no Production mutation, no Planning mutation, no Maintenance mutation,
 * no Quality mutation, no ERP mutation, no OEE, no scrap workflow, no downtime workflow,
 * no alarm workflow, no machine command, no real PLC transport, no MQTT/OPC dependency.
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

function assertNotContains(label, content, text) {
  assert(label, !content.includes(text), `must not contain "${text}"`);
}

function assertContains(label, content, text) {
  assert(label, content.includes(text), `expected "${text}"`);
}

// All EDGE-2 source files to check
const EDGE2_SOURCES = [
  "packages/NevaxisFactOS.Persistence/Edge/EdgeTelemetryIngestionService.cs",
  "packages/NevaxisFactOS.Persistence/Edge/EdgeCycleIngestionService.cs",
  "packages/NevaxisFactOS.Persistence/Edge/EdgeCounterIngestionService.cs",
];

// ---------------------------------------------------------------------------
// 1. No Production mutation in any EDGE-2 service
// ---------------------------------------------------------------------------
console.log("\n[1] No Production mutation in EDGE-2 services");

for (const srcPath of EDGE2_SOURCES) {
  const src = readFile(srcPath);
  const name = srcPath.split("/").pop();
  assertNotContains(`${name}: no ProductionOrder mutation`, src, "ProductionOrder");
  assertNotContains(`${name}: no ProductionExecutionEntry mutation`, src, "ProductionExecutionEntry");
  assertNotContains(`${name}: no quantity.good.record`, src, "quantity.good");
  assertNotContains(`${name}: no quantity.scrap.record`, src, "quantity.scrap");
  assertNotContains(`${name}: no production.order.complete`, src, "production.order.complete");
}

// ---------------------------------------------------------------------------
// 2. No Planning mutation
// ---------------------------------------------------------------------------
console.log("\n[2] No Planning mutation");

for (const srcPath of EDGE2_SOURCES) {
  const src = readFile(srcPath);
  const name = srcPath.split("/").pop();
  assertNotContains(`${name}: no ProductionPlan mutation`, src, "ProductionPlan");
  assertNotContains(`${name}: no planning.plan mutation`, src, "planning.plan");
}

// ---------------------------------------------------------------------------
// 3. No Maintenance mutation
// ---------------------------------------------------------------------------
console.log("\n[3] No Maintenance mutation");

for (const srcPath of EDGE2_SOURCES) {
  const src = readFile(srcPath);
  const name = srcPath.split("/").pop();
  assertNotContains(`${name}: no MaintenanceWorkOrder mutation`, src, "MaintenanceWorkOrder");
  assertNotContains(`${name}: no maintenance.workorder`, src, "maintenance.workorder");
}

// ---------------------------------------------------------------------------
// 4. No Quality mutation
// ---------------------------------------------------------------------------
console.log("\n[4] No Quality mutation");

for (const srcPath of EDGE2_SOURCES) {
  const src = readFile(srcPath);
  const name = srcPath.split("/").pop();
  assertNotContains(`${name}: no QualityInspection mutation`, src, "QualityInspection");
  assertNotContains(`${name}: no quality.inspection`, src, "quality.inspection");
}

// ---------------------------------------------------------------------------
// 5. No ERP mutation
// ---------------------------------------------------------------------------
console.log("\n[5] No ERP mutation");

for (const srcPath of EDGE2_SOURCES) {
  const src = readFile(srcPath);
  const name = srcPath.split("/").pop();
  assertNotContains(`${name}: no ErpSyncRun mutation`, src, "ErpSyncRun");
  assertNotContains(`${name}: no erp.sync`, src, "erp.sync");
}

// ---------------------------------------------------------------------------
// 6. No OEE
// ---------------------------------------------------------------------------
console.log("\n[6] No OEE implementation");

const entities = readFile("packages/NevaxisFactOS.Persistence/Edge/EdgeEntities.cs");
assertNotContains("entities: no OEE field", entities, "OEE");
assertNotContains("entities: no Availability field", entities, "Availability");
assertNotContains("entities: no Performance field", entities, "Performance");

for (const srcPath of EDGE2_SOURCES) {
  const src = readFile(srcPath);
  const name = srcPath.split("/").pop();
  assertNotContains(`${name}: no OEE calculation`, src, "CalculateOee");
  assertNotContains(`${name}: no Availability calc`, src, "Availability =");
}

// ---------------------------------------------------------------------------
// 7. No scrap/alarm/downtime/setup workflows
// ---------------------------------------------------------------------------
console.log("\n[7] No scrap/alarm/downtime/setup workflows");

for (const srcPath of EDGE2_SOURCES) {
  const src = readFile(srcPath);
  const name = srcPath.split("/").pop();
  assertNotContains(`${name}: no scrap`, src, "Scrap");
  assertNotContains(`${name}: no alarm event type`, src, '"alarm"');
  assertNotContains(`${name}: no downtime booking`, src, "ProductionDowntime");
  assertNotContains(`${name}: no setup session`, src, "SetupSession");
}

const migration = readFile("packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260810140000_AddEdgeTelemetryCycleFoundation.cs");
assertNotContains("migration: no alarm table", migration, "machine_alarms");
assertNotContains("migration: no downtime table", migration, "machine_downtime_events");
assertNotContains("migration: no setup table", migration, "machine_setup_events");

// ---------------------------------------------------------------------------
// 8. No machine write/control commands
// ---------------------------------------------------------------------------
console.log("\n[8] No machine write/control commands");

const allEdgeSources = [
  ...EDGE2_SOURCES,
  "packages/NevaxisFactOS.Persistence/Edge/EdgeIngestionService.cs",
  "apps/api/Edge/EdgeEndpointRouteBuilderExtensions.cs",
];

for (const srcPath of allEdgeSources) {
  const src = readFile(srcPath);
  const name = srcPath.split("/").pop();
  assertNotContains(`${name}: no WriteTag`, src, "WriteTag");
  assertNotContains(`${name}: no WriteCommand`, src, "WriteCommand");
  assertNotContains(`${name}: no MachineCommand`, src, "MachineCommand");
  assertNotContains(`${name}: no SendCommand`, src, "SendCommand");
  assertNotContains(`${name}: no recipe upload`, src, "UploadRecipe");
  assertNotContains(`${name}: no ParameterDownload`, src, "ParameterDownload");
}

// ---------------------------------------------------------------------------
// 9. No real PLC/MQTT/OPC implementation
// ---------------------------------------------------------------------------
console.log("\n[9] No real PLC/MQTT/OPC implementation");

const simFile = readFile("tools/NevaxisFactOS.MachineSimulator/MachineSimulatorScenarios.cs");
assertNotContains("simulator: no MQTTnet", simFile, "MQTTnet");
assertNotContains("simulator: no OPC Foundation", simFile, "OpcUa");
assertNotContains("simulator: no Siemens S7", simFile, "SiemensS7");
assertNotContains("simulator: no Beckhoff ADS", simFile, "BeckhoffAds");

// Check csproj files don't reference real PLC SDKs
const simCsproj = readFileOpt("tools/NevaxisFactOS.MachineSimulator/NevaxisFactOS.MachineSimulator.csproj");
if (simCsproj) {
  assertNotContains("simulator csproj: no MQTTnet pkg", simCsproj, "MQTTnet");
  assertNotContains("simulator csproj: no OPC Foundation pkg", simCsproj, "OPCFoundation");
  assertNotContains("simulator csproj: no Modbus pkg", simCsproj, "NModbus");
}

const persistCsproj = readFileOpt("packages/NevaxisFactOS.Persistence/NevaxisFactOS.Persistence.csproj");
if (persistCsproj) {
  assertNotContains("persistence csproj: no MQTTnet pkg", persistCsproj, "MQTTnet");
  assertNotContains("persistence csproj: no OPC Foundation pkg", persistCsproj, "OPCFoundation");
}

// ---------------------------------------------------------------------------
// 10. Ownership — EDGE owns observations, not execution
// ---------------------------------------------------------------------------
console.log("\n[10] EDGE owns observations");

// Telemetry service explicitly checks it does not mutate Production
assertContains("no-Production note in telemetry svc", readFile(EDGE2_SOURCES[0]), "No Production mutation");
assertContains("no-Production note in cycle svc", readFile(EDGE2_SOURCES[1]), "No Production mutation");
assertContains("no-Production note in counter svc", readFile(EDGE2_SOURCES[2]), "No Production mutation");

// Machine signal projection is read-only for downstream consumers
const entities2 = readFile("packages/NevaxisFactOS.Persistence/Edge/EdgeEntities.cs");
assertContains("MachineSignalProjection marked read-only downstream", entities2, "Read-only downstream");

// ---------------------------------------------------------------------------
// 11. Append-only history
// ---------------------------------------------------------------------------
console.log("\n[11] Append-only history enforcement");

assertContains("RLS append-only telemetry", migration, "ApplyTenantRlsAppendOnly(migrationBuilder, \"machine_telemetry\"");
assertContains("RLS append-only cycles", migration, "ApplyTenantRlsAppendOnly(migrationBuilder, \"machine_observed_cycles\"");
assertContains("RLS append-only counter obs", migration, "ApplyTenantRlsAppendOnly(migrationBuilder, \"machine_counter_observations\"");
assertContains("RLS append-only state transitions", migration, "ApplyTenantRlsAppendOnly(migrationBuilder, \"machine_state_transitions\"");
assertContains("history GRANT: no UPDATE on telemetry", migration, "GRANT SELECT, INSERT ON TABLE machine_telemetry");
assertContains("history GRANT: no UPDATE on cycles", migration, "GRANT SELECT, INSERT ON TABLE machine_observed_cycles");

// ---------------------------------------------------------------------------
// 12. No TimescaleDB/InfluxDB/Kafka/ClickHouse
// ---------------------------------------------------------------------------
console.log("\n[12] No exotic storage engines");

assertNotContains("migration: no TimescaleDB", migration, "timescaledb");
assertNotContains("migration: no hypertable", migration, "hypertable");

const apiProgram = readFile("apps/api/Program.cs");
assertNotContains("API: no Kafka client", apiProgram, "Confluent.Kafka");
assertNotContains("API: no InfluxDB client", apiProgram, "InfluxDB");

// ---------------------------------------------------------------------------
// Summary
// ---------------------------------------------------------------------------
console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-2 Boundary Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
