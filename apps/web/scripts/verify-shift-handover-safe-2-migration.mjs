#!/usr/bin/env node
import { readdirSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATIONS = resolve(ROOT, "packages/CrevaFactOS.Persistence.Migrations/Migrations");

function fail(message) {
  console.error(`SHIFT-HANDOVER-SAFE-2 migration verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

const files = readdirSync(MIGRATIONS);
const migrationFile = files.find((name) => name.endsWith("AddFactoryShiftHandoverFoundation.cs") && !name.includes("Designer"));
assert("generated migration present", Boolean(migrationFile));
const designerFile = files.find((name) => name.includes("AddFactoryShiftHandoverFoundation.Designer.cs"));
assert("designer present", Boolean(designerFile));

const migration = read(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${migrationFile}`);
const snapshot = read("packages/CrevaFactOS.Persistence.Migrations/Migrations/CrevaFactOSDbContextModelSnapshot.cs");
const entities = read("packages/CrevaFactOS.Persistence/ShiftHandover/ShiftHandoverEntities.cs");
const entityConfig = read("packages/CrevaFactOS.Persistence/ShiftHandover/ShiftHandoverEntityConfigurations.cs");

assert("handovers table", migration.includes('name: "factory_shift_handovers"'));
assert("items table", migration.includes('name: "factory_shift_handover_items"'));
assert("events table", migration.includes('name: "factory_shift_handover_events"'));
assert("read permission", migration.includes("shift-handover.read"));
assert("manage permission", migration.includes("shift-handover.manage"));
assert("acknowledge permission", migration.includes("shift-handover.acknowledge"));
assert("handovers rls", migration.includes('ApplyTenantRls(migrationBuilder, "factory_shift_handovers"'));
assert("events append-only rls", migration.includes('ApplyTenantRlsAppendOnly(migrationBuilder, "factory_shift_handover_events"'));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("entity aggregate", entities.includes("class FactoryShiftHandover"));
assert("entity item", entities.includes("class ShiftHandoverItem"));
assert("entity event", entities.includes("class ShiftHandoverEvent"));
assert("linked ai task column", entityConfig.includes("linked_ai_task_id"));
assert("snapshot handovers", snapshot.includes("factory_shift_handovers"));
assert("snapshot items", snapshot.includes("factory_shift_handover_items"));
assert("snapshot events", snapshot.includes("factory_shift_handover_events"));

console.log("SHIFT-HANDOVER-SAFE-2 migration verification passed.");
