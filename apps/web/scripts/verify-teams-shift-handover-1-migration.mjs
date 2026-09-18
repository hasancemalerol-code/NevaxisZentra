#!/usr/bin/env node
import { readdirSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATIONS = resolve(ROOT, "packages/NevaxisZentra.Persistence.Migrations/Migrations");

function fail(message) {
  console.error(`TEAMS-SHIFT-HANDOVER-1 migration verification failed: ${message}`);
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
const migrationFile = files.find((name) => name.endsWith("AddTeamsShiftHandovers.cs") && !name.includes("Designer"));
assert("generated migration present", Boolean(migrationFile), files.join(", "));
const designerFile = files.find((name) => name.includes("AddTeamsShiftHandovers.Designer.cs"));
assert("designer present", Boolean(designerFile));

const migration = read(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${migrationFile}`);
const designer = read(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${designerFile}`);
const snapshot = read("packages/NevaxisZentra.Persistence.Migrations/Migrations/NevaxisZentraDbContextModelSnapshot.cs");
const entityConfig = read("packages/NevaxisZentra.Persistence/Teams/TeamsEntityConfigurations.cs");
const entities = read("packages/NevaxisZentra.Persistence/Teams/TeamsEntities.cs");

assert("handovers table", migration.includes('name: "teams_shift_handovers"'));
assert("items table", migration.includes('name: "teams_shift_handover_items"'));
assert("acks table", migration.includes('name: "teams_shift_handover_acknowledgements"'));
assert("audit table", migration.includes('name: "teams_shift_handover_audit_events"'));
assert("idempotency unique", migration.includes("ux_teams_shift_handovers_tenant_idempotency"));
assert("ack unique", migration.includes("ux_teams_shift_handover_acks_tenant_handover_member"));
assert("read permission", migration.includes("teams.handover.read"));
assert("create permission", migration.includes("teams.handover.create"));
assert("acknowledge permission", migration.includes("teams.handover.acknowledge"));
assert("manage permission", migration.includes("teams.handover.manage"));
assert("handovers rls", migration.includes('ApplyTenantRls(migrationBuilder, "teams_shift_handovers"'));
assert("items delete rls", migration.includes('ApplyTenantRlsWithDelete(migrationBuilder, "teams_shift_handover_items"'));
assert("acks append-only rls", migration.includes("teams_shift_handover_acknowledgements") && migration.includes("ApplyTenantRlsAppendOnly"));
assert("audit append-only rls", migration.includes("teams_shift_handover_audit_events") && migration.includes("ApplyTenantRlsAppendOnly"));
assert("acks grant insert", migration.includes("GRANT SELECT, INSERT ON TABLE teams_shift_handover_acknowledgements"));
assert("audit grant insert", migration.includes("GRANT SELECT, INSERT ON TABLE teams_shift_handover_audit_events"));
assert("no delete grant on acks", !migration.includes("GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE teams_shift_handover_acknowledgements"));
assert("no delete grant on audit", !migration.includes("GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE teams_shift_handover_audit_events"));
assert("designer name", designer.includes("AddTeamsShiftHandovers"));
assert("entity handover", entities.includes("class TeamsShiftHandover"));
assert("entity item", entities.includes("class TeamsShiftHandoverItem"));
assert("entity ack", entities.includes("class TeamsShiftHandoverAcknowledgement"));
assert("entity audit", entities.includes("class TeamsShiftHandoverAuditEvent"));
assert("config handover table", entityConfig.includes('entity.ToTable("teams_shift_handovers")'));
assert("snapshot handovers", snapshot.includes("teams_shift_handovers"));
assert("snapshot items", snapshot.includes("teams_shift_handover_items"));
assert("snapshot acks", snapshot.includes("teams_shift_handover_acknowledgements"));
assert("snapshot audit", snapshot.includes("teams_shift_handover_audit_events"));

console.log("TEAMS-SHIFT-HANDOVER-1 migration verification passed.");
