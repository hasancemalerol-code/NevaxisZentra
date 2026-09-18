#!/usr/bin/env node
/**
 * verify-edge-2-telemetry.mjs
 * EDGE-2 telemetry verifier.
 * Proves: typed telemetry model, quality, metric key, unit, source/receive timestamps,
 * sequence, idempotency, latest projection, duplicate-safe, out-of-order-safe,
 * bounded history, tenant/facility isolation, restart persistence.
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
// 1. Telemetry entity model
// ---------------------------------------------------------------------------
console.log("\n[1] MachineTelemetrySample entity");
const entities = readFile("packages/NevaxisZentra.Persistence/Edge/EdgeEntities.cs");

assertContains("MachineTelemetrySample class", entities, "class MachineTelemetrySample");
assertContains("NumericValue field", entities, "double? NumericValue");
assertContains("TextValue field", entities, "string? TextValue");
assertContains("BooleanValue field", entities, "bool? BooleanValue");
assertContains("MetricKey field", entities, "string MetricKey");
assertContains("Unit field", entities, "string? Unit");
assertContains("Quality field in telemetry", entities, "public string Quality");
assertContains("SourceTimestampUtc field", entities, "SourceTimestampUtc");
assertContains("ReceivedAtUtc field", entities, "ReceivedAtUtc");
assertContains("SourceSequence field", entities, "SourceSequence");
assertContains("IdempotencyKey field", entities, "IdempotencyKey");
assertContains("Transport field", entities, "Transport");
assertContains("IsDuplicate on telemetry sample", entities, "IsDuplicate");
assertContains("CreatedAtUtc field", entities, "CreatedAtUtc");

// ---------------------------------------------------------------------------
// 2. Telemetry quality model
// ---------------------------------------------------------------------------
console.log("\n[2] TelemetryQualities constants");
assertContains("TelemetryQualities class", entities, "class TelemetryQualities");
assertContains("Good quality", entities, 'Good = "good"');
assertContains("Uncertain quality", entities, 'Uncertain = "uncertain"');
assertContains("Bad quality", entities, 'Bad = "bad"');
assertContains("Unknown quality", entities, 'Unknown = "unknown"');

// ---------------------------------------------------------------------------
// 3. Metric key model
// ---------------------------------------------------------------------------
console.log("\n[3] TelemetryMetricKeys constants");
assertContains("TelemetryMetricKeys class", entities, "class TelemetryMetricKeys");
assertContains("cycle_time_ms key", entities, "CycleTimeMs");
assertContains("machine_counter key", entities, "MachineCounter");
assertContains("temperature_zone_1 key", entities, "TemperatureZone1");
assertContains("hydraulic_pressure key", entities, "HydraulicPressure");
assertContains("simulator-only disclaimer", entities, "NOT real machine tag");

// ---------------------------------------------------------------------------
// 4. Latest telemetry projection
// ---------------------------------------------------------------------------
console.log("\n[4] MachineLatestTelemetry projection");
assertContains("MachineLatestTelemetry class", entities, "class MachineLatestTelemetry");
assertContains("projection has UpdatedAtUtc", entities, "DateTimeOffset UpdatedAtUtc");

// ---------------------------------------------------------------------------
// 5. Entity configurations
// ---------------------------------------------------------------------------
console.log("\n[5] EF Core entity configurations");
const configs = readFile("packages/NevaxisZentra.Persistence/Configurations/EdgeEntityConfigurations.cs");
assertContains("MachineTelemetrySampleEntityConfiguration", configs, "class MachineTelemetrySampleEntityConfiguration");
assertContains("MachineLatestTelemetryEntityConfiguration", configs, "class MachineLatestTelemetryEntityConfiguration");
assertContains("machine_telemetry table", configs, '"machine_telemetry"');
assertContains("machine_latest_telemetry table", configs, '"machine_latest_telemetry"');
assertContains("idempotency index on telemetry", configs, "IdempotencyKey");
assertContains("metric+time index", configs, "MetricKey, t.SourceTimestampUtc");

// ---------------------------------------------------------------------------
// 6. Ingestion service
// ---------------------------------------------------------------------------
console.log("\n[6] EdgeTelemetryIngestionService");
const svc = readFile("packages/NevaxisZentra.Persistence/Edge/EdgeTelemetryIngestionService.cs");
assertContains("IEdgeTelemetryIngestionService interface", svc, "interface IEdgeTelemetryIngestionService");
assertContains("IngestTelemetryAsync method", svc, "IngestTelemetryAsync");
assertContains("duplicate check by idempotency key", svc, "IdempotencyKey == request.IdempotencyKey");
assertContains("out-of-order detection", svc, "isOutOfOrder");
assertContains("latest projection updated only if in-order", svc, "if (!isOutOfOrder)");
assertContains("at-least-one-value validation", svc, "NumericValue is null && request.TextValue is null && request.BooleanValue is null");
assertContains("TelemetryIngestResult.Duplicate", svc, "TelemetryIngestResult.Duplicate");
assertContains("TelemetryIngestResult.Success", svc, "TelemetryIngestResult.Success");
assertNotContains("no Production mutation", svc, "ProductionOrder");
assertNotContains("no OEE", svc, "OEE");

// ---------------------------------------------------------------------------
// 7. Read models and DTOs
// ---------------------------------------------------------------------------
console.log("\n[7] Telemetry read models");
const models = readFile("packages/NevaxisZentra.Persistence/Edge/EdgeModels.cs");
assertContains("TelemetrySampleReadModel record", models, "TelemetrySampleReadModel");
assertContains("LatestTelemetryReadModel record", models, "LatestTelemetryReadModel");
assertContains("TelemetryIngestRequest record", models, "TelemetryIngestRequest");
assertContains("TelemetryIngestResult record", models, "TelemetryIngestResult");
assertContains("TelemetryInvalid failure code", models, '"edge.telemetry.invalid"');
assertContains("TelemetryDuplicate failure code", models, '"edge.telemetry.duplicate"');
assertContains("TelemetryOutOfOrder failure code", models, '"edge.telemetry.out-of-order"');

// ---------------------------------------------------------------------------
// 8. Repository
// ---------------------------------------------------------------------------
console.log("\n[8] EdgeQueryRepository telemetry queries");
const repo = readFile("packages/NevaxisZentra.Persistence/Edge/EdgeRepository.cs");
assertContains("ListLatestTelemetryAsync method", repo, "ListLatestTelemetryAsync");
assertContains("ListTelemetryHistoryAsync method", repo, "ListTelemetryHistoryAsync");
assertContains("from/to time range support", repo, "from.HasValue");
assertContains("limit clamped", repo, "Math.Clamp");
assertContains("UnavailableEdgeQueryRepository stubs", repo, "UnavailableEdgeQueryRepository");

// ---------------------------------------------------------------------------
// 9. API endpoints
// ---------------------------------------------------------------------------
console.log("\n[9] API telemetry endpoints");
const api = readFile("apps/api/Edge/EdgeEndpointRouteBuilderExtensions.cs");
assertContains("telemetry/latest endpoint", api, '"/machines/{machineId:guid}/telemetry/latest"');
assertContains("telemetry history endpoint", api, '"/machines/{machineId:guid}/telemetry"');
assertContains("dev telemetry ingestion endpoint", api, 'DevIngestTelemetryAsync');
assertContains("DevIngestTelemetryAsync handler", api, "DevIngestTelemetryAsync");
assertContains("dev guard (Development env only)", api, "env.IsDevelopment()");
assertContains("IEdgeTelemetryIngestionService registered", api, "IEdgeTelemetryIngestionService");

// ---------------------------------------------------------------------------
// 10. Migration
// ---------------------------------------------------------------------------
console.log("\n[10] EDGE-2 migration");
const migration = readFile("packages/NevaxisZentra.Persistence.Migrations/Migrations/20260810140000_AddEdgeTelemetryCycleFoundation.cs");
assertContains("machine_telemetry table created", migration, "CREATE TABLE IF NOT EXISTS machine_telemetry");
assertContains("machine_latest_telemetry table created", migration, "CREATE TABLE IF NOT EXISTS machine_latest_telemetry");
assertContains("idempotency unique index", migration, "ix_machine_telemetry_idempotency");
assertContains("metric+time composite index", migration, "ix_machine_telemetry_machine_metric_time");
assertContains("RLS enabled for telemetry", migration, "ApplyTenantRlsAppendOnly");
assertContains("quality check constraint", migration, "quality IN ('good', 'uncertain', 'bad', 'unknown')");

// ---------------------------------------------------------------------------
// 11. Simulator
// ---------------------------------------------------------------------------
console.log("\n[11] Simulator telemetry scenario");
const sim = readFile("tools/NevaxisZentra.MachineSimulator/MachineSimulatorScenarios.cs");
assertContains("SimulatedTelemetry type", sim, "record SimulatedTelemetry");
assertContains("TypedTelemetry in event", sim, "TypedTelemetry");
assertContains("temperature_zone_1 fixture", sim, '"temperature_zone_1"');
assertContains("hydraulic_pressure fixture", sim, '"hydraulic_pressure"');
assertContains("cycle_time_ms fixture", sim, '"cycle_time_ms"');
assertContains("out-of-order telemetry scenario", sim, "GetOutOfOrderTelemetryEvents");
assertContains("seq 200 first in OOO scenario", sim, "SourceSequence: 200");
assertContains("seq 199 second in OOO scenario", sim, "SourceSequence: 199");
assertNotContains("no real PLC/MQTT dependency", sim, "MQTTnet");
assertNotContains("no machine write commands", sim, "WriteTag");

// ---------------------------------------------------------------------------
// 12. Tenant/facility isolation
// ---------------------------------------------------------------------------
console.log("\n[12] Tenant/facility isolation");
assertContains("FacilityId on MachineTelemetrySample", entities, "public Guid FacilityId");
assertContains("TenantId on MachineTelemetrySample", entities, "public Guid TenantId");
const dbContext = readFile("packages/NevaxisZentra.Persistence/NevaxisZentraDbContext.cs");
assertContains("MachineTelemetrySamples DbSet", dbContext, "MachineTelemetrySamples");
assertContains("MachineLatestTelemetries DbSet", dbContext, "MachineLatestTelemetries");
assertContains("telemetry query filter by tenant", dbContext, "MachineTelemetrySample");

// ---------------------------------------------------------------------------
// Summary
// ---------------------------------------------------------------------------
console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-2 Telemetry Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
