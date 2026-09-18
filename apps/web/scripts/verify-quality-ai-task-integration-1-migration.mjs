#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`QUALITY-AI-TASK-INTEGRATION-1 migration verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running QUALITY-AI-TASK-INTEGRATION-1 migration verifier.");

const migration = read(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260829133201_AddQualityNcrFollowUpTaskIntegration.cs",
);
const grants = read(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260829133827_AddQualityNcrFollowUpAuditGrants.cs",
);
const entityConfig = read("packages/NevaxisZentra.Persistence/Configurations/QualityEntityConfigurations.cs");
const snapshot = read("packages/NevaxisZentra.Persistence.Migrations/Migrations/NevaxisZentraDbContextModelSnapshot.cs");

assert("follow_up_status column", migration.includes("follow_up_status"));
assert("linked_ai_task_id column", migration.includes("linked_ai_task_id"));
assert("audit events table", migration.includes("quality_ncr_follow_up_audit_events"));
assert("follow-up backfill sql", migration.includes("requires-review"));
assert("audit grants migration", grants.includes("GRANT SELECT, INSERT ON TABLE quality_ncr_follow_up_audit_events"));
assert("audit rls select policy", grants.includes("rls_qnfae_tenant_select"));
assert("audit rls insert policy", grants.includes("rls_qnfae_tenant_insert"));
assert("entity config linked task", entityConfig.includes("LinkedAiTaskId"));
assert("entity config follow up status", entityConfig.includes("FollowUpStatus"));
assert("snapshot audit table", snapshot.includes("quality_ncr_follow_up_audit_events"));

console.log("QUALITY-AI-TASK-INTEGRATION-1 migration verification passed.");
