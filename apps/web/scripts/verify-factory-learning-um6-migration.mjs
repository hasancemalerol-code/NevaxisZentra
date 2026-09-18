#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");
const migration = readFileSync(
  resolve(ROOT, "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260913074429_AddFactoryLearningFoundation.cs"),
  "utf8",
);

const tables = [
  "factory_experiment_measurements",
  "factory_experiments",
  "factory_learning_memories",
  "factory_loss_patterns",
  "factory_losses",
  "factory_root_cause_analyses",
  "improvement_opportunities",
  "verified_improvements",
];

for (const table of tables) {
  if (!migration.includes(`name: "${table}"`)) {
    throw new Error(`${table} table missing`);
  }
}

const permissions = [
  "000000000f50",
  "continuous-improvement.read",
  "000000000f51",
  "continuous-improvement.manage",
  "000000000f52",
  "experiment.create",
  "000000000f53",
  "experiment.approve",
  "000000000f54",
  "experiment.stop",
  "000000000f55",
  "improvement.standardize",
];

for (const permission of permissions) {
  if (!migration.includes(permission)) {
    throw new Error(`permission ${permission} missing`);
  }
}

if (!migration.includes("ENABLE ROW LEVEL SECURITY")) {
  throw new Error("RLS missing");
}

if (!migration.includes("GRANT SELECT, INSERT, UPDATE ON TABLE factory_losses TO crevafactos_runtime")) {
  throw new Error("runtime grants missing");
}

if (!migration.includes("INSERT INTO tenant_role_permissions")) {
  throw new Error("tenant_role_permissions inserts missing");
}

console.log("FACTORY_LEARNING_UM6_MIGRATION_VERIFIER = PASS");
