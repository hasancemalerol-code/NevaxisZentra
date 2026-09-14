#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");
const migration = readFileSync(
  resolve(ROOT, "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260913214325_AddEnterpriseReliabilityFoundation.cs"),
  "utf8",
);

const tables = [
  "enterprise_reliability_policies",
  "system_component_health_snapshots",
  "integration_circuit_breaker_states",
  "recovery_reconciliation_runs",
  "background_job_leases",
  "technical_operational_alerts",
  "edge_agent_registrations",
  "backup_verification_runs",
  "integration_quarantine_replay_audit",
];

for (const table of tables) {
  if (!migration.includes(`name: "${table}"`)) {
    throw new Error(`${table} table missing`);
  }
}

const permissions = [
  "000000000f70",
  "operability.read",
  "000000000f71",
  "operability.manage",
  "000000000f72",
  "operability.alert.manage",
  "000000000f73",
  "operability.reconcile.run",
  "000000000f74",
  "operability.quarantine.replay",
  "000000000f75",
  "operability.backup.verify",
];

for (const permission of permissions) {
  if (!migration.includes(permission)) {
    throw new Error(`permission ${permission} missing`);
  }
}

if (!migration.includes("ENABLE ROW LEVEL SECURITY")) {
  throw new Error("RLS missing");
}

if (!migration.includes("bb000001-0009-4000-8000-000000000901")) {
  throw new Error("pilot edge agent seed missing");
}

console.log("UM9_MIGRATION_VERIFIER = PASS");
