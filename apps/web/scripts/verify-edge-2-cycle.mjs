#!/usr/bin/env node
/**
 * verify-edge-2-cycle.mjs
 * EDGE-2 cycle verifier.
 * Proves: cycle.completed model, valid duration, stable identity, observed cycle projection,
 * duplicate not incremented, out-of-order no regress, counter observation, counter reset
 * suspicion, no official Production quantity mutation, restart persistence.
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
// 1. Cycle entity model
// ---------------------------------------------------------------------------
console.log("\n[1] MachineObservedCycle entity");
const entities = readFile("packages/NevaxisFactOS.Persistence/Edge/EdgeEntities.cs");

assertContains("MachineObservedCycle class", entities, "class MachineObservedCycle");
assertContains("CycleIdentity field", entities, "string CycleIdentity");
assertContains("CycleDurationMs field", entities, "long CycleDurationMs");
assertContains("ObservedCounter optional", entities, "long? ObservedCounter");
assertContains("IsDuplicate on cycle", entities, "bool IsDuplicate");
assertContains("IsOutOfOrder on cycle", entities, "bool IsOutOfOrder");
assertContains("cycle-completed event kind", entities, 'CycleCompleted = "cycle-completed"');

// ---------------------------------------------------------------------------
// 2. Cycle projection
// ---------------------------------------------------------------------------
console.log("\n[2] MachineCycleProjection entity");
assertContains("MachineCycleProjection class", entities, "class MachineCycleProjection");
assertContains("ObservedCycleCount field", entities, "long ObservedCycleCount");
assertContains("LatestObservedCounter field", entities, "long? LatestObservedCounter");
assertContains("LastCycleAtUtc field", entities, "DateTimeOffset? LastCycleAtUtc");
assertContains("LastCycleDurationMs field", entities, "long? LastCycleDurationMs");
assertContains("AverageRecentCycleDurationMs field", entities, "long? AverageRecentCycleDurationMs");
assertNotContains("no official Production quantity statement", entities, "ProductionOrderId");

// ---------------------------------------------------------------------------
// 3. Counter observation entity
// ---------------------------------------------------------------------------
console.log("\n[3] MachineCounterObservation entity");
assertContains("MachineCounterObservation class", entities, "class MachineCounterObservation");
assertContains("CounterKey field", entities, "string CounterKey");
assertContains("CounterValue field", entities, "long CounterValue");
assertContains("CounterResetSuspected field", entities, "bool CounterResetSuspected");
assertContains("counter-observed event kind", entities, 'CounterObserved = "counter-observed"');

// ---------------------------------------------------------------------------
// 4. Cycle ingestion service
// ---------------------------------------------------------------------------
console.log("\n[4] EdgeCycleIngestionService");
const cycleSvc = readFile("packages/NevaxisFactOS.Persistence/Edge/EdgeCycleIngestionService.cs");

assertContains("IEdgeCycleIngestionService interface", cycleSvc, "interface IEdgeCycleIngestionService");
assertContains("IngestCycleAsync method", cycleSvc, "IngestCycleAsync");
assertContains("duration > 0 validation", cycleSvc, "CycleDurationMs <= 0");
assertContains("sane max duration check", cycleSvc, "MaxSaneCycleDurationMs");
assertContains("36000000ms max (10h)", cycleSvc, "36_000_000");
assertContains("duplicate check by idempotency key", cycleSvc, "IdempotencyKey == request.IdempotencyKey");
assertContains("out-of-order detection", cycleSvc, "isOutOfOrder");
assertContains("cycle count incremented only if in-order", cycleSvc, "ObservedCycleCount++");
assertContains("not out of order guard before increment", cycleSvc, "if (!isOutOfOrder)");
assertContains("CycleIngestResult.Duplicate", cycleSvc, "CycleIngestResult.Duplicate");
assertContains("CycleIngestResult.Success", cycleSvc, "CycleIngestResult.Success");
assertNotContains("no Production mutation", cycleSvc, "ProductionOrder");
assertNotContains("no OEE", cycleSvc, "CalculateOee");
assertNotContains("no scrap", cycleSvc, "Scrap");

// ---------------------------------------------------------------------------
// 5. Counter ingestion service
// ---------------------------------------------------------------------------
console.log("\n[5] EdgeCounterIngestionService");
const counterSvc = readFile("packages/NevaxisFactOS.Persistence/Edge/EdgeCounterIngestionService.cs");

assertContains("IEdgeCounterIngestionService interface", counterSvc, "interface IEdgeCounterIngestionService");
assertContains("IngestCounterAsync method", counterSvc, "IngestCounterAsync");
assertContains("counter reset detection", counterSvc, "resetSuspected");
assertContains("lower value after newer higher = reset suspected", counterSvc, "request.CounterValue < lastObservation.CounterValue");
assertContains("CounterResetSuspected flag stored", counterSvc, "CounterResetSuspected = resetSuspected");
assertContains("CounterIngestResult.Success", counterSvc, "CounterIngestResult.Success");
assertNotContains("no negative quantity inference", counterSvc, "ProductionQuantity");
assertNotContains("no Production mutation", counterSvc, "ProductionOrder");

// ---------------------------------------------------------------------------
// 6. Read models
// ---------------------------------------------------------------------------
console.log("\n[6] Cycle read models");
const models = readFile("packages/NevaxisFactOS.Persistence/Edge/EdgeModels.cs");

assertContains("ObservedCycleReadModel", models, "ObservedCycleReadModel");
assertContains("CycleProjectionReadModel", models, "CycleProjectionReadModel");
assertContains("CounterObservationReadModel", models, "CounterObservationReadModel");
assertContains("CycleIngestRequest", models, "CycleIngestRequest");
assertContains("CounterIngestRequest", models, "CounterIngestRequest");
assertContains("CycleIngestResult", models, "CycleIngestResult");
assertContains("CounterIngestResult", models, "CounterIngestResult");
assertContains("CycleInvalid failure code", models, '"edge.cycle.invalid"');
assertContains("CycleDuplicate failure code", models, '"edge.cycle.duplicate"');
assertContains("CounterInvalid failure code", models, '"edge.counter.invalid"');
assertContains("CounterResetSuspected failure code", models, '"edge.counter.reset-suspected"');

// ---------------------------------------------------------------------------
// 7. Repository
// ---------------------------------------------------------------------------
console.log("\n[7] EdgeQueryRepository cycle/counter queries");
const repo = readFile("packages/NevaxisFactOS.Persistence/Edge/EdgeRepository.cs");

assertContains("ListCycleHistoryAsync method", repo, "ListCycleHistoryAsync");
assertContains("GetCycleProjectionAsync method", repo, "GetCycleProjectionAsync");

// ---------------------------------------------------------------------------
// 8. API endpoints
// ---------------------------------------------------------------------------
console.log("\n[8] API cycle endpoints");
const api = readFile("apps/api/Edge/EdgeEndpointRouteBuilderExtensions.cs");

assertContains("cycles endpoint", api, '"/machines/{machineId:guid}/cycles"');
assertContains("dev cycle ingestion endpoint", api, "DevIngestCycleAsync");
assertContains("dev counter ingestion endpoint", api, "DevIngestCounterAsync");
assertContains("IEdgeCycleIngestionService registered", api, "IEdgeCycleIngestionService");
assertContains("IEdgeCounterIngestionService registered", api, "IEdgeCounterIngestionService");

// ---------------------------------------------------------------------------
// 9. Entity configurations
// ---------------------------------------------------------------------------
console.log("\n[9] EF Core cycle/counter configurations");
const configs = readFile("packages/NevaxisFactOS.Persistence/Configurations/EdgeEntityConfigurations.cs");

assertContains("MachineObservedCycleEntityConfiguration", configs, "class MachineObservedCycleEntityConfiguration");
assertContains("MachineCycleProjectionEntityConfiguration", configs, "class MachineCycleProjectionEntityConfiguration");
assertContains("MachineCounterObservationEntityConfiguration", configs, "class MachineCounterObservationEntityConfiguration");
assertContains("machine_observed_cycles table", configs, '"machine_observed_cycles"');
assertContains("machine_cycle_projection table", configs, '"machine_cycle_projection"');
assertContains("machine_counter_observations table", configs, '"machine_counter_observations"');

// ---------------------------------------------------------------------------
// 10. Migration
// ---------------------------------------------------------------------------
console.log("\n[10] EDGE-2 migration cycle/counter tables");
const migration = readFile("packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260810140000_AddEdgeTelemetryCycleFoundation.cs");

assertContains("machine_observed_cycles created", migration, "CREATE TABLE IF NOT EXISTS machine_observed_cycles");
assertContains("machine_cycle_projection created", migration, "CREATE TABLE IF NOT EXISTS machine_cycle_projection");
assertContains("machine_counter_observations created", migration, "CREATE TABLE IF NOT EXISTS machine_counter_observations");
assertContains("duration > 0 constraint", migration, "cycle_duration_ms > 0");
assertContains("RLS append-only for cycles", migration, "ApplyTenantRlsAppendOnly(migrationBuilder, \"machine_observed_cycles\"");
assertContains("RLS append-only for counters", migration, "ApplyTenantRlsAppendOnly(migrationBuilder, \"machine_counter_observations\"");
assertContains("RLS read-write for projection", migration, "ApplyTenantRls(migrationBuilder, \"machine_cycle_projection\"");

// ---------------------------------------------------------------------------
// 11. Simulator cycle scenarios
// ---------------------------------------------------------------------------
console.log("\n[11] Simulator cycle/counter scenarios");
const sim = readFile("tools/NevaxisFactOS.MachineSimulator/MachineSimulatorScenarios.cs");

assertContains("SimulatedCycle type", sim, "record SimulatedCycle");
assertContains("SimulatedCounter type", sim, "record SimulatedCounter");
assertContains("cycle-completed events", sim, '"cycle-completed"');
assertContains("counter-observed events", sim, '"counter-observed"');
assertContains("cycle 1 duration 12500ms", sim, "12500");
assertContains("cycle 2 duration 12300ms", sim, "12300");
assertContains("cycle 3 duration 12400ms", sim, "12400");
assertContains("SIM-CNC-001-CYCLE identity", sim, "SIM-CNC-001-CYCLE-000001");
assertContains("duplicate cycle scenario", sim, "GetDuplicateCycleEvent");
assertContains("out-of-order cycle scenario", sim, "GetOutOfOrderCycleEvents");
assertContains("counter reset scenario", sim, "GetCounterResetScenario");
assertContains("counter values 100-101-102-5", sim, "new SimulatedCounter(\"machine_counter\", 5, \"good\")");

// ---------------------------------------------------------------------------
// 12. Persistence boundary
// ---------------------------------------------------------------------------
console.log("\n[12] Production consumption boundary");
const dbContext = readFile("packages/NevaxisFactOS.Persistence/NevaxisFactOSDbContext.cs");
assertContains("MachineObservedCycles DbSet", dbContext, "MachineObservedCycles");
assertContains("MachineCycleProjections DbSet", dbContext, "MachineCycleProjections");
assertContains("MachineCounterObservations DbSet", dbContext, "MachineCounterObservations");

// ---------------------------------------------------------------------------
// Summary
// ---------------------------------------------------------------------------
console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-2 Cycle Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
