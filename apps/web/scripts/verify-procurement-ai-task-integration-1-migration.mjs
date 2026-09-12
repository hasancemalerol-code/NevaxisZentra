#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`PROCUREMENT-AI-TASK-INTEGRATION-1 migration verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running PROCUREMENT-AI-TASK-INTEGRATION-1 migration verifier.");

const migration = read(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260829201502_AddProcurementDeliveryRiskFollowUpTaskIntegration.cs",
);
const entityConfig = read("packages/NevaxisFactOS.Persistence/Configurations/ProcurementDeliveryRiskEntityConfigurations.cs");
const snapshot = read("packages/NevaxisFactOS.Persistence.Migrations/Migrations/NevaxisFactOSDbContextModelSnapshot.cs");

assert("follow_up_status column", migration.includes("follow_up_status"));
assert("linked_ai_task_id column", migration.includes("linked_ai_task_id"));
assert("audit events table", migration.includes("procurement_delivery_risk_follow_up_audit_events"));
assert("follow-up backfill sql", migration.includes("requires-review") && migration.includes("procurement_delivery_risk_assessments"));
assert("assessment update grant", migration.includes("GRANT SELECT, INSERT, UPDATE ON TABLE procurement_delivery_risk_assessments"));
assert("assessment update rls policy", migration.includes("rls_pdra_tenant_update"));
assert("audit rls select policy", migration.includes("rls_pdrfae_tenant_select"));
assert("audit rls insert policy", migration.includes("rls_pdrfae_tenant_insert"));
assert("entity config linked task", entityConfig.includes("LinkedAiTaskId"));
assert("entity config follow up status", entityConfig.includes("FollowUpStatus"));
assert("snapshot audit table", snapshot.includes("procurement_delivery_risk_follow_up_audit_events"));

console.log("PROCUREMENT-AI-TASK-INTEGRATION-1 migration verification passed.");
