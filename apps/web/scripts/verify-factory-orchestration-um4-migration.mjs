#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");
const migration = readFileSync(
  resolve(ROOT, "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260911205554_AddFactoryOrchestrationFoundation.cs"),
  "utf8",
);

if (!migration.includes("factory_case_dependencies")) throw new Error("factory_case_dependencies table missing");
if (!migration.includes("factory_event_correlations")) throw new Error("factory_event_correlations table missing");
if (!migration.includes("orchestration_root_case_id")) throw new Error("digital_work_cases orchestration columns missing");
if (!migration.includes("factory-case.read")) throw new Error("factory-case permissions missing");
if (!migration.includes("ENABLE ROW LEVEL SECURITY")) throw new Error("RLS missing");
if (!migration.includes("GRANT SELECT, INSERT, UPDATE ON TABLE factory_case_dependencies")) {
  throw new Error("runtime grants missing");
}

console.log("FACTORY_ORCHESTRATION_UM4_MIGRATION_VERIFIER = PASS");
