#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");
const migration = readFileSync(
  resolve(ROOT, "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260913092041_AddAdaptiveProductionPlanningFoundation.cs"),
  "utf8",
);

const tables = [
  "production_plan_versions",
  "production_plan_operations",
  "planning_constraint_snapshots",
  "planning_scenarios",
  "planning_scenario_metrics",
  "plan_deviations",
  "plan_changes",
  "adaptive_planning_policies",
  "plan_activity_events",
];

for (const table of tables) {
  if (!migration.includes(`name: "${table}"`)) {
    throw new Error(`${table} table missing`);
  }
}

if (!migration.includes("active_plan_version_id")) {
  throw new Error("production_plans.active_plan_version_id missing");
}

const permissions = [
  "000000000f60",
  "production.planning.simulate",
  "000000000f61",
  "production.planning.publish",
  "000000000f62",
  "production.planning.autonomy.manage",
  "000000000f63",
  "production.planning.policy.manage",
];

for (const permission of permissions) {
  if (!migration.includes(permission)) {
    throw new Error(`permission ${permission} missing`);
  }
}

if (!migration.includes("ENABLE ROW LEVEL SECURITY")) {
  throw new Error("RLS missing");
}

if (!migration.includes("GRANT SELECT, INSERT, UPDATE ON TABLE production_plan_versions TO crevafactos_runtime")) {
  throw new Error("runtime grants missing");
}

if (!migration.includes("INSERT INTO tenant_role_permissions")) {
  throw new Error("tenant_role_permissions inserts missing");
}

console.log("ADAPTIVE_PLANNING_UM7_MIGRATION_VERIFIER = PASS");
