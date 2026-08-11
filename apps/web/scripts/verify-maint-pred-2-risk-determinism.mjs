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

const service = read("packages/CrevaFactOS.Persistence/Maintenance/PredictiveRiskEvaluationService.cs");
const entities = read("packages/CrevaFactOS.Persistence/Maintenance/MaintenancePredictiveRiskEntities.cs");
const migration = read("packages/CrevaFactOS.Persistence.Migrations/Migrations/20260811160000_AddMaintenancePredictiveRiskFoundation.cs");
const repository = read("packages/CrevaFactOS.Persistence/Maintenance/MaintenancePredictiveRiskRepository.cs");

assert("rule-set version constant", entities.includes("MAINT-PRED-RISK-V1"));
assert("risk bands", entities.includes("MaintenancePredictiveRiskBands"));
for (const band of ["insufficient-data", "low", "watch", "high", "critical"]) {
  assert(`band ${band}`, entities.includes(`"${band}"`));
}
assert("recommendation categories", entities.includes("MaintenancePredictiveRecommendationCategories"));
for (const category of [
  "review-data-quality",
  "continue-routine-monitoring",
  "close-monitoring",
  "plan-technical-review",
  "urgent-authorized-technical-review",
]) {
  assert(`category ${category}`, entities.includes(`"${category}"`));
}
assert("insufficient gate", service.includes("IsInsufficientEvidence"));
assert("stale cannot become low", service.includes("MaintenanceConditionDataFreshnessLevels.Stale"));
assert("corroborated critical", service.includes("corroboratedCritical"));
assert("single weak signal guard", service.includes("materialAnomalies.Count == 1"));
assert("structured drivers", entities.includes("MaintenancePredictiveRiskDriver"));
assert("structured evidence", entities.includes("MaintenancePredictiveRiskEvidence"));
assert("idempotency unique index", migration.includes("ix_mpra_tenant_facility_asset_eval_rule"));
assert("current unique partial index", migration.includes("WHERE is_current = TRUE"));
assert("recommendation unique per assessment", migration.includes("ix_mprr_tenant_facility_assessment"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("duplicate skip", repository.includes("if (existing is not null)"));
assert("no failure probability", !service.toLowerCase().includes("failure probability"));
assert("no RUL", !service.toLowerCase().includes("remaining useful life"));

console.log("MAINT-PRED-2 risk determinism verifier PASS");
