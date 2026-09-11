#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");
const migration = readFileSync(
  resolve(ROOT, "packages/CrevaFactOS.Persistence.Migrations/Migrations/20260911215515_AddProductionManagerFoundation.cs"),
  "utf8",
);

if (!migration.includes("factory_state_snapshots")) throw new Error("factory_state_snapshots table missing");
if (!migration.includes("factory_predictions")) throw new Error("factory_predictions table missing");
if (!migration.includes("factory_decisions")) throw new Error("factory_decisions table missing");
if (!migration.includes("factory_decision_options")) throw new Error("factory_decision_options table missing");
if (!migration.includes("factory_simulations")) throw new Error("factory_simulations table missing");
if (!migration.includes("factory_decision_outcomes")) throw new Error("factory_decision_outcomes table missing");
if (!migration.includes("factory_decision_memories")) throw new Error("factory_decision_memories table missing");
if (!migration.includes("production_manager_policies")) throw new Error("production_manager_policies table missing");
if (!migration.includes("production-manager.read")) throw new Error("production-manager permissions missing");
if (!migration.includes("ENABLE ROW LEVEL SECURITY")) throw new Error("RLS missing");
if (!migration.includes("GRANT SELECT, INSERT, UPDATE ON TABLE factory_decisions")) {
  throw new Error("runtime grants missing");
}

console.log("PRODUCTION_MANAGER_UM5_MIGRATION_VERIFIER = PASS");
