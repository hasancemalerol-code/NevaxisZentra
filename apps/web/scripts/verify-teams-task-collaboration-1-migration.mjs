#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`TEAMS-TASK-COLLABORATION-1 migration verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running TEAMS-TASK-COLLABORATION-1 migration verifier.");

const migration = read("packages/CrevaFactOS.Persistence.Migrations/Migrations/20260829230114_AddTeamsMessageTaskLinks.cs");
const designer = read("packages/CrevaFactOS.Persistence.Migrations/Migrations/20260829230114_AddTeamsMessageTaskLinks.Designer.cs");
const snapshot = read("packages/CrevaFactOS.Persistence.Migrations/Migrations/CrevaFactOSDbContextModelSnapshot.cs");
const entityConfig = read("packages/CrevaFactOS.Persistence/Teams/TeamsEntityConfigurations.cs");
const entities = read("packages/CrevaFactOS.Persistence/Teams/TeamsEntities.cs");

assert("teams_message_task_links table", migration.includes('name: "teams_message_task_links"'));
assert("link purpose column", migration.includes("link_purpose"));
assert("idempotency key column", migration.includes("idempotency_key"));
assert("source context reference column", migration.includes("source_context_reference"));
assert("unique tenant message purpose index", migration.includes("ux_teams_message_task_links_tenant_message_purpose"));
assert("tenant task index", migration.includes("ix_teams_message_task_links_tenant_task"));
assert("tenant facility message index", migration.includes("ix_teams_message_task_links_tenant_facility_message"));
assert("append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE teams_message_task_links"));
assert("rls helper", migration.includes('ApplyTenantRlsAppendOnly(migrationBuilder, "teams_message_task_links", "tmtl")'));
assert("designer migration attribute", designer.includes("AddTeamsMessageTaskLinks"));
assert("entity config link table", entityConfig.includes('entity.ToTable("teams_message_task_links")'));
assert("entity TeamsMessageTaskLink", entities.includes("class TeamsMessageTaskLink"));
assert("snapshot link table", snapshot.includes("teams_message_task_links"));

console.log("TEAMS-TASK-COLLABORATION-1 migration verification passed.");
