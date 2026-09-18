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
const repo = read("packages/NevaxisZentra.Persistence/Maintenance/MaintenanceConditionRepository.cs");
const entities = read("packages/NevaxisZentra.Persistence/Maintenance/MaintenanceConditionEntities.cs");

assert("anomaly entity", entities.includes("MaintenanceConditionAnomaly"));
assert("evaluation identity", entities.includes("EvaluationIdentity"));
assert("severity model", entities.includes("MaintenanceAnomalySeverities"));
assert("direction model", entities.includes("MaintenanceAnomalyDirections"));
assert("evidence summary", entities.includes("EvidenceSummary"));
assert("deviation detection", engine.includes("DeviationThresholdPercent"));
assert("volatility detection", engine.includes("VolatilityThresholdPercent"));
assert("cycle drift", engine.includes("EvaluateCycleDrift"));
assert("idempotency check", repo.includes("EvaluationIdentity == anomaly.EvaluationIdentity"));
assert("normalization", repo.includes("MaintenanceAnomalyStatuses.Normalized"));
assert("append-only anomaly insert", repo.includes("MaintenanceConditionAnomalies.Add"));
assert("no failure probability UI terms in engine", !engine.includes("failure probability"));
assert("no RUL terms", !engine.includes("remaining useful life"));
assert("no days until failure", !engine.includes("days until failure"));

console.log("MAINT-PRED-1 anomaly verifier PASS");
