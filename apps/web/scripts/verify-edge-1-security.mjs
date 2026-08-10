#!/usr/bin/env node
/**
 * verify-edge-1-security.mjs
 * EDGE-1 security verifier.
 * Ensures read-only enforcement, no write/control commands, no plaintext credentials,
 * provider isolation, and no production/maintenance mutation.
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
// 1. Read-only adapter contract enforcement
// ---------------------------------------------------------------------------

console.log("\n[1] Read-only adapter contract");
const adapter = readFile("packages/CrevaFactOS.Persistence/Edge/IEdgeGatewayAdapter.cs");

assertContains("IsReadOnly enforced in interface", adapter, "bool IsReadOnly => true");
assertNotContains("No write method in interface", adapter, "WriteAsync");
assertNotContains("No control command method", adapter, "SendControl");
assertNotContains("No set-point method", adapter, "SetPoint");
assertNotContains("No stop method exposed", adapter, "StopMachine");
assertNotContains("No start method exposed", adapter, "StartMachine");

// ---------------------------------------------------------------------------
// 2. Simulator adapter security
// ---------------------------------------------------------------------------

console.log("\n[2] Simulator adapter security");
const simAdapter = readFile("packages/CrevaFactOS.Persistence/Edge/EdgeSimulatorAdapter.cs");

assertNotContains("No hardcoded password", simAdapter, 'Password = "');
assertNotContains("No hardcoded token", simAdapter, 'Token = "');
assertNotContains("No hardcoded secret", simAdapter, 'Secret = "');
assertNotContains("No API key inline", simAdapter, 'ApiKey = "');
assertNotContains("No direct SQL in adapter", simAdapter, "ExecuteSql");
assertNotContains("No machine write in adapter", simAdapter, "WriteToMachine");
assertNotContains("No OPC UA write node", simAdapter, "WriteNodeAsync");

// ---------------------------------------------------------------------------
// 3. Ingestion service: no mutation beyond approved boundary
// ---------------------------------------------------------------------------

console.log("\n[3] Ingestion service boundary");
const ingestion = readFile("packages/CrevaFactOS.Persistence/Edge/EdgeIngestionService.cs");

assertNotContains("Does not touch ProductionOrder", ingestion, "ProductionOrders");
assertNotContains("Does not touch MaintenanceWorkOrder", ingestion, "MaintenanceWorkOrders");
assertNotContains("Does not issue write commands to machine", ingestion, "WriteToMachine");
assertNotContains("Does not touch ERP entities", ingestion, "ErpConnectionProfiles");
assertContains("Uses transaction via SaveChangesAsync", ingestion, "SaveChangesAsync");

// ---------------------------------------------------------------------------
// 4. Migration: append-only events, no UPDATE on machine_events
// ---------------------------------------------------------------------------

console.log("\n[4] Migration append-only enforcement");
const migration = readFile("packages/CrevaFactOS.Persistence.Migrations/Migrations/20260810120000_AddEdgeGatewayMachineFoundation.cs");

assertContains("machine_events INSERT only (no UPDATE)", migration, "GRANT SELECT, INSERT ON TABLE machine_events");
assertNotContains("machine_events must not allow UPDATE", migration, "GRANT SELECT, INSERT, UPDATE ON TABLE machine_events");
assertContains("ApplyTenantRlsAppendOnly for machine_events", migration, 'ApplyTenantRlsAppendOnly(migrationBuilder, "machine_events"');
assertContains("RLS applied to gateways", migration, 'ApplyTenantRls(migrationBuilder, "edge_gateways"');
assertContains("RLS applied to machines", migration, 'ApplyTenantRls(migrationBuilder, "machine_registry"');
assertContains("RLS applied to runtime", migration, 'ApplyTenantRls(migrationBuilder, "machine_runtime"');

// ---------------------------------------------------------------------------
// 5. API endpoints: authorization required
// ---------------------------------------------------------------------------

console.log("\n[5] API endpoint authorization");
const api = readFile("apps/api/Edge/EdgeEndpointRouteBuilderExtensions.cs");

assertContains("RequireAuthorization on group", api, "RequireAuthorization()");
assertContains("Read permission checked for gateways", api, "ResolveReadAccessAsync");
assertContains("Ingest permission checked for ingestion", api, "ResolveIngestAccessAsync");
assertNotContains("No anonymous endpoint", api, ".AllowAnonymous()");

// ---------------------------------------------------------------------------
// 6. Provider isolation — Edge does not fall back to ERP adapters
// ---------------------------------------------------------------------------

console.log("\n[6] Provider isolation");
const edgeIngestionFile = readFile("packages/CrevaFactOS.Persistence/Edge/EdgeIngestionService.cs");
const edgeRepoFile = readFile("packages/CrevaFactOS.Persistence/Edge/EdgeRepository.cs");

assertNotContains("Ingestion does not reference Logo", edgeIngestionFile, "LogoErpAdapter");
assertNotContains("Ingestion does not reference Mikro", edgeIngestionFile, "MikroErpAdapter");
assertNotContains("Ingestion does not reference Netsis", edgeIngestionFile, "NetsisErpAdapter");
assertNotContains("Repo does not reference ERP adapters", edgeRepoFile, "IErpAdapter");

// ---------------------------------------------------------------------------
// 7. MachineSimulator: no real PLC code
// ---------------------------------------------------------------------------

console.log("\n[7] MachineSimulator no real PLC");
const simScenarios = readFile("tools/CrevaFactOS.MachineSimulator/MachineSimulatorScenarios.cs");
const simProgram = readFile("tools/CrevaFactOS.MachineSimulator/Program.cs");

assertNotContains("Scenarios: no OPC UA", simScenarios, "OpcUaClient");
assertNotContains("Scenarios: no MQTT", simScenarios, "MqttClient");
assertNotContains("Scenarios: no serial port", simScenarios, "SerialPort");
assertNotContains("Program: no OPC UA", simProgram, "OpcUaClient");
assertNotContains("Program: no MQTT", simProgram, "MqttClient");
assertContains("Simulator is read-only API", simProgram, "MapGet");
assertNotContains("Program: no write to machine", simProgram, "WriteToMachine");

// ---------------------------------------------------------------------------
// 8. No production/maintenance mutation anywhere in Edge code
// ---------------------------------------------------------------------------

console.log("\n[8] No production/maintenance mutation");
const edgeFiles = [
  "packages/CrevaFactOS.Persistence/Edge/EdgeEntities.cs",
  "packages/CrevaFactOS.Persistence/Edge/EdgeIngestionService.cs",
  "packages/CrevaFactOS.Persistence/Edge/EdgeRepository.cs",
  "apps/api/Edge/EdgeEndpointRouteBuilderExtensions.cs",
];

for (const f of edgeFiles) {
  const content = readFile(f);
  assertNotContains(`${f}: no production mutation`, content, "StartProductionOrder");
  assertNotContains(`${f}: no maintenance mutation`, content, "CreateMaintenanceWorkOrder");
  assertNotContains(`${f}: no OEE calculation`, content, "OeePercent");
}

// ---------------------------------------------------------------------------
// Summary
// ---------------------------------------------------------------------------

const total = passed + failed;
console.log(`\nEDGE-1 security: ${passed}/${total} passed`);
if (failed > 0) {
  console.error(`EDGE-1 security verification FAILED: ${failed} check(s) failed.`);
  process.exit(1);
}
console.log("EDGE-1 security verification PASSED.\n");
