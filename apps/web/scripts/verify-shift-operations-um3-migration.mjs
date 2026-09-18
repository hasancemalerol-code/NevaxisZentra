#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");
const migration = readFileSync(
  resolve(ROOT, "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260911191746_AddShiftOperationsSmartHandoverFoundation.cs"),
  "utf8",
);
const grants = readFileSync(
  resolve(ROOT, "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260911193739_AddShiftOperationsRuntimeGrants.cs"),
  "utf8",
);

if (!migration.includes("operational_shift_instances")) throw new Error("shift instances table missing");
if (!migration.includes("shift_open_items")) throw new Error("shift open items table missing");
if (!migration.includes("shift.handover.prepare")) throw new Error("shift permissions missing");
if (!migration.includes("ENABLE ROW LEVEL SECURITY")) throw new Error("RLS missing");
if (!grants.includes("GRANT SELECT, INSERT, UPDATE ON TABLE shift_open_items")) {
  throw new Error("runtime grants missing");
}

console.log("SHIFT_OPERATIONS_UM3_MIGRATION_VERIFIER = PASS");
