#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const MIGRATION_ID = "20260828143401_AddCommercialPricingScenariosFoundation";
const MIGRATION_BASENAME = `${MIGRATION_ID}.cs`;
const DESIGNER_BASENAME = `${MIGRATION_ID}.Designer.cs`;
const CLOSURE_PREFIX = "COMMPROFIT7-CLOSURE";
const PROFILE = "bb000001-0310-4000-8000-000000000310";
const FACILITY = "bb000001-0005-4000-8000-000000000005";

const RLS_TABLES = [
  "commercial_pricing_scenarios",
  "commercial_pricing_scenario_evaluation_snapshots",
  "commercial_pricing_scenario_audit_events",
];

const SCENARIOS_TABLE = "commercial_pricing_scenarios";
const SNAPSHOTS_TABLE = "commercial_pricing_scenario_evaluation_snapshots";
const AUDIT_TABLE = "commercial_pricing_scenario_audit_events";

function fail(message) {
  console.error(`COMMERCIAL-PROFITABILITY-7 migration verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  const absolutePath = resolve(ROOT, relativePath);
  if (!existsSync(absolutePath)) fail(`Missing ${relativePath}`);
  return readFileSync(absolutePath, "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function tablePrivileges(grantee, tableName) {
  return db(`
    SELECT COALESCE(string_agg(DISTINCT privilege_type, ',' ORDER BY privilege_type), '')
    FROM information_schema.table_privileges
    WHERE grantee = '${grantee}' AND table_name = '${tableName}'
  `);
}

function readMigrationConnectionEnv() {
  const appsettingsPath = resolve(ROOT, "apps/api/appsettings.Development.json");
  const appsettings = JSON.parse(readFileSync(appsettingsPath, "utf8"));
  const persistence = appsettings.Persistence ?? {};
  return {
    ...process.env,
    NEVAXIS_FACTOS__MIGRATIONS__HOST: persistence.Host ?? "127.0.0.1",
    NEVAXIS_FACTOS__MIGRATIONS__PORT: String(persistence.Port ?? 5432),
    NEVAXIS_FACTOS__MIGRATIONS__DATABASE: persistence.Database ?? "crevafactos_local",
    NEVAXIS_FACTOS__MIGRATIONS__USERNAME: "crevafactos",
    NEVAXIS_FACTOS__MIGRATIONS__PASSWORD: process.env.NEVAXIS_FACTOS__MIGRATIONS__PASSWORD
      ?? process.env.NEVAXIS_FACTOS_POSTGRES_PASSWORD
      ?? "local_dev_only_change_me",
    NEVAXIS_FACTOS__MIGRATIONS__SSL_MODE: "Disable",
    NEVAXIS_FACTOS__MIGRATIONS__CONNECTION_TIMEOUT_SECONDS: "15",
    NEVAXIS_FACTOS__MIGRATIONS__COMMAND_TIMEOUT_SECONDS: "120",
  };
}

function runMigrationApply() {
  const migrationProject = resolve(ROOT, "packages/NevaxisFactOS.Persistence.Migrations/NevaxisFactOS.Persistence.Migrations.csproj");
  return spawnSync(
    "dotnet",
    [
      "tool",
      "run",
      "dotnet-ef",
      "--",
      "database",
      "update",
      "--project",
      migrationProject,
      "--startup-project",
      migrationProject,
      "--context",
      "NevaxisFactOSDbContext",
    ],
    {
      cwd: ROOT,
      encoding: "utf8",
      env: readMigrationConnectionEnv(),
    },
  );
}

function removeClosureFixtures() {
  db(`
    UPDATE commercial_pricing_scenarios
    SET latest_evaluation_snapshot_id = NULL
    WHERE source_provenance LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM commercial_pricing_scenario_audit_events
    WHERE scenario_id IN (
      SELECT id FROM commercial_pricing_scenarios WHERE source_provenance LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_pricing_scenario_evaluation_snapshots
    WHERE scenario_id IN (
      SELECT id FROM commercial_pricing_scenarios WHERE source_provenance LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_pricing_scenarios WHERE source_provenance LIKE '${CLOSURE_PREFIX}%';
  `);
}

function pilotSnapshot() {
  return {
    profileCount: Number(db(`SELECT COUNT(*) FROM commercial_profitability_profiles WHERE id = '${PROFILE}'`)),
    assessmentCount: Number(db(`
      SELECT COUNT(*) FROM commercial_profitability_assessments
      WHERE profile_id = '${PROFILE}'
    `)),
    salesEvidenceWithin: Number(db(`
      SELECT COUNT(*) FROM commercial_profitability_evidence_observations
      WHERE id = 'bb000001-0321-4000-8000-000000000321'
    `)),
    salesEvidenceBelow: Number(db(`
      SELECT COUNT(*) FROM commercial_profitability_evidence_observations
      WHERE id = 'bb000001-0323-4000-8000-000000000323'
    `)),
    salesEvidenceMissingCost: Number(db(`
      SELECT COUNT(*) FROM commercial_profitability_evidence_observations
      WHERE id = 'bb000001-0326-4000-8000-000000000326'
    `)),
    duplicateSnapshotSequence: Number(db(`
      SELECT COUNT(*) FROM (
        SELECT tenant_id, facility_id, scenario_id, evaluation_sequence, COUNT(*) AS c
        FROM commercial_pricing_scenario_evaluation_snapshots
        GROUP BY tenant_id, facility_id, scenario_id, evaluation_sequence
        HAVING COUNT(*) > 1
      ) dup
    `)),
  };
}

function assertPilotStable(baseline, current, label) {
  assert(`${label} pilot profile count stable`, current.profileCount === baseline.profileCount, `${baseline.profileCount}→${current.profileCount}`);
  assert(`${label} pilot assessment count stable`, current.assessmentCount === baseline.assessmentCount, `${baseline.assessmentCount}→${current.assessmentCount}`);
  assert(`${label} within sales evidence stable`, current.salesEvidenceWithin === baseline.salesEvidenceWithin);
  assert(`${label} below sales evidence stable`, current.salesEvidenceBelow === baseline.salesEvidenceBelow);
  assert(`${label} missing-cost sales evidence stable`, current.salesEvidenceMissingCost === baseline.salesEvidenceMissingCost);
  assert(`${label} no duplicate snapshot sequences`, current.duplicateSnapshotSequence === 0, `count=${current.duplicateSnapshotSequence}`);
}

function getListeningPid(port) {
  const result = spawnSync("powershell", [
    "-NoProfile",
    "-Command",
    `(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1).OwningProcess`,
  ], { encoding: "utf8" });
  const pid = Number(result.stdout.trim());
  return Number.isFinite(pid) && pid > 0 ? pid : null;
}

function stopApi() {
  const pid = getListeningPid(5000);
  if (!pid) return;
  spawnSync("powershell", [
    "-NoProfile",
    "-Command",
    `Stop-Process -Id ${pid} -Force -ErrorAction SilentlyContinue`,
  ], { encoding: "utf8" });
}

function startApi() {
  spawnSync("powershell", [
    "-NoProfile",
    "-Command",
    `Start-Process -WindowStyle Hidden -WorkingDirectory '${ROOT}' -FilePath 'dotnet' -ArgumentList 'run','--project','apps/api/NevaxisFactOS.Api.csproj'`,
  ], { encoding: "utf8" });
}

async function waitForApiReady(timeoutMs = 120000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    try {
      const response = await fetch(`${BASE}/health/ready`);
      if (response.status === 200) return true;
    } catch {
      // retry
    }
    await new Promise((resolveDelay) => setTimeout(resolveDelay, 500));
  }
  return false;
}

async function restartApiCycle(label) {
  stopApi();
  await new Promise((resolveDelay) => setTimeout(resolveDelay, 2000));
  startApi();
  const ready = await waitForApiReady();
  assert(`${label} API ready`, ready, `base=${BASE}`);
}

console.log("Running COMMERCIAL-PROFITABILITY-7 migration verifier.");

const migration = readRequired(`packages/NevaxisFactOS.Persistence.Migrations/Migrations/${MIGRATION_BASENAME}`);
const designer = readRequired(`packages/NevaxisFactOS.Persistence.Migrations/Migrations/${DESIGNER_BASENAME}`);
const snapshot = readRequired("packages/NevaxisFactOS.Persistence.Migrations/Migrations/NevaxisFactOSDbContextModelSnapshot.cs");
const constants = readRequired("packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialPricingScenarioConstants.cs");
const profitabilitySeed = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotCommercialProfitabilitySeed.cs");

assert("migration source present", migration.includes("AddCommercialPricingScenariosFoundation"));
assert("designer present", designer.includes("AddCommercialPricingScenariosFoundation"));
assert("snapshot includes scenarios table", snapshot.includes('ToTable("commercial_pricing_scenarios"'));
assert("snapshot includes evaluation snapshots table", snapshot.includes('ToTable("commercial_pricing_scenario_evaluation_snapshots"'));
assert("snapshot includes scenario audit table", snapshot.includes('ToTable("commercial_pricing_scenario_audit_events"'));
assert("rule set version constant", constants.includes('COMMERCIAL-PROFITABILITY-7.0'));
assert("guardrail passes constant", constants.includes('Passes = "passes"'));
assert("guardrail needs-review constant", constants.includes('NeedsReview = "needs-review"'));
assert("guardrail blocked constant", constants.includes('Blocked = "blocked"'));
assert("guardrail insufficient-data constant", constants.includes('InsufficientData = "insufficient-data"'));
assert("profitability seed within scenario", profitabilitySeed.includes("commprofit1-pilot-within-threshold"));
assert("profitability seed missing cost scenario", profitabilitySeed.includes("commprofit1-pilot-missing-energy"));
assert("snapshot sequence unique index", migration.includes("ux_commprofit7_snapshots_scenario_sequence"));
assert("profile FK restrict", migration.includes("onDelete: ReferentialAction.Restrict"));

const firstApplyResult = runMigrationApply();
const firstApplyOutput = `${firstApplyResult.stdout}${firstApplyResult.stderr}`;
assert("migration apply succeeds", firstApplyResult.status === 0, firstApplyOutput.slice(-500));

const historyCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("history row present", historyCount === 1, `count=${historyCount}`);

const lifecycleConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commprofit7_scenarios_lifecycle'
`);
assert(
  "scenario lifecycle check constraint",
  lifecycleConstraint.includes("'draft'")
    && lifecycleConstraint.includes("'submitted-for-approval'")
    && lifecycleConstraint.includes("'approved'")
    && lifecycleConstraint.includes("'rejected'"),
  lifecycleConstraint,
);

const guardrailConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commprofit7_scenarios_guardrail'
`);
assert(
  "scenario guardrail check constraint",
  guardrailConstraint.includes("'passes'")
    && guardrailConstraint.includes("'needs-review'")
    && guardrailConstraint.includes("'blocked'")
    && guardrailConstraint.includes("'insufficient-data'"),
  guardrailConstraint,
);

const scenariosPrivileges = tablePrivileges("crevafactos_runtime", SCENARIOS_TABLE);
forbidText(scenariosPrivileges, "DELETE", `${SCENARIOS_TABLE} runtime privileges`);
assert(`${SCENARIOS_TABLE} SELECT grant`, scenariosPrivileges.includes("SELECT"), scenariosPrivileges);
assert(`${SCENARIOS_TABLE} INSERT grant`, scenariosPrivileges.includes("INSERT"), scenariosPrivileges);
assert(`${SCENARIOS_TABLE} UPDATE grant`, scenariosPrivileges.includes("UPDATE"), scenariosPrivileges);

const snapshotsPrivileges = tablePrivileges("crevafactos_runtime", SNAPSHOTS_TABLE);
forbidText(snapshotsPrivileges, "DELETE", `${SNAPSHOTS_TABLE} runtime privileges`);
forbidText(snapshotsPrivileges, "UPDATE", `${SNAPSHOTS_TABLE} runtime privileges`);
assert(`${SNAPSHOTS_TABLE} SELECT grant`, snapshotsPrivileges.includes("SELECT"), snapshotsPrivileges);
assert(`${SNAPSHOTS_TABLE} INSERT grant`, snapshotsPrivileges.includes("INSERT"), snapshotsPrivileges);

const auditPrivileges = tablePrivileges("crevafactos_runtime", AUDIT_TABLE);
forbidText(auditPrivileges, "DELETE", `${AUDIT_TABLE} runtime privileges`);
forbidText(auditPrivileges, "UPDATE", `${AUDIT_TABLE} runtime privileges`);
assert(`${AUDIT_TABLE} SELECT grant`, auditPrivileges.includes("SELECT"), auditPrivileges);
assert(`${AUDIT_TABLE} INSERT grant`, auditPrivileges.includes("INSERT"), auditPrivileges);

for (const tableName of RLS_TABLES) {
  const forced = Number(db(`
    SELECT COUNT(*) FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = '${tableName}' AND c.relrowsecurity = true AND c.relforcerowsecurity = true
  `));
  assert(`${tableName} FORCE RLS enabled`, forced === 1, `count=${forced}`);

  const tenantSelectPolicy = Number(db(`
    SELECT COUNT(*) FROM pg_policies
    WHERE tablename = '${tableName}'
      AND policyname LIKE 'rls_%_tenant_select'
      AND qual LIKE '%current_setting(''NevaxisFactOS.tenant_id''%'
  `));
  assert(`${tableName} tenant select policy`, tenantSelectPolicy >= 1, `count=${tenantSelectPolicy}`);

  const tenantInsertPolicy = Number(db(`
    SELECT COUNT(*) FROM pg_policies
    WHERE tablename = '${tableName}'
      AND policyname LIKE 'rls_%_tenant_insert'
      AND with_check LIKE '%current_setting(''NevaxisFactOS.tenant_id''%'
  `));
  assert(`${tableName} tenant insert policy`, tenantInsertPolicy >= 1, `count=${tenantInsertPolicy}`);
}

const scenariosUpdatePolicy = Number(db(`
  SELECT COUNT(*) FROM pg_policies
  WHERE tablename = '${SCENARIOS_TABLE}'
    AND policyname LIKE 'rls_%_tenant_update'
    AND qual LIKE '%current_setting(''NevaxisFactOS.tenant_id''%'
    AND with_check LIKE '%current_setting(''NevaxisFactOS.tenant_id''%'
`));
assert("scenarios tenant update policy", scenariosUpdatePolicy >= 1, `count=${scenariosUpdatePolicy}`);

const appendOnlyUpdatePolicies = Number(db(`
  SELECT COUNT(*) FROM pg_policies
  WHERE tablename IN ('${SNAPSHOTS_TABLE}', '${AUDIT_TABLE}')
    AND cmd = 'UPDATE'
`));
assert("append-only tables have no update policies", appendOnlyUpdatePolicies === 0, `count=${appendOnlyUpdatePolicies}`);

const orphanAuditEvents = Number(db(`
  SELECT COUNT(*) FROM commercial_pricing_scenario_audit_events ae
  LEFT JOIN commercial_pricing_scenarios scenario ON scenario.id = ae.scenario_id
  WHERE scenario.id IS NULL
`));
const orphanSnapshots = Number(db(`
  SELECT COUNT(*) FROM commercial_pricing_scenario_evaluation_snapshots snap
  LEFT JOIN commercial_pricing_scenarios scenario ON scenario.id = snap.scenario_id
  WHERE scenario.id IS NULL
`));
assert("no orphan scenario audit events", orphanAuditEvents === 0, `count=${orphanAuditEvents}`);
assert("no orphan evaluation snapshots", orphanSnapshots === 0, `count=${orphanSnapshots}`);

const duplicateSnapshotSequence = Number(db(`
  SELECT COUNT(*) FROM (
    SELECT tenant_id, facility_id, scenario_id, evaluation_sequence, COUNT(*) AS c
    FROM commercial_pricing_scenario_evaluation_snapshots
    GROUP BY tenant_id, facility_id, scenario_id, evaluation_sequence
    HAVING COUNT(*) > 1
  ) dup
`));
assert("no duplicate snapshot sequences", duplicateSnapshotSequence === 0, `count=${duplicateSnapshotSequence}`);

const historyBefore = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
const applyResult = runMigrationApply();
const applyOutput = `${applyResult.stdout}${applyResult.stderr}`;
assert("second migration apply succeeds", applyResult.status === 0, applyOutput.slice(-500));
assert(
  "second migration apply is no-op",
  /No migrations were applied|already up to date|Done\./i.test(applyOutput),
  applyOutput.slice(-500),
);
const historyAfter = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("second apply did not duplicate history", historyAfter === historyBefore, `${historyBefore}→${historyAfter}`);

const pilotProfileCount = Number(db(`SELECT COUNT(*) FROM commercial_profitability_profiles WHERE id = '${PROFILE}'`));
if (pilotProfileCount === 0) {
  fail("Commercial profitability pilot not bootstrapped — start API with DevelopmentPilot enabled before bootstrap count checks");
}
assert("pilot profile seeded", pilotProfileCount === 1, `count=${pilotProfileCount}`);

const baselinePilot = pilotSnapshot();
assert("pilot assessments seeded", baselinePilot.assessmentCount >= 10, `count=${baselinePilot.assessmentCount}`);
assert("within-threshold sales evidence seeded", baselinePilot.salesEvidenceWithin === 1, `count=${baselinePilot.salesEvidenceWithin}`);
assert("below-threshold sales evidence seeded", baselinePilot.salesEvidenceBelow === 1, `count=${baselinePilot.salesEvidenceBelow}`);
assert("missing-cost sales evidence seeded", baselinePilot.salesEvidenceMissingCost === 1, `count=${baselinePilot.salesEvidenceMissingCost}`);

const energyCount = Number(db(`SELECT COUNT(*) FROM energy_measurement_points WHERE id = 'bb000001-0160-4000-8000-000000000160'`));
const iotCount = Number(db(`SELECT COUNT(*) FROM iot_plc_endpoints WHERE id = 'bb000001-01c0-4000-8000-0000000001c0'`));
assert("energy pilot preserved", energyCount === 1, `count=${energyCount}`);
assert("iot pilot preserved", iotCount === 1, `count=${iotCount}`);

try {
  const initiallyReady = await waitForApiReady(5000);
  if (initiallyReady) {
    await restartApiCycle("first");
    assertPilotStable(baselinePilot, pilotSnapshot(), "after first restart");
    await restartApiCycle("second");
    assertPilotStable(baselinePilot, pilotSnapshot(), "after second restart");
  } else {
    fail("API not ready for pilot seed stability checks — start API with DevelopmentPilot enabled on port 5000");
  }
} finally {
  removeClosureFixtures();
  const closureScenarios = Number(db(`SELECT COUNT(*) FROM commercial_pricing_scenarios WHERE source_provenance LIKE '${CLOSURE_PREFIX}%'`));
  assert("closure scenarios removed", closureScenarios === 0, `count=${closureScenarios}`);
}

console.log("COMMERCIAL-PROFITABILITY-7 migration verification passed.");
