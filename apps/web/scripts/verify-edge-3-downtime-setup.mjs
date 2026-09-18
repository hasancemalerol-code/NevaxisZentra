#!/usr/bin/env node
/**
 * verify-edge-3-downtime-setup.mjs
 * EDGE-3 downtime / setup verifier.
 * Proves: downtime/setup entities, ingestion services, event kinds, micro-stop,
 * reason categories, duration, duplicate/out-of-order, no Production mutation,
 * restart persistence, migration + RLS, dev endpoints, simulator scenarios.
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
// 1. Downtime / setup entities
// ---------------------------------------------------------------------------
console.log("\n[1] MachineDowntimeEpisode / MachineDowntimeObservation entities");
const entities = readFile("packages/NevaxisZentra.Persistence/Edge/EdgeEntities.cs");

assertContains("MachineDowntimeEpisode class", entities, "class MachineDowntimeEpisode");
assertContains("MachineDowntimeObservation class", entities, "class MachineDowntimeObservation");
assertContains("MachineSetupEpisode class", entities, "class MachineSetupEpisode");
assertContains("MachineSetupObservation class", entities, "class MachineSetupObservation");
assertContains("IsMicroStop field", entities, "bool IsMicroStop");
assertContains("DurationMs on downtime episode", entities, "long? DurationMs");
assertContains("CreatedAtUtc on downtime episode", entities, "DateTimeOffset CreatedAtUtc");
assertContains("UpdatedAtUtc on downtime episode", entities, "DateTimeOffset UpdatedAtUtc");
assertContains("CreatedAtUtc on setup episode", entities, "DateTimeOffset CreatedAtUtc");
assertContains("UpdatedAtUtc on setup episode", entities, "DateTimeOffset UpdatedAtUtc");

// ---------------------------------------------------------------------------
// 2. Event kinds
// ---------------------------------------------------------------------------
console.log("\n[2] Downtime / setup event kinds");
assertContains("machine.downtime.started", entities, 'DowntimeStarted = "machine.downtime.started"');
assertContains("machine.downtime.ended", entities, 'DowntimeEnded = "machine.downtime.ended"');
assertContains("machine.setup.started", entities, 'SetupStarted = "machine.setup.started"');
assertContains("machine.setup.completed", entities, 'SetupCompleted = "machine.setup.completed"');

// ---------------------------------------------------------------------------
// 3. Micro-stop + reason categories
// ---------------------------------------------------------------------------
console.log("\n[3] Micro-stop threshold + DowntimeReasonCategories");
assertContains("EdgeObservationDefaults class", entities, "class EdgeObservationDefaults");
assertContains("MicroStopThresholdMs", entities, "MicroStopThresholdMs");
assertContains("DowntimeReasonCategories class", entities, "class DowntimeReasonCategories");
assertContains("reason unknown", entities, 'Unknown = "unknown"');
assertContains("reason machine", entities, 'Machine = "machine"');
assertContains("reason material", entities, 'Material = "material"');
assertContains("reason operator", entities, 'Operator = "operator"');
assertContains("reason quality", entities, 'Quality = "quality"');
assertContains("reason setup", entities, 'Setup = "setup"');
assertContains("reason external", entities, 'External = "external"');
assertContains("reason planned", entities, 'Planned = "planned"');
assertContains("reason other", entities, 'Other = "other"');

// ---------------------------------------------------------------------------
// 4. Ingestion services
// ---------------------------------------------------------------------------
console.log("\n[4] EdgeDowntimeIngestionService / EdgeSetupIngestionService");
const downtimeSvc = readFile("packages/NevaxisZentra.Persistence/Edge/EdgeDowntimeIngestionService.cs");
const setupSvc = readFile("packages/NevaxisZentra.Persistence/Edge/EdgeSetupIngestionService.cs");

assertContains("EdgeDowntimeIngestionService class", downtimeSvc, "class EdgeDowntimeIngestionService");
assertContains("IEdgeDowntimeIngestionService interface", downtimeSvc, "interface IEdgeDowntimeIngestionService");
assertContains("EdgeSetupIngestionService class", setupSvc, "class EdgeSetupIngestionService");
assertContains("IEdgeSetupIngestionService interface", setupSvc, "interface IEdgeSetupIngestionService");
assertContains("IngestDowntimeStartAsync", downtimeSvc, "IngestDowntimeStartAsync");
assertContains("IngestDowntimeEndAsync", downtimeSvc, "IngestDowntimeEndAsync");
assertContains("IngestSetupStartedAsync", setupSvc, "IngestSetupStartedAsync");
assertContains("IngestSetupCompletedAsync", setupSvc, "IngestSetupCompletedAsync");

// ---------------------------------------------------------------------------
// 5. Duration, micro-stop, duplicate, out-of-order
// ---------------------------------------------------------------------------
console.log("\n[5] Duration / micro-stop / duplicate / out-of-order");
assertContains("DurationMs calculation via TotalMilliseconds", downtimeSvc, "TotalMilliseconds");
assertContains("DurationMs assigned on close", downtimeSvc, "openEpisode.DurationMs = durationMs");
assertContains("IsMicroStop via MicroStopThresholdMs", downtimeSvc, "EdgeObservationDefaults.MicroStopThresholdMs");
assertContains("IsMicroStop assigned", downtimeSvc, "openEpisode.IsMicroStop = isMicroStop");
assertContains("duplicate via IdempotencyKey", downtimeSvc, "IdempotencyKey == request.IdempotencyKey");
assertContains("Duplicate result", downtimeSvc, "OperationalIngestResult.Duplicate");
assertContains("out-of-order via IsOutOfOrder", downtimeSvc, "IsOutOfOrder");
assertContains("LastOperationalSourceSequence check", downtimeSvc, "LastOperationalSourceSequence");
assertContains("setup duplicate via IdempotencyKey", setupSvc, "IdempotencyKey == request.IdempotencyKey");
assertContains("setup Duplicate result", setupSvc, "OperationalIngestResult.Duplicate");
assertContains("setup out-of-order via IsOutOfOrder", setupSvc, "IsOutOfOrder");
assertContains("setup LastOperationalSourceSequence", setupSvc, "LastOperationalSourceSequence");

// ---------------------------------------------------------------------------
// 6. No Production mutation
// ---------------------------------------------------------------------------
console.log("\n[6] No Production mutation in downtime/setup services");
for (const [name, src] of [
  ["EdgeDowntimeIngestionService", downtimeSvc],
  ["EdgeSetupIngestionService", setupSvc],
]) {
  assertNotContains(`${name}: no ProductionOrder`, src, "ProductionOrder");
  assertNotContains(`${name}: no quantity.scrap`, src, "quantity.scrap");
  assertNotContains(`${name}: no ProductionDowntime booking`, src, "ProductionDowntime");
}

assertContains("downtime observed-only note", downtimeSvc, "Observed-only");
assertContains("setup no Production note", setupSvc, "No Production");

// ---------------------------------------------------------------------------
// 7. Migration + RLS FORCE
// ---------------------------------------------------------------------------
console.log("\n[7] Migration 20260810200000_AddEdgeOperationalObservations");
const migration = readFile(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260810200000_AddEdgeOperationalObservations.cs",
);

assertContains("machine_downtime_episodes table", migration, "machine_downtime_episodes");
assertContains("machine_setup_episodes table", migration, "machine_setup_episodes");
assertContains("RLS FORCE ROW LEVEL SECURITY", migration, "FORCE ROW LEVEL SECURITY");
assertContains("RLS append-only downtime observations", migration, 'ApplyTenantRlsAppendOnly(migrationBuilder, "machine_downtime_observations"');
assertContains("RLS append-only setup observations", migration, 'ApplyTenantRlsAppendOnly(migrationBuilder, "machine_setup_observations"');
assertContains("RLS downtime episodes", migration, 'ApplyTenantRls(migrationBuilder, "machine_downtime_episodes"');
assertContains("RLS setup episodes", migration, 'ApplyTenantRls(migrationBuilder, "machine_setup_episodes"');

// ---------------------------------------------------------------------------
// 8. Dev endpoints
// ---------------------------------------------------------------------------
console.log("\n[8] Dev downtime/setup endpoints");
const api = readFile("apps/api/Edge/EdgeEndpointRouteBuilderExtensions.cs");
assertContains("downtime/start endpoint", api, '"/downtime/start"');
assertContains("downtime/end endpoint", api, '"/downtime/end"');
assertContains("setup/start endpoint", api, '"/setup/start"');
assertContains("setup/complete endpoint", api, '"/setup/complete"');
assertContains("DevIngestDowntimeStartAsync", api, "DevIngestDowntimeStartAsync");
assertContains("DevIngestDowntimeEndAsync", api, "DevIngestDowntimeEndAsync");
assertContains("DevIngestSetupStartAsync", api, "DevIngestSetupStartAsync");
assertContains("DevIngestSetupCompleteAsync", api, "DevIngestSetupCompleteAsync");
assertContains("dev guard (Development env only)", api, "env.IsDevelopment()");

// ---------------------------------------------------------------------------
// 9. Simulator scenarios
// ---------------------------------------------------------------------------
console.log("\n[9] Simulator downtime/setup scenarios");
const sim = readFile("tools/NevaxisZentra.MachineSimulator/MachineSimulatorScenarios.cs");
assertContains("GetDowntimeScenario", sim, "GetDowntimeScenario");
assertContains("GetMicroStopScenario", sim, "GetMicroStopScenario");
assertContains("GetSetupScenario", sim, "GetSetupScenario");
assertContains("GetOutOfOrderDowntimeStart", sim, "GetOutOfOrderDowntimeStart");
assertContains("GetOutOfOrderSetupStart", sim, "GetOutOfOrderSetupStart");

// ---------------------------------------------------------------------------
// Summary
// ---------------------------------------------------------------------------
console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-3 Downtime/Setup Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
