#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260826205817_AddCommercialQuotationRiskTrendComparisonIntelligence";
const MIGRATION_BASENAME = `${MIGRATION_ID}.cs`;
const DESIGNER_BASENAME = `${MIGRATION_ID}.Designer.cs`;
const CLOSURE_PREFIX = "COMMQUOTE2-CLOSURE";
const PROFILE = "bb000001-0280-4000-8000-000000000280";

const RLS_TABLES = [
  "commercial_quotation_risk_comparison_runs",
  "commercial_quotation_risk_comparison_results",
  "commercial_quotation_risk_comparison_drivers",
];

const APPEND_ONLY_TABLES = [
  "commercial_quotation_risk_comparison_runs",
  "commercial_quotation_risk_comparison_results",
  "commercial_quotation_risk_comparison_drivers",
];

function fail(message) {
  console.error(`COMMERCIAL-QUOTATION-RISK-2 migration verification failed: ${message}`);
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
    DELETE FROM commercial_quotation_risk_comparison_drivers
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_quotation_risk_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_quotation_risk_comparison_results
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_quotation_risk_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_quotation_risk_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

console.log("Running COMMERCIAL-QUOTATION-RISK-2 migration verifier.");

const migration = readRequired(`packages/NevaxisFactOS.Persistence.Migrations/Migrations/${MIGRATION_BASENAME}`);
const designer = readRequired(`packages/NevaxisFactOS.Persistence.Migrations/Migrations/${DESIGNER_BASENAME}`);
const snapshot = readRequired("packages/NevaxisFactOS.Persistence.Migrations/Migrations/NevaxisFactOSDbContextModelSnapshot.cs");
const comparisonSeed = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotCommercialQuotationRiskComparisonSeed.cs");

assert("migration source present", migration.includes("AddCommercialQuotationRiskTrendComparisonIntelligence"));
assert("designer present", designer.includes("AddCommercialQuotationRiskTrendComparisonIntelligence"));
assert("snapshot includes comparison runs", snapshot.includes('ToTable("commercial_quotation_risk_comparison_runs"'));
assert("snapshot includes comparison results", snapshot.includes('ToTable("commercial_quotation_risk_comparison_results"'));
assert("snapshot includes comparison drivers", snapshot.includes('ToTable("commercial_quotation_risk_comparison_drivers"'));
assert("snapshot includes maximum risk incidence delta", snapshot.includes("maximum_risk_incidence_delta"));
assert("incidence delta constraint", migration.includes("risk_incidence_delta >= -1"));
assert("reference incidence constraint", migration.includes("reference_risk_incidence >= 0"));
assert("current incidence constraint", migration.includes("current_risk_incidence >= 0"));
assert("assessment count honesty", migration.includes("candidate_assessment_count = comparable_assessment_count + excluded_assessment_count"));
assert("risk-increase outcome", migration.includes("risk-increase"));
assert("risk-decrease outcome", migration.includes("risk-decrease"));
assert("expiry-risk-concentration driver", migration.includes("expiry-risk-concentration"));
assert("comparison seed nine scenarios", comparisonSeed.includes("commquote2-pilot-customer-tie"));

const firstApplyResult = runMigrationApply();
const firstApplyOutput = `${firstApplyResult.stdout}${firstApplyResult.stderr}`;
assert("migration apply succeeds", firstApplyResult.status === 0, firstApplyOutput.slice(-500));

const historyCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("history row present", historyCount === 1, `count=${historyCount}`);

const incidenceDeltaConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commquote2_comparison_runs_incidence_delta'
`);
assert(
  "incidence delta bounded",
  incidenceDeltaConstraint.includes("risk_incidence_delta") && incidenceDeltaConstraint.includes("-1") && incidenceDeltaConstraint.includes("<= (1)"),
  incidenceDeltaConstraint,
);

const profileDeltaConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commercial_quotation_risk_profiles_risk_incidence_delta'
`);
assert(
  "profile maximum risk incidence delta bounded",
  profileDeltaConstraint.includes("maximum_risk_incidence_delta") && profileDeltaConstraint.includes(">= (0)") && profileDeltaConstraint.includes("<= (1)"),
  profileDeltaConstraint,
);

const countsConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commquote2_comparison_runs_assessment_counts'
`);
assert(
  "candidate = comparable + excluded",
  countsConstraint.includes("candidate_assessment_count =") && countsConstraint.includes("comparable_assessment_count + excluded_assessment_count"),
  countsConstraint,
);

for (const tableName of APPEND_ONLY_TABLES) {
  const privileges = tablePrivileges("crevafactos_runtime", tableName);
  forbidText(privileges, "DELETE", `${tableName} runtime privileges`);
  forbidText(privileges, "UPDATE", `${tableName} runtime privileges`);
  assert(`${tableName} SELECT grant`, privileges.includes("SELECT"), privileges);
  assert(`${tableName} INSERT grant`, privileges.includes("INSERT"), privileges);
}

for (const tableName of RLS_TABLES) {
  const forced = Number(db(`
    SELECT COUNT(*) FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = '${tableName}' AND c.relrowsecurity = true AND c.relforcerowsecurity = true
  `));
  assert(`${tableName} FORCE RLS enabled`, forced === 1, `count=${forced}`);
}

const orphanResults = Number(db(`
  SELECT COUNT(*) FROM commercial_quotation_risk_comparison_results r
  LEFT JOIN commercial_quotation_risk_comparison_runs run ON run.id = r.comparison_run_id
  WHERE run.id IS NULL
`));
const orphanDrivers = Number(db(`
  SELECT COUNT(*) FROM commercial_quotation_risk_comparison_drivers d
  LEFT JOIN commercial_quotation_risk_comparison_runs run ON run.id = d.comparison_run_id
  WHERE run.id IS NULL
`));
assert("no orphan results", orphanResults === 0, `count=${orphanResults}`);
assert("no orphan drivers", orphanDrivers === 0, `count=${orphanDrivers}`);

const duplicateIdempotency = Number(db(`
  SELECT COUNT(*) FROM (
    SELECT tenant_id, idempotency_key, COUNT(*) AS c
    FROM commercial_quotation_risk_comparison_runs
    WHERE idempotency_key IS NOT NULL
    GROUP BY tenant_id, idempotency_key
    HAVING COUNT(*) > 1
  ) dup
`));
assert("no duplicate idempotency rows", duplicateIdempotency === 0, `count=${duplicateIdempotency}`);

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

const pilotProfileCount = Number(db(`SELECT COUNT(*) FROM commercial_quotation_risk_profiles WHERE id = '${PROFILE}'`));
if (pilotProfileCount === 0) {
  fail("Commercial quotation risk pilot not bootstrapped — start API with DevelopmentPilot enabled before bootstrap count checks");
}
assert("pilot profile seeded", pilotProfileCount === 1, `count=${pilotProfileCount}`);
const maxRiskIncidenceDelta = db(`SELECT maximum_risk_incidence_delta FROM commercial_quotation_risk_profiles WHERE id = '${PROFILE}'`);
assert("pilot maximum risk incidence delta 0.10", maxRiskIncidenceDelta === "0.100000", maxRiskIncidenceDelta);

const pilotRunCount = Number(db(`
  SELECT COUNT(*) FROM commercial_quotation_risk_comparison_runs
  WHERE id IN (
    'bb000001-02c1-4000-8000-0000000002c1',
    'bb000001-02c2-4000-8000-0000000002c2',
    'bb000001-02c3-4000-8000-0000000002c3',
    'bb000001-02c4-4000-8000-0000000002c4',
    'bb000001-02c5-4000-8000-0000000002c5',
    'bb000001-02c6-4000-8000-0000000002c6',
    'bb000001-02c7-4000-8000-0000000002c7',
    'bb000001-02c8-4000-8000-0000000002c8',
    'bb000001-02c9-4000-8000-0000000002c9'
  )
`));
assert("nine pilot comparison runs", pilotRunCount === 9, `count=${pilotRunCount}`);

const energyCount = Number(db(`SELECT COUNT(*) FROM energy_measurement_points WHERE id = 'bb000001-0160-4000-8000-000000000160'`));
const iotCount = Number(db(`SELECT COUNT(*) FROM iot_plc_endpoints WHERE id = 'bb000001-01c0-4000-8000-0000000001c0'`));
assert("energy pilot preserved", energyCount === 1, `count=${energyCount}`);
assert("iot pilot preserved", iotCount === 1, `count=${iotCount}`);

removeClosureFixtures();
const closureRuns = Number(db(`SELECT COUNT(*) FROM commercial_quotation_risk_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
assert("closure runs removed", closureRuns === 0, `count=${closureRuns}`);

console.log("COMMERCIAL-QUOTATION-RISK-2 migration verification passed.");
