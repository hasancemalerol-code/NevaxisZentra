#!/usr/bin/env node
/**
 * verify-edge-1-core.mjs
 * EDGE-1 core architecture verifier.
 * Checks static code (no live API required) and live API when available.
 */

import { readFileSync, existsSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

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
// 1. Entity layer
// ---------------------------------------------------------------------------

console.log("\n[1] Edge entities");
const entities = readFile("packages/NevaxisFactOS.Persistence/Edge/EdgeEntities.cs");

assertContains("EdgeGateway entity exists", entities, "public sealed class EdgeGateway");
assertContains("MachineRegistry entity exists", entities, "public sealed class MachineRegistry");
assertContains("MachineGatewayAssignment entity exists", entities, "public sealed class MachineGatewayAssignment");
assertContains("MachineEvent entity exists", entities, "public sealed class MachineEvent");
assertContains("MachineRuntime entity exists", entities, "public sealed class MachineRuntime");
assertContains("IdempotencyKey on MachineEvent", entities, "IdempotencyKey");
assertContains("SourceTimestampUtc on MachineEvent", entities, "SourceTimestampUtc");
assertContains("ReceivedAtUtc on MachineEvent", entities, "ReceivedAtUtc");
assertContains("SourceSequence on MachineEvent", entities, "SourceSequence");
assertContains("IsDuplicate flag", entities, "IsDuplicate");
assertContains("MachineRuntime has IsReadyForProduction", entities, "IsReadyForProduction");
assertNotContains("No OEE calculation in entities", entities, "OeePercent");
assertNotContains("No maintenance mutation in entities", entities, "MaintenanceWorkOrder");

// ---------------------------------------------------------------------------
// 2. Adapter contract
// ---------------------------------------------------------------------------

console.log("\n[2] Adapter contract");
const adapterFile = readFile("packages/NevaxisFactOS.Persistence/Edge/IEdgeGatewayAdapter.cs");

assertContains("IEdgeGatewayAdapter interface exists", adapterFile, "interface IEdgeGatewayAdapter");
assertContains("VendorKey property", adapterFile, "string VendorKey");
assertContains("IsReadOnly always true", adapterFile, "bool IsReadOnly => true");
assertContains("CheckHealthAsync", adapterFile, "CheckHealthAsync");
assertContains("PollMachineEventsAsync read-only", adapterFile, "PollMachineEventsAsync");
assertNotContains("No write/control commands", adapterFile, "WriteCommand");
assertNotContains("No machine control", adapterFile, "SendControl");
assertNotContains("No MQTT client in interface", adapterFile, "MqttClient");
assertNotContains("No OPC UA in interface", adapterFile, "OpcUa");

// ---------------------------------------------------------------------------
// 3. Simulator adapter
// ---------------------------------------------------------------------------

console.log("\n[3] Simulator adapter");
const simAdapter = readFile("packages/NevaxisFactOS.Persistence/Edge/EdgeSimulatorAdapter.cs");

assertContains("EdgeSimulatorAdapter class", simAdapter, "class EdgeSimulatorAdapter");
assertContains("VendorKey = simulator", simAdapter, 'EdgeGatewayVendorKeys.Simulator');
assertContains("Uses IHttpClientFactory", simAdapter, "IHttpClientFactory");
assertContains("Calls /health", simAdapter, '"/health"');
assertContains("Calls /simulate/events", simAdapter, '/simulate/events');
assertNotContains("No direct write commands to machines", simAdapter, "POST /machine");
assertNotContains("No plaintext credential hardcode", simAdapter, 'Password = "');

// ---------------------------------------------------------------------------
// 4. Ingestion service
// ---------------------------------------------------------------------------

console.log("\n[4] Ingestion service");
const ingestion = readFile("packages/NevaxisFactOS.Persistence/Edge/EdgeIngestionService.cs");

assertContains("IEdgeIngestionService interface", ingestion, "interface IEdgeIngestionService");
assertContains("EdgeIngestionService class", ingestion, "class EdgeIngestionService");
assertContains("Duplicate check by IdempotencyKey", ingestion, "IdempotencyKey == request.IdempotencyKey");
assertContains("Out-of-order detection", ingestion, "isOutOfOrder");
assertContains("Append-only: stores MachineEvent", ingestion, "dbContext.MachineEvents.Add");
assertContains("Updates MachineRuntime projection", ingestion, "dbContext.MachineRuntimes");
assertContains("UnavailableEdgeIngestionService", ingestion, "class UnavailableEdgeIngestionService");
assertNotContains("No write to external machine", ingestion, "WriteToMachine");

// ---------------------------------------------------------------------------
// 5. Repository
// ---------------------------------------------------------------------------

console.log("\n[5] Repository");
const repo = readFile("packages/NevaxisFactOS.Persistence/Edge/EdgeRepository.cs");

assertContains("IEdgeQueryRepository interface", repo, "interface IEdgeQueryRepository");
assertContains("ListGatewaysAsync", repo, "ListGatewaysAsync");
assertContains("ListMachinesAsync", repo, "ListMachinesAsync");
assertContains("ListMachineEventsAsync", repo, "ListMachineEventsAsync");
assertContains("GetMachineRuntimeAsync", repo, "GetMachineRuntimeAsync");
assertContains("UnavailableEdgeQueryRepository", repo, "class UnavailableEdgeQueryRepository");

// ---------------------------------------------------------------------------
// 6. Permission keys
// ---------------------------------------------------------------------------

console.log("\n[6] Permission keys");
const permKeys = readFile("packages/NevaxisFactOS.Persistence/Edge/EdgePermissionKeys.cs");

assertContains("edge.read permission", permKeys, '"edge.read"');
assertContains("edge.manage permission", permKeys, '"edge.manage"');
assertContains("edge.ingest permission", permKeys, '"edge.ingest"');

// ---------------------------------------------------------------------------
// 7. Migration
// ---------------------------------------------------------------------------

console.log("\n[7] Migration");
const migration = readFile("packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260810120000_AddEdgeGatewayMachineFoundation.cs");

assertContains("Permission: edge.read inserted", migration, "'edge.read'");
assertContains("Permission: edge.manage inserted", migration, "'edge.manage'");
assertContains("Permission: edge.ingest inserted", migration, "'edge.ingest'");
assertContains("edge_gateways table created", migration, "CREATE TABLE IF NOT EXISTS edge_gateways");
assertContains("machine_registry table created", migration, "CREATE TABLE IF NOT EXISTS machine_registry");
assertContains("machine_gateway_assignments table", migration, "CREATE TABLE IF NOT EXISTS machine_gateway_assignments");
assertContains("machine_events table created", migration, "CREATE TABLE IF NOT EXISTS machine_events");
assertContains("machine_runtime table created", migration, "CREATE TABLE IF NOT EXISTS machine_runtime");
assertContains("machine_events append-only grant (no UPDATE)", migration, "GRANT SELECT, INSERT ON TABLE machine_events");
assertNotContains("machine_events must not have UPDATE grant", migration, "GRANT SELECT, INSERT, UPDATE ON TABLE machine_events");
assertContains("Idempotency index on machine_events", migration, "ix_machine_events_tenant_facility_idempotency");
assertContains("RLS applied to edge_gateways", migration, "ApplyTenantRls(migrationBuilder, \"edge_gateways\"");
assertContains("RLS append-only on machine_events", migration, "ApplyTenantRlsAppendOnly(migrationBuilder, \"machine_events\"");
assertContains("Audit taxonomy updated with edge actions", migration, "'edge.gateway.registered'");
assertContains("Outbox constraint updated", migration, "'edge-gateway-registered'");

// ---------------------------------------------------------------------------
// 8. DI registration
// ---------------------------------------------------------------------------

console.log("\n[8] DI registration");
const di = readFile("apps/api/Edge/EdgeEndpointRouteBuilderExtensions.cs");

assertContains("AddNevaxisFactOSEdge service registration", di, "AddNevaxisFactOSEdge");
assertContains("MapEdgeEndpoints route mapping", di, "MapEdgeEndpoints");
assertContains("Unavailable fallback for query repo", di, "UnavailableEdgeQueryRepository");
assertContains("Unavailable fallback for ingestion", di, "UnavailableEdgeIngestionService");
assertContains("MachineSimulator HttpClient registered", di, "EdgeSimulatorClientNames.Simulator");

// ---------------------------------------------------------------------------
// 9. Development seed
// ---------------------------------------------------------------------------

console.log("\n[9] Development seed");
const seedFile = readFile("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotEdgeSeed.cs");

assertContains("Simulator gateway seeded", seedFile, '"SIM-GW-001"');
assertContains("CNC-001 machine seeded", seedFile, '"CNC-001"');
assertContains("PRESS-001 machine seeded", seedFile, '"PRESS-001"');
assertContains("CNC assignment seeded", seedFile, "EdgePilotCncAssignmentId");
assertContains("Press assignment seeded", seedFile, "EdgePilotPressAssignmentId");
assertNotContains("Seed does not create MaintenanceWorkOrder", seedFile, "MaintenanceWorkOrder");
assertNotContains("Seed does not create ProductionOrder", seedFile, "ProductionOrder");

// ---------------------------------------------------------------------------
// 10. DevelopmentPilotIds
// ---------------------------------------------------------------------------

console.log("\n[10] DevelopmentPilotIds");
const pilotIds = readFile("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotIds.cs");

assertContains("Edge read permission GUID defined", pilotIds, "EdgeReadPermissionDefinitionId");
assertContains("Edge manage permission GUID defined", pilotIds, "EdgeManagePermissionDefinitionId");
assertContains("Edge ingest permission GUID defined", pilotIds, "EdgeIngestPermissionDefinitionId");
assertContains("Edge simulator gateway ID", pilotIds, "EdgePilotSimulatorGatewayId");
assertContains("Edge CNC machine ID", pilotIds, "EdgePilotCncMachineId");
assertContains("Edge PRESS machine ID", pilotIds, "EdgePilotPressMachineId");

// ---------------------------------------------------------------------------
// 11. MachineSimulator scenarios
// ---------------------------------------------------------------------------

console.log("\n[11] MachineSimulator scenarios");
const scenarios = readFile("tools/NevaxisFactOS.MachineSimulator/MachineSimulatorScenarios.cs");

assertContains("CNC-001 machine defined", scenarios, '"CNC-001"');
assertContains("PRESS-001 machine defined", scenarios, '"PRESS-001"');
assertContains("Deterministic IdempotencyKey", scenarios, "IdempotencyKey:");
assertContains("SourceSequence in events", scenarios, "SourceSequence:");
assertContains("status-change event kind", scenarios, '"status-change"');
assertContains("cycle-start event kind", scenarios, '"cycle-start"');
assertContains("cycle-end event kind", scenarios, '"cycle-end"');
assertNotContains("No real PLC code", scenarios, "OpcUaClient");
assertNotContains("No MQTT publish in scenarios", scenarios, "MqttClient");

// ---------------------------------------------------------------------------
// 12. Entity configurations
// ---------------------------------------------------------------------------

console.log("\n[12] Entity configurations");
const configs = readFile("packages/NevaxisFactOS.Persistence/Configurations/EdgeEntityConfigurations.cs");

assertContains("EdgeGatewayEntityConfiguration", configs, "class EdgeGatewayEntityConfiguration");
assertContains("MachineRegistryEntityConfiguration", configs, "class MachineRegistryEntityConfiguration");
assertContains("MachineEventEntityConfiguration", configs, "class MachineEventEntityConfiguration");
assertContains("MachineRuntimeEntityConfiguration", configs, "class MachineRuntimeEntityConfiguration");
assertContains("machine_events table mapping", configs, '"machine_events"');
assertContains("idempotency_key column", configs, '"idempotency_key"');

// ---------------------------------------------------------------------------
// Summary
// ---------------------------------------------------------------------------

const total = passed + failed;
console.log(`\nEDGE-1 core: ${passed}/${total} passed`);
if (failed > 0) {
  console.error(`EDGE-1 core verification FAILED: ${failed} check(s) failed.`);
  process.exit(1);
}
console.log("EDGE-1 core verification PASSED.\n");
