#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260824141632_AddCommercialDeviationSupplierComparisonIntelligence";
const MIGRATION_BASENAME = `${MIGRATION_ID}.cs`;
const DESIGNER_BASENAME = `${MIGRATION_ID}.Designer.cs`;
const CLOSURE_PREFIX = "COMMINT2-CLOSURE";

const RLS_TABLES = [
  "commercial_purchase_price_comparison_runs",
  "commercial_purchase_price_comparison_results",
  "commercial_purchase_price_comparison_drivers",
];

const APPEND_ONLY_TABLES = [
  "commercial_purchase_price_comparison_runs",
  "commercial_purchase_price_comparison_results",
  "commercial_purchase_price_comparison_drivers",
];

function fail(message) {
  console.error(`COMMERCIAL-INTELLIGENCE-2 migration verification failed: ${message}`);
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
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
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
  const migrationProject = resolve(ROOT, "packages/NevaxisZentra.Persistence.Migrations/NevaxisZentra.Persistence.Migrations.csproj");
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
      "NevaxisZentraDbContext",
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
    DELETE FROM commercial_purchase_price_comparison_drivers
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_purchase_price_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_purchase_price_comparison_results
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_purchase_price_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_purchase_price_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

console.log("Running COMMERCIAL-INTELLIGENCE-2 migration verifier.");

const migration = readRequired(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${MIGRATION_BASENAME}`);
const designer = readRequired(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${DESIGNER_BASENAME}`);
const snapshot = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/NevaxisZentraDbContextModelSnapshot.cs");
const seed = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotCommercialIntelligenceComparisonSeed.cs");

assert("migration source present", migration.includes("AddCommercialDeviationSupplierComparisonIntelligence"));
assert("designer present", designer.includes("AddCommercialDeviationSupplierComparisonIntelligence"));
assert("snapshot includes comparison runs", snapshot.includes('ToTable("commercial_purchase_price_comparison_runs"'));
assert("snapshot includes comparison results", snapshot.includes('ToTable("commercial_purchase_price_comparison_results"'));
assert("snapshot includes comparison drivers", snapshot.includes('ToTable("commercial_purchase_price_comparison_drivers"'));
assert("signed variance constraint", migration.includes("signed_variance_ratio > -1"));
assert("supplier spread lower bound", migration.includes("supplier_spread_ratio >= 0"));
assert("position ratio lower bound", migration.includes("relative_price_position_ratio >= 0"));
const migrationUp = migration.split("protected override void Down")[0];
forbidText(migrationUp, "signed_variance_ratio <= 1", "migration up");
forbidText(migrationUp, "supplier_spread_ratio <= 1", "migration up");

const firstApplyResult = runMigrationApply();
const firstApplyOutput = `${firstApplyResult.stdout}${firstApplyResult.stderr}`;
assert("migration apply succeeds", firstApplyResult.status === 0, firstApplyOutput.slice(-500));

const historyCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("history row exactly once", historyCount === 1, `count=${historyCount}`);

const signedVarianceConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commercial_purchase_price_comparison_runs_signed_variance'
`);
assert(
  "signed variance ratio constraint lower bound only",
  signedVarianceConstraint.includes("signed_variance_ratio >") && signedVarianceConstraint.includes("-1"),
  signedVarianceConstraint,
);

const spreadConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commercial_purchase_price_comparison_runs_supplier_spread'
`);
assert("supplier spread ratio >= 0", spreadConstraint.includes(">= ") && spreadConstraint.includes("0"), spreadConstraint);

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
  SELECT COUNT(*) FROM commercial_purchase_price_comparison_results r
  LEFT JOIN commercial_purchase_price_comparison_runs run ON run.id = r.comparison_run_id
  WHERE run.id IS NULL
`));
const orphanDrivers = Number(db(`
  SELECT COUNT(*) FROM commercial_purchase_price_comparison_drivers d
  LEFT JOIN commercial_purchase_price_comparison_runs run ON run.id = d.comparison_run_id
  WHERE run.id IS NULL
`));
assert("no orphan results", orphanResults === 0, `count=${orphanResults}`);
assert("no orphan drivers", orphanDrivers === 0, `count=${orphanDrivers}`);

const duplicateIdempotency = Number(db(`
  SELECT COUNT(*) FROM (
    SELECT tenant_id, idempotency_key, COUNT(*) AS c
    FROM commercial_purchase_price_comparison_runs
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

assert("bootstrap seed references comparison pilot", seed.includes("CommercialIntelligenceComparisonPilotStableTemporalRunId"));
const pilotRunCount = Number(db(`SELECT COUNT(*) FROM commercial_purchase_price_comparison_runs WHERE id = 'bb000001-0260-4000-8000-000000000260'`));
if (pilotRunCount === 0) {
  fail("Commercial intelligence comparison pilot not bootstrapped — start API with DevelopmentPilot enabled before bootstrap count checks");
}
assert("pilot stable comparison seeded", pilotRunCount === 1, `count=${pilotRunCount}`);

removeClosureFixtures();
const closureRuns = Number(db(`SELECT COUNT(*) FROM commercial_purchase_price_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
assert("closure runs removed", closureRuns === 0, `count=${closureRuns}`);

console.log("COMMERCIAL-INTELLIGENCE-2 migration verification passed.");
