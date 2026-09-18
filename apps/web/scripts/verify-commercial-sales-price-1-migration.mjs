#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260824195725_AddCommercialSalesPriceIntelligenceFoundation";
const MIGRATION_BASENAME = `${MIGRATION_ID}.cs`;
const DESIGNER_BASENAME = `${MIGRATION_ID}.Designer.cs`;
const READ_PERMISSION_ID = "10000000-0000-4000-8000-000000000082";
const MANAGE_PERMISSION_ID = "10000000-0000-4000-8000-000000000083";
const CLOSURE_PREFIX = "COMMSALE1-CLOSURE";
const PROFILE = "bb000001-0270-4000-8000-000000000270";

const RLS_TABLES = [
  "commercial_sales_price_profiles",
  "commercial_sales_price_evidence_observations",
  "commercial_sales_price_assessments",
  "commercial_sales_price_assessment_drivers",
];

const APPEND_ONLY_TABLES = [
  "commercial_sales_price_evidence_observations",
  "commercial_sales_price_assessments",
  "commercial_sales_price_assessment_drivers",
];

function fail(message) {
  console.error(`COMMERCIAL-SALES-PRICE-1 migration verification failed: ${message}`);
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
    DELETE FROM commercial_sales_price_assessment_drivers
    WHERE assessment_id IN (
      SELECT id FROM commercial_sales_price_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_sales_price_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM commercial_sales_price_profiles WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

console.log("Running COMMERCIAL-SALES-PRICE-1 migration verifier.");

const migration = readRequired(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${MIGRATION_BASENAME}`);
const designer = readRequired(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${DESIGNER_BASENAME}`);
const snapshot = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/NevaxisZentraDbContextModelSnapshot.cs");
const bootstrap = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotCommercialSalesPriceSeed.cs");

assert("migration source present", migration.includes("AddCommercialSalesPriceIntelligenceFoundation"));
assert("variance ratio lower bound in migration", migration.includes("variance_ratio > -1"));
forbidText(migration, "permission_definitions", "sales migration permissions");
forbidText(migration, "InsertData", "sales migration permissions");
assert("designer present", designer.includes("AddCommercialSalesPriceIntelligenceFoundation"));
assert("snapshot includes profiles", snapshot.includes('ToTable("commercial_sales_price_profiles"'));
assert("snapshot includes assessments", snapshot.includes('ToTable("commercial_sales_price_assessments"'));
assert("check constraints present", migration.includes("ck_commercial_sales_price_assessments_outcome"));

const firstApplyResult = runMigrationApply();
const firstApplyOutput = `${firstApplyResult.stdout}${firstApplyResult.stderr}`;
assert("migration apply succeeds", firstApplyResult.status === 0, firstApplyOutput.slice(-500));

const historyCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("history row exactly once", historyCount === 1, `count=${historyCount}`);

const varianceConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commercial_sales_price_assessments_variance_ratio'
`);
assert(
  "variance ratio constraint lower bound only",
  varianceConstraint.includes("variance_ratio >") && varianceConstraint.includes("-1"),
  varianceConstraint,
);
forbidText(varianceConstraint, "<= 1", "applied variance ratio constraint");

for (const tableName of APPEND_ONLY_TABLES) {
  const privileges = tablePrivileges("crevafactos_runtime", tableName);
  forbidText(privileges, "DELETE", `${tableName} runtime privileges`);
  forbidText(privileges, "UPDATE", `${tableName} runtime privileges`);
}

const profilePrivileges = tablePrivileges("crevafactos_runtime", "commercial_sales_price_profiles");
assert("profiles SELECT grant", profilePrivileges.includes("SELECT"), profilePrivileges);
assert("profiles INSERT grant", profilePrivileges.includes("INSERT"), profilePrivileges);
assert("profiles UPDATE grant", profilePrivileges.includes("UPDATE"), profilePrivileges);
forbidText(profilePrivileges, "DELETE", "profiles runtime privileges");

for (const tableName of RLS_TABLES) {
  const forced = Number(db(`
    SELECT COUNT(*) FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = '${tableName}' AND c.relrowsecurity = true AND c.relforcerowsecurity = true
  `));
  assert(`${tableName} FORCE RLS enabled`, forced === 1, `count=${forced}`);
}

const readPermission = db(`SELECT key FROM permission_definitions WHERE id = '${READ_PERMISSION_ID}'`);
const managePermission = db(`SELECT key FROM permission_definitions WHERE id = '${MANAGE_PERMISSION_ID}'`);
assert("commercial-intelligence.read permission seeded", readPermission === "commercial-intelligence.read", readPermission);
assert("commercial-intelligence.manage permission seeded", managePermission === "commercial-intelligence.manage", managePermission);

const orphanDrivers = Number(db(`
  SELECT COUNT(*) FROM commercial_sales_price_assessment_drivers d
  LEFT JOIN commercial_sales_price_assessments a ON a.id = d.assessment_id
  WHERE a.id IS NULL
`));
assert("no orphan drivers", orphanDrivers === 0, `count=${orphanDrivers}`);

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

assert("bootstrap seed references pilot profile", bootstrap.includes("CommercialSalesPricePilotProfileId"));
const pilotProfileCount = Number(db(`SELECT COUNT(*) FROM commercial_sales_price_profiles WHERE id = '${PROFILE}'`));
if (pilotProfileCount === 0) {
  fail("Commercial sales price pilot not bootstrapped — start API with DevelopmentPilot enabled before bootstrap count checks");
}
assert("pilot profile seeded", pilotProfileCount === 1, `count=${pilotProfileCount}`);
const pilotVarianceThreshold = db(`SELECT maximum_positive_variance_ratio FROM commercial_sales_price_profiles WHERE id = '${PROFILE}'`);
assert("pilot maximum variance threshold 0.10", pilotVarianceThreshold === "0.100000", pilotVarianceThreshold);

const energyCount = Number(db(`SELECT COUNT(*) FROM energy_measurement_points WHERE id = 'bb000001-0160-4000-8000-000000000160'`));
const iotCount = Number(db(`SELECT COUNT(*) FROM iot_plc_endpoints WHERE id = 'bb000001-01c0-4000-8000-0000000001c0'`));
const operationalRiskCount = Number(db(`SELECT COUNT(*) FROM operational_risk_cases WHERE id = 'bb000001-0200-4000-8000-000000000200'`));
assert("energy pilot preserved", energyCount === 1, `count=${energyCount}`);
assert("iot pilot preserved", iotCount === 1, `count=${iotCount}`);
assert("operational risk pilot preserved", operationalRiskCount === 1, `count=${operationalRiskCount}`);

removeClosureFixtures();
const closureProfiles = Number(db(`SELECT COUNT(*) FROM commercial_sales_price_profiles WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
const closureAssessments = Number(db(`SELECT COUNT(*) FROM commercial_sales_price_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
assert("closure profiles removed", closureProfiles === 0, `count=${closureProfiles}`);
assert("closure assessments removed", closureAssessments === 0, `count=${closureAssessments}`);

console.log("COMMERCIAL-SALES-PRICE-1 migration verification passed.");
