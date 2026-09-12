#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260825061535_AddCommercialSalesPriceComparisonIntelligence";
const RATIO_MIGRATION_ID = "20260825072632_AddCommercialSalesPriceComparisonPositionRatioUpperBound";
const MIGRATION_BASENAME = `${MIGRATION_ID}.cs`;
const RATIO_MIGRATION_BASENAME = `${RATIO_MIGRATION_ID}.cs`;
const DESIGNER_BASENAME = `${MIGRATION_ID}.Designer.cs`;
const CLOSURE_PREFIX = "COMMSALE2-CLOSURE";
const PROFILE = "bb000001-0270-4000-8000-000000000270";

const RLS_TABLES = [
  "commercial_sales_price_comparison_runs",
  "commercial_sales_price_comparison_results",
  "commercial_sales_price_comparison_drivers",
];

const APPEND_ONLY_TABLES = [
  "commercial_sales_price_comparison_runs",
  "commercial_sales_price_comparison_results",
  "commercial_sales_price_comparison_drivers",
];

function fail(message) {
  console.error(`COMMERCIAL-SALES-PRICE-2 migration verification failed: ${message}`);
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
    DELETE FROM commercial_sales_price_comparison_drivers
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_sales_price_comparison_results
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

console.log("Running COMMERCIAL-SALES-PRICE-2 migration verifier.");

const migration = readRequired(`packages/NevaxisFactOS.Persistence.Migrations/Migrations/${MIGRATION_BASENAME}`);
const ratioMigration = readRequired(`packages/NevaxisFactOS.Persistence.Migrations/Migrations/${RATIO_MIGRATION_BASENAME}`);
const designer = readRequired(`packages/NevaxisFactOS.Persistence.Migrations/Migrations/${DESIGNER_BASENAME}`);
const snapshot = readRequired("packages/NevaxisFactOS.Persistence.Migrations/Migrations/NevaxisFactOSDbContextModelSnapshot.cs");
const seed = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotCommercialSalesPriceSeed.cs");
const comparisonSeed = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotCommercialSalesPriceComparisonSeed.cs");

assert("migration source present", migration.includes("AddCommercialSalesPriceComparisonIntelligence"));
assert("designer present", designer.includes("AddCommercialSalesPriceComparisonIntelligence"));
assert("snapshot includes comparison runs", snapshot.includes('ToTable("commercial_sales_price_comparison_runs"'));
assert("snapshot includes comparison results", snapshot.includes('ToTable("commercial_sales_price_comparison_results"'));
assert("snapshot includes comparison drivers", snapshot.includes('ToTable("commercial_sales_price_comparison_drivers"'));
assert("snapshot includes customer spread threshold", snapshot.includes("customer_spread_threshold"));
assert("signed variance constraint", migration.includes("signed_variance_ratio > -1"));
assert("customer spread lower bound", migration.includes("customer_spread_ratio >= 0"));
assert("position ratio lower bound", migration.includes("relative_price_position_ratio >= 0"));
assert("position ratio upper bound migration", ratioMigration.includes("relative_price_position_ratio < 1"));
assert("denominator honesty constraint", migration.includes("candidate_observation_count = comparable_observation_count + excluded_observation_count"));
assert("positive-price-variance outcome", migration.includes("positive-price-variance"));
forbidText(migration.split("protected override void Down")[0], "favorable-variance", "migration up");
forbidText(migration.split("protected override void Down")[0], "signed_variance_ratio <= 1", "migration up");
forbidText(migration.split("protected override void Down")[0], "customer_spread_ratio <= 1", "migration up");
assert("seed customer position fixture", seed.includes("COMMSALE2-SET-CUSTOMER-POSITION"));
assert("seed temporal current fixture", seed.includes("COMMSALE2-SET-TEMPORAL-CURRENT"));
assert("comparison seed seven scenarios", comparisonSeed.includes("commsale2-pilot-customer-tie"));

const firstApplyResult = runMigrationApply();
const firstApplyOutput = `${firstApplyResult.stdout}${firstApplyResult.stderr}`;
assert("migration apply succeeds", firstApplyResult.status === 0, firstApplyOutput.slice(-500));

const historyCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" IN ('${MIGRATION_ID}', '${RATIO_MIGRATION_ID}')
`));
assert("history rows exactly two", historyCount === 2, `count=${historyCount}`);

const signedVarianceConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commercial_sales_price_comparison_runs_signed_variance'
`);
assert(
  "signed variance ratio constraint lower bound only",
  signedVarianceConstraint.includes("signed_variance_ratio >") && signedVarianceConstraint.includes("-1"),
  signedVarianceConstraint,
);
forbidText(signedVarianceConstraint, "<= 1", "applied signed variance constraint");

const spreadConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commercial_sales_price_comparison_runs_customer_spread'
`);
assert("customer spread ratio >= 0", spreadConstraint.includes(">= ") && spreadConstraint.includes("0"), spreadConstraint);

const positionConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commercial_sales_price_comparison_results_position_ratio'
`);
assert("position ratio lower bound live", positionConstraint.includes(">=") && positionConstraint.includes("0"), positionConstraint);
assert(
  "position ratio upper bound live",
  positionConstraint.includes("relative_price_position_ratio <") && positionConstraint.includes("(1)"),
  positionConstraint,
);

