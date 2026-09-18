#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function read(rel) {
  const abs = resolve(ROOT, rel);
  if (!existsSync(abs)) throw new Error(`Missing ${rel}`);
  return readFileSync(abs, "utf8");
}

function assert(label, ok, detail = "") {
  if (!ok) {
    console.error(`✗ ${label}${detail ? `: ${detail}` : ""}`);
    process.exit(1);
  }
  console.log(`✓ ${label}`);
}

const engine = read("packages/NevaxisZentra.Persistence/Maintenance/ConditionEvaluationService.cs");
const entities = read("packages/NevaxisZentra.Persistence/Maintenance/MaintenanceConditionEntities.cs");
const migration = read("packages/NevaxisZentra.Persistence.Migrations/Migrations/20260811120000_AddMaintenanceConditionIntelligenceFoundation.cs");
const seed = read("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotMaintenanceConditionSeed.cs");

for (const text of ["MaintenanceConditionStatuses", "MaintenanceConditionDataSufficiencyLevels", "MaintenanceConditionMetricDefinition"]) {
  assert(`entities contain ${text}`, entities.includes(text));
}
assert("baseline median/mean", entities.includes("MaintenanceConditionBaselineMethods"));
assert("evaluation engine present", engine.includes("EvaluateMetric"));
assert("minimum samples gate", engine.includes("MinimumSamples"));
assert("bad quality excluded", engine.includes('"bad"'));
assert("stale freshness", engine.includes("MaintenanceConditionDataFreshnessLevels.Stale"));
assert("data sufficiency insufficient", engine.includes("MaintenanceConditionDataSufficiencyLevels.Insufficient"));
assert("trend states", entities.includes("MaintenanceConditionTrendStates"));
assert("migration tables", migration.includes("maintenance_condition_profiles"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("pilot metric definitions", seed.includes("TelemetryMetricKeys.TemperatureZone1"));
assert("asset-machine mapping seed", seed.includes("MaintenanceAssetMachineMapping"));
assert("no failure probability", !engine.toLowerCase().includes("failure probability"));
assert("no RUL", !engine.toLowerCase().includes("remaining useful life"));

console.log("MAINT-PRED-1 condition verifier PASS");
