#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");
const migration = readFileSync(
  resolve(ROOT, "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260914083341_AddGoldenDemoP1Foundation.cs"),
  "utf8",
);

const tables = [
  "golden_demo_production_events",
  "golden_demo_cycle_baselines",
  "golden_demo_cycle_observations",
  "golden_demo_recovery_plans",
  "golden_demo_recovery_plan_actions",
  "golden_demo_agent_findings",
  "golden_demo_verification_runs",
  "golden_demo_digital_work_case_links",
];

for (const table of tables) {
  if (!migration.includes(`name: "${table}"`)) {
    throw new Error(`${table} table missing`);
  }
}

const permissions = [
  "000000000f76",
  "golden-demo-p1.read",
  "000000000f77",
  "golden-demo-p1.manage",
  "000000000f78",
  "golden-demo-p1.approve",
  "000000000f79",
  "golden-demo-p1.execute",
  "000000000f7a",
  "golden-demo-p1.verify",
];

for (const permission of permissions) {
  if (!migration.includes(permission)) {
    throw new Error(`permission ${permission} missing`);
  }
}

if (!migration.includes("ENABLE ROW LEVEL SECURITY")) {
  throw new Error("RLS missing");
}

console.log("GOLDEN_DEMO_P1_MIGRATION_VERIFIER = PASS");
