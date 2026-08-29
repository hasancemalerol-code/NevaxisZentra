#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`TEAMS-FOUNDATION-1 migration verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running TEAMS-FOUNDATION-1 migration verifier.");

const migration = read("packages/CrevaFactOS.Persistence.Migrations/Migrations/20260829214322_AddTeamsFoundation.cs");
const snapshot = read("packages/CrevaFactOS.Persistence.Migrations/Migrations/CrevaFactOSDbContextModelSnapshot.cs");
const entityConfig = read("packages/CrevaFactOS.Persistence/Teams/TeamsEntityConfigurations.cs");

assert("teams_channels table", migration.includes('name: "teams_channels"'));
assert("teams_channel_members table", migration.includes('name: "teams_channel_members"'));
assert("teams_messages table", migration.includes('name: "teams_messages"'));
assert("teams_message_mentions table", migration.includes('name: "teams_message_mentions"'));
assert("teams_channel_read_states table", migration.includes('name: "teams_channel_read_states"'));
assert("teams_message_audit_events table", migration.includes('name: "teams_message_audit_events"'));
assert("permission insert teams.read", migration.includes("'teams.read'"));
assert("permission insert teams.write", migration.includes("'teams.write'"));
assert("permission insert teams.manage", migration.includes("'teams.manage'"));
assert("channel type check", migration.includes("ck_teams_channels_channel_type"));
assert("member role check", migration.includes("ck_teams_channel_members_role"));
assert("messages append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE teams_messages"));
assert("audit append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE teams_message_audit_events"));
assert("messages rls helper", migration.includes('ApplyTenantRlsAppendOnly(migrationBuilder, "teams_messages", "tm")'));
assert("audit rls helper", migration.includes('ApplyTenantRlsAppendOnly(migrationBuilder, "teams_message_audit_events", "tmae")'));
assert("channels rls helper", migration.includes('ApplyTenantRls(migrationBuilder, "teams_channels", "tc")'));
assert("idempotency unique index", migration.includes("ux_teams_messages_tenant_idempotency"));
assert("entity config channel table", entityConfig.includes('entity.ToTable("teams_channels")'));
assert("snapshot teams tables", snapshot.includes("teams_messages") && snapshot.includes("teams_channel_members"));

console.log("TEAMS-FOUNDATION-1 migration verification passed.");
