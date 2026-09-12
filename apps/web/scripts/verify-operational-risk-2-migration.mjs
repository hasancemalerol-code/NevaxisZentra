#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260823120837_AddOperationalRiskDetectionFoundation";
const MIGRATION_BASENAME = `${MIGRATION_ID}.cs`;
const DESIGNER_BASENAME = `${MIGRATION_ID}.Designer.cs`;
const CLOSURE_PREFIX = "OPRISK2-CLOSURE";

const RLS_TABLES = [
  "operational_risk_detection_profiles",
  "operational_risk_detection_rules",
  "operational_risk_detection_runs",
  "operational_risk_detection_results",
  "operational_risk_detection_drivers",
];

const APPEND_ONLY_TABLES = [
  "operational_risk_detection_runs",
  "operational_risk_detection_results",
  "operational_risk_detection_drivers",
];

function fail(message) {
  console.error(`OPERATIONAL-RISK-2 migration verification failed: ${message}`);
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
    DELETE FROM operational_risk_detection_drivers
    WHERE result_id IN (
      SELECT id FROM operational_risk_detection_results
      WHERE run_id IN (SELECT id FROM operational_risk_detection_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%')
    );
    DELETE FROM operational_risk_detection_results
    WHERE run_id IN (SELECT id FROM operational_risk_detection_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM operational_risk_detection_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM operational_risk_detection_rules
    WHERE profile_id IN (SELECT id FROM operational_risk_detection_profiles WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM operational_risk_detection_profiles WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

console.log("Running OPERATIONAL-RISK-2 migration verifier.");

const migration = readRequired(`packages/NevaxisFactOS.Persistence.Migrations/Migrations/${MIGRATION_BASENAME}`);
const designer = readRequired(`packages/NevaxisFactOS.Persistence.Migrations/Migrations/${DESIGNER_BASENAME}`);
const snapshot = readRequired("packages/NevaxisFactOS.Persistence.Migrations/Migrations/NevaxisFactOSDbContextModelSnapshot.cs");
const bootstrap = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotOperationalRiskDetectionSeed.cs");

assert("migration source present", migration.includes("AddOperationalRiskDetectionFoundation"));
assert("designer present", designer.includes("AddOperationalRiskDetectionFoundation"));
assert("snapshot includes detection profiles", snapshot.includes('ToTable("operational_risk_detection_profiles"'));
assert("snapshot includes detection runs", snapshot.includes('ToTable("operational_risk_detection_runs"'));
assert("case detection key column", migration.includes("detection_key"));
assert("open detection key unique index", migration.includes("ux_operational_risk_cases_tenant_detection_key_open"));
assert("window check constraint", migration.includes("ck_operational_risk_detection_runs_window"));
assert("score check constraint", migration.includes("ck_operational_risk_detection_results_score"));

const firstApplyResult = runMigrationApply();
const firstApplyOutput = `${firstApplyResult.stdout}${firstApplyResult.stderr}`;
assert("migration apply succeeds", firstApplyResult.status === 0, firstApplyOutput.slice(-500));

const migrationDiscoveryCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("migration discovered exactly once", migrationDiscoveryCount === 1, `count=${migrationDiscoveryCount}`);

for (const tableName of APPEND_ONLY_TABLES) {
  const privileges = tablePrivileges("crevafactos_runtime", tableName);
  forbidText(privileges, "DELETE", `${tableName} runtime privileges`);
  forbidText(privileges, "UPDATE", `${tableName} runtime privileges`);
}

const profilePrivileges = tablePrivileges("crevafactos_runtime", "operational_risk_detection_profiles");
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

const historyBefore = migrationDiscoveryCount;
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

assert("bootstrap seed references pilot profile", bootstrap.includes("OperationalRiskDetectionPilotProfileId"));
const pilotProfileCount = Number(db(`SELECT COUNT(*) FROM operational_risk_detection_profiles WHERE id = 'bb000001-0210-4000-8000-000000000210'`));
if (pilotProfileCount === 0) {
  fail("Detection pilot not bootstrapped — restart API with DevelopmentPilot enabled after migration");
}
assert("pilot detection profile seeded", pilotProfileCount === 1, `count=${pilotProfileCount}`);

const or1CaseCount = Number(db(`SELECT COUNT(*) FROM operational_risk_cases WHERE id = 'bb000001-0200-4000-8000-000000000200'`));
const energyCount = Number(db(`SELECT COUNT(*) FROM energy_measurement_points WHERE id = 'bb000001-0160-4000-8000-000000000160'`));
const iotCount = Number(db(`SELECT COUNT(*) FROM iot_plc_endpoints WHERE id = 'bb000001-01c0-4000-8000-0000000001c0'`));
const maintenanceCount = Number(db(`SELECT COUNT(*) FROM maintenance_asset_machine_mappings WHERE id = 'bb000001-0099-4000-8000-000000000099'`));
assert("OPERATIONAL-RISK-1 pilot preserved", or1CaseCount === 1, `count=${or1CaseCount}`);
assert("energy pilot preserved", energyCount === 1, `count=${energyCount}`);
assert("iot-plc pilot preserved", iotCount === 1, `count=${iotCount}`);
assert("maintenance pilot preserved", maintenanceCount === 1, `count=${maintenanceCount}`);

removeClosureFixtures();

const orphanDrivers = Number(db(`
  SELECT COUNT(*) FROM operational_risk_detection_drivers d
  LEFT JOIN operational_risk_detection_results r ON r.id = d.result_id
  WHERE r.id IS NULL
`));
const orphanResults = Number(db(`
  SELECT COUNT(*) FROM operational_risk_detection_results r
  LEFT JOIN operational_risk_detection_runs run ON run.id = r.run_id
  WHERE run.id IS NULL
`));
const duplicateOpenKeys = Number(db(`
  SELECT COUNT(*) FROM (
    SELECT tenant_id, detection_key, COUNT(*) cnt
    FROM operational_risk_cases
    WHERE detection_key IS NOT NULL
      AND lifecycle_status NOT IN ('resolved','dismissed')
    GROUP BY tenant_id, detection_key
    HAVING COUNT(*) > 1
  ) dup
`));
assert("no orphan drivers", orphanDrivers === 0, `count=${orphanDrivers}`);
assert("no orphan results", orphanResults === 0, `count=${orphanResults}`);
assert("no duplicate unresolved detection keys", duplicateOpenKeys === 0, `count=${duplicateOpenKeys}`);

console.log("OPERATIONAL-RISK-2 migration verification passed.");