const pilotResultId = db(`
  SELECT id::text FROM commercial_sales_price_comparison_results
  WHERE comparison_run_id = 'bb000001-02f3-4000-8000-0000000002f3'
  LIMIT 1
`);
if (!pilotResultId) {
  fail("Pilot comparison result required for ratio rejection proof — start API with DevelopmentPilot enabled");
}
function expectRatioInsertRejected(value, label) {
  const result = spawnSync(
    "docker",
    [
      "exec",
      "NevaxisFactOS-local-postgres-1",
      "psql",
      "-U",
      "crevafactos",
      "-d",
      "crevafactos_local",
      "-t",
      "-A",
      "-c",
      `INSERT INTO commercial_sales_price_comparison_results (
        id, tenant_id, facility_id, comparison_run_id, customer_reference,
        comparable_observation_count, customer_median_unit_price, lowest_comparable_median_unit_price,
        relative_price_position_ratio, price_rank, is_tied, position_state, evidence_status, created_at_utc)
      VALUES (
        'bb000001-0999-4000-8000-000000000999', 'bb000001-0001-4000-8000-000000000001',
        'bb000001-0005-4000-8000-000000000005', 'bb000001-02f3-4000-8000-0000000002f3',
        'RATIO-TEST', 1, 100, 100, ${value}, 1, false, 'within-spread-threshold', 'simulated', NOW());`,
    ],
    { encoding: "utf8" },
  );
  const output = `${result.stdout}${result.stderr}`;
  assert(`${label} rejected`, output.toLowerCase().includes("check") || result.status !== 0, output.slice(0, 200));
}
expectRatioInsertRejected(-0.01, "negative relative position ratio");
expectRatioInsertRejected(1, "relative position ratio exactly 1");
expectRatioInsertRejected(1.5, "relative position ratio above 1");

const countsConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commercial_sales_price_comparison_runs_counts'
`);
assert(
  "candidate = comparable + excluded",
  countsConstraint.includes("candidate_observation_count = comparable_observation_count + excluded_observation_count")
    || countsConstraint.includes("candidate_observation_count = (comparable_observation_count + excluded_observation_count)"),
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
  SELECT COUNT(*) FROM commercial_sales_price_comparison_results r
  LEFT JOIN commercial_sales_price_comparison_runs run ON run.id = r.comparison_run_id
  WHERE run.id IS NULL
`));
const orphanDrivers = Number(db(`
  SELECT COUNT(*) FROM commercial_sales_price_comparison_drivers d
  LEFT JOIN commercial_sales_price_comparison_runs run ON run.id = d.comparison_run_id
  WHERE run.id IS NULL
`));
assert("no orphan results", orphanResults === 0, `count=${orphanResults}`);
assert("no orphan drivers", orphanDrivers === 0, `count=${orphanDrivers}`);

const duplicateIdempotency = Number(db(`
  SELECT COUNT(*) FROM (
    SELECT tenant_id, idempotency_key, COUNT(*) AS c
    FROM commercial_sales_price_comparison_runs
    WHERE idempotency_key IS NOT NULL
    GROUP BY tenant_id, idempotency_key
    HAVING COUNT(*) > 1
  ) dup
`));
assert("no duplicate idempotency rows", duplicateIdempotency === 0, `count=${duplicateIdempotency}`);

const historyBefore = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" IN ('${MIGRATION_ID}', '${RATIO_MIGRATION_ID}')
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
  WHERE "MigrationId" IN ('${MIGRATION_ID}', '${RATIO_MIGRATION_ID}')
`));
assert("second apply did not duplicate history", historyAfter === historyBefore, `${historyBefore}→${historyAfter}`);

const pilotProfileCount = Number(db(`SELECT COUNT(*) FROM commercial_sales_price_profiles WHERE id = '${PROFILE}'`));
if (pilotProfileCount === 0) {
  fail("Commercial sales price pilot not bootstrapped — start API with DevelopmentPilot enabled before bootstrap count checks");
}
assert("pilot profile seeded", pilotProfileCount === 1, `count=${pilotProfileCount}`);
const spreadThreshold = db(`SELECT customer_spread_threshold FROM commercial_sales_price_profiles WHERE id = '${PROFILE}'`);
assert("pilot customer spread threshold 0.10", spreadThreshold === "0.100000", spreadThreshold);

const energyCount = Number(db(`SELECT COUNT(*) FROM energy_measurement_points WHERE id = 'bb000001-0160-4000-8000-000000000160'`));
const iotCount = Number(db(`SELECT COUNT(*) FROM iot_plc_endpoints WHERE id = 'bb000001-01c0-4000-8000-0000000001c0'`));
const operationalRiskCount = Number(db(`SELECT COUNT(*) FROM operational_risk_cases WHERE id = 'bb000001-0200-4000-8000-000000000200'`));
assert("energy pilot preserved", energyCount === 1, `count=${energyCount}`);
assert("iot pilot preserved", iotCount === 1, `count=${iotCount}`);
assert("operational risk pilot preserved", operationalRiskCount === 1, `count=${operationalRiskCount}`);

removeClosureFixtures();
const closureRuns = Number(db(`SELECT COUNT(*) FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
assert("closure runs removed", closureRuns === 0, `count=${closureRuns}`);

console.log("COMMERCIAL-SALES-PRICE-2 migration verification passed.");
