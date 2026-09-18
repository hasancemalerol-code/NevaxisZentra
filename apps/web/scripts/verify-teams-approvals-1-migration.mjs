#!/usr/bin/env node
import { readdirSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATIONS = resolve(ROOT, "packages/NevaxisZentra.Persistence.Migrations/Migrations");

function fail(message) {
  console.error(`TEAMS-APPROVALS-1 migration verification failed: ${message}`);
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
const migrationFile = files.find((name) => name.endsWith("AddTeamsApprovalRequests.cs") && !name.includes("Designer"));
assert("generated migration present", Boolean(migrationFile), files.join(", "));
const designerFile = files.find((name) => name.includes("AddTeamsApprovalRequests.Designer.cs"));
assert("designer present", Boolean(designerFile));

const migration = read(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${migrationFile}`);
const designer = read(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${designerFile}`);
const snapshot = read("packages/NevaxisZentra.Persistence.Migrations/Migrations/NevaxisZentraDbContextModelSnapshot.cs");
const entityConfig = read("packages/NevaxisZentra.Persistence/Teams/TeamsEntityConfigurations.cs");
const entities = read("packages/NevaxisZentra.Persistence/Teams/TeamsEntities.cs");

assert("requests table", migration.includes('name: "teams_approval_requests"'));
assert("reviewers table", migration.includes('name: "teams_approval_reviewers"'));
assert("decisions table", migration.includes('name: "teams_approval_decisions"'));
assert("audit table", migration.includes('name: "teams_approval_audit_events"'));
assert("idempotency unique", migration.includes("ux_teams_approval_requests_tenant_idempotency"));
assert("reviewer unique", migration.includes("ux_teams_approval_reviewers_request_membership"));
assert("decision unique", migration.includes("ux_teams_approval_decisions_tenant_request"));
assert("create permission", migration.includes("teams.approvals.create"));
assert("review permission", migration.includes("teams.approvals.review"));
assert("manage permission", migration.includes("teams.approvals.manage"));
assert("requests rls", migration.includes('ApplyTenantRls("teams_approval_requests"') || migration.includes('ApplyTenantRls(migrationBuilder, "teams_approval_requests"'));
assert("decisions append-only rls", migration.includes("teams_approval_decisions") && migration.includes("ApplyTenantRlsAppendOnly"));
assert("audit append-only rls", migration.includes("teams_approval_audit_events") && migration.includes("ApplyTenantRlsAppendOnly"));
assert("decisions grant insert", migration.includes("GRANT SELECT, INSERT ON TABLE teams_approval_decisions"));
assert("no delete grant on decisions", !migration.includes("GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE teams_approval_decisions"));
assert("designer name", designer.includes("AddTeamsApprovalRequests"));
assert("entity request", entities.includes("class TeamsApprovalRequest"));
assert("entity reviewer", entities.includes("class TeamsApprovalReviewer"));
assert("entity decision", entities.includes("class TeamsApprovalDecision"));
assert("entity audit", entities.includes("class TeamsApprovalAuditEvent"));
assert("config request table", entityConfig.includes('entity.ToTable("teams_approval_requests")'));
assert("snapshot requests", snapshot.includes("teams_approval_requests"));
assert("snapshot reviewers", snapshot.includes("teams_approval_reviewers"));
assert("snapshot decisions", snapshot.includes("teams_approval_decisions"));
assert("snapshot audit", snapshot.includes("teams_approval_audit_events"));

console.log("TEAMS-APPROVALS-1 migration verification passed.");
