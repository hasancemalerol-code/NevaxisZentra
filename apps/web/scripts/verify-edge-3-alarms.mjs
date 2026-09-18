#!/usr/bin/env node
/**
 * verify-edge-3-alarms.mjs
 * EDGE-3 alarm verifier.
 * Proves: alarm observation/active/projection entities, severities Rank/Higher,
 * raise/clear ingestion, ActiveAlarmCount/HighestSeverity, unmatched clear,
 * duplicate/out-of-order, no Maintenance/WriteTag, migration + RLS,
 * dev endpoints, simulator scenarios.
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
// 1. Alarm entity model
// ---------------------------------------------------------------------------
console.log("\n[1] MachineAlarmObservation / MachineActiveAlarm / MachineAlarmProjection");
const entities = readFile("packages/NevaxisZentra.Persistence/Edge/EdgeEntities.cs");

assertContains("MachineAlarmObservation class", entities, "class MachineAlarmObservation");
assertContains("MachineActiveAlarm class", entities, "class MachineActiveAlarm");
assertContains("MachineAlarmProjection class", entities, "class MachineAlarmProjection");
assertContains("ActiveAlarmCount field", entities, "int ActiveAlarmCount");
assertContains("HighestSeverity field", entities, "string? HighestSeverity");
assertContains("IsUnmatchedClear field", entities, "bool IsUnmatchedClear");
assertContains("machine.alarm.raised", entities, 'AlarmRaised = "machine.alarm.raised"');
assertContains("machine.alarm.cleared", entities, 'AlarmCleared = "machine.alarm.cleared"');

// ---------------------------------------------------------------------------
// 2. Alarm severities
// ---------------------------------------------------------------------------
console.log("\n[2] AlarmSeverities info/warning/error/critical/unknown + Rank/Higher");
assertContains("AlarmSeverities class", entities, "class AlarmSeverities");
assertContains("Info severity", entities, 'Info = "info"');
assertContains("Warning severity", entities, 'Warning = "warning"');
assertContains("Error severity", entities, 'Error = "error"');
assertContains("Critical severity", entities, 'Critical = "critical"');
assertContains("Unknown severity", entities, 'Unknown = "unknown"');
assertContains("Rank method", entities, "public static int Rank");
assertContains("Higher method", entities, "public static string Higher");

// ---------------------------------------------------------------------------
// 3. EdgeAlarmIngestionService raise/clear
// ---------------------------------------------------------------------------
console.log("\n[3] EdgeAlarmIngestionService raise/clear");
const alarmSvc = readFile("packages/NevaxisZentra.Persistence/Edge/EdgeAlarmIngestionService.cs");

assertContains("EdgeAlarmIngestionService class", alarmSvc, "class EdgeAlarmIngestionService");
assertContains("IEdgeAlarmIngestionService interface", alarmSvc, "interface IEdgeAlarmIngestionService");
assertContains("IngestAlarmRaisedAsync", alarmSvc, "IngestAlarmRaisedAsync");
assertContains("IngestAlarmClearedAsync", alarmSvc, "IngestAlarmClearedAsync");
assertContains("ActiveAlarmCount projection update", alarmSvc, "projection.ActiveAlarmCount");
assertContains("HighestSeverity projection update", alarmSvc, "projection.HighestSeverity");
assertContains("signal ActiveAlarmCount sync", alarmSvc, "signal.ActiveAlarmCount = projection.ActiveAlarmCount");
assertContains("signal HighestAlarmSeverity sync", alarmSvc, "signal.HighestAlarmSeverity = projection.HighestSeverity");

// ---------------------------------------------------------------------------
// 4. Unmatched clear + duplicate + out-of-order
// ---------------------------------------------------------------------------
console.log("\n[4] Unmatched clear / duplicate / out-of-order");
const models = readFile("packages/NevaxisZentra.Persistence/Edge/EdgeModels.cs");
assertContains("edge.alarm.unmatched-clear failure code", models, '"edge.alarm.unmatched-clear"');
assertContains("AlarmUnmatchedClear constant", models, "AlarmUnmatchedClear");
assertContains("IsUnmatchedClear set when no active", alarmSvc, "IsUnmatchedClear = active is null");
assertContains("AlarmUnmatchedClear failure path", alarmSvc, "EdgeFailureCodes.AlarmUnmatchedClear");
assertContains("duplicate via IdempotencyKey", alarmSvc, "IdempotencyKey == request.IdempotencyKey");
assertContains("Duplicate result", alarmSvc, "OperationalIngestResult.Duplicate");
assertContains("IsOutOfOrder flag", alarmSvc, "IsOutOfOrder");
assertContains("LastOperationalSourceSequence", alarmSvc, "LastOperationalSourceSequence");

// ---------------------------------------------------------------------------
// 5. No Maintenance / no machine write commands
// ---------------------------------------------------------------------------
console.log("\n[5] No MaintenanceWorkOrder / WriteTag / MachineCommand");
assertNotContains("alarm svc: no MaintenanceWorkOrder", alarmSvc, "MaintenanceWorkOrder");
assertNotContains("alarm svc: no maintenance.workorder", alarmSvc, "maintenance.workorder");
assertNotContains("alarm svc: no WriteTag", alarmSvc, "WriteTag");
assertNotContains("alarm svc: no MachineCommand", alarmSvc, "MachineCommand");
assertContains("no Maintenance WO note", alarmSvc, "No Maintenance WO");

// ---------------------------------------------------------------------------
// 6. Migration + RLS
// ---------------------------------------------------------------------------
console.log("\n[6] Migration machine_alarm_observations / machine_active_alarms + RLS");
const migration = readFile(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260810200000_AddEdgeOperationalObservations.cs",
);

assertContains("machine_alarm_observations table", migration, "machine_alarm_observations");
assertContains("machine_active_alarms table", migration, "machine_active_alarms");
assertContains("machine_alarm_projection table", migration, "machine_alarm_projection");
assertContains("RLS FORCE", migration, "FORCE ROW LEVEL SECURITY");
assertContains("RLS append-only alarm observations", migration, 'ApplyTenantRlsAppendOnly(migrationBuilder, "machine_alarm_observations"');
assertContains("RLS active alarms with delete", migration, 'ApplyTenantRlsWithDelete(migrationBuilder, "machine_active_alarms"');
assertContains("severity check constraint", migration, "severity IN ('info', 'warning', 'error', 'critical', 'unknown')");

// ---------------------------------------------------------------------------
// 7. Dev endpoints
// ---------------------------------------------------------------------------
console.log("\n[7] Dev alarm/raise + alarm/clear");
const api = readFile("apps/api/Edge/EdgeEndpointRouteBuilderExtensions.cs");
assertContains("alarm/raise endpoint", api, '"/alarm/raise"');
assertContains("alarm/clear endpoint", api, '"/alarm/clear"');
assertContains("DevIngestAlarmRaiseAsync", api, "DevIngestAlarmRaiseAsync");
assertContains("DevIngestAlarmClearAsync", api, "DevIngestAlarmClearAsync");
assertContains("dev guard (Development env only)", api, "env.IsDevelopment()");

// ---------------------------------------------------------------------------
// 8. Simulator scenarios
// ---------------------------------------------------------------------------
console.log("\n[8] Simulator alarm scenarios");
const sim = readFile("tools/NevaxisZentra.MachineSimulator/MachineSimulatorScenarios.cs");
assertContains("GetAlarmScenario", sim, "GetAlarmScenario");
assertContains("GetMultipleAlarmScenario", sim, "GetMultipleAlarmScenario");
assertContains("GetOutOfOrderAlarmRaise", sim, "GetOutOfOrderAlarmRaise");

// ---------------------------------------------------------------------------
// Summary
// ---------------------------------------------------------------------------
console.log(`\n${"─".repeat(55)}`);
console.log(`EDGE-3 Alarms Verifier: ${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
